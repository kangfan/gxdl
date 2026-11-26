# 从商城图集重裁 currency / footer-bar，只擦数字与文字（保留图标与装饰符）。
from PIL import Image, ImageFilter

atlas = Image.open("ResourceCache/shop-20261125/raw/atlas.png").convert("RGBA")

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
    return (min(xs), min(ys), max(xs) + 1, max(ys) + 1) if xs else None

def inpaint(im, b, pad=3):
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
    im.paste(region.filter(ImageFilter.GaussianBlur(1.5)), (max(0, x0 - 2), max(0, y0 - 2)))

# currency：图标在左 (~x8-42)，"380" 与 "+" 在 x48 之后
cur = atlas.crop((498, 198, 628, 252))
b = text_bbox(cur, (48, 8, 126, 46), "bright", 165)
print("currency bbox", b, cur.size)
if b: inpaint(cur, b)
cur.save("ResourceCache/shop-20261125/processed/currency.png")

# footer-bar：装饰符在 x75 / x374 附近，只擦中间文字
foot = atlas.crop((22, 1078, 438, 1132))
b = text_bbox(foot, (95, 8, 355, 46), "bright", 150)
print("footer bbox", b, foot.size)
if b: inpaint(foot, b)
foot.save("ResourceCache/shop-20261125/processed/footer-bar.png")
print("done")
