# Manual QA Starter (초보자용, 30분 완주)

이 저장소는 **정해진 메뉴얼 기반 고객응대**를 아주 간단하게 맛볼 수 있는 최소 예제입니다.
- **단계 1:** LLM 없이도 동작 (키워드 점수로 근거 문단 찾기)
- **단계 2:** 나중에 임베딩/FAISS/LLM 붙이기 쉬운 구조

## 0) 준비
```bash
python -m venv .venv
source .venv/bin/activate   # Windows: .venv\Scripts\activate
pip install -r requirements.txt
```

## 1) 서버 실행
```bash
uvicorn app:app --reload --port 8000
```
- 확인: http://127.0.0.1:8000/health

## 2) 메뉴얼 업로드(ingest)
`sample_manual.txt`를 올려보세요.

### (A) curl
```bash
curl -X POST "http://127.0.0.1:8000/ingest"   -F "title=샘플메뉴얼"   -F "file=@sample_manual.txt"
```

### (B) Python으로 업로드
```python
import requests
r = requests.post("http://127.0.0.1:8000/ingest",
                  files={"file": open("sample_manual.txt","rb")},
                  data={"title":"샘플메뉴얼"})
print(r.json())
```

## 3) 질문하기
```bash
curl -X POST "http://127.0.0.1:8000/ask" -H "Content-Type: application/json"   -d "{"query":"반품 기간은?"}"
```

## 4) (선택) 간단 GUI 실행
다른 터미널에서:
```bash
python gui.py
```
텍스트 박스에 질문 입력 → “질문하기” 버튼 → 답변 확인

---

## 파일 구조
```
manual_qa_starter/
├─ app.py              # FastAPI 서버 (ingest / ask / health)
├─ gui.py              # Tkinter GUI (서버에 질문/답변 표시)
├─ requirements.txt    # 최소 의존성 (fastapi, uvicorn, requests)
├─ sample_manual.txt   # 예시 메뉴얼
└─ README.md
```

## 동작 원리(간단)
- `/ingest`: 텍스트를 여러 문단/조각으로 나눠 인메모리 DB에 저장
- `/ask`: 질문 토큰을 기준으로 각 조각에 점수 부여 → 상위 3개 근거 제공
- **주의**: 지금은 LLM 없이 “요약 문구”만 틀 잡아줍니다. 실무에선 LLM 연결 + 근거 강제 프롬프트를 추가하세요.

## 다음 스텝(추천)
1. 근거 조각에 **제목/섹션/페이지** 메타데이터 붙이기
2. 금지/민감어 룰셋(yaml) 추가 → 에스컬레이션
3. 임베딩 + 벡터 검색(FAISS/Chroma) 추가
4. OpenAI/Ollama 연결(프롬프트: “근거에서만 답변, 없으면 모른다고”)
5. 평가셋 50~100문항으로 자동 점수화

겁먹을 필요 없습니다. **1단계**만으로도 “업로드→질문→근거”가 바로 돌아갑니다. 
그 다음 한 단계씩만 올리면 됩니다. 화이팅! 🙌
