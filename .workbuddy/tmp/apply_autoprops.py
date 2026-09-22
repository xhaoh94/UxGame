"""把 Combat 模块里 'prop => _field;' 形式的转发属性改成自动属性 { get; private set; }。

设计：先把全部文件的替换在内存里做完并全部断言通过，再统一落盘 ——
任何一处断言失败都不会留下半写状态。
"""
import io
import re
import sys

BASE = r'C:\Project\UxGame\Unity\Assets'
A = BASE + r'\HotfixBase\Manager\Combat\Runtime\Unit\AttributeSet.cs'
B = BASE + r'\HotfixBase\Manager\Combat\Runtime\Core\CombatStageBuffers.cs'
C = BASE + r'\HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs'
D = BASE + r'\Hotfix\Common\Combat\CombatComponent.cs'
E = BASE + r'\HotfixBase\Manager\Combat\Runtime\Unit\CombatController.cs'


def load(path):
    with io.open(path, 'r', encoding='utf-8', newline='') as fh:
        raw = fh.read()
    crlf = raw.count('\r\n')
    lf = raw.count('\n')
    if crlf == lf:
        eol = '\r\n'
    elif crlf == 0:
        eol = '\n'
    else:
        print('ABORT: %s 混行尾 (CRLF=%d LF=%d)' % (path, crlf, lf))
        sys.exit(1)
    print('     eol=%r' % eol)
    return raw.replace('\r\n', '\n'), raw.count('\n') + 1, eol


def save(path, text, eol):
    out = text.replace('\n', eol)
    with io.open(path, 'w', encoding='utf-8', newline='') as fh:
        fh.write(out)


def sub(text, name, old, new, expect=1):
    got = text.count(old)
    if got != expect:
        print('ABORT [%s]: 命中 %d 次，期望 %d 次' % (name, got, expect))
        sys.exit(1)
    return text.replace(old, new)


def rename(text, name, token, new_token, expect):
    pat = re.compile(r'(?<![A-Za-z0-9_.])' + re.escape(token) + r'(?![A-Za-z0-9_])')
    got = len(pat.findall(text))
    if got != expect:
        print('ABORT [%s]: %s 命中 %d 次，期望 %d 次' % (name, token, got, expect))
        sys.exit(1)
    return pat.sub(new_token, text)


# ─────────────────────────── AttributeSet.cs ───────────────────────────
def patch_a(t):
    t = sub(t, 'A1 字段+属性块', '''        private int _maxHp;
        private int _hp;
        private bool _initialized;

        public bool IsInitialized => _initialized;

        public int MaxHp => _maxHp;

        public int Hp => _hp;

''', '''        public bool IsInitialized { get; private set; }

        public int MaxHp { get; private set; }

        public int Hp { get; private set; }

''')
    t = rename(t, 'A2 _maxHp', '_maxHp', 'MaxHp', 7)
    t = rename(t, 'A3 _hp', '_hp', 'Hp', 11)
    t = rename(t, 'A4 _initialized', '_initialized', 'IsInitialized', 6)
    return t


# ───────────────────────── CombatStageBuffers.cs ────────────────────────
def patch_b(t):
    t = sub(t, 'B1 事件表字段+属性', '''        private readonly List<CombatFrameEventSet> _pool = new();
        private int _count;
        private long _frame = -1;

        public long Frame => _frame;

        public int Count => _count;
''', '''        private readonly List<CombatFrameEventSet> _pool = new();

        public long Frame { get; private set; } = -1;

        public int Count { get; private set; }
''')
    t = sub(t, 'B2 事件表 BeginFrame', '''        public void BeginFrame(long frame)
        {
            _frame = frame;
            _count = 0;
        }
''', '''        public void BeginFrame(long frame)
        {
            Frame = frame;
            Count = 0;
        }
''')
    t = sub(t, 'B3 Append 容量判断', '''            if (_count == _pool.Count)''', '''            if (Count == _pool.Count)''')
    t = sub(t, 'B4 Append 取项', '''            var set = _pool[_count++];''', '''            var set = _pool[Count++];''')
    t = sub(t, 'B5 TryFind 循环', '''            for (var i = 0; i < _count; i++)''', '''            for (var i = 0; i < Count; i++)''')
    t = sub(t, 'B6 命中缓冲字段+属性', '''        private readonly List<CombatHitCandidate> _hits = new();
        private long _frame = -1;

        public long Frame => _frame;
''', '''        private readonly List<CombatHitCandidate> _hits = new();

        public long Frame { get; private set; } = -1;
''')
    t = sub(t, 'B7 命中缓冲 BeginFrame', '''        public void BeginFrame(long frame)
        {
            _frame = frame;
            _hits.Clear();
        }
''', '''        public void BeginFrame(long frame)
        {
            Frame = frame;
            _hits.Clear();
        }
''')
    t = sub(t, 'B8 命中缓冲 Clear', '''        public void Clear()
        {
            _frame = -1;
            _hits.Clear();
        }
''', '''        public void Clear()
        {
            Frame = -1;
            _hits.Clear();
        }
''')
    return t


