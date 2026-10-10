"""对比备份与改后文件：行尾(CRLF/裸LF)与 BOM 是否被改动脚本破坏。"""
import io, os

BACKUP = r"C:\Project\UxGame\.workbuddy\tmp\datapaths-backup"
ROOT = r"C:\Project\UxGame\Unity"

def stats(path):
    raw = io.open(path, "rb").read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig", errors="ignore")
    crlf = text.count("\r\n")
    lf_only = text.count("\n") - crlf
    return bom, crlf, lf_only

bad = 0
checked = 0
for base, _, files in os.walk(BACKUP):
    for name in files:
        bak = os.path.join(base, name)
        rel = os.path.relpath(bak, BACKUP)
        cur = os.path.join(ROOT, rel)
        if not os.path.exists(cur):
            print(f"!! 改后文件不存在: {rel}")
            bad += 1
            continue
        checked += 1
        b1, c1, l1 = stats(bak)
        b2, c2, l2 = stats(cur)
        problems = []
        if b1 != b2:
            problems.append(f"BOM {b1}->{b2}")
        if c1 != c2:
            problems.append(f"CRLF数 {c1}->{c2}")
        if l1 != l2:
            problems.append(f"裸LF数 {l1}->{l2}")
        if problems:
            bad += 1
            print(f"!! {rel}: " + ", ".join(problems))
print("---")
print(f"对比文件数: {checked}  异常: {bad}")