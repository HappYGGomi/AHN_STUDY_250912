# app.py — Manual QA (RAG: FAISS + Whoosh + bge-m3 + LLM Extractive JSON)
# 실행: python -m uvicorn app:app --reload --port 8000
# 필요 패키지:
#   pip install fastapi "uvicorn[standard]" requests sentence-transformers faiss-cpu numpy pymupdf whoosh

from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from pydantic import BaseModel
from typing import List, Dict
import os, re, json, logging, requests
import numpy as np
from FlagEmbedding import FlagReranker
RERANKER = FlagReranker('BAAI/bge-reranker-base', use_fp16=False)  # CPU면 False


# ---------- Logging ----------
logging.basicConfig(level=logging.INFO)
log = logging.getLogger("qa")

# ---------- (선택) PDF 추출 ----------
try:
    import fitz  # PyMuPDF
except Exception:
    fitz = None

# ---------- Embedding / FAISS ----------
from sentence_transformers import SentenceTransformer
import faiss

# 임베딩 모델 (한국어/다국어 강함)
EMB_MODEL = SentenceTransformer("BAAI/bge-m3")
DIM = EMB_MODEL.get_sentence_embedding_dimension()
FAISS_INDEX = faiss.IndexFlatIP(DIM)   # normalized + inner-product = cosine
INDEX2DOC: List[int] = []              # FAISS row -> DOCS index 매핑

# bge 프리픽스
BGE_QUERY_PREFIX   = "query: "
BGE_PASSAGE_PREFIX = "passage: "

def embed_passages(texts: List[str]) -> np.ndarray:
    inputs = [BGE_PASSAGE_PREFIX + (t or "") for t in texts]
    return EMB_MODEL.encode(inputs, normalize_embeddings=True).astype("float32")

def embed_queries(texts: List[str]) -> np.ndarray:
    inputs = [BGE_QUERY_PREFIX + (t or "") for t in texts]
    return EMB_MODEL.encode(inputs, normalize_embeddings=True).astype("float32")

# ---------- Whoosh (BM25 키워드 검색) ----------
from whoosh.fields import Schema, TEXT, ID
from whoosh.index import create_in, open_dir
from whoosh.qparser import MultifieldParser
from whoosh import scoring

WHOOSH_DIR = "whoosh_index"
os.makedirs(WHOOSH_DIR, exist_ok=True)
SCHEMA = Schema(id=ID(stored=True), title=TEXT(stored=True), text=TEXT(stored=True))
IX = create_in(WHOOSH_DIR, SCHEMA) if not os.listdir(WHOOSH_DIR) else open_dir(WHOOSH_DIR)

def add_to_whoosh(new_docs: List[Dict]):
    writer = IX.writer()
    for d in new_docs:
        writer.add_document(id=d["id"], title=d["title"], text=d["text"])
    writer.commit()

def search_keyword(query: str, top_k=12):
    with IX.searcher(weighting=scoring.BM25F()) as searcher:
        q = MultifieldParser(["title", "text"], schema=IX.schema).parse(query)
        hits = list(searcher.search(q, limit=top_k))
        return [(float(h.score), h["id"]) for h in hits]

# ---------- App ----------
app = FastAPI(title="Manual QA (RAG + bge-m3 + Hybrid + LLM-JSON)")

# In-memory 문서 저장
DOCS: List[Dict] = []   # {"id": str, "title": str, "chunk_idx": int, "text": str}

