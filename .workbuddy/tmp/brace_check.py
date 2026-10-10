import io, os

ROOT = r"C:\Project\UxGame\Unity\Assets\Editor"

FILES = [
    r"Timeline\TimelineWindow.cs",
    r"Timeline\TimelineClipView.cs",
    r"Timeline\TimelineTrackView.cs",
    r"Timeline\TimelineClipItem.cs",
    r"Timeline\TimelineTrackItem.cs",
    r"Timeline\Animation\TLAnimClipInspector.cs",
    r"Timeline\Animation\TLAnimTrackInspector.cs",
    r"Debugger\DebuggerUiUtil.cs",
    r"Debugger\Event\EventDebuggerItem.cs",
    r"Debugger\Event\EventDebuggerWindow.cs",
    r"Debugger\Res\ResDebuggerWindow.cs",
    r"Debugger\UI\UIDebuggerWindow.cs",
    r"Debugger\UI\UIDebuggerItem.cs",
    r"Debugger\Time\TimeDebuggerItem.cs",
    r"Debugger\Time\TimeDebuggerItemSub1.cs",
    r"Debugger\Time\TimeDebuggerItemSub2.cs",
    r"Debugger\Time\TimeDebuggerWindow.cs",
    r"Build\Config\ConfigWindow.cs",
    r"Build\Proto\ProtoWindow.cs",
    r"Build\Version\VersionPackageViewer.cs",
    r"Build\Version\VersionWindow.cs",
    r"Build\UI\UIClassifyWindow.cs",
    r"Build\UI\UICodeMemberItem.cs",
    r"Build\UI\UICodeGenWindow.cs",
]


def strip(src):
    out, i, n, state = [], 0, len(src), None
    while i < n:
        c = src[i]
        nxt = src[i + 1] if i + 1 < n else ''
        if state is None:
            if c == '/' and nxt == '/':
                state = 'line'; i += 2; continue
            if c == '/' and nxt == '*':
                state = 'block'; i += 2; continue
            if c == '@' and nxt == '"':
                state = 'verbatim'; i += 2; continue
            if c == '"':
                state = 'str'; i += 1; continue
            if c == "'":
                state = 'char'; i += 1; continue
            out.append(c); i += 1; continue
        if state == 'line':
            if c == '\n':
                state = None; out.append(c)
            i += 1; continue
        if state == 'block':
            if c == '*' and nxt == '/':
                state = None; i += 2; continue
            if c == '\n':
                out.append(c)
            i += 1; continue
        if state == 'str':
            if c == '\\':
                i += 2; continue
            if c == '"':
                state = None
            i += 1; continue
        if state == 'char':
            if c == '\\':
                i += 2; continue
            if c == "'":
                state = None
            i += 1; continue
        if state == 'verbatim':
            if c == '"' and nxt == '"':
                i += 2; continue
            if c == '"':
                state = None
            i += 1; continue
    return ''.join(out)

bad = 0
for rel in FILES:
    path = os.path.join(ROOT, rel)
    src = io.open(path, encoding='utf-8-sig', newline='').read()
    code = strip(src)
    c = {k: code.count(k) for k in '{}()'}
    ok = c['{'] == c['}'] and c['('] == c[')']
    if not ok:
        bad += 1
    print(f"{'OK ' if ok else 'BAD'} {rel:45s} {{={c['{']:3d} }}={c['}']:3d} (={c['(']:3d} )={c[')']:3d}")
print('---')
print('不平衡文件数:', bad)
