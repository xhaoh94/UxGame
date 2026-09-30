"""把 Dummy 下所有角色 FBX 的导入类型改成 Generic —— 与项目播放链路一致。

为什么：项目用 AnimationClipPlayable + AnimationMixerPlayable 直接驱动 Animator（无 AnimatorController），
Generic 剪辑的曲线按骨骼路径直接写入层级，这条路径可用；Humanoid 剪辑是 muscle 空间，
需要 Mecanim 的 retargeting 管线才能真正落到骨骼上，裸 Playables 不做这一步 → 动画必然错乱。
旧的 Hero_ZS 资源（能正常播）就是 animationType: 2 (Generic) + 模型 avatarSetup: 0。
"""
import glob
import os
import re
import shutil
import sys

DUMMY = r"C:\Project\UxGame\Unity\Assets\Data\Art\Model\Unit\Dummy"
BACKUP = r"C:\Project\UxGame\.workbuddy\tmp\generic-backup"


def is_character(stem):
    return "@" in stem or "_Model" in stem or "dummy" in stem.lower()


def main():
    targets = []
    for meta in glob.glob(os.path.join(DUMMY, "**", "*.fbx.meta"), recursive=True):
        stem = os.path.basename(meta)[:-len(".fbx.meta")]
        if is_character(stem):
            targets.append(meta)
    print(f"[扫描] 角色 FBX {len(targets)} 个")

    os.makedirs(BACKUP, exist_ok=True)
    for meta in targets:
        dst = os.path.join(BACKUP, os.path.relpath(meta, DUMMY).replace(os.sep, "__"))
        if not os.path.exists(dst):
            shutil.copy2(meta, dst)
    print(f"[备份] -> {BACKUP}")

    changed = 0
    for meta in targets:
        text = open(meta, encoding="utf-8").read()
        before = text
        text = re.sub(r"animationType: \d+", "animationType: 2", text)
        text = re.sub(r"avatarSetup: \d+", "avatarSetup: 0", text)
        if text != before:
            open(meta, "w", encoding="utf-8", newline="").write(text)
            changed += 1
    print(f"[写入] 改为 Generic 的: {changed} 个")

    # 校验
    bad = []
    for meta in targets:
        text = open(meta, encoding="utf-8").read()
        if not re.search(r"animationType: 2\b", text):
            bad.append(os.path.basename(meta))
    print(f"[校验] 仍是非 Generic 的: {len(bad)}")
    for name in bad[:10]:
        print("   ", name)
    return 0


if __name__ == "__main__":
    sys.exit(main())
