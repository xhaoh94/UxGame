"""按"类型优先"重排 GameRes 目录：UI → Res/UI/<包>/{Prefab,Atlas}，场景 → Res/Scene。

用法：
    python .workbuddy/migrate_layout.py           # dry-run，只打印
    python .workbuddy/migrate_layout.py --apply   # 真正执行

前置条件：Unity 必须关闭（Temp/UnityLockfile 不存在）。
"""
import os
import shutil
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Unity"))
ASSETS = os.path.join(ROOT, "Assets")
LOCK = os.path.join(ROOT, "Temp", "UnityLockfile")

# (源目录, 目标目录)，目录和它同级的 .meta 一起搬
MOVES = [
    ("GameRes/Res/Prefab/UI/Common", "GameRes/Res/UI/Common/Prefab"),
    ("GameRes/Res/Prefab/UI/Login", "GameRes/Res/UI/Login/Prefab"),
    ("GameRes/Res/Prefab/UI/Main", "GameRes/Res/UI/Main/Prefab"),
    ("GameRes/Res/Prefab/UI/Tip", "GameRes/Res/UI/Tip/Prefab"),
    ("GameRes/Res/Prefab/UI/Test", "GameRes/Res/UI/Test/Prefab"),
    ("GameRes/Res/Prefab/UI/Bag", "GameRes/Res/UI/Bag/Prefab"),
    ("GameRes/Res/Prefab/UI/Multiple", "GameRes/Res/UI/Multiple/Prefab"),
    ("GameRes/Res/Prefab/Common", "GameRes/Res/Scene/Common/Prefab"),
    ("GameRes/Res/Prefab/Unit/Role", "GameRes/Res/Scene/Common/Unit/Role"),
    ("GameRes/Res/Prefab/Map/Map001", "GameRes/Res/Scene/Map001/Prefab"),
    # 图集源图必须和 .spriteatlas 同目录（骨架已存在后执行），整目录搬以保留文件夹 GUID
    ("GameRes/Art/Icons/@\u8d27\u5e01", "GameRes/Res/UI/Common/Atlas"),
]

# 单文件搬迁（文件 + .meta）
FILE_MOVES = [
    ("GameRes/Res/Atlas/items.spriteatlasv2", "GameRes/Res/UI/Common/Atlas/items.spriteatlasv2"),
]

# 搬完后清理空目录（自底向上），连同 .meta
CLEAN_ROOTS = ["GameRes/Res/Prefab", "GameRes/Res/Atlas", "GameRes/Art/Icons"]

COLLECTOR_ASSET = os.path.join(ASSETS, "Settings/YooAsset/AssetBundleCollectorSetting.asset")

UI_PACKS = ["Bag", "Multiple"]


def p(rel):
    return os.path.join(ASSETS, rel.replace("/", os.sep))


def unity_running():
    return os.path.exists(LOCK)


def move_dir(src_rel, dst_rel, dry):
    src, dst = p(src_rel), p(dst_rel)
    if not os.path.isdir(src):
        if os.path.isdir(dst):
            print(f"  skip  {src_rel}（已搬过）")
            return
        raise SystemExit(f"源目录不存在：{src_rel}")
    if os.path.exists(dst):
        raise SystemExit(f"目标已存在，先人工确认：{dst_rel}")
    print(f"  dir   {src_rel}  ->  {dst_rel}")
    if dry:
        return
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.move(src, dst)
    src_meta, dst_meta = src + ".meta", dst + ".meta"
    if os.path.exists(src_meta):
        shutil.move(src_meta, dst_meta)


def move_file(src_rel, dst_rel, dry):
    src, dst = p(src_rel), p(dst_rel)
    if not os.path.isfile(src):
        if os.path.isfile(dst):
            print(f"  skip  {src_rel}（已搬过）")
            return
        raise SystemExit(f"源文件不存在：{src_rel}")
    print(f"  file  {src_rel}  ->  {dst_rel}")
    if dry:
        return
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.move(src, dst)
    if os.path.exists(src + ".meta"):
        shutil.move(src + ".meta", dst + ".meta")


def clean_empty(root_rel, dry):
    root = p(root_rel)
    if not os.path.isdir(root):
        return
    for dirpath, dirnames, filenames in os.walk(root, topdown=False):
        if dirnames or filenames:
            continue
        rel = os.path.relpath(dirpath, ASSETS).replace(os.sep, "/")
        print(f"  rm    {rel}/（空）")
        if not dry:
            os.rmdir(dirpath)
            meta = dirpath + ".meta"
            if os.path.exists(meta):
                os.remove(meta)


# ---------------------------------------------------------------- 收集器配置

