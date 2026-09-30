import io, os

D = r'C:\Project\UxGame\.workbuddy\memory'
ref = os.path.join(D, '2026-09-21.md')
with io.open(ref, 'r', encoding='utf-8', newline='') as f:
    r = f.read()
crlf = r.count('\r\n'); lf = r.count('\n')
print('ref 2026-09-21 LF=%d CRLF=%d' % (lf, crlf))
use_crlf = (crlf == lf and crlf > 0)

path = os.path.join(D, '2026-09-28.md')
exists = os.path.exists(path)

BLOCK = u'''# 2026-09-28

## Combat 表现层每帧拼 OwnerKey 的开销（分析，未改码）

现场：`CombatComponent.TickLogic:175` 每逻辑帧每单位调一次 `CombatTimelineResolver.Resolve`。

单次 Resolve 的实际开销（按贵的排序）：
1. `profile.GetStatePresentation` 是**线性扫描** `statePresentations`，且**每个元素**都要读 `presentation.VariantId` —— 那是属性，内部再跑一遍 `NormalizeVariantId`（`IsNullOrWhiteSpace` + `Trim`）。Resolve 里最多调 3 次（Life / Control / Locomotion），加开头自己那次 `NormalizeVariantId(variantId)`。
2. `OwnerKey` 是 `$"..."` 插值。Unity 2021 = C# 9，没有 interpolated string handler → 走 `string.Format`，每次 1 个 string 分配 + 值类型装箱。Locomotion 那条**无条件构造**，action 那条在有招式时构造。
3. 同帧的 `CombatComponent.RefreshTimeline:341-344` 算的 `changed`，**7 个调用点全部丢弃返回值** → 那 3 次字符串比较是死工作。

口径问题：模块里其它地方刻意零分配（`_ordered` 数组复用、避开 `SortedDictionary` 枚举器装箱、`ActionActiveEntities` 少扫一遍），唯独这里每帧分配。

`OwnerKey` 的全部用途只是"和上一帧比是否变了"（`CombatTimelinePlayer.cs:85/87/99/100`），没有消费者读它的内容 —— 只有 `CombatTimelinePlayerTests.cs:12/24/26` 在做往返断言。

推荐方案：key 从 `string` 换成结构化值 + 引用身份。`CombatStatePresentation` 是 `sealed class`，**引用相等即身份**；action 槽用 `InstanceId`(long)。`default(TimelineOwner)` 不会和真实 key 碰撞（`InstanceId = ++LocalSequence`，从 1 起），可以顶替原 `string.Empty` 那个哨兵。保留 `StateId` 字段是为了和现在的字符串语义**逐项等价**（"换了状态但都没映射"仍要判为变化）。

否决的方案：事件驱动（订阅 `StateChanged`/`ActionChanged`，只在变化时 Resolve）—— `Frame` 每帧都要读，Resolve 无论如何每帧都要跑；key 免费之后没东西可省，还引入新订阅耦合（`StateChanged` 至今零订阅者）。

影响面：`CombatTimelineSelection` / `CombatTimelinePlan` / `CombatTimelinePlayer`（2 个 owner 字段 + 比较）/`CombatComponent.RefreshTimeline` / 1 个测试文件 / `COMBAT_DESIGN.md:22`。这两个 struct 是 public 且在热更 dll（HotfixBase）→ 改完要重编 dll。

## 维护
- MEMORY.md 超注入上限被截断，已备份到 `.workbuddy/tmp/backup_MEMORY.md` 后收敛：12469 字符 → 4284 字符（删掉改名/删轴的过程考古与重复表述，只留规则与坑）。
'''

body = BLOCK if not exists else ''
out = body
if use_crlf:
    out = out.replace('\n', '\r\n')
if exists:
    with io.open(path, 'a', encoding='utf-8', newline='') as f:
        f.write(out)
else:
    with io.open(path, 'w', encoding='utf-8', newline='') as f:
        f.write(out)

with io.open(path, 'r', encoding='utf-8', newline='') as f:
    c = f.read()
print('WROTE %s bytes=%d LF=%d CRLF=%d' % (path, len(c.encode('utf-8')), c.count('\n'), c.count('\r\n')))
