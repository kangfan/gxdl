from PIL import Image, ImageFilter
from collections import deque

RAW = "ResourceCache/residence-atlas/raw"
OUT = "ResourceCache/residence-atlas/processed"

import os
os.makedirs(OUT, exist_ok=True)

atlas = Image.open(f"{RAW}/residence-atlas.png").convert("RGBA")


def inpaint(im, box):
    x0, y0, x1, y1 = box
    px = im.load()
    w, h = im.size
    top = max(0, y0 - 4)
    bot = min(h - 1, y1 + 3)
    for x in range(x0, x1):
        c0 = px[x, top]
        c1 = px[x, bot]
        span = max(1, y1 - y0)
        for y in range(y0, y1):
            k = (y - y0) / span
            r = int(c0[0] * (1 - k) + c1[0] * k)
            g = int(c0[1] * (1 - k) + c1[1] * k)
            b = int(c0[2] * (1 - k) + c1[2] * k)
            px[x, y] = (r, g, b, px[x, y][3])
    region = im.crop((x0 - 2, y0 - 2, x1 + 2, y1 + 2)).filter(ImageFilter.GaussianBlur(1.2))
    im.paste(region, (x0 - 2, y0 - 2))


def darkest(im, box):
    px = im.load()
    vals = []
    for y in range(box[1], box[3]):
        for x in range(box[0], box[2]):
            p = px[x, y]
            vals.append((p[0] + p[1] + p[2], p))
    vals.sort()
    top = [v[1] for v in vals[: max(1, len(vals) // 50)]]
    r = sum(p[0] for p in top) // len(top)
    g = sum(p[1] for p in top) // len(top)
    b = sum(p[2] for p in top) // len(top)
    return "#{:02X}{:02X}{:02X}".format(r, g, b)


def extract_object(im, box, alpha_thresh=8):
    """Photoshop 式对象选取：以框中心为种子取主对象连通体（含柔光晕），
    其余组件若触到框边界（属于邻近元素的入侵部分）清零，框内完整小件保留。"""
    region = im.crop(box)
    w, h = region.size
    alpha = region.getchannel("A")
    ap = alpha.load()

    # 从中心向外找第一个不透明像素作种子
    seed = None
    cx, cy = w // 2, h // 2
    for r in range(max(cx, cy) + 1):
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if max(abs(dx), abs(dy)) != r:
                    continue
                x, y = cx + dx, cy + dy
                if 0 <= x < w and 0 <= y < h and ap[x, y] > alpha_thresh:
                    seed = (x, y)
                    break
            if seed:
                break
        if seed:
            break
    if seed is None:
        return region

    seen = bytearray(w * h)
    keep = bytearray(w * h)
    main = []
    q = deque([seed])
    seen[seed[1] * w + seed[0]] = 1
    while q:
        x, y = q.popleft()
        main.append((x, y))
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h:
                j = ny * w + nx
                if not seen[j] and ap[nx, ny] > alpha_thresh:
                    seen[j] = 1
                    q.append((nx, ny))
    for x, y in main:
        keep[y * w + x] = 1
    for sy in range(h):
        for sx in range(w):
            if seen[sy * w + sx] or ap[sx, sy] <= alpha_thresh:
                continue
            comp = []
            touches = False
            q = deque([(sx, sy)])
            seen[sy * w + sx] = 1
            while q:
                x, y = q.popleft()
                comp.append((x, y))
                if x == 0 or y == 0 or x == w - 1 or y == h - 1:
                    touches = True
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if 0 <= nx < w and 0 <= ny < h:
                        j = ny * w + nx
                        if not seen[j] and ap[nx, ny] > alpha_thresh:
                            seen[j] = 1
                            q.append((nx, ny))
            if not touches:
                for x, y in comp:
                    keep[y * w + x] = 1
    px = region.load()
    for y in range(h):
        for x in range(w):
            if not keep[y * w + x]:
                r, g, b, _ = px[x, y]
                px[x, y] = (r, g, b, 0)
    bbox = alpha.getbbox()
    if bbox:
        region = region.crop(bbox)
    return region


TEXTS = {
    "plaque-banner": ((198, 520, 428, 576), (8, 486, 612, 602)),
    "btn-bag": ((695, 578, 822, 626), (642, 570, 832, 635)),
    "btn-companion": ((898, 576, 1004, 626), (847, 565, 1015, 640)),
    "btn-social": ((708, 661, 834, 710), (642, 652, 840, 717)),
    "btn-shop": ((920, 660, 1010, 714), (847, 655, 1015, 725)),
    "btn-plaque": ((683, 206, 777, 245), (647, 197, 815, 255)),
    "btn-hover": ((683, 276, 777, 312), (647, 267, 815, 320)),
    "btn-blue-click": ((868, 276, 992, 312), (835, 267, 1022, 320)),
}
work = atlas.copy()
for name, (erase, _) in TEXTS.items():
    print(name, "ink", darkest(work, erase))
    inpaint(work, erase)

SLICES = {
    "residence-panel": (52, 8, 622, 480),
    "residence-plaque-title": (4, 484, 620, 604),
    "residence-window": (46, 616, 364, 818),
    "residence-divider": (408, 645, 608, 682),
    "residence-btn-teal-1": (647, 267, 815, 320),
    "residence-btn-gold-1": (642, 570, 832, 635),
    "residence-btn-teal-2": (847, 565, 1015, 640),
    "residence-btn-teal-3": (642, 652, 840, 717),
    "residence-btn-teal-4": (835, 267, 1022, 320),
    "residence-btn-gold-2": (847, 655, 1015, 725),
    "residence-plaque": (647, 197, 815, 255),
    "residence-cloud": (412, 700, 605, 748),
}
MARGIN = 4
for name, box in SLICES.items():
    x0, y0, x1, y1 = box
    box = (max(0, x0 - MARGIN), max(0, y0 - MARGIN), min(1024, x1 + MARGIN), min(1024, y1 + MARGIN))
    crop = extract_object(work, box)
    crop.save(f"{OUT}/{name}.png")
    print(name, crop.size)

names = list(SLICES)
tiles = [Image.open(f"{OUT}/{n}.png") for n in names]
cw = max(t.width for t in tiles) + 8
ch = max(t.height for t in tiles) + 8
cols = 4
rows = (len(tiles) + cols - 1) // cols
m = Image.new("RGB", (cw * cols, ch * rows), (40, 44, 48))
for i, t in enumerate(tiles):
    m.paste(t, (i % cols * cw + 4, i // cols * ch + 4), t)
m.save(OUT + "/_review.png")
print("done")
