"""把硬编码的 Assets/Data 路径改成 Assets/GameRes，保留原分隔符与行尾，改前备份。"""
import io, os, re, shutil

ROOT = r"C:\Project\UxGame\Unity"
BACKUP = r"C:\Project\UxGame\.workbuddy\tmp\datapaths-backup"
SKIP_DIRS = {"Library", ".git", "Temp", "Logs", "obj", "bin", "Bundles", "UserSettings", ".vs", ".idea"}
EXTS = {".cs", ".asset", ".xml", ".json", ".txt", ".rsp", ".bat", ".ps1", ".md",
        ".asmdef", ".py", ".sh", ".yml", ".yaml", ".csv", ".html", ".uxml"}

# 只替换 "Assets" + 分隔符 + "Data" + 分隔符；保留原分隔符；不会误伤 HybridCLRData
PAT = re.compile(r"Assets([\\/]+)Data(?=[\\/])")

changed = []
for base, dirs, files in os.walk(ROOT):
    dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
    for name in files:
        if os.path.splitext(name)[1].lower() not in EXTS:
            continue
        path = os.path.join(base, name)
        try:
            raw = io.open(path, encoding="utf-8-sig", errors="ignore", newline="").read()
        except OSError:
            continue
        if not PAT.search(raw):
            continue
        rel = os.path.relpath(path, ROOT)
        bak = os.path.join(BACKUP, rel)
        os.makedirs(os.path.dirname(bak), exist_ok=True)
        shutil.copy2(path, bak)
        n = len(PAT.findall(raw))
        out = PAT.sub(r"Assets\1GameRes", raw)
        # 原样写回：newline='' 保证行尾不被转换
        io.open(path, "w", encoding="utf-8", errors="ignore", newline="").write(out)
        changed.append((n, rel))

for n, rel in sorted(changed, reverse=True):
    print(f"{n:3d}  {rel}")
print("---")
print(f"改动文件数: {len(changed)}  总替换处: {sum(n for n, _ in changed)}")
print(f"备份目录: {BACKUP}")