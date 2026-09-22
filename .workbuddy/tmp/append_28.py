import io, os

path = r"C:\Project\UxGame\.workbuddy\memory\2026-09-21.md"
with open(path, "rb") as f:
    raw = f.read()

eol = "\r\n" if b"\r\n" in raw else "\n"
text = raw.decode("utf-8")

block = """
## 讲解：删掉 Action 层之后的优缺点（用户提问，无代码改动）

用户问"少了 Action 这个层，有什么优点和缺点"。

**定位**：删掉的不是"一层逻辑"，而是一份与 `CombatActionRunner.HasAction` 完全重复的存储（bool 投影成 int 状态）。所以好处全部来自"去掉副本"，代价全部来自"少一个统一读/写入口"。

**优点**（收益递减）：① 唯一真相源，不存在"状态机说 Executing、Runner 说没动作"；② 消除每帧同步点（`SyncAction` 是顺序敏感的追同步，正是 Frame fork 的土壤）；③ 消除粒度损失（Action 层只有 2 值，Runner 有 `ActionFrame`/`InstanceId`/`HasHitConfirmed`）；④ 快照只剩一份，回滚不会撕裂；⑤ `Layers`/`_layers`/`LayerIndex`/`CaptureSnapshot`/`RestoreSnapshot`/`GetLayer` 六处同步表各少一项（这几处漏改不报编译错）；⑥ 每帧常数开销下降；⑦ 概念边界清楚（状态层答"宏观处境"，Runner 答"在不在出招"）。

**缺点**：① 失去统一事件面 —— `StateChanged` 不再覆盖动作起停（**但 `CombatStateMachine.StateChanged` 与 `CombatActionRunner.ActionChanged` 当前全项目零订阅**，属结构缺口不是回归）；② 失去统一查询面 —— `GetCurrentStateId(StateLayer)` / `GetStateFrame` / `IsPlaying` / `CaptureSnapshot` 不再覆盖动作；③ **枚举/快照格式变更：`StateLayer` 从 4 值变 3 值且是连续重编号（0/1/2）。当前快照无落盘、无网络传输，所以安全；一旦引入网络同步或落盘，必须先冻结枚举值**（否则旧的 `Action=0` 会被读成 `Locomotion`）；④ 表现层少一个状态映射源（`Action/Executing` 不能再配表现，动作表现只能走 `profile.GetActionTimeline`）；⑤ **命名坑：`TimelinePlaybackLayer.Action`（表现层，值 100）仍在**，与已删的 `StateLayer.Action` 同名不同域，读文档易混；⑥ 若将来动作需要多子状态，不能再往 Action 层加值（应由 `CombatActionRunner` 结构承担）。

**顺带澄清**：combo-cancel（同帧 `EndCurrent(Cancelled)` + `Start`）在删除前 Action 层是 `Executing→Executing` 被 `ChangeState` 去重 → 静默；Runner 层却发两条 `ActionChanged`。"表面无变化、实际换了动作"的伪一致性随删轴消失，语义更诚实。

**代码零改动**，本轮只读核查 + 讲解。
"""

if not text.endswith(eol):
    text += eol
text += block.replace("\n", eol)

with open(path, "wb") as f:
    f.write(text.encode("utf-8"))

with open(path, "rb") as f:
    raw2 = f.read()
print("bytes", len(raw), "->", len(raw2))
print("crlf", raw2.count(b"\r\n"), "lf", raw2.count(b"\n"))
print("bare_lf", raw2.count(b"\n") - raw2.count(b"\r\n"))