def parse_collector_asset(text):
    """返回 (preamble_lines, [group])，group = {header: [], collectors: [[line]]}。"""
    lines = text.split("\n")
    preamble, groups, group, collector = [], [], None, None
    for line in lines:
        if line.startswith("    - GroupName: "):
            group = {"header": [line], "collectors": []}
            groups.append(group)
            collector = None
        elif group is None:
            preamble.append(line)
        elif line.startswith("      - CollectPath: "):
            collector = [line]
            group["collectors"].append(collector)
        elif collector is not None and line.startswith("        "):
            collector.append(line)
        else:
            collector = None
            group["header"].append(line)
    return preamble, groups


def collector_field(block, key):
    for line in block:
        s = line.strip()
        if s.startswith("- "):
            s = s[2:]
        if s.startswith(key + ":"):
            return s[len(key) + 1:].strip()
    return None


def set_field(block, key, value):
    for i, line in enumerate(block):
        s = line.strip()
        has_dash = s.startswith("- ")
        if has_dash:
            s = s[2:]
        if s.startswith(key + ":"):
            indent = line[:len(line) - len(line.lstrip())]
            dash = "- " if has_dash else ""
            block[i] = f"{indent}{dash}{key}: {value}"
            return
    block.append(f"        {key}: {value}")


def serialize(preamble, groups):
    out = list(preamble)
    for g in groups:
        # 文件末尾的空行会被解析进最后一组的 header，必须丢掉，否则会被挪到 collectors 前面
        out.extend([x for x in g["header"] if x != ""])
        for c in g["collectors"]:
            out.extend([x for x in c if x != ""])
    text = "\n".join(out)
    while "\n\n\n" in text:
        text = text.replace("\n\n\n", "\n\n")
    if not text.endswith("\n"):
        text += "\n"
    return text


def patch_collector_asset(dry):
    with open(COLLECTOR_ASSET, "r", encoding="utf-8", newline="") as f:
        text = f.read()
    if "\r\n" in text:
        raise SystemExit("收集器配置不是 LF，先人工确认行尾")

    preamble, groups = parse_collector_asset(text)
    by_name = {g["header"][0].split(":", 1)[1].strip(): g for g in groups}

    # 1) UI 组：路径前移到 Res/UI，内置/预加载改成按目录拆包
    ui = by_name.get("UI")
    if ui is None:
        raise SystemExit("找不到 UI 收集器组")
    for block in ui["collectors"]:
        path = collector_field(block, "CollectPath")
        new_path = path.replace("Assets/GameRes/Res/Prefab/UI", "Assets/GameRes/Res/UI")
        set_field(block, "CollectPath", new_path)
        if collector_field(block, "FilterRuleName") in ("CollectBuiltinUI", "CollectPreloadUI"):
            set_field(block, "PackRuleName", "PackDirectory")

    # 2) 图集组：图集并入 Res/UI/Common/Atlas，由 UI 组的 CollectBuiltinUI 收集，整组删除
    if "Atlas" in by_name:
        groups.remove(by_name["Atlas"])

    # 3) Prefab 组：改成"场景"组（保留组名以维持 Prefab_ 地址前缀）
    prefab = by_name.get("Prefab")
    if prefab is None:
        raise SystemExit("找不到 Prefab 收集器组")
    guids = [collector_field(b, "CollectorGUID") for b in prefab["collectors"]]
    prefab["collectors"] = []

    def scene_collector(path, tag, guid):
        return [
            f"      - CollectPath: {path}",
            f"        CollectorGUID: {guid}",
            "        CollectorType: 0",
            "        AddressRuleName: AddressByGroupAndFileName",
            "        PackRuleName: PackDirectory",
            "        FilterRuleName: CollectAll",
            f"        AssetTags: {tag}",
            "        UserData: ",
        ]

    prefab["collectors"].append(
        scene_collector("Assets/GameRes/Res/Scene/Common", "builtin", guids[0]))
    prefab["collectors"].append(
        scene_collector("Assets/GameRes/Res/Scene/Map001", "", guids[1]))

    new_text = serialize(preamble, groups)
    if new_text == text:
        print("  收集器配置无需修改")
        return
    print("  yaml  Settings/YooAsset/AssetBundleCollectorSetting.asset")
    if not dry:
        with open(COLLECTOR_ASSET, "w", encoding="utf-8", newline="") as f:
            f.write(new_text)


def main():
    apply = "--apply" in sys.argv
    dry = not apply
    print("== 目录重排 ==", "(dry-run)" if dry else "(apply)")
    if apply and unity_running():
        raise SystemExit("Unity 正在运行（Temp/UnityLockfile 存在），先关闭 Unity 再执行。")

    for src, dst in MOVES:
        move_dir(src, dst, dry)
    for src, dst in FILE_MOVES:
        move_file(src, dst, dry)
    for root in CLEAN_ROOTS:
        clean_empty(root, dry)

    print("== 收集器配置 ==")
    patch_collector_asset(dry)
    print("完成。" if not dry else "以上为 dry-run，未改动任何文件。")


if __name__ == "__main__":
    main()
