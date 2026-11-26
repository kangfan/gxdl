from PIL import Image, ImageFilter, ImageDraw

RAW = "ResourceCache/bag-20261125/raw"
OUT = "ResourceCache/bag-20261125/processed"

bag = Image.open(f"{RAW}/bag.png").convert("RGB")
det = Image.open(f"{RAW}/detail.png").convert("RGB")


def sample_text_color(im, rect, light=False):
    region = im.crop(rect)
    px = list(region.getdata())
    px.sort(key=lambda c: (c[0] + c[1] + c[2]) * (-1 if light else 1))
    pick = px[: max(1, len(px) // 50)]
    n = len(pick)
    return "#%02X%02X%02X" % tuple(sum(c[k] for c in pick) // n for k in range(3))


def inpaint(im, rect, pad=3, blur=3.0):
    x0, y0, x1, y1 = rect
    src = im.load()

    def avg_rows(xx, rows):
        cs = [src[xx, yy] for yy in rows]
        return tuple(sum(c[k] for c in cs) // len(cs) for k in range(3))

    for x in range(x0, x1):
        tr = range(max(0, y0 - pad - 3), max(0, y0 - pad))
        br = range(min(im.height - 1, y1 + pad), min(im.height - 1, y1 + pad + 3))
        top = avg_rows(x, tr) if tr else src[x, max(0, y0 - 1)]
        bot = avg_rows(x, br) if br else src[x, min(im.height - 1, y1)]
        for y in range(y0, y1):
            t = (y - y0) / (y1 - y0)
            src[x, y] = tuple(int(top[k] * (1 - t) + bot[k] * t) for k in range(3))
    box = (max(0, x0 - 4), max(0, y0 - 4), min(im.width, x1 + 4), min(im.height, y1 + 4))
    im.paste(im.crop(box).filter(ImageFilter.GaussianBlur(blur)), box)


def fill_inside(im, rect, blur=5, noise=2):
    x0, y0, x1, y1 = rect
    src = im.load()

    def row_avg(yy):
        cs = [src[x, yy] for x in range(x0, x1)]
        return tuple(sum(c[k] for c in cs) // len(cs) for k in range(3))

    top = row_avg(min(im.height - 1, y0 + 1))
    bot = row_avg(max(0, y1 - 1))
    for y in range(y0, y1):
        t = (y - y0) / (y1 - y0)
        base = tuple(int(top[k] * (1 - t) + bot[k] * t) for k in range(3))
        for x in range(x0, x1):
            d = noise if (x + y) % 2 == 0 else -noise
            src[x, y] = tuple(max(0, min(255, base[k] + d)) for k in range(3))
    box = (max(0, x0 - 4), max(0, y0 - 4), min(im.width, x1 + 4), min(im.height, y1 + 4))
    im.paste(im.crop(box).filter(ImageFilter.GaussianBlur(blur)), box)


def flat_fill(im, rect, sample_rect, blur=1.5):
    """Fill rect with the average color of sample_rect (pill interior), plus light blur."""
    region = im.crop(sample_rect)
    px = list(region.getdata())
    n = len(px)
    base = tuple(sum(c[k] for c in px) // n for k in range(3))
    x0, y0, x1, y1 = rect
    src = im.load()
    for y in range(y0, y1):
        for x in range(x0, x1):
            d = 2 if (x + y) % 2 == 0 else -2
            src[x, y] = tuple(max(0, min(255, base[k] + d)) for k in range(3))
    box = (x0 - 2, y0 - 2, x1 + 2, y1 + 2)
    im.paste(im.crop(box).filter(ImageFilter.GaussianBlur(blur)), box)


def circle_alpha(im):
    im = im.convert("RGBA")
    mask = Image.new("L", im.size, 0)
    d = ImageDraw.Draw(mask)
    w, h = im.size
    r = min(w, h) / 2
    d.ellipse((w / 2 - r + 1, h / 2 - r + 1, w / 2 + r - 1, h / 2 + r - 1), fill=255)
    im.putalpha(mask)
    return im


def save(im, name, max_side):
    w, h = im.size
    s = min(1.0, max_side / max(w, h))
    if s < 1.0:
        im = im.resize((round(w * s), round(h * s)), Image.LANCZOS)
    im.save(f"{OUT}/{name}.png")
    print(name, im.size)


colors = {
    "bagTitle": sample_text_color(bag, (270, 520, 425, 635)),
    "header": sample_text_color(bag, (627, 697, 817, 737)),
    "slotLabel": sample_text_color(bag, (210, 1015, 380, 1065)),
    "footer": sample_text_color(bag, (350, 1848, 1290, 1900)),
    "badge": sample_text_color(bag, (322, 797, 385, 827), light=True),
    "detName": sample_text_color(det, (560, 705, 650, 740)),
    "detSub": sample_text_color(det, (560, 765, 705, 790)),
    "detHeader": sample_text_color(det, (530, 975, 640, 1002)),
    "detStat": sample_text_color(det, (437, 1025, 597, 1057)),
    "detDesc": sample_text_color(det, (382, 1185, 770, 1247)),
    "btnGold": sample_text_color(det, (420, 1365, 500, 1405)),
    "btnTeal": sample_text_color(det, (655, 1365, 730, 1405)),
}
for k, v in colors.items():
    print("color", k, v)

# ---- bag slices ----
pieces = bag.copy()
fill_inside(pieces, (190, 785, 400, 995))
save(pieces.crop((170, 765, 418, 1012)), "bag-slot", 128)

badge = bag.copy()
flat_fill(badge, (322, 796, 390, 830), (308, 800, 318, 826))
save(badge.crop((306, 784, 406, 838)), "bag-badge", 128)

inpaint(pieces, (255, 510, 455, 650))
save(pieces.crop((185, 500, 650, 658)), "bag-title", 256)
save(circle_alpha(pieces.crop((1222, 522, 1356, 656))), "bag-close", 128)
inpaint(pieces, (620, 692, 825, 742))
save(pieces.crop((505, 670, 985, 760)), "bag-header", 256)
save(pieces.crop((580, 1772, 860, 1836)), "bag-divider", 256)

panel = bag.copy()
inpaint(panel, (255, 510, 455, 650))               # 背包标题烘进面板，文字实时
fill_inside(panel, (120, 668, 1330, 1895), blur=6)
save(panel.crop((85, 642, 1362, 1952)), "bag-panel", 1024)

# ---- detail slices (detail.png 1125x2000) ----
icon_src = det.copy()
fill_inside(icon_src, (342, 664, 520, 822))
save(icon_src.crop((325, 648, 538, 838)), "gear-detail-iconbox", 256)

hdr = det.copy()
inpaint(hdr, (510, 972, 665, 1005))
save(hdr.crop((455, 965, 700, 1012)), "gear-detail-header", 256)

save(circle_alpha(det.copy().crop((778, 580, 870, 672))), "gear-detail-close", 128)

btn = det.copy()
inpaint(btn, (415, 1360, 505, 1410))
save(btn.crop((352, 1345, 568, 1425)), "gear-btn-gold", 256)
inpaint(btn, (648, 1360, 738, 1410))
save(btn.crop((590, 1345, 800, 1425)), "gear-btn-teal", 256)

dp = det.copy()
# 用原图干净纸带竖向拉伸填充内部，保留纸纹底色，避免与切片背景出现色差方块。
band = det.crop((340, 795, 812, 842)).resize((480, 730), Image.LANCZOS)
dp.paste(band, (335, 615))
seam = dp.crop((330, 608, 820, 625)).filter(ImageFilter.GaussianBlur(4))
dp.paste(seam, (330, 608))
seam = dp.crop((330, 1338, 820, 1355)).filter(ImageFilter.GaussianBlur(4))
dp.paste(seam, (330, 1338))
seam = dp.crop((328, 610, 345, 1350)).filter(ImageFilter.GaussianBlur(4))
dp.paste(seam, (328, 610))
seam = dp.crop((808, 610, 825, 1350)).filter(ImageFilter.GaussianBlur(4))
dp.paste(seam, (808, 610))
save(dp.crop((300, 576, 845, 1458)), "gear-detail-panel", 1024)
print("done")
