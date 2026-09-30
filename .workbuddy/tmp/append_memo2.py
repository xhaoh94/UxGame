import io, os

P = r'C:\Project\UxGame\.workbuddy\memory\2026-09-28.md'

with io.open(P, 'r', encoding='utf-8', newline='') as f:
    raw = f.read()
crlf = raw.count('\r\n')
lf = raw.count('\n')
eol = '\r\n' if crlf == lf and lf > 0 else ('\n' if crlf == 0 else None)
if eol is None:
    raise SystemExit('ABORT: 行尾不统一 CRLF=%d LF=%d' % (crlf, lf))
print('原文 CRLF=%d LF=%d -> 用 %r' % (crlf, lf, eol))

text = raw.replace('\r\n', '\n')

ADD = """
## 复核更正：判等语义的变化不止一处

上一节写的"只有一处刻意差异（对空轨道重复 StopLayer）"**不完整**。真正的语义变化是：

**身份来源从"状态的坐标（定义域）"换成了"状态的产物（值域）"** —— 旧 `OwnerKey` 编码 `(layer, stateId, stableId, variant)`，新 `SameOwner` 只比 `Asset` 引用 + `InstanceId`。

当 `状态条目 → TimelineAsset` 不是单射时，两者结论相反：

| 现场 | 旧判定 | 新判定 |
|---|---|---|
| 两个不同 stateId 的条目共用同一条资产 | 变了 → `PlayOnLayer`（FadeOut 旧实例 + **新建 Timeline 实例** + 从权重 0 淡入） | 没变 → 只靠 `Evaluate` 推帧 |
| 换 variant，但 exact 与 default 两条候选命中同一条资产 | 同上 | 同上 |

`CharacterCombatProfile.ValidateRuntime` **不禁止**这种配置：它只查 `(layer, stateId, variant)` 三元组重复（`:286-291`）和 StableId 重复（`:280-284`），没有"一条资产只能配一次"的约束。所以多对一是合法资产配置，**不是理论上的**。

另注：`GetStatePresentation:46` 是三级 fallback（`exact` → `defaultPresentation` → `fallback`，靠 `SelectBetter` 挑），且旧 OwnerKey 用的是**条目自己的** `StableId`/`VariantId`（不是请求的 variant），所以对同一解析结果 key 是稳定的 —— 失配只来自上面两种"多对一"。

**其余小尾巴（不影响行为）**：
- `CombatTimelinePlayer._baseOwner/_actionOwner` 存整个 `CombatTimelineSelection`，但 `SameOwner` 只读 `Asset` + `InstanceId`，`Frame` 是**零读者**的死字段（存的是上次切轨道时的帧号）。原版只存一个 string 引用。
- `SameOwner` 内部用 `ReferenceEquals`，而同一表达式里的 `plan.Base.Asset != null` 走 Unity `Object.operator !=`（把已销毁引用当 null）。对 `ScriptableObject` 子类两者不等价；实际后果无害（销毁后判为"来源变了"，正好触发 `StopLayer`），但两套语义并存且无注释。
- `CombatTimelineSelection` 由 `{string,int}` 变 `{TimelineAsset,int,long}`（16 → 24 字节），`CombatTimelinePlan` 约 56 字节。当前所有调用点都传 `in`，无拷贝；这是隐式契约，将来漏写 `in` 的拷贝成本比旧版高。
- `SameOwner` 不比 `StateId`，"两个都没配 Timeline 的状态之间切换"不再走 `SyncLayer`（省一次幂等 `StopLayer`）。这是净好；唯一理论边界是"外部绕过 `CombatTimelinePlayer` 往 Base 层塞过东西"时不再被清 —— 当前无人绕过。
"""

out = text.rstrip('\n') + '\n' + ADD
with io.open(P, 'w', encoding='utf-8', newline='') as f:
    f.write(out.replace('\n', eol))

with io.open(P, 'r', encoding='utf-8', newline='') as f:
    chk = f.read()
print('落盘后 CRLF=%d LF=%d bytes=%d' % (chk.count('\r\n'), chk.count('\n'), len(chk.encode('utf-8'))))
print('中文探针存在=%s' % ('多对一' in chk))
