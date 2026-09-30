# -*- coding: utf-8 -*-
"""写记忆：MEMORY.md 补一条不变量 + 当日日志追加本轮实现结果。"""
import io, os, sys

D = r'C:\Project\UxGame\.workbuddy\memory'
MEM = os.path.join(D, 'MEMORY.md')
LOG = os.path.join(D, '2026-09-28.md')


def load(p):
    with io.open(p, 'r', encoding='utf-8', newline='') as f:
        raw = f.read()
    crlf, lf = raw.count('\r\n'), raw.count('\n')
    eol = '\r\n' if crlf == lf else ('\n' if crlf == 0 else None)
    if eol is None:
        print('ABORT: %s 混行尾' % p); sys.exit(1)
    return raw.replace('\r\n', '\n'), eol


# --- MEMORY.md：在"零消费者的扩展点"之后补一条 ---
text, eol = load(MEM)
ANCHOR = u'- 零消费者的扩展点：`StateChanged`、`IsPlaying(StateLayer,int)`、`CombatFrameEventTable.TryFind`、取消窗口。\n'
ADD = u'- 表现层"要不要换轨道"靠 `CombatTimelineSelection.SameOwner`（**Asset 引用 + InstanceId**）判断，不拼字符串 —— `Resolve` 每逻辑帧每单位跑一次，插值会分配。**`Frame` 必须不参与比较**（每帧都在变，参与了就等于每帧重播）；`InstanceId` 必须参与（连招第二下要重播）。\n'
if text.count(ANCHOR) != 1:
    print('ABORT: MEMORY 锚点命中 %d 次' % text.count(ANCHOR)); sys.exit(1)
text = text.replace(ANCHOR, ANCHOR + ADD)
with io.open(MEM, 'w', encoding='utf-8', newline='') as f:
    f.write(text.replace('\n', eol))
print('MEMORY.md 已补一条（%d 字符）' % len(text))

# --- 当日日志追加 ---
BLOCK = u'''
## 落地：表现层 owner key 结构化（去每帧字符串分配）

改了 4 个文件：
- `Presentation/CombatTimelinePlayer.cs`：`CombatTimelineSelection` 去掉 `string OwnerKey`，加 `long InstanceId`；新增 `SameOwner(in ...)`（`ReferenceEquals(Asset)` + `InstanceId`）；`Resolve` 四处构造改为传资产；`CombatTimelinePlayer._baseOwner/_actionOwner` 由 `string` 改成整个 `CombatTimelineSelection`（**不额外存 Asset，避免又造一份副本**），`string.Empty` 哨兵换成 `default`。
- `Hotfix/Common/Combat/CombatComponent.cs`：`RefreshTimeline` 的 `changed` 与 3 次字符串比较删除（7 个调用点全部丢弃返回值），返回类型 `bool` → `void`；连带 `_framePlanInitialized` 变成只写字段 → 一并删除（声明 + 3 处赋值）。
- `Editor/Timeline/TimelineWindow.cs`：预览的 `baseKey`/`preview:action:{InstanceID}` 字符串去掉，身份直接用资产。
- `Editor/Combat/Tests/CombatTimelinePlayerTests.cs`：两个老测试适配 + 新增 `SameOwnerIgnoresFrameAndTracksAssetAndInstance`（Frame 不参与、InstanceId 参与、null 资产相等）。

行为等价性：只有一处刻意差异 —— 曾有一次"对空轨道重复 `StopLayer`"不再发生（旧代码 `""` vs `null` 的哨兵不匹配恰好每次都触发一次同步）。轨道本来就没东西在播，且真实的"动作结束"路径仍会停轨道。

`GetStatePresentation` 的线性扫描**没有动**：重新估了一下，资产条目是个位数，扫描成本与字符串插值同量级；要缓存就得处理编辑器改动后的失效，性价比不够。上一轮说它是"最贵"是我高估了。

校验：4 文件行尾按原样保留（全 CRLF）；`git diff --check` 空；`OwnerKey`/`_framePlanInitialized`/`preview:action:` 全仓零残留；括号净配对差为 0；**往返证明**（反向应用规则重建旧版再正向跑一遍，与落盘逐字节相同）→ 改动就是规则本身。回滚副本在 `.workbuddy/tmp/backup_ownerkey_pre/`。`COMBAT_DESIGN.md:22` 的 `CombatTimelinePlayer.cs:36` → `:50`（Resolve 下移）。
**未编译** —— 需在 Unity 里过一遍；这两个 struct 是 public 且在热更 dll（HotfixBase），改完要重编 dll。
'''

L, leol = load(LOG)
if not L.endswith('\n'):
    L += '\n'
with io.open(LOG, 'w', encoding='utf-8', newline='') as f:
    f.write((L + BLOCK).replace('\n', leol))
c = io.open(LOG, 'r', encoding='utf-8', newline='').read()
print('日志已追加：%d 字节 crlf=%d lf=%d' % (len(c.encode('utf-8')), c.count('\r\n'), c.count('\n')))
