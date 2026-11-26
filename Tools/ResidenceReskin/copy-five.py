import os
import shutil

MAP = {"商城": "shop", "图鉴": "codex", "修行": "growth", "寻虚": "ruins", "战备": "prep"}
for cn, en in MAP.items():
    src = os.path.join("Art", cn)
    dst = os.path.join("ResourceCache", f"{en}-20261125", "raw")
    os.makedirs(dst, exist_ok=True)
    for f in os.listdir(src):
        fp = os.path.join(src, f)
        if not os.path.isfile(fp):
            continue
        lower = f.lower()
        if lower.endswith((".png", ".webp", ".jpg")):
            if "效果" in f:
                alias = "mockup.png"
            elif "切图" in f:
                alias = "atlas.png"
            elif "待替换" in f or (len(os.path.splitext(f)[0]) == 36 and f[8] == "-"):
                alias = "current-page.png"
            else:
                alias = f
            shutil.copyfile(fp, os.path.join(dst, alias))
            print(en, "<-", f, "->", alias)
