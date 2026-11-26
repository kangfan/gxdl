from PIL import Image, ImageFilter

RAW = "ResourceCache/residence-20261125/raw"
OUT = "ResourceCache/residence-20261125/processed"

dongfu = Image.open(f"{RAW}/dongfu.png").convert("RGB")
popup = Image.open(f"{RAW}/popup.png").convert("RGB")


def sample_text_color(im, rect, dark=True):
    """Average of the darkest (or most saturated) text pixels inside rect."""
    region = im.crop(rect)
    px = list(region.getdata())
    px.sort(key=lambda c: c[0] + c[1] + c[2])
    pick = px[: max(1, len(px) // 50)]
    n = len(pick)
    return tuple(sum(c[k] for c in pick) // n for k in range(3))


def lightest_text_color(im, rect):
    region = im.crop(rect)
    px = list(region.getdata())
    px.sort(key=lambda c: -(c[0] + c[1] + c[2]))
    pick = px[: max(1, len(px) // 50)]
    n = len(pick)
    return tuple(sum(c[k] for c in pick) // n for k in range(3))


def inpaint(im, rect, pad=3, blur=3.0):
    """Fill rect by per-column vertical gradient between averaged rows outside it, then blur."""
    x0, y0, x1, y1 = rect
    src = im.load()
    h = y1 - y0

    def avg_rows(xx, rows):
        cs = [src[xx, yy] for yy in rows]
        n = len(cs)
        return tuple(sum(c[k] for c in cs) // n for k in range(3))

    for x in range(x0, x1):
        top_rows = range(max(0, y0 - pad - 3), max(0, y0 - pad))
        bot_rows = range(min(im.height - 1, y1 + pad), min(im.height - 1, y1 + pad + 3))
        top = avg_rows(x, top_rows) if top_rows else src[x, max(0, y0 - 1)]
        bot = avg_rows(x, bot_rows) if bot_rows else src[x, min(im.height - 1, y1)]
        for y in range(y0, y1):
            t = (y - y0) / h
            src[x, y] = tuple(int(top[k] * (1 - t) + bot[k] * t) for k in range(3))
    box = (max(0, x0 - 4), max(0, y0 - 4), min(im.width, x1 + 4), min(im.height, y1 + 4))
    region = im.crop(box).filter(ImageFilter.GaussianBlur(blur))
    im.paste(region, box)


def fill_gradient(im, rect, pad=3):
    """Large interior fill: uniform vertical gradient between row averages, then blur."""
    x0, y0, x1, y1 = rect
    src = im.load()

    def row_avg(yy):
        cs = [src[x, yy] for x in range(x0, x1)]
        n = len(cs)
        return tuple(sum(c[k] for c in cs) // n for k in range(3))

    top = row_avg(max(0, y0 - pad))
    bot = row_avg(min(im.height - 1, y1 + pad))
    h = y1 - y0
    noise = 2
    for y in range(y0, y1):
        t = (y - y0) / h
        base = tuple(int(top[k] * (1 - t) + bot[k] * t) for k in range(3))
        for x in range(x0, x1):
            d = noise if (x + y) % 2 == 0 else -noise
            src[x, y] = tuple(max(0, min(255, base[k] + d)) for k in range(3))
    box = (max(0, x0 - 4), max(0, y0 - 4), min(im.width, x1 + 4), min(im.height, y1 + 4))
    region = im.crop(box).filter(ImageFilter.GaussianBlur(5))
    im.paste(region, box)


def save(im, name, max_side):
    w, h = im.size
    scale = min(1.0, max_side / max(w, h))
    if scale < 1.0:
        im = im.resize((round(w * scale), round(h * scale)), Image.LANCZOS)
    im.save(f"{OUT}/{name}.png")
    print(name, im.size)


# ---- text colors (sample before erasing) ----
colors = {
    "title": sample_text_color(dongfu, (255, 100, 550, 155)),
    "storyTitle": sample_text_color(dongfu, (500, 220, 860, 278)),
    "storyBody": sample_text_color(dongfu, (510, 315, 710, 400)),
    "plaqueLabel": sample_text_color(dongfu, (200, 470, 350, 505)),
    "plaqueValue": sample_text_color(dongfu, (200, 510, 350, 558)),
    "btnTeal": lightest_text_color(dongfu, (300, 590, 500, 645)),
    "btnGold": sample_text_color(dongfu, (730, 585, 870, 645)),
    "popName": sample_text_color(popup, (340, 345, 700, 395)),
    "popPower": sample_text_color(popup, (340, 408, 670, 468)),
    "popStats": sample_text_color(popup, (350, 508, 670, 552)),
    "popAffix": sample_text_color(popup, (350, 556, 630, 598)),
    "popBtnTeal": lightest_text_color(popup, (810, 430, 940, 490)),
    "popBtnGold": sample_text_color(popup, (810, 545, 930, 600)),
}
for k, v in colors.items():
    print("color", k, "#%02X%02X%02X" % v)

# ---- dongfu pieces (from a copy so panel erase stays independent) ----
pieces = dongfu.copy()
# buttons: crop then erase text (keep left icon)
btn_rects = {
    "residence-btn-teal-1": ((150, 575, 545, 662), (272, 590, 524, 648)),
    "residence-btn-gold-1": ((592, 570, 900, 662), (655, 586, 890, 648)),
    "residence-btn-teal-2": ((150, 672, 545, 762), (272, 686, 524, 748)),
    "residence-btn-teal-3": ((588, 672, 900, 762), (633, 686, 882, 748)),
    "residence-btn-teal-4": ((150, 770, 545, 860), (272, 784, 524, 846)),
    "residence-btn-gold-2": ((592, 768, 900, 862), (665, 786, 790, 840)),
}
for name, (crop, erase) in btn_rects.items():
    b = pieces.crop(crop)
    e = (erase[0] - crop[0], erase[1] - crop[1], erase[2] - crop[0], erase[3] - crop[1])
    inpaint(b, e)
    save(b, name, 512)

save(pieces.crop((140, 195, 442, 448)), "residence-window", 256)
inpaint(pieces, (248, 460, 412, 566))
save(pieces.crop((142, 450, 492, 576)), "residence-plaque", 256)
save(pieces.crop((555, 278, 875, 310)), "residence-divider", 256)
save(pieces.crop((898, 92, 972, 162)), "residence-close", 128)

# ---- dongfu panel: erase interior + ribbon title, keep border/ribbon ----
panel = dongfu.copy()
inpaint(panel, (250, 98, 555, 158))          # ribbon title
fill_gradient(panel, (98, 172, 952, 928))    # whole interior
save(panel.crop((58, 76, 990, 948)), "residence-panel", 2048)

# ---- dongfu full-page background compose (750x1334) ----
BG_W, BG_H = 750, 1334
top_px = dongfu.getpixel((512, 45))
bot_px = dongfu.getpixel((512, 1000))
bg = Image.new("RGB", (BG_W, BG_H))
bp = bg.load()
for y in range(BG_H):
    t = y / (BG_H - 1)
    c = tuple(int(top_px[k] * (1 - t) + bot_px[k] * t) for k in range(3))
    for x in range(BG_W):
        bp[x, y] = c
# paste stretched clean sky from the top margin and bottom margin
top_strip = dongfu.crop((0, 0, 1024, 75)).resize((BG_W, 200), Image.LANCZOS)
bg.paste(top_strip.filter(ImageFilter.GaussianBlur(6)), (0, 0))
bot_strip = dongfu.crop((0, 880, 1024, 1024)).resize((BG_W, 240), Image.LANCZOS)
bg.paste(bot_strip.filter(ImageFilter.GaussianBlur(6)), (0, BG_H - 240))
# blend the seams
seam = bg.crop((0, 190, BG_W, 215)).filter(ImageFilter.GaussianBlur(8))
bg.paste(seam, (0, 190))
seam = bg.crop((0, BG_H - 250, BG_W, BG_H - 225)).filter(ImageFilter.GaussianBlur(8))
bg.paste(seam, (0, BG_H - 250))
save(bg, "residence-bg", 2048)

# ---- popup panel: erase dynamic text + item art, keep glow/swirls/divider ----
pp = popup.copy()
for r in [(320, 342, 730, 398), (320, 404, 692, 470), (335, 504, 692, 555),
          (335, 554, 652, 600), (105, 355, 295, 565),
          (802, 428, 948, 494), (798, 538, 948, 608),
          # 效果图里的关闭 X 属于示意；弹窗必须二选一，不能关闭，烘焙的 X 会误导。
          (912, 268, 1002, 352)]:
    inpaint(pp, r, blur=4.0)
# smooth the item-area smudge into the surrounding glow
glow_box = (70, 318, 332, 602)
pp.paste(pp.crop(glow_box).filter(ImageFilter.GaussianBlur(14)), glow_box)
# buttons are cut out as standalone skins and erased from the panel
teal_btn = popup.crop((735, 415, 985, 500))
inpaint(teal_btn, (65, 12, 215, 78))
save(teal_btn, "relic-btn-teal", 256)
gold_btn = popup.crop((735, 525, 985, 615))
inpaint(gold_btn, (55, 14, 215, 80))
save(gold_btn, "relic-btn-gold", 256)
inpaint(pp, (733, 413, 988, 502))
inpaint(pp, (733, 523, 988, 617))
save(pp.crop((15, 275, 1000, 720)), "relic-modal-panel", 2048)
print("done")