# ───────────────────────── CombatActionRunner.cs ────────────────────────
def patch_c(t):
    t = sub(t, 'C1 删除字段声明', '''        private long _localSequence;
        private long _simulationFrame;
''', '''        private long _simulationFrame;
''')
    t = sub(t, 'C2 属性转自动属性', '''        public long LocalSequence => _localSequence;''',
            '''        public long LocalSequence { get; private set; }''')
    t = rename(t, 'C3 _localSequence', '_localSequence', 'LocalSequence', 7)
    return t


# ─────────────────────────── CombatComponent.cs ─────────────────────────
def patch_d(t):
    t = sub(t, 'D1 删除字段声明', '''        private bool _registered;
        private string _presentationVariant = CombatStatePresentation.DefaultVariantId;
''', '''        private bool _registered;
''')
    t = sub(t, 'D2 属性转自动属性', '''        public string PresentationVariant => _presentationVariant;''',
            '''        public string PresentationVariant { get; private set; } = CombatStatePresentation.DefaultVariantId;''')
    t = rename(t, 'D3 _presentationVariant', '_presentationVariant', 'PresentationVariant', 5)
    return t


# ─────────────────────────── CombatController.cs ────────────────────────
def patch_e(t):
    t = sub(t, 'E1 删除墓碑注释', '''        // private bool _grounded = true;

        public CharacterCombatProfile Profile { get; private set; }
''', '''        public CharacterCombatProfile Profile { get; private set; }
''')
    t = sub(t, 'E2 IsGrounded 补默认值', '''        public bool IsGrounded {get;private set;}''',
            '''        public bool IsGrounded { get; private set; } = true;''')
    return t


TASKS = [
    (A, patch_a, ['_maxHp', '_hp', '_initialized']),
    (B, patch_b, ['_frame', '_count']),
    (C, patch_c, ['_localSequence']),
    (D, patch_d, ['_presentationVariant']),
    (E, patch_e, ['_grounded']),
]

pending = []
for path, fn, dead in TASKS:
    print(path.split('\\')[-1])
    text, lines, eol = load(path)
    new_text = fn(text)
    new_lines = new_text.count('\n') + 1
    # 转自动属性会新增成对的 {}，所以只比对"净配对差"是否被破坏
    if (new_text.count('{') - new_text.count('}')) != (text.count('{') - text.count('}')):
        print('ABORT [%s]: 花括号净配对差变化' % path)
        sys.exit(1)
    if (new_text.count('(') - new_text.count(')')) != (text.count('(') - text.count(')')):
        print('ABORT [%s]: 圆括号净配对差变化' % path)
        sys.exit(1)
    for tok in dead:
        hits = len(re.findall(r'(?<![A-Za-z0-9_.])' + re.escape(tok) + r'(?![A-Za-z0-9_])', new_text))
        if hits:
            print('ABORT [%s]: 残留 %s x%d' % (path, tok, hits))
            sys.exit(1)
    pending.append((path, text, new_text, lines, new_lines, eol))
    print('OK  %-24s 行数 %d -> %d' % (path.split('\\')[-1], lines, new_lines))

for path, text, new_text, lines, new_lines, eol in pending:
    save(path, new_text, eol)
print('\n全部落盘完成')
