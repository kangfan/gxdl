# 擦除商城成品切片上残留的烘焙文字：检测文字像素 bbox 后做垂直渐变修补。
# 用法: python erase-shop-text.py <processed_dir>  (处理 buy-gold/buy-blue/currency/footer-bar/subtitle-bar)
import sys, os
from PIL import Image, ImageFilter

D = sys.argv[1] if len(sys.argv) > 1 else "."

def text_bbox(im, box, mode, thresh):
    x0, y0, x1, y1 = box
    px = im.load()
    xs, ys = [], []
    for y in range(y0, y1):
        for x in range(x0, x1):
            p = px[x, y]
            lum = (p[0] + p[1] + p[2]) / 3
            if (mode == "dark" and lum < thresh) or (mode == "bright" and lum > thresh):
                xs.append(x); ys.append(y)
    if not xs:
        return None
    return (min(xs), min(ys), max(xs) + 1, max(ys) + 1)

def inpaint(im, b, pad=4):
    x0 = max(0, b[0] - pad); y0 = max(0, b[1] - pad)
    x1 = min(im.width, b[2] + pad); y1 = min(im.height, b[3] + pad)
    px = im.load()
    top = max(0, y0 - 3); bot = min(im.height - 1, y1 + 2)
    span = max(1, y1 - y0)
    for x in range(x0, x1):
        c0 = px[x, top]; c1 = px[x, bot]
        for y in range(y0, y1):
            k = (y - y0) / span
            p = px[x, y]
            px[x, y] = (int(c0[0] * (1 - k) + c1[0] * k), int(c0[1] * (1 - k) + c1[1] * k),
                        int(c0[2] * (1 - k) + c1[2] * k), p[3])
    region = im.crop((max(0, x0 - 2), max(0, y0 - 2), min(im.width, x1 + 2), min(im.height, y1 + 2)))
    region = region.filter(ImageFilter.GaussianBlur(1.5))
    im.paste(region, (max(0, x0 - 2), max(0, y0 - 2)))

jobs = [
    # (file, mode, thresh, search region)
    ("buy-gold.png", "dark", 120, None),
    ("buy-blue.png", "dark", 120, None),
    ("currency.png", "bright", 165, None),
    ("footer-bar.png", "bright", 150, None),
    ("subtitle-bar.png", "dark", 130, None),
]
for name, mode, thresh, region in jobs:
    path = os.path.join(D, name)
    if not os.path.exists(path):
        print("skip", name); continue
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    if region is None:
        # 中部区域，避开边框装饰
        region = (int(w * 0.12), int(h * 0.22), int(w * 0.9), int(h * 0.85))
    b = text_bbox(im, region, mode, thresh)
    print(name, im.size, "bbox=", b)
    if b:
        inpaint(im, b)
        im.save(path)
print("done")
