import io, os, re

ROOT = r"C:\Project\UxGame\Unity\Assets\Editor"

TARGETS = [
    r"Timeline\TimelineWindow.cs", r"Timeline\TimelineClipView.cs", r"Timeline\TimelineTrackView.cs",
    r"Timeline\TimelineClipItem.cs", r"Timeline\TimelineTrackItem.cs",
    r"Timeline\TimelineWindowHelper.cs",
    r"Timeline\Animation\TLAnimClipInspector.cs", r"Timeline\Animation\TLAnimTrackInspector.cs",
    r"Debugger\Event\EventDebuggerItem.cs", r"Debugger\Event\EventDebuggerWindow.cs",
    r"Debugger\Res\ResDebuggerWindow.cs", r"Debugger\UI\UIDebuggerWindow.cs", r"Debugger\UI\UIDebuggerItem.cs",
    r"Debugger\Time\TimeDebuggerItem.cs", r"Debugger\Time\TimeDebuggerItemSub1.cs",
    r"Debugger\Time\TimeDebuggerItemSub2.cs", r"Debugger\Time\TimeDebuggerWindow.cs",
    r"Build\Config\ConfigWindow.cs", r"Build\Proto\ProtoWindow.cs",
    r"Build\Version\VersionPackageViewer.cs", r"Build\Version\VersionWindow.cs",
    r"Build\UI\UIClassifyWindow.cs", r"Build\UI\UICodeMemberItem.cs", r"Build\UI\UICodeGenWindow.cs",
]

# Unity 生命周期 / 编辑器回调：由引擎反射调用，不能算死代码
WHITELIST = {
    "Awake", "Start", "Update", "LateUpdate", "FixedUpdate", "OnEnable", "OnDisable", "OnDestroy",
    "OnGUI", "CreateGUI", "OnFocus", "OnLostFocus", "OnSelectionChange", "OnProjectChange",
    "OnInspectorUpdate", "OnHierarchyChange", "ShowExample", "ShowConfigWindon", "ShowWindow",
    "CreateAssetMenu", "GetWindow", "OnSceneGUI", "Reset", "OnValidate", "OnMouseDown",
}

buckets = {}
for base, _, files in os.walk(ROOT):
    for name in files:
        if not name.endswith(".cs"):
            continue
        path = os.path.join(base, name)
        rel = os.path.relpath(path, ROOT)
        top = rel.split(os.sep)[0]
        text = io.open(path, encoding="utf-8-sig", errors="ignore", newline="").read()
        buckets.setdefault(top, []).append(text)

METHOD = re.compile(r"^\s*(?:private|protected|internal|public)?\s*(?:static\s+|virtual\s+|override\s+|async\s+)*"
                    r"[\w<>\[\],\.]+\s+(\w+)\s*\([^;]*\)\s*$", re.M)

total = 0
for rel in TARGETS:
    path = os.path.join(ROOT, rel)
    src = io.open(path, encoding="utf-8-sig", newline="").read()
    module = rel.split(os.sep)[0]
    corpus = "\n".join(buckets.get(module, []))
    dead = []
    for m in METHOD.finditer(src):
        name = m.group(1)
        if name in WHITELIST:
            continue
        count = len(re.findall(r"(?<![\w.])" + re.escape(name) + r"(?![\w])", corpus))
        if count <= 1:
            dead.append(name)
    if dead:
        total += len(dead)
        print(f"{module:10s} {rel:38s} {', '.join(dead)}")
print("---")
print("零引用方法数:", total)