# ---------- Utils ----------
def normalize(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()

def is_header_like(s: str) -> bool:
    ss = normalize(s)
    if len(ss) <= 4:
        return True
    if ss.startswith("[") and ss.endswith("]"):
        return True
    if "섹션" in ss:
        return True
    if re.fullmatch(r"[A-Za-z가-힣0-9\s\-/_,.]+[:：]?", ss) and not re.search(r"[.!?]|다\.|요\.|입니다\.", ss):
        return True
    return False

def split_sentences(text: str):
    """lookbehind 없이 안전하게 문장 분리 + 헤더/섹션 제거"""
    s = normalize(text)
    parts = re.split(r"(?:\r?\n| - |•|·|;)", s)
    out = []
    for p in parts:
        p = p.strip()
        if not p:
            continue
        matches = re.findall(r".+?(?:다\.|요\.|습니다\.|[.!?])", p)
        if matches:
            out.extend([m.strip(" -•·") for m in matches])
        else:
            out.append(p)
    return [x for x in out if len(x) >= 2 and not is_header_like(x)]

def chunk_text(txt: str, size: int = 400, overlap: int = 80):
    t = normalize(txt)
    chunks, start = [], 0
    while start < len(t):
        end = min(start + size, len(t))
        chunks.append(t[start:end])
        if end >= len(t):
            break
        start = end - overlap
    return chunks

def score_chunk(query: str, chunk: str) -> int:
    q = normalize(query).lower()
    c = chunk.lower()
    tokens = re.findall(r"[\w가-힣]+", q)
    return sum(c.count(tok) for tok in tokens)

def add_to_index(new_docs: List[Dict]):
    if not new_docs:
        return
    vecs = embed_passages([d["text"] for d in new_docs])
    FAISS_INDEX.add(vecs)
    start = len(DOCS) - len(new_docs)
    INDEX2DOC.extend([start + i for i in range(len(new_docs))])
    try:
        log.info(
            f"add_to_index: n={len(new_docs)}, vecs={vecs.shape}, dtype={vecs.dtype}, "
            f"norm≈{float(np.linalg.norm(vecs[0])):.3f}"
        )
    except Exception:
        pass

def search_vector(query: str, top_k: int = 4) -> List[Dict]:
    try:
        if FAISS_INDEX.ntotal == 0:
            log.info("search_vector: index empty")
            return []
        qv = embed_queries([query])
        if qv.ndim == 1:
            qv = qv.reshape(1, -1)
        D, I = FAISS_INDEX.search(qv, max(top_k * 3, 6))
        try:
            log.info(f"search_vector: q_norm≈{float(np.linalg.norm(qv[0])):.3f}, topI={I[0][:5].tolist()}")
        except Exception:
            pass
        seen, picked = set(), []
        for idx in I[0]:
            if idx == -1:
                continue
            doc_idx = INDEX2DOC[idx]
            if doc_idx in seen:
                continue
            seen.add(doc_idx)
            picked.append(DOCS[doc_idx])
            if len(picked) >= top_k:
                break
        return picked
    except Exception as e:
        log.exception(f"search_vector error: {e}")
        return []

def search_hybrid(query: str, top_k=4, alpha=0.6) -> List[Dict]:
    # 1) 벡터 후보
    vec_docs = search_vector(query, top_k=max(top_k*3, 12))
    # 2) 키워드 후보
    kw_hits = search_keyword(query, top_k=max(top_k*6, 24))
    # 3) 가중 결합
    pool = {}
    for rank, d in enumerate(vec_docs, start=1):
        pool[d["id"]] = pool.get(d["id"], 0.0) + alpha * (1.0 / (60.0 + rank))
    for score, did in kw_hits:
        pool[did] = pool.get(did, 0.0) + (1.0 - alpha) * (score / 100.0)
    ranked = sorted(pool.items(), key=lambda x: x[1], reverse=True)
    out = []
    for did, _ in ranked:
        doc = next((x for x in DOCS if x["id"] == did), None)
        if doc and doc not in out:
            out.append(doc)
        if len(out) >= top_k:
            break
    for d in vec_docs:
        if len(out) >= top_k: break
        if d not in out: out.append(d)
    return out

def rerank(query: str, docs: List[Dict], top_k=4) -> List[Dict]:
    # 하이브리드 상위 후보를 교차-인코더로 정밀 재정렬
    if not docs:
        return []
    pairs = [[query, d["text"]] for d in docs]
    scores = RERANKER.compute_score(pairs, batch_size=16)
    ranked = sorted(zip(scores, docs), key=lambda x: x[0], reverse=True)
    return [d for _, d in ranked[:top_k]]


# ---------- 질의 정규화 / 의도 ----------
def normalize_query_kor(q: str) -> str:
    s = normalize(q)
    repl = [
        (r"배송\s*몇[일|일|칠]\??", "기본 배송 소요 기간은 며칠인가요?"),
        (r"배송\s*얼마나", "기본 배송 소요 기간은 며칠인가요?"),
        (r"배송.*언제", "기본 배송 소요 기간은 며칠인가요?"),
        (r"도착.*언제", "기본 배송 소요 기간은 며칠인가요?"),
        (r"언제\s*오(나요|는지)", "기본 배송 소요 기간은 며칠인가요?"),
        (r"반품\s*(언제|몇일|며칠|기간|가능)", "반품 가능 기간은 언제까지인가요?"),
        (r"교환\s*(언제|몇일|며칠|기간|가능)", "교환 가능 조건과 기간은 어떻게 되나요?"),
        (r"(as|a/s)", "A/S"),
        (r"카드\s*취소\s*며칠", "카드 결제 취소 처리 기간은 며칠인가요?"),
    ]
    for pat, to in repl:
        s = re.sub(pat, to, s, flags=re.IGNORECASE)
    return s

def intent_hint(q: str) -> str:
    s = normalize(q).lower()
    if re.search(r"(반품|반환).*(언제|기간|기한|며칠|몇일|가능)", s):
        return "return_window"
    if re.search(r"(배송|도착|소요).*(언제|며칠|몇일|얼마나|기간|걸리)", s):
        return "shipping_time"
    return "generic"

# ---------- 추출 요약 ----------
def extractive_answer(query: str, contexts: List[Dict], topn: int = 2) -> str:
    intent = intent_hint(query)
    q_tokens = re.findall(r"[\w가-힣]+", normalize(query).lower())
    cands = []
    for c in contexts:
        for sent in split_sentences(c["text"]):
            if is_header_like(sent):  # 안전
                continue
            tok_score = sum(sent.lower().count(t) for t in q_tokens)
            boost = 0
            if re.search(r"\d+\s*(영업)?일", sent):
                boost += 2
            if intent == "return_window" and ("반품" in sent or "반환" in sent):
                boost += 1
            if intent == "shipping_time" and ("배송" in sent or "소요" in sent or "도착" in sent):
                boost += 1
            cands.append((tok_score + boost, len(sent), sent))
    cands.sort(key=lambda x: (x[0], -x[1]), reverse=True)
    picked = [s for sc, ln, s in cands if sc > 0][:topn]
    if not picked:
        # 숫자/일 포함 문장 백업
        for sc, ln, s in cands:
            if re.search(r"\d+\s*(영업)?일", s):
                picked = [s]; break
    if not picked and contexts:
        first_ctx_sents = split_sentences(contexts[0]["text"])
        picked = first_ctx_sents[:1] if first_ctx_sents else [contexts[0]["text"]]
    summary = " ".join(picked).strip()
    if not summary.endswith(("입니다.", "니다.", "요.", ".")):
        summary += "입니다."
    return summary

# ---------- LLM: Extractive JSON 모드 (Ollama) ----------
USE_LLM = True
LLM_PROVIDER = os.environ.get("QA_LLM", "ollama")   # "ollama" | "none"
OLLAMA_URL  = os.environ.get("OLLAMA_URL", "http://127.0.0.1:11434")
OLLAMA_MODEL = os.environ.get("OLLAMA_MODEL", "llama3.1:8b")
OLLAMA_TIMEOUT = int(os.environ.get("OLLAMA_TIMEOUT", "300"))

def select_top_sentences_semantic_filtered(text: str, query: str, intent: str,
                                           max_sents=2, max_chars=240):
    sents = split_sentences(text)
    if not sents:
        return ""
    # 의도별 키워드 필터
    pat = None
    if intent == "shipping_time":
        pat = r"(배송|소요|도착)"
    elif intent == "return_window":
        pat = r"(반품|반환)"
    cand = [s for s in sents if (pat is None or re.search(pat, s))]
    if not cand:
        cand = sents
    vecs = EMB_MODEL.encode([BGE_PASSAGE_PREFIX + s for s in cand], normalize_embeddings=True)
    qv = EMB_MODEL.encode([BGE_QUERY_PREFIX + query], normalize_embeddings=True)[0]
    scores = np.dot(vecs, qv)
    idxs = np.argsort(-scores)[:max_sents]
    picked = [cand[i] for i in idxs]
    txt = " ".join(picked)
    return (txt[:max_chars] + "…") if len(txt) > max_chars else txt

def llm_answer_extractive_json(query: str, contexts: List[Dict]) -> (str, List[str]):
    qn = normalize_query_kor(query)
    intent = intent_hint(qn)
    bullets = []
    for c in contexts:
        snippet = select_top_sentences_semantic_filtered(c["text"], qn, intent, max_sents=2, max_chars=200)
        bullets.append({"id": f"{c['title']}#{c['chunk_idx']}", "text": snippet or c["text"][:200]})

    sys_prompt = (
        "당신은 고객지원 에이전트입니다. 반드시 '근거 텍스트'에서만 답을 추출하세요. "
        "출력은 JSON 한 줄만, 다른 말 금지.\n"
        "요구사항:\n"
        "- final_answer에는 근거에서 발견한 정확한 숫자/단위(예: 7일, 2~3영업일)를 그대로 포함하세요.\n"
        "- 근거에 없으면 '메뉴얼에 근거가 없어 답변드리기 어렵습니다.'로 하세요.\n"
        "JSON 스키마:\n"
        "{"
        "\"final_answer\": \"한국어 존댓말 한 문장, 50자 이내\","
        "\"support\": [\"근거에서 그대로 복사한 문장 1개\"],"
        "\"citations\": [\"문장이 나온 근거 id(예: 문서명#청크)\"]"
        "}"
    )
    prompt = f"{sys_prompt}\n\n질문:\n{qn}\n\n근거:\n{json.dumps(bullets, ensure_ascii=False)}\n\nJSON:"

    try:
        r = requests.post(
            f"{OLLAMA_URL}/api/generate",
            json={
                "model": OLLAMA_MODEL,
                "prompt": prompt,
                "stream": False,
                "keep_alive": "1h",
                "options": {
                    "num_predict": 64,
                    "temperature": 0.1,
                    "top_p": 0.9,
                    "num_ctx": 1024,
                    "num_thread": os.cpu_count() or 4
                }
            },
            timeout=OLLAMA_TIMEOUT,
        )
        r.raise_for_status()
        raw = r.json().get("response", "").strip()

        # Fence 제거 및 JSON 블록 추출
        if raw.startswith("```"):
            raw = raw.strip("` \n")
            if raw.lower().startswith("json"):
                raw = raw[4:].strip()
        if not raw.startswith("{"):
            m = re.search(r"\{.*\}", raw, flags=re.DOTALL)
            if m:
                raw = m.group(0)

        data = json.loads(raw)
    except Exception as e:
        log.warning(f"llm JSON parse fallback: {e}")
        return extractive_answer(query, contexts, topn=2), []

    # support 검증 + 의도 키워드 검증
    final = (data.get("final_answer") or "").strip()
    if len(final) > 60:
        final = final[:60].rstrip() + "..."
    if not final.endswith(("입니다.", "니다.", "요.", ".")):
        final += "입니다."

    corpus = " ".join([c["text"] for c in contexts])
    support_ok = any((s and s in corpus) for s in data.get("support", []) if isinstance(s, str))

    need_pat = None
    intent = intent_hint(query)
    if intent == "shipping_time":
        need_pat = r"(배송|소요|도착)"
    elif intent == "return_window":
        need_pat = r"(반품|반환)"

    if (need_pat and not re.search(need_pat, final)) or not support_ok:
        final = extractive_answer(query, contexts, topn=1)

    cites = [str(c) for c in data.get("citations", []) if isinstance(c, str)]
    return final, cites

# ---------- API ----------
class AskReq(BaseModel):
    query: str
    top_k: int = 4

@app.get("/health")
def health():
    return {"status": "ok", "docs": len(DOCS), "faiss_rows": FAISS_INDEX.ntotal}

@app.post("/ingest")
async def ingest(title: str = Form(...), file: UploadFile = File(...)):
    raw = (await file.read()).decode("utf-8", errors="ignore")
    chunks = chunk_text(raw, size=400, overlap=80)
    new_docs: List[Dict] = []
    start_idx = len(DOCS)
    for i, ch in enumerate(chunks):
        new_docs.append({"id": f"{title}:{start_idx+i}", "title": title, "chunk_idx": start_idx+i, "text": ch})
    DOCS.extend(new_docs)
    add_to_index(new_docs)
    add_to_whoosh(new_docs)
    return {"ok": True, "added": len(new_docs), "total_docs": len(DOCS), "faiss_rows": FAISS_INDEX.ntotal}

@app.post("/ingest_pdf")
async def ingest_pdf(title: str = Form(...), file: UploadFile = File(...)):
    if fitz is None:
        raise HTTPException(status_code=500, detail="pymupdf 미설치: pip install pymupdf")
    data = await file.read()
    try:
        doc = fitz.open(stream=data, filetype="pdf")
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"PDF 열기 실패: {e}")

    new_docs: List[Dict] = []
    start_idx = len(DOCS)
    added = 0
    for pi, page in enumerate(doc):
        page_text = page.get_text("text") or ""
        for ch in chunk_text(page_text, size=400, overlap=80):
            new_docs.append({
                "id": f"{title}:p{pi}:{start_idx+added}",
                "title": title,
                "chunk_idx": start_idx + added,
                "text": ch
            })
            added += 1
    DOCS.extend(new_docs)
    add_to_index(new_docs)
    add_to_whoosh(new_docs)
    return {"ok": True, "added": added, "total_docs": len(DOCS), "faiss_rows": FAISS_INDEX.ntotal}

