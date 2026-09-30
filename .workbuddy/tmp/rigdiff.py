"""比对模型与动画 FBX 的骨骼层级（读 meta 里的 skeleton 段）。"""
import os
import re

DUMMY = r"C:\Project\UxGame\Unity\Assets\Data\Art\Model\Unit\Dummy"


def hierarchy(meta_path):
    text = open(meta_path, encoding="utf-8").read()
    if "skeleton:" not in text:
        return {}
    seg = text.split("skeleton:")[1].split("human:")[0]
    entries = {}
    current = None
    for line in seg.splitlines():
        m = re.match(r"^    - name: (.*)$", line)
        if m:
            current = m.group(1).strip()
            entries[current] = None
            continue
        m = re.match(r"^      parentName: (.*)$", line)
        if m and current:
            entries[current] = m.group(1).strip()
    return entries


model = hierarchy(os.path.join(DUMMY, r"Human\Meshes\HumanM_Model.fbx.meta"))
clip = hierarchy(os.path.join(DUMMY, r"BasicMotions\Animations\Male\Idles\HumanM@Idle01.fbx.meta"))
clip_run = hierarchy(os.path.join(DUMMY, r"BasicMotions\Animations\Male\Movement\Run\HumanM@Run01_Forward.fbx.meta"))

print(f"模型骨骼 {len(model)} 个 / Idle 剪辑 {len(clip)} 个 / Run 剪辑 {len(clip_run)} 个")
print()
others = sorted(set(model) - set(clip))
print("只在模型里有:", others)
missing = sorted(set(clip) - set(model))
print("只在剪辑里有:", missing)
print()

diff = []
for bone in sorted(set(model) & set(clip)):
    if model[bone] != clip[bone]:
        diff.append((bone, model[bone], clip[bone]))
print(f"父子关系不一致的骨骼: {len(diff)} 个")
for bone, a, b in diff:
    print(f"   {bone}: 模型父={a}   剪辑父={b}")

print()
print("模型里的 B- 骨骼链（前 20）:")
for bone in sorted(k for k in model if k.startswith("B-")):
    print(f"   {bone:22} <- {model[bone]}")
