# -*- coding: utf-8 -*-
import io

P = r'C:\Project\UxGame\.workbuddy\memory\2026-09-21.md'

BLOCK = '''
## 「私有字段 + 转发属性」统一改成自动属性（用户要求）

把 Combat 模块里 `public X Foo => _foo;` 形式的转发属性改成 `public X Foo { get; private set; }`。

**改了 8 个属性 / 5 个文件**

| 文件 | 改动 |
|---|---|
| `Runtime/Unit/AttributeSet.cs` | `IsInitialized` / `MaxHp` / `Hp`（3 个字段下线） |
| `Runtime/Core/CombatStageBuffers.cs` | `CombatFrameEventTable.Frame` / `.Count`、`CombatHitBuffer.Frame`（保留 `= -1` 初值） |
| `Runtime/Unit/CombatActionRunner.cs` | `LocalSequence` |
| `Hotfix/Common/Combat/CombatComponent.cs` | `PresentationVariant`（初值搬到属性上） |
| `Runtime/Unit/CombatController.cs` | `IsGrounded` 补 `= true`，删掉墓碑注释 `// private bool _grounded = true;` |

**没改的 2 个（转不了，不是不想改）**

`BattleWorld.OrderedEntities`（`_ordered` 是 `ICombatEntity[]`）和 `ActionActiveEntities`（`_actionActive` 是 `readonly List<>`）——
两者对外类型是 `IReadOnlyList<>`，没有可写索引器 / `Add` / `Clear`，字段必须在类内保持具体类型。硬转会把「缓存复用、零分配」的意图拆掉。

**另外 3 个也不是转发属性**（虽然形如 `=> _x.y`）：`CombatFrameEventTable.this[int]`、`CombatHitBuffer.this[int]`、`CombatHitBuffer.Count => _hits.Count`、`CombatBuff.Count` —— 都是计算/投影，没有 1:1 后置字段。

**结论：这类转换是纯改写，零行为变化。** 依据三条：

1. 8 个字段全是非 `readonly` 的普通字段（`readonly` 字段不能用 `private set` 表达，得用 `{ get; }`）；
2. 这 5 个文件里没有任何 `[SerializeField]`（`[SerializeField]` 全在 `Asset/` 下的资产类），不会破坏 Unity 序列化；
3. 自动属性的初始化器与字段初始化器在同一初始化阶段写编译器生成的后置字段，`= -1` / `= true` / `= DefaultVariantId` 的时机完全不变。

**行为等价的两个细节**（都在类内，`private set` 可访问）：
`_pool[Count++]`（`++` 作用在带可访问 setter 的属性上合法）、`Hp -= applied`、`InstanceId = ++LocalSequence`。

**保留的写法歧义（与用户自己的选择保持一致）**：`AttributeSet.CaptureSnapshot` 里成了 `MaxHp = MaxHp,` / `Hp = Hp,`；`CombatController.CaptureSnapshot` 里用户先前已写成 `IsGrounded = IsGrounded,`。
左侧是 `UnitAttributeSnapshot`/`UnitCombatSnapshot` 的字段，右侧是 `this` 的属性，**能编译且语义正确**（无限定名的 `MaxHp` 不可能指到结构体字段），只是读起来像自赋值。加 `this.` 可读性更好，但用户没加，所以没动。

### 两个环境发现（已写进技能）

- **本仓库不是统一 CRLF。** `AttributeSet.cs`、`CombatStageBuffers.cs` 是**纯 LF**；`CombatActionRunner.cs`、`CombatComponent.cs`、`CombatController.cs` 是 CRLF。脚本必须**按文件探测并保留**行尾，不能假定 CRLF —— 我第一版脚本就在这个断言上直接 abort 了。
- **别靠"读截断的扫描输出"数期望值。** 我把 `_presentationVariant` 的期望命中数写成 3，实际是 5：扫描报告把行截断到 90 字符，第 176/341 行末尾的传参 token 没显示出来，但它是真命中。断言前应让脚本自己给出精确计数，而不是人工从打印里数。

### 校验方法（本次用的那套）

改 `.cs` 时可以证明「中文注释一个字节没动」：把两个版本的**非 ASCII 字符单独抽出来拼成字符串**再比对。
本次改动全是 ASCII 令牌替换，所以投影必须逐字符相等 —— 5 个文件全部相等（185/479/1198/1159/710 个非 ASCII 字符）。
加上「对外成员名集合比对」（前后一致）和 `git diff --check`（输出长度 0），够替代编译验证里"没误伤"的那一半。
'''

with io.open(P, 'r', encoding='utf-8', newline='') as fh:
    raw = fh.read()
assert '\r\n' not in raw or raw.count('\r\n') == raw.count('\n'), '混行尾'
raw = raw.rstrip('\n') + '\n' + BLOCK
with io.open(P, 'w', encoding='utf-8', newline='') as fh:
    fh.write(raw)

with io.open(P, 'r', encoding='utf-8', newline='') as fh:
    chk = fh.read()
print('lines=%d crlf=%d lf=%d' % (chk.count('\n'), chk.count('\r\n'), chk.count('\n')))
