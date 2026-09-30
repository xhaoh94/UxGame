import io
import re
import sys
from pathlib import Path

FILES = [
    'Unity/Assets/HotfixBase/Manager/Combat/Asset/CombatActionAsset.cs',
    'Unity/Assets/HotfixBase/Manager/Combat/Asset/CharacterCombatProfile.cs',
    'Unity/Assets/HotfixBase/Manager/Combat/Runtime/Unit/CombatActionRunner.cs',
    'Unity/Assets/HotfixBase/Manager/Combat/Runtime/Core/CombatStageBuffers.cs',
    'Unity/Assets/HotfixBase/Manager/Combat/Runtime/Systems/CombatTimelineSystem.cs',
    'Unity/Assets/Editor/Combat/CombatEditorUtility.cs',
    'Unity/Assets/Editor/Combat/Timeline/CombatLogicTimelineSource.cs',
    'Unity/Assets/Editor/Combat/Timeline/CombatTransitionWindowEditorAdapters.cs',
    'Unity/Assets/Editor/Combat/Timeline/CombatTransitionWindowInspectors.cs',
    'Unity/Assets/Editor/Combat/Tests/CombatLogicTimelineSourceTests.cs',
]

def strip_code(text):
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    text = re.sub(r'//[^\n]*', '', text)
    text = re.sub(r'@"(?:[^"]|"")*"', '""', text)
    text = re.sub(r'\$?"(?:\\.|[^"\\])*"', '""', text)
    text = re.sub(r"'(?:\\.|[^'\\])'", "''", text)
    return text

for path in FILES:
    raw = Path(path).read_bytes()
    text = raw.decode('utf-8-sig')
    code = strip_code(text)
    if code.count('{') != code.count('}') or code.count('(') != code.count(')'):
        raise SystemExit(f'unbalanced: {path}')
print('transition split stripped delimiter check: OK')
