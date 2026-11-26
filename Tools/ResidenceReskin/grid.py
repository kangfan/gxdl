import glob
import shutil
from PIL import Image, ImageDraw

RAW = "ResourceCache/residence-20261125/raw"
OUT = "ResourceCache/residence-20261125/processed"

files = sorted(glob.glob("Art/*/ *.png")) + sorted(glob.glob("Art/*/*.png"))
copies = {}
for f in files:
    if "效果图" in f and "弹窗" in f:
        copies[f] = "popup.png"
    elif "效果图" in f:
        copies[f] = "dongfu.png"
for src, dst in copies.items():
    shutil.copyfile(src, f"{RAW}/{dst}")
    print("copied", dst, "<-", repr(src))

for name in ["dongfu", "popup"]:
    im = Image.open(f"{RAW}/{name}.png").convert("RGB")
    print(name, im.size)
    d = ImageDraw.Draw(im)
    for x in range(0, im.width, 50):
        major = x % 100 == 0
        d.line([(x, 0), (x, im.height)], fill=(255, 0, 0) if major else (255, 170, 170), width=2 if major else 1)
        if major:
            d.text((x + 2, 2), str(x), fill=(255, 0, 0))
    for y in range(0, im.height, 50):
        major = y % 100 == 0
        d.line([(0, y), (im.width, y)], fill=(255, 0, 0) if major else (255, 170, 170), width=2 if major else 1)
        if major:
            d.text((2, y + 2), str(y), fill=(255, 0, 0))
    im.save(f"{OUT}/_grid-{name}.png")
print("done")
