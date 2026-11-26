from PIL import Image, ImageFilter, ImageDraw

RAW = "ResourceCache/bag-atlas/raw"
OUT = "ResourceCache/bag-atlas/processed"

bag = Image.open(f"{RAW}/atlas-bag.png").convert("RGBA")
det = Image.open(f"{RAW}/atlas-detail.png").convert("RGBA")


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
            px = tuple(int(top[k] * (1 - t) + bot[k] * t) for k in range(3))
            src[x, y] = px if len(top) == 4 else (*px, 255)
    box = (max(0, x0 - 4), max(0, y0 - 4), min(im.width, x1 + 4), min(im.height, y1 + 4))
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


# ---- atlas-bag ----
panel = bag.copy()
inpaint(panel, (118, 30, 220, 90))       # 背包
inpaint(panel, (325, 108, 440, 140))     # 当前装备
inpaint(panel, (160, 560, 570, 595))     # footer note
save(panel.crop((20, 25, 730, 612)), "bag-panel", 1024)
save(bag.crop((735, 170, 818, 243)), "bag-slot", 128)
save(circle_alpha(bag.crop((640, 33, 720, 112))), "bag-close", 128)
save(bag.crop((735, 530, 965, 640)), "bag-cloud", 256)

# ---- atlas-detail ----
hdr = det.crop((690, 262, 852, 312))
save(hdr, "gear-detail-header-l", 256)
save(det.crop((852, 262, 1005, 312)), "gear-detail-header-r", 256)
save(circle_alpha(det.crop((800, 55, 955, 215))), "gear-detail-close", 128)

btn = det.copy()
inpaint(btn, (805, 712, 920, 770))       # 卸下
save(btn.crop((730, 692, 1000, 795)), "gear-btn-gold", 256)
inpaint(btn, (815, 850, 922, 902))       # 强化
save(btn.crop((730, 820, 1000, 908)), "gear-btn-teal", 256)

save(det.crop((30, 55, 685, 962)), "gear-detail-panel", 1024)
print("done")
