import io

path = r"C:\Project\UxGame\.workbuddy\memory\2026-09-21.md"
with open(path, "rb") as f:
    raw = f.read()
eol = "\r\n" if b"\r\n" in raw else "\n"
text = raw.decode("utf-8")

block = """
## 更正：「表现层少一个状态映射源」这条缺点不成立（用户追问"不能再配表现啥意思"）

上一节我把缺点 ④ 写成"`Action/Executing` 不能再配表现"，措辞不准确。**查了 `HEAD` 版本（= 删轴前，未提交改动都在工作区）后确认：Action 层在删轴之前就已经被显式禁止配表现了，这条是名义代价，零能力损失。**

**"配表现"的机制**：资产 `CharacterCombatProfile.statePresentations` 是一张 `(StateLayer layer, int stateId, string variantId) → TimelineAsset` 的映射表（`CombatStatePresentation.cs:14`）。编辑器入口是 `CombatEditorWindow.AddNextDefaultPresentation`（`:1436`），它遍历 `PresentationLayers` × `CombatStateId.GetMappableStateIds(layer)` 找出还没配的组合，插一条新条目。运行时由 `CombatTimelineResolver.Resolve` 调 `profile.GetStatePresentation(layer, stateId, variant)` 取出，放进 `CombatTimelinePlan.Base`，播在 `TimelinePlaybackLayer.Base`。

**删轴前的三道关卡（全部来自 `HEAD` 版本实测）**：

1. `CombatStateId.IsStatePresentationMappable`（HEAD `CombatState.cs:126-131`）**第一句就是** `if (layer == StateLayer.Action) { return false; }`；doc 注释原文：「只排除结构性错误项：- Action 层整层禁止（由技能列表管理，ValidateRuntime 会拒绝）；- Control.Normal、Life.Alive……」→ 删轴时这 4 行随枚举一起删掉。
2. `CombatEditorWindow.PresentationLayers`（HEAD `:94-99`）**删轴前就只有 3 项** `Locomotion / Control / Life`，从来不含 Action。
3. `CharacterCombatProfile.ValidateRuntime`（`:273-279`）对非法映射直接 `throw new InvalidOperationException($"状态表现不能映射该状态: ...")`；编辑器侧 `CombatEditorUtility`（`:655-664`）报 `CombatValidationIssue(Error, "...不是允许配置表现映射的逻辑状态。")`。

**动作表现走的是完全独立的第二张表**：`CombatActionPresentation`（`CombatActionPresentation.cs:11`）= `(CombatActionAsset action, TimelineAsset timeline)`，存在 `actionPresentations` 里，键是 `actionId`，与 `StateLayer` 无关。`CombatTimelineResolver.Resolve`（`CombatTimelinePlayer.cs:61-68`）在 `actions.HasAction` 时用 `profile.GetActionTimeline(actions.Current.ActionId)` 取出，放进 `CombatTimelinePlan.Action`，播在 `TimelinePlaybackLayer.Action`。

**结论修正**：删轴在这条上实际是**减法** —— 少了一个"专门用来否决整个枚举值"的分支（`IsStatePresentationMappable` 从 3 个否决条件降到 2 个）。上一节的"缺点 ④"应改写为：「`statePresentations` 的候选层从 4 个枚举值降到 3 个；但 Action 层在被删前就已由三道校验禁止配置，属零能力损失。」

**教训**：判断"某个枚举值/能力是否真被使用"时，不能只看"它是否存在于类型里"，必须查**它是否被显式的允许列表/校验条件放行**。这次的 `PresentationLayers` 与 `IsStatePresentationMappable` 就是两道"存在但被否决"的例子 —— 用 `git show HEAD:<path>` 取改动前版本可直接对比，比翻对话历史可靠。
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
