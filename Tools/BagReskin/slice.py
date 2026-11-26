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


def fill_inside(im, rect, pad=3, blur=5, noise=2):
    """Uniform vertical gradient between row averages just inside rect edges."""
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


def circle_alpha(im):
    im = im.convert("RGBA")
    mask = Image.new("L", im.size, 0)
    d = ImageDraw.Draw(mask)
    w, h = im.size
    d.ellipse((w / 2 - min(w, h) / 2 + 1, h / 2 - min(w, h) / 2 + 1,
               w / 2 + min(w, h) / 2 - 1, h / 2 + min(w, h) / 2 - 1), fill=255)
    im.putalpha(mask)
    return im


def save(im, name, max_side):
    w, h = im.size
    s = min(1.0, max_side / max(w, h))
    if s < 1.0:
        im = im.resize((round(w * s), round(h * s)), Image.LANCZOS)
    im.save(f"{OUT}/{name}.png")
    print(name, im.size)


# ---- text colors (sample before erase) ----
colors = {
    "bagTitle": sample_text_color(bag, (270, 520, 425, 635)),
    "header": sample_text_color(bag, (627, 697, 817, 737)),
    "slotLabel": sample_text_color(bag, (210, 1015, 380, 1065)),
    "footer": sample_text_color(bag, (350, 1848, 1290, 1900)),
    "badge": sample_text_color(bag, (322, 797, 385, 827), light=True),
    "detName": sample_text_color(det, (624, 788, 726, 838)),
    "detSub": sample_text_color(det, (624, 855, 793, 894)),
    "detStat": sample_text_color(det, (500, 985, 600, 1029)),
    "detDesc": sample_text_color(det, (430, 1339, 850, 1423)),
    "btnGold": sample_text_color(det, (467, 1541, 563, 1589)),
    "btnTeal": sample_text_color(det, (698, 1541, 855, 1589)),
}
for k, v in colors.items():
    print("color", k, v)

# ---- bag pieces ----
pieces = bag.copy()
fill_inside(pieces, (190, 785, 400, 995))          # slot1 icon interior (before badge slice? badge outside this rect)
badge_src = pieces.crop((306, 784, 406, 838))
inpaint(badge_src, (12, 8, 84, 48))
save(badge_src, "bag-badge", 128)

inpaint(pieces, (265, 518, 430, 640))              # 背包
save(pieces.crop((185, 500, 650, 658)), "bag-title", 256)
save(circle_alpha(pieces.crop((1222, 522, 1356, 656))), "bag-close", 128)
inpaint(pieces, (620, 692, 825, 742))              # 当前装备
save(pieces.crop((505, 670, 985, 760)), "bag-header", 256)
save(pieces.crop((170, 765, 418, 1012)), "bag-slot", 128)
save(pieces.crop((580, 1772, 860, 1836)), "bag-divider", 256)

# ---- bag panel: fill whole interior (grid/header/footer all rebuilt live) ----
panel = bag.copy()
fill_inside(panel, (120, 710, 1330, 1895), blur=6)
save(panel.crop((85, 642, 1362, 1952)), "bag-panel", 1024)

# ---- detail pieces ----
dp = det.copy()
fill_inside(dp, (372, 735, 905, 1505), blur=6)     # main interior (iconbox/headers/texts erased)
inpaint(dp, (460, 1536, 570, 1594))                # 卸下
inpaint(dp, (690, 1536, 862, 1594))                # 强化
save(dp.crop((330, 692, 945, 1580)), "gear-detail-panel", 1024)
save(dp.crop((398, 730, 602, 932)), "gear-detail-iconbox", 256)
inpaint(det_copy := det.copy(), (578, 962, 735, 1004))
save(det_copy.crop((480, 952, 790, 1012)), "gear-detail-header", 256)
save(circle_alpha(det.copy().crop((866, 656, 980, 764))), "gear-detail-close", 128)
save(dp.crop((395, 1516, 638, 1610)), "gear-btn-gold", 256)
save(dp.crop((655, 1516, 898, 1610)), "gear-btn-teal", 256)
print("done")
