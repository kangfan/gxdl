import os
from PIL import Image

for d in ["商城", "图鉴", "修行", "寻虚", "战备"]:
    p = os.path.join("Art", d)
    print("==", d, "==")
    if not os.path.isdir(p):
        print("  (missing)")
        continue
    for f in sorted(os.listdir(p)):
        fp = os.path.join(p, f)
        if os.path.isfile(fp):
            size = os.path.getsize(fp)
            dim = ""
            if f.lower().endswith((".png", ".webp", ".jpg")):
                try:
                    im = Image.open(fp)
                    dim = f" {im.size[0]}x{im.size[1]}"
                except Exception as e:
                    dim = f" err:{e}"
            print(f"  {f}  {size}{dim}")