@app.post("/ask")
def ask(req: AskReq):
    if not DOCS:
        return {"answer": "먼저 /ingest 또는 /ingest_pdf 로 메뉴얼을 업로드해 주세요.", "contexts": []}

    # 1) 질의 정규화 후 하이브리드 검색
    qn = normalize_query_kor(req.query)
    # 넉넉히 뽑아서
    cands = search_hybrid(qn, top_k=max(req.top_k, 12))
    # 정밀 재정렬 후 최종 top_k만 사용
    contexts = rerank(qn, cands, top_k=req.top_k)

    # 2) 백업: 토큰 스코어 기반
    if not contexts:
        scored = sorted([(score_chunk(qn, d["text"]), d) for d in DOCS],
                        key=lambda x: x[0], reverse=True)
        contexts = [d for s, d in scored[:req.top_k] if s > 0]

    if not contexts:
        return {"answer": "관련 근거를 찾지 못했습니다. 담당자에게 확인 후 안내드립니다.", "contexts": []}

    # 3) 최종 답 생성 (LLM JSON → 실패 시 추출요약)
    if USE_LLM and LLM_PROVIDER == "ollama":
        brief, cites = llm_answer_extractive_json(req.query, contexts)
    else:
        brief, cites = extractive_answer(req.query, contexts, topn=2), []

    # 4) 응답 구성
    ctx_texts = "\n\n".join([f"[근거 {i+1}] {d['text']}" for i, d in enumerate(contexts)])
    if not cites:
        cites = [f"{d['title']}#{d['chunk_idx']}" for d in contexts]

    answer = (
        f"답변(요약, ~합니다): {brief}\n"
        f"- 근거 출처: {', '.join(cites)}\n\n"
        f"아래는 인용된 근거입니다.\n\n{ctx_texts}"
    )
    return {"answer": answer, "contexts": contexts}
