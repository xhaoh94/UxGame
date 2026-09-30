# -*- coding: utf-8 -*-
"""向 COMBAT_DESIGN.md 追加批次 I 章节（保持纯 CRLF）。"""
import io

PATH = r'C:\Project\UxGame\Unity\Assets\Editor\Combat\COMBAT_DESIGN.md'
MARKER = '批次 I：三连击示例与特效轨运行时绑定'

LINES = [
    '#### 批次 I：三连击示例与特效轨运行时绑定（已完成）',
    '',
    '前八个批次都在收敛工具与边界，本批次换一条路径验证：**只用现有机制能不能做出玩得通的连招，'
    '并把表现层的特效轨真正接上运行时**。结论是机制够用，缺口只在一处绑定代码。',
    '',
    '**连招的数据载体是取消窗口。** 普攻三段 `HeroZSAttack01/02/03` 的取消窗口依次指向下一段，'
    '第三段指回第一段构成环。链的拓扑完全由资源决定，加第四段不需要改任何 C#。',
    '',
    '**输入层只认识链头。** `OperateComponent` 把同一个按键固定映射到链头 `1001`；'
    '`CombatActionRunner.ResolveComboTarget(chainRoot)` 按当前动作已打开的取消窗口算出'
    '"这一段该推到哪个动作"，`CombatComponent.RequestComboAttack` 用它入队。'
    '所以"按的是同一个键、出的是第几段"这件事由资源回答。',
    '',
    '**查表与消费共用判据。** `ResolveComboTarget` 与 `TryCancel` 都走 '
    '`ActionCancelWindow.IsOpen(actionFrame, hasHitConfirmed)`，因此查表返回的动作号在下一帧一定'
    '能被接受。唯一例外是窗口最后一帧按下 —— 命令要下一帧才被消费，那时窗口已关。'
    '这是"命令延迟一帧"的固有代价而非缺陷，已用 '
    '`ComboQueryOnLastWindowFrameFallsOutsideAfterOneFrameDelay` 固化。',
    '',
    '**表现层的真实缺口在绑定，不在数据。** 编辑器预览路径（`TimelineWindow.AutoBindMissingTracks`）'
    '会给粒子轨自动挑一个系统，运行时 `CombatTimelinePlayer.SyncLayer` 却只绑 `Animator` —— '
    '于是特效轨配了数据也永远不播。本批次把绑定收进 `BindTracks`：'
    '`AnimationTrackAsset → Animator`、`ParticleAssetTrack → ParticleSystem`，在 `PlayOnLayer` '
    '**之后**执行。顺序是不变量：`PlayOnLayer` 会新建播放实例并让旧实例淡出，先绑会被新实例丢掉。',
    '',
    '**占位特效。** 粒子轨没有资产级引用，只能由外部提供一个 `ParticleSystem`：'
    '`CombatVfxHost.Ensure` 优先复用模型上美术已摆好的系统，没有才按 URP 粒子 Shader 建一个临时宿主。'
    '`ParticleClipAsset.startColor` 让三段颜色可区分，肉眼能看出连到了第几段。'
    '`TLParticleClip` 用 `Simulate` 按权威帧重建粒子状态，所以宿主必须保持 `playOnAwake = false` '
    '且停止，否则自动播放会和 `Simulate` 叠加。',
    '',
    '**测试与资产。** 新增 6 项单元测试（`ResolveComboTarget*` / `ComboQuery*`）覆盖回落到链头、'
    '窗口半开区间、命中确认前置、三段环链推进，以及"查表结果下一帧可被消费"这条等价性。'
    '资产由脚本生成并静态校验：新 guid 的**持有文件集合**必须精确等于预期（比计数强，'
    '能逮到多发的一处引用）、行尾不混用、每段命中/取消窗口区间落在时长内、Profile 引用闭合。',
]


def main():
    with io.open(PATH, 'r', encoding='utf-8', newline='') as f:
        text = f.read()

    assert MARKER not in text, '批次 I 章节已存在，跳过'
    assert '\r\n' in text and '\n' not in text.replace('\r\n', ''), '原文件不是纯 CRLF'

    body = '\r\n'.join(LINES)
    text = text.rstrip('\r\n') + '\r\n\r\n' + body + '\r\n'

    with io.open(PATH, 'w', encoding='utf-8', newline='') as f:
        f.write(text)

    raw = io.open(PATH, 'rb').read()
    assert raw.count(b'\r\n') == raw.count(b'\n'), '行尾被破坏'
    assert b'\t' not in raw, '含 tab'
    raw.decode('utf-8')
    print('CRLF=%d LF=%d 批次 I 已追加' % (raw.count(b'\r\n'), raw.count(b'\n')))


if __name__ == '__main__':
    main()
