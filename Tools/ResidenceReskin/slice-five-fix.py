"""五系统图集切片：像素对象选取（洞府轮新方法）+ 烘焙文字擦除 + 颜色采样。"""
from PIL import Image, ImageFilter
from collections import deque
import os

RAW = "ResourceCache/{sys}-20261125/raw"
OUT = "ResourceCache/{sys}-20261125/processed"
SYSTEMS = ["shop", "codex", "growth", "ruins", "prep"]
for s in SYSTEMS:
    os.makedirs(OUT.format(sys=s), exist_ok=True)


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
            px[x, y] = (int(c0[0] * (1 - k) + c1[0] * k), int(c0[1] * (1 - k) + c1[1] * k),
                        int(c0[2] * (1 - k) + c1[2] * k), px[x, y][3])
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
    return "#{:02X}{:02X}{:02X}".format(*(sum(p[i] for p in top) // len(top) for i in range(3)))


def lightest(im, box):
    px = im.load()
    vals = []
    for y in range(box[1], box[3]):
        for x in range(box[0], box[2]):
            p = px[x, y]
            vals.append((p[0] + p[1] + p[2], p))
    vals.sort(reverse=True)
    top = [v[1] for v in vals[: max(1, len(vals) // 50)]]
    return "#{:02X}{:02X}{:02X}".format(*(sum(p[i] for p in top) // len(top) for i in range(3)))


def extract_object(im, box, alpha_thresh=8):
    region = im.crop(box)
    w, h = region.size
    alpha = region.getchannel("A")
    ap = alpha.load()
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
    q = deque([seed])
    seen[seed[1] * w + seed[0]] = 1
    while q:
        x, y = q.popleft()
        keep[y * w + x] = 1
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h:
                j = ny * w + nx
                if not seen[j] and ap[nx, ny] > alpha_thresh:
                    seen[j] = 1
                    q.append((nx, ny))
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



def slice_sys(sys, erase, slices, margin=5):
    atlas = Image.open(RAW.format(sys=sys) + "/atlas.png").convert("RGBA")
    work = atlas.copy()
    for name, box in erase.items():
        print(sys, name, "ink", darkest(work, box))
        inpaint(work, box)
    for name, box in slices.items():
        x0, y0, x1, y1 = box
        box2 = (max(0, x0 - margin), max(0, y0 - margin), min(atlas.width, x1 + margin), min(atlas.height, y1 + margin))
        crop = extract_object(work, box2)
        crop.save(OUT.format(sys=sys) + f"/{name}.png")
        print(sys, name, crop.size)


slice_sys("shop",
    {"back": (527, 32, 566, 48), "subtitle": (495, 172, 660, 200),
     "buy-gold": (503, 1188, 555, 1234), "buy-blue": (633, 1188, 700, 1234)},
    {"back": (455, 15, 608, 80), "title-art": (470, 75, 700, 165),
     "subtitle-bar": (466, 160, 672, 208), "currency": (498, 198, 648, 252),
     "card": (18, 430, 702, 592), "buy-gold": (488, 1157, 596, 1242),
     "buy-blue": (598, 1157, 716, 1242), "footer-bar": (22, 1078, 438, 1132)})

slice_sys("ruins", {},
    {"title": (22, 18, 318, 88), "bar": (352, 22, 592, 82),
     "vertical-plaque": (618, 8, 682, 98), "divider": (32, 98, 688, 138),
     "nav-bar": (28, 1138, 698, 1258),
     "ch-1": (8, 188, 352, 338), "ch-3": (8, 338, 352, 488), "ch-5": (8, 488, 352, 638),
     "ch-7": (8, 638, 352, 788), "ch-9": (8, 788, 352, 938),
     "ch-2": (368, 150, 712, 300), "ch-4": (368, 290, 712, 440),
     "ch-6": (368, 442, 712, 592), "ch-8": (368, 605, 712, 747),
     "ch-10": (368, 740, 712, 890)})
print("fix done")
