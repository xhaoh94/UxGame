import io

PATH = r'C:\Project\UxGame\.workbuddy\memory\2026-09-28.md'

with io.open(PATH, 'r', encoding='utf-8', newline='') as f:
    raw = f.read()

crlf = raw.count('\r\n')
lf = raw.count('\n')
eol = '\r\n' if (crlf > 0 and crlf == lf) else '\n'
print('BEFORE crlf=%d lf=%d bytes=%d eol=%r' % (crlf, lf, len(raw), eol))

block = eol.join([
    '',
    '### 回滚会不会重置表现层（已核实）',
    '',
    '**会重置，机制是"换新实例 + 直接定位"，不是倒带。**',
    '',
    '- 链路：`CombatComponent.RestoreSnapshot`（`Hotfix/Common/Combat/CombatComponent.cs:286`）→ `Controller.RestoreSnapshot` + **`RefreshTimeline(true)`** + `_commands.DiscardBefore(snapshot.SimulationFrame + 1)`。',
    '- `RefreshTimeline(force)`（`:329`）→ `Synchronize(plan, animator, force)`；`CombatTimelinePlayer.Synchronize`（`:52`）里 `force` 直接让 `baseChanged`/`actionChanged` 都为真 → 两层都走 `SyncLayer`。',
    '- `SyncLayer`（`:95`）：`Asset == null` → `StopLayer`（淡出）；否则 `PlayOnLayer` —— `TimelineComponent.cs:67` 里是「旧实例 `FadeOut` + 新建实例 `StartWeightFade` 从 0 淡入」。',
    '- 最后 `SetLayerFrame(layer, Frame, replayFrameZero: false)` → `Timeline.Set(frame, false)` → `EvaluateInternal(..., Seek)`。`TimelineComponent.cs:107` 注释原文：「只定位指定层；**回滚**、编辑器拖标尺等定位**不会触发表现事件**」→ 不会把已经放过的表现事件重播一遍。',
    '',
    '**特效不是独立生成的 GameObject**：`ParticleAssetTrack` / `TLParticleClip` 绑的是**单位身上的 `ParticleSystem`**（`TLParticleTrack.BoundParticle` ← `Component.GetBinding<ParticleSystem>(Asset)`），由 clip 在每个 `OnEvaluate` 里 `particle.Simulate(localTime, restart:true, fixedTimeStep:true, false)` 驱动；`OnStop`/`OnDisable` 走 `Stop(true, StopEmittingAndClear)`。',
    '→ 所以回滚时特效也一起被重置/停止，**不会留下"刀光还在飘但服务器说没打中"的残留**（它不是自己活的对象）。',
    '',
    '**三个入口的 force 取值不是随手写的**：',
    '- `RestoreSnapshot` → `force: true`（回滚后什么都可能变）',
    '- `ConfirmAction`（`:256`）→ `force: true` —— 关键是 `Confirm` 纠正的核心是 **`ActionFrame`**（`CombatActionRunner.cs:285` 按 `authoritativeStartFrame` 重算），而 `Frame` **刻意不参与** `SameOwner` 判等，所以身份恰好没变时只有 force 才能重新对齐。',
    '- `RejectAction`（`:270`）→ `force: false` —— 动作已被 `EndCurrent(Rejected)` 摘掉，`plan.Action.Asset` 天然变 null，靠身份差异就能走到 `StopLayer`，不需要强制重装。',
    '',
])

if not raw.endswith(eol):
    block = eol + block

out = raw + block
with io.open(PATH, 'w', encoding='utf-8', newline='') as f:
    f.write(out)

with io.open(PATH, 'r', encoding='utf-8', newline='') as f:
    chk = f.read()
print('AFTER  crlf=%d lf=%d bytes=%d' % (chk.count('\r\n'), chk.count('\n'), len(chk)))
print('PREFIX_PRESERVED=%s EOL_UNCHANGED=%s' % (chk.startswith(raw), chk.count('\r\n') == crlf))
