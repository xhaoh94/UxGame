"""把基准模型的有效人形骨骼映射写进受损坏 clip 的 .meta。

背景：CopyFromOther 那次运行把剪辑自身的 humanDescription 清空了，Unity 又只在
Rig 类型变化时才重建映射，所以这些文件一直靠"复制来的源 Avatar"顶着，重新导入即失效。
这里直接把基准模型的映射（boneName → humanName）写进 meta，Unity 会用它生成 Avatar。
"""
import glob
import os
import re
import shutil
import sys

DUMMY = r"C:\Project\UxGame\Unity\Assets\Data\Art\Model\Unit\Dummy"
BASE = os.path.join(DUMMY, r"Human\Meshes\HumanM_Model.fbx.meta")
BACKUP = r"C:\Project\UxGame\.workbuddy\tmp\rigfix-backup"

HD_START = "  humanDescription:"
HD_END = "  lastHumanDescriptionAvatarSource:"


def is_character(stem):
    return "@" in stem or "_Model" in stem or "dummy" in stem.lower()


def mapping_size(text):
    return len(re.findall(r"^    - boneName: ", text, re.M))


def build_block(base_path):
    """取基准模型的 humanDescription 块，并把 skeleton 裁成只有 B- 骨骼。"""
    text = open(base_path, encoding="utf-8").read()
    block = text[text.index(HD_START):text.index(HD_END)]

    lines = block.splitlines(keepends=True)
    out, i, dropped = [], 0, 0
    while i < len(lines):
        line = lines[i]
        if not line.startswith("    skeleton:"):
            out.append(line)
            i += 1
            continue

        out.append(line)
        i += 1
        while i < len(lines) and lines[i].startswith("    - name: "):
            entry = [lines[i]]
            i += 1
            while i < len(lines) and lines[i].startswith("      "):
                entry.append(lines[i])
                i += 1
            if "name: B-" in entry[0]:
                out.extend(entry)
            else:
                dropped += 1
    print(f"[基准] skeleton 条目保留 {sum(1 for l in out if l.startswith('    - name: '))} 个，丢弃非骨骼 {dropped} 个")
    return "".join(out)


def main():
    block = build_block(BASE)

    targets = []
    for meta in glob.glob(os.path.join(DUMMY, "**", "*.fbx.meta"), recursive=True):
        stem = os.path.basename(meta)[:-len(".fbx.meta")]
        if not is_character(stem):
            continue
        text = open(meta, encoding="utf-8").read()
        if mapping_size(text) == 0:
            targets.append(meta)

    print(f"[扫描] 待修复 {len(targets)} 个")
    if not targets:
        return 0

    os.makedirs(BACKUP, exist_ok=True)
    if not os.path.isdir(BACKUP):
        print(f"[错误] 备份目录创建失败: {BACKUP}")
        return 1

    for meta in targets:
        name = os.path.relpath(meta, DUMMY).replace(os.sep, "__")
        dst = os.path.join(BACKUP, name)
        if not os.path.exists(dst):
            shutil.copy2(meta, dst)
    print(f"[备份] {len(targets)} 个 meta -> {BACKUP}")

    fixed, broken = 0, []
    for meta in targets:
        text = open(meta, encoding="utf-8").read()
        if HD_START not in text or HD_END not in text:
            broken.append(meta)
            continue
        patched = text[:text.index(HD_START)] + block + text[text.index(HD_END):]
        patched = re.sub(r"animationType: \d+", "animationType: 3", patched)
        patched = re.sub(r"avatarSetup: \d+", "avatarSetup: 1", patched)
        open(meta, "w", encoding="utf-8", newline="").write(patched)
        fixed += 1

    print(f"[写入] {fixed} 个")
    if broken:
        print(f"[跳过] 结构异常 {len(broken)} 个")
        for path in broken[:10]:
            print("   ", path)

    # 抽查
    for sample in ["BasicMotions/Animations/Male/Idles/HumanM@Idle01.fbx.meta",
                   "Human/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx.meta"]:
        path = os.path.join(DUMMY, sample.replace("/", os.sep))
        if os.path.exists(path):
            text = open(path, encoding="utf-8").read()
            print(f"[抽查] {sample}: 映射={mapping_size(text)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
