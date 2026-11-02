import os
import shutil
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import migrate_layout as m

src = m.COLLECTOR_ASSET
dst = os.path.join(os.path.dirname(__file__), "backup", "test_collector.asset")
shutil.copy2(src, dst)
m.COLLECTOR_ASSET = dst
m.patch_collector_asset(dry=False)

new = open(dst, encoding="utf-8", newline="").read()
print("=== 组/收集器概览 ===")
pre, groups = m.parse_collector_asset(new)
for g in groups:
    name = g["header"][0].split(":", 1)[1].strip()
    print(f"[{name}]")
    for c in g["collectors"]:
        print("   ", m.collector_field(c, "CollectPath"),
              "|", m.collector_field(c, "PackRuleName"),
              "|", m.collector_field(c, "FilterRuleName"),
              "| tag=", m.collector_field(c, "AssetTags") or "(none)")
