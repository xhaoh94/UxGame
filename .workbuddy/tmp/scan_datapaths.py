"""扫出所有硬编码 "Assets/Data" 路径引用，为目录改名做清单。只读，不修改。"""
import io, os, re

ROOTS = [r"C:\Project\UxGame\Unity"]
SKIP_DIRS = {"Library", ".git", "Temp", "Logs", "obj", "bin", "Bundles", "UserSettings", ".vs", ".idea"}
EXTS = {".cs", ".asset", ".xml", ".json", ".txt", ".rsp", ".bat", ".ps1", ".md",
        ".asmdef", ".py", ".sh", ".yml", ".yaml", ".csv", ".html", ".uxml"}
# Assets[\\/]+Data[\\/] —— + 兼容 JSON 里的双反斜杠；不会误伤 HybridCLRData（前面没有 "Assets/"）
PAT = re.compile(r"Assets[\\/]+Data[\\/]")

total = 0
for root in ROOTS:
    for base, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
        for name in files:
            ext = os.path.splitext(name)[1].lower()
            if ext not in EXTS:
                continue
            path = os.path.join(base, name)
            try:
                text = io.open(path, encoding="utf-8-sig", errors="ignore", newline="").read()
            except OSError:
                continue
            hits = PAT.findall(text)
            if not hits:
                continue
            total += len(hits)
            rel = os.path.relpath(path, root)
            lines = [i + 1 for i, line in enumerate(text.splitlines()) if PAT.search(line)]
            print(f"{len(hits):3d}  {rel}  行 {lines}")
print("---")
print("总命中:", total)