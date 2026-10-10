import io, os, re

ROOT = r"C:\Project\UxGame\Unity\Assets\Editor"

TARGETS = [
    r"Timeline\TimelineWindow.cs", r"Timeline\TimelineClipView.cs", r"Timeline\TimelineTrackView.cs",
    r"Timeline\TimelineClipItem.cs", r"Timeline\TimelineTrackItem.cs",
    r"Timeline\Animation\TLAnimClipInspector.cs", r"Timeline\Animation\TLAnimTrackInspector.cs",
    r"Debugger\Event\EventDebuggerItem.cs", r"Debugger\Event\EventDebuggerWindow.cs",
    r"Debugger\Res\ResDebuggerWindow.cs", r"Debugger\UI\UIDebuggerWindow.cs", r"Debugger\UI\UIDebuggerItem.cs",
    r"Debugger\Time\TimeDebuggerItem.cs", r"Debugger\Time\TimeDebuggerItemSub1.cs",
    r"Debugger\Time\TimeDebuggerItemSub2.cs", r"Debugger\Time\TimeDebuggerWindow.cs",
    r"Build\Config\ConfigWindow.cs", r"Build\Proto\ProtoWindow.cs",
    r"Build\Version\VersionPackageViewer.cs", r"Build\Version\VersionWindow.cs",
    r"Build\UI\UIClassifyWindow.cs", r"Build\UI\UICodeMemberItem.cs", r"Build\UI\UICodeGenWindow.cs",
]

# 整个 Editor 目录的文本（按模块分桶）用于统计引用
buckets = {}
for base, _, files in os.walk(ROOT):
    for name in files:
        if not name.endswith(".cs"):
            continue
        path = os.path.join(base, name)
        rel = os.path.relpath(path, ROOT)
        top = rel.split(os.sep)[0]
        try:
            text = io.open(path, encoding="utf-8-sig", errors="ignore", newline="").read()
        except OSError:
            continue
        buckets.setdefault(top, []).append((rel, text))

FIELD = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(?:protected|public|private|internal)\s+"
                   r"(?:static\s+|readonly\s+|const\s+)*[\w<>\[\],\.\?]+\s+(\w+)\s*(?:=[^;]*)?;", re.M)

print("模块       文件                                零引用字段")
total = 0
for rel in TARGETS:
    path = os.path.join(ROOT, rel)
    src = io.open(path, encoding="utf-8-sig", newline="").read()
    module = rel.split(os.sep)[0]
    corpus = "\n".join(t for r, t in buckets.get(module, []))
    dead = []
    for m in FIELD.finditer(src):
        name = m.group(1)
        # 只统计"像字段访问/赋值"的出现次数，避免注释里提到也算
        count = len(re.findall(r"(?<![\w.])" + re.escape(name) + r"(?![\w])", corpus))
        if count <= 1:
            dead.append(f"{name}({count})")
    if dead:
        total += len(dead)
        print(f"{module:10s} {rel:36s} {', '.join(dead)}")
print("---")
print("零引用字段总数:", total)
