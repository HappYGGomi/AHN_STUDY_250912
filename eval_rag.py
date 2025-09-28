# eval_rag.py
import argparse, json, re, sys, csv
import requests
from pathlib import Path

def norm(s: str, ignore_tokens=None) -> str:
    if s is None:
        return ""
    t = s.lower()
    # 공백 제거
    t = re.sub(r"\s+", "", t)
    # 평가시 무시할 토큰 제거(예: '영업' → '영업일'/'일' 표현 차이 완화)
    ignore_tokens = ignore_tokens or []
    for tok in ignore_tokens:
        t = t.replace(tok, "")
    return t

def call_ask(api: str, query: str, top_k: int, timeout: int) -> str:
    r = requests.post(api, json={"query": query, "top_k": top_k}, timeout=timeout)
    r.raise_for_status()
    data = r.json()
    return data.get("answer", "")

def eval_basic(api: str, path: Path, top_k: int, timeout: int, ignore_tokens):
    ok = 0; tot = 0
    rows = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip(): 
            continue
        item = json.loads(line)
        q = item["query"]; gold = item["answer_span"]
        try:
            pred = call_ask(api, q, top_k, timeout)
        except Exception as e:
            pred = f"[ERROR] {e}"
        tot += 1
        hit = norm(gold, ignore_tokens) in norm(pred, ignore_tokens)
        rows.append({"query": q, "answer_span": gold, "prediction": pred, "status": "OK" if hit else "XX"})
        print(f"[{'OK' if hit else 'XX'}] Q={q} | want={gold} | got={pred[:80]}...")
        ok += 1 if hit else 0

    acc = ok / tot if tot else 0.0
    print(f"\n== BASIC RESULT ==")
    print(f"Exact-Substring@Ans = {ok}/{tot} = {acc:.2%}")
    return rows, {"ok": ok, "tot": tot, "acc": acc}

def eval_v2(api: str, path: Path, top_k: int, timeout: int, ignore_tokens):
    ok = 0; partial = 0; wrong = 0; tot = 0
    rows = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        item = json.loads(line)
        q = item["query"]
        req_spans = item.get("required_spans", [])
        forb_spans = item.get("forbidden_spans", [])
        opt_spans  = item.get("optional_spans", [])

        try:
            pred = call_ask(api, q, top_k, timeout)
        except Exception as e:
            pred = f"[ERROR] {e}"

        P = norm(pred, ignore_tokens)
        req_hits = sum(1 for s in req_spans if norm(s, ignore_tokens) in P)
        forb_hits = sum(1 for s in forb_spans if norm(s, ignore_tokens) in P)
        opt_hits = sum(1 for s in opt_spans if norm(s, ignore_tokens) in P)

        # 판정 규칙:
        # - 금지 구절이 하나라도 들어가면 WRONG
        # - 금지 없음 + 모든 필수 포함 → OK
        # - 금지 없음 + 일부만 포함(>=1) → PARTIAL
        # - 그 외(필수 0개) → WRONG
        if forb_hits > 0:
            label = "WRONG"
            wrong += 1
        else:
            if req_spans and req_hits == len(req_spans):
                label = "OK"
                ok += 1
            elif req_hits >= 1:
                label = "PARTIAL"
                partial += 1
            else:
                label = "WRONG"
                wrong += 1

        tot += 1
        rows.append({
            "query": q,
            "prediction": pred,
            "required_spans": "|".join(req_spans),
            "forbidden_spans": "|".join(forb_spans),
            "optional_spans": "|".join(opt_spans),
            "req_hits": req_hits,
            "forb_hits": forb_hits,
            "opt_hits": opt_hits,
            "label": label
        })
        print(f"[{label}] Q={q} | req={req_hits}/{len(req_spans)} forb={forb_hits} opt={opt_hits} | pred={pred[:80]}...")

    strict = ok / tot if tot else 0.0
    blended = (ok + 0.5 * partial) / tot if tot else 0.0  # PARTIAL 0.5 가중치
    print(f"\n== V2 RESULT ==")
    print(f"OK={ok}  PARTIAL={partial}  WRONG={wrong}  TOT={tot}")
    print(f"Strict@OK = {strict:.2%}")
    print(f"Blended(OK + 0.5*PARTIAL) = {blended:.2%}")
    return rows, {"ok": ok, "partial": partial, "wrong": wrong, "tot": tot,
                  "strict": strict, "blended": blended}

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--api", default="http://127.0.0.1:8000/ask")
    ap.add_argument("--file", required=True, help="data/eval.jsonl or data/eval_v2.jsonl")
    ap.add_argument("--mode", choices=["basic","v2"], required=True)
    ap.add_argument("--topk", type=int, default=4)
    ap.add_argument("--timeout", type=int, default=30)
    ap.add_argument("--ignore", nargs="*", default=["영업"])  # '영업일' vs '일' 표현 차이를 줄이기
    ap.add_argument("--out", default="eval_report.csv")
    args = ap.parse_args()

    path = Path(args.file)
    if not path.exists():
        print(f"File not found: {path}")
        sys.exit(1)

    if args.mode == "basic":
        rows, metrics = eval_basic(args.api, path, args.topk, args.timeout, args.ignore)
    else:
        rows, metrics = eval_v2(args.api, path, args.topk, args.timeout, args.ignore)

    # CSV 저장
    with open(args.out, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()) if rows else [])
        if rows:
            writer.writeheader()
            writer.writerows(rows)
    print(f"\nSaved report → {args.out}")

if __name__ == "__main__":
    main()
