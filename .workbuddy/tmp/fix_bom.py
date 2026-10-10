"""从备份重新做替换，用 utf-8-sig 写回以保留 BOM。"""
import io, os, re

BACKUP = r"C:\Project\UxGame\.workbuddy\tmp\datapaths-backup"
ROOT = r"C:\Project\UxGame\Unity"
PAT = re.compile(r"Assets([\\/]+)Data(?=[\\/])")

fixed = 0
for base, _, files in os.walk(BACKUP):
    for name in files:
        bak = os.path.join(base, name)
        rel = os.path.relpath(bak, BACKUP)
        cur = os.path.join(ROOT, rel)
        raw = io.open(bak, "rb").read()
        had_bom = raw.startswith(b"\xef\xbb\xbf")
        text = raw.decode("utf-8-sig")            # 去 BOM 读入
        new_text, n = PAT.subn(r"Assets\1GameRes", text)
        data = new_text.encode("utf-8")
        if had_bom:
            data = b"\xef\xbb\xbf" + data         # 原样补回 BOM
        io.open(cur, "wb").write(data)
        fixed += 1
        print(f"{n:3d}  BOM={had_bom}  {rel}")
print("---")
print("重写文件数:", fixed)