# -*- coding: utf-8 -*-
"""本轮改动的 C# 文件静态自检：剥注释后的括号平衡、UTF-8、tab、行尾一致性。

沙箱编不了 C#（见项目记忆），所以这里只做"结构层"兜底：
括号/花括号在剥掉注释与字符串字面量后必须配平；行尾必须单一不混用。
"""
import io
import os
import re
import sys

ROOT = r'C:\Project\UxGame\Unity\Assets'

FILES = [
    r'HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs',
    r'HotfixBase\Manager\Combat\Runtime\Presentation\CombatTimelinePlayer.cs',
    r'HotfixBase\Manager\Timeline\Asset\Particle\ParticleClipAsset.cs',
    r'HotfixBase\Manager\Timeline\Runtime\Particle\TLParticleClip.cs',
    r'Hotfix\Common\Combat\CombatComponent.cs',
    r'Hotfix\Common\Combat\CombatVfxHost.cs',
    r'Hotfix\Common\Operate\OperateComponent.cs',
    r'Editor\Timeline\TimelineAssetInspectors.cs',
    r'Editor\Timeline\TimelineAssetEditorSource.cs',
    r'Editor\Timeline\TimelineWindow.cs',
    r'Editor\Combat\Tests\CombatRuntimeTests.cs',
]


def strip_code(text):
    """去掉块注释、行注释、逐字字符串与非逐字字符串字面量（含 $ 前缀）。"""
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    text = re.sub(r'//[^\n]*', '', text)
    text = re.sub(r'@"(?:[^"]|"")*"', '""', text)
    text = re.sub(r'\$?"(?:\\.|[^"\\])*"', '""', text)
    text = re.sub(r"'(?:\\.|[^'\\])'", "''", text)
    return text


def check_line(path):
    raw = io.open(path, 'rb').read()
    lf = raw.count(b'\n')
    crlf = raw.count(b'\r\n')
    assert crlf in (0, lf), '行尾混用: CRLF=%d LF=%d' % (crlf, lf)
    assert lf == 0 or crlf == lf or crlf == 0, '行尾怪异'
    assert b'\t' not in raw, '含 tab'
    text = raw.decode('utf-8')
    code = strip_code(text)
    o, c = code.count('{'), code.count('}')
    po, pc = code.count('('), code.count(')')
    ok = o == c and po == pc
    eol = 'CRLF' if crlf else 'LF'
    print('  %s  %-6s braces %d/%d  parens %d/%d  %s'
          % ('OK ' if ok else 'BAD', eol, o, c, po, pc,
             os.path.basename(path)))
    return ok


def main():
    bad = 0
    print('=== C# 静态自检（剥注释/字符串后）===')
    for rel in FILES:
        path = os.path.join(ROOT, rel)
        if not os.path.exists(path):
            print('  MISSING  %s' % rel)
            bad += 1
            continue
        try:
            if not check_line(path):
                bad += 1
        except AssertionError as e:
            print('  FAIL  %s -> %s' % (rel, e))
            bad += 1
    print('=== %s ===' % ('ALL OK' if bad == 0 else '%d 个文件有问题' % bad))
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
