import io

path = r"C:\Project\UxGame\.workbuddy\memory\2026-09-21.md"
with open(path, "rb") as f:
    raw = f.read()
eol = "\r\n" if b"\r\n" in raw else "\n"
text = raw.decode("utf-8")

block = """
## 讲解：删轴的代价重新分级 + "什么时候真的需要新状态轴"（用户追问）

用户问两件事：①"删除后反而是优点？" ②"若将来动作需要多子状态，什么情况会需要"。

### 代价重新分级（修正上一节的列表）

| 代价 | 性质 | 说明 |
|---|---|---|
| 表现映射源少一个 | **假代价** | 见上一节更正：删轴前三道校验已禁止 Action 层配表现 |
| 未来多子状态 | **极弱** | 见下节判据，四条同时成立才需要，且那时几乎必落 `Control` |
| 枚举值冻结 | **阶段性免费** | 关键发现：这条不是"删轴"带来的，而是"一旦落盘/联网就冻结任何枚举"的通用约束。**删轴发生在网络同步之前，所以现在删是免费的**；若先联网再删，才需要数据迁移 |
| 统一事件面 / 查询面 | 真实，但当前零消费者 | `StateChanged` 与 `ActionChanged` 全项目零订阅 |
| `TimelinePlaybackLayer.Action` 同名不同域 | 真实且持续 | 纯阅读成本，不随代码演进消失 |

**结论**：不能笼统说"删除反而是优点"。准确表述是**在本项目当前时点删轴的净成本几乎为零**；收益来自"去掉副本"（中性减法），不是"删本身产生好处"。

### "什么情况会需要新状态轴"——四条判据

往 `StateLayer` 加值（无论叫不叫 Action）需要**同时**满足：

1. **对所有动作通用** —— 不是某一招特有
2. **需要外部能 set** —— 不只是生产方自己推进
3. **需要"进入/离开"的边沿语义** —— 而不是连续变化的数值
4. **与其它状态互斥** —— 同一时刻只能处其一

只满足部分的情况都有现成去处（本轮已核）：

- 只对某一招成立 → `CombatActionAsset` 字段（现有 `durationFrames` / `movementPolicy` / `cancelWindows` / `hitWindows` / `damage` / `appliedBuff`，`:121-146`）
- 只是招式内进度 → 从 `ActionFrame` 派生
- 语义是异常/控制 → 归 `Control` 层（`ControlState` = Normal / Stunned / Knockback / Frozen，`CombatController.SetControl` 是 public 外部入口）—— **`Control` 层本身就是"外部可 set + 互斥 + 有边沿"这一类，所以新出现的同类中间态会先落这里，而不是回动作轴**
- 只是跨单位查询 → 直接读 `entity.Controller.ActionRunner.HasAction`（`BattleWorld.cs:371` 已这么做，无需进状态层）

真正会触发新轴的需求形态：**需要在"没有任何招式实例存在"时依然成立的、不属于移动/控制的通用动作处境**（例如互斥的"戒备姿态 / 锁定姿态"这类，且各自要独立 Timeline）。当前跑不出这类需求。

### 一个重要澄清

**"不能再往 Action 层加值" ≠ "不能再加新状态轴"。** `StateLayer` 现在是 `0/1/2`，将来直接追加一个 `= 3` 即可 —— 不需要重编号、不影响现有三个值。删掉的是**那个特定的镜像轴**（把 `ActionRunner.HasAction` 这个 bool 投影成状态），不是"加轴"的能力。

**代码依旧零改动**，本轮只读核查 + 讲解。
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
