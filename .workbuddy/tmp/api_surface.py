"""对比改动前后每个文件的对外成员名集合，确认没有成员被误删/改名。"""
import io
import os
import re

BK = r'C:\Project\UxGame\.workbuddy\tmp\backup_autoprops'
TARGETS = [
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Unit\AttributeSet.cs',
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Core\CombatStageBuffers.cs',
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Unit\CombatActionRunner.cs',
    r'C:\Project\UxGame\Unity\Assets\Hotfix\Common\Combat\CombatComponent.cs',
    r'C:\Project\UxGame\Unity\Assets\HotfixBase\Manager\Combat\Runtime\Unit\CombatController.cs',
]

# 取"可见性修饰符 + 中缀 + 成员名 + 跟在后面的 => / { / ( / ; / [" 的成员名
MEM = re.compile(r'\b(?:public|protected|internal)\s+(?:static\s+|readonly\s+|sealed\s+|override\s+|abstract\s+|new\s+|partial\s+|const\s+)*'
                 r'[A-Za-z0-9_<>,\.\[\]\? ]*?\s([A-Za-z0-9_]+|this)\s*(?:=>|\{|\(|;)')


def read(path):
    with io.open(path, 'r', encoding='utf-8', newline='') as fh:
        return fh.read().replace('\r\n', '\n')


for cur in TARGETS:
    name = os.path.basename(cur)
    old = read(os.path.join(BK, name))
    new = read(cur)
    so, sn = set(MEM.findall(old)), set(MEM.findall(new))
    gone = sorted(so - sn)
    added = sorted(sn - so)
    status = 'OK' if not gone and not added else 'DIFF'
    print('%-26s %s  消失=%s  新增=%s' % (name, status, gone or '-', added or '-'))
