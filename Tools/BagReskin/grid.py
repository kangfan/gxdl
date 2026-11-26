import glob
import shutil
from PIL import Image, ImageDraw

RAW = "ResourceCache/bag-20261125/raw"
OUT = "ResourceCache/bag-20261125/processed"

import os
os.makedirs(RAW, exist_ok=True)
os.makedirs(OUT, exist_ok=True)

shutil.copyfile("Art/背包/效果图.png", f"{RAW}/bag.png")
src = glob.glob(r"C:/Users/admin/AppData/Local/Temp/claude/**/images/1.webp", recursive=True)
if src:
    Image.open(src[0]).convert("RGB").save(f"{RAW}/detail.png")
    print("detail copied from", src[0])

for name in ["bag", "detail"]:
    im = Image.open(f"{RAW}/{name}.png").convert("RGB")
    print(name, im.size)
    d = ImageDraw.Draw(im)
    step = 100
    for x in range(0, im.width, 50):
        major = x % step == 0
        d.line([(x, 0), (x, im.height)], fill=(255, 0, 0) if major else (255, 170, 170), width=2 if major else 1)
        if major:
            d.text((x + 2, 2), str(x), fill=(255, 0, 0))
    for y in range(0, im.height, 50):
        major = y % step == 0
        d.line([(0, y), (im.width, y)], fill=(255, 0, 0) if major else (255, 170, 170), width=2 if major else 1)
        if major:
            d.text((2, y + 2), str(y), fill=(255, 0, 0))
    im.save(f"{OUT}/_grid-{name}.png")
print("done")
