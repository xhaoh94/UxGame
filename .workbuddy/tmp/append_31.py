import io

path = r"C:\Project\UxGame\.workbuddy\memory\2026-09-21.md"
with open(path, "rb") as f:
    raw = f.read()
eol = "\r\n" if b"\r\n" in raw else "\n"
text = raw.decode("utf-8")

block = """
## 核查：`CombatController.IsGrounded` 改 auto-property（用户自己改的，问能否这样写）

用户把 `private bool _grounded = true;` + `public bool IsGrounded => _grounded;` 改成了 `public bool IsGrounded { get; private set; }`（声明处第 15 行留了 `// private bool _grounded = true;` 墓碑注释，属性**没有** `= true` 初始化器），问是否可行。

**结论：可行，功能完全等价，零行为变化 —— 但理由不是"省了变量"这么简单：`= true` 那行本来就是死代码（对外不可观测）。**

原因链：`Initialize()` 第一句就是 `Release()`，而 `Release()` 末尾写了 `IsGrounded = true`。所以在任何外部代码能读到 `IsGrounded` 之前，它已被置 true。且所有读取路径都有 `IsInitialized` 守卫：`Tick` 早退、`CaptureSnapshot` 返 null、`BattleWorld.ComputeStateHash`（`:361`）跳过未初始化实体、`SetControl`/`SetLife` 早退。测试也全部先 `Initialize`（`CombatRuntimeTests.cs:1040`）。所以"构造后未 Initialize"这个窗口没有任何读者。

**残留脆弱点（值得记）**：`IsGrounded` 的"默认着地"意图现在**只活在 `Release()` 的一行里**（原来声明处与 `Release()` 两处一致、互为印证）。若将来有人改掉 `Release()` 那行，初始值会静默变成 false，症状是 `Tick`（`:90`）第一帧就把 Locomotion 强制成 `Airborne`。建议补 `= true` 初始化器（功能等同，但把不变量留在声明处），或抽 `const bool DefaultGrounded` 让两处共用一处定义。

**另外发现的两处小尾巴**（未改，用户未要求）：`CaptureSnapshot` 里 `IsGrounded = IsGrounded,`（`:153`）现在读起来像自赋值 —— 左边是 `UnitCombatSnapshot` 的字段（定义在 `CombatActionRunner.cs:43`，public），右边是 `CombatController` 的属性，同名不同物；建议写 `this.IsGrounded` 消歧义。以及 `{get;private set;}` 的空格风格与全文件其余 5 处 `{ get; private set; }` 不一致。

**代码零改动**，本轮只读核查。
"""

if not text.endswith(eol):
    text += eol
text += block.replace("\n", eol)

with open(path, "wb") as f:
    f.write(text.encode("utf-8"))

with open(path, "rb") as f:
    raw2 = f.read()
print("bytes", len(raw), "->", len(raw2))
print("crlf", raw2.count(b"\r\n"), "lf", raw2.count(b"\n"), "bare", raw2.count(b"\n") - raw2.count(b"\r\n"))
