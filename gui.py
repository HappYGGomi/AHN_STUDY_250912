import tkinter as tk
import requests

API = "http://127.0.0.1:8000"

def ask():
    q = entry.get("1.0","end").strip()
    if not q:
        return
    try:
        r = requests.post(f"{API}/ask", json={"query": q, "top_k": 3})
        data = r.json()
        output.delete("1.0","end")
        output.insert("end", data.get("answer","(no answer)"))
    except Exception as e:
        output.delete("1.0","end")
        output.insert("end", f"에러: {e}")

root = tk.Tk()
root.title("메뉴얼 QA (Beginner)")
root.geometry("760x600")

lbl = tk.Label(root, text="질문을 입력하세요:")
lbl.pack(padx=8, pady=(8,0), anchor="w")

entry = tk.Text(root, height=6, width=90)
entry.pack(padx=8, pady=4)

btn = tk.Button(root, text="질문하기", command=ask)
btn.pack(padx=8, pady=6)

output_label = tk.Label(root, text="답변:")
output_label.pack(padx=8, pady=(10,0), anchor="w")

output = tk.Text(root, height=24, width=90)
output.pack(padx=8, pady=4)

root.mainloop()
