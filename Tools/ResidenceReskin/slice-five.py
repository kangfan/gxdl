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


def sample(mocksys, samples):
    mock = Image.open(RAW.format(sys=mocksys) + "/mockup.png").convert("RGB")
    for name, box, mode in samples:
        fn = darkest if mode == "dark" else lightest
        print(mocksys, name, fn(mock, box))


# ---- shop：擦 返回钮/副题条/购买钮/提示条 文字 ----
slice_sys("shop",
    {"back": (500, 28, 565, 62), "subtitle": (525, 168, 665, 203),
     "buy-gold": (492, 1162, 562, 1204), "buy-blue": (615, 1162, 682, 1204),
     "footer": (140, 1086, 400, 1124)},
    {"back": (455, 15, 608, 80), "title-art": (472, 78, 612, 162),
     "subtitle-bar": (468, 163, 718, 208), "currency": (498, 198, 648, 252),
     "card": (18, 430, 702, 592), "buy-gold": (472, 1148, 600, 1218),
     "buy-blue": (593, 1148, 718, 1218), "footer-bar": (22, 1078, 438, 1132)})

# ---- codex：无烘焙文字 ----
slice_sys("codex", {},
    {"panel": (25, 68, 600, 878), "banner": (603, 68, 1017, 198),
     "wide-card": (627, 207, 983, 378), "cell": (623, 387, 745, 528),
     "close": (607, 850, 685, 938), "cloud": (760, 845, 1020, 1010)})

# ---- growth：无烘焙文字 ----
slice_sys("growth", {},
    {"card-forge": (50, 25, 765, 235), "card-realm": (20, 245, 745, 460),
     "card-talents": (35, 455, 760, 670), "card-gems": (25, 650, 765, 855),
     "btn-blue": (780, 113, 1015, 195), "btn-gold-off": (45, 833, 517, 943),
     "btn-blue-wide": (535, 835, 1017, 940)})

# ---- ruins：无烘焙文字 ----
slices = {"title": (22, 18, 318, 88), "bar": (352, 22, 592, 82),
          "vertical-plaque": (618, 8, 682, 98), "divider": (32, 98, 688, 138),
          "nav-bar": (28, 1138, 698, 1258)}
for i in range(10):
    col = i % 2
    row = i // 2
    x0 = 8 + col * 360
    y0 = 188 + row * 150
    slices[f"ch-{i + 1}"] = (x0, y0, x0 + 344, y0 + 150)
slice_sys("ruins", {}, slices)

# ---- prep：无烘焙文字 ----
slice_sys("prep", {},
    {"title": (50, 25, 580, 135), "divider": (130, 160, 615, 205),
     "skill-1": (52, 222, 193, 398), "skill-2": (219, 227, 341, 393),
     "skill-3": (349, 227, 471, 393), "skill-4": (545, 227, 665, 393),
     "nameplate": (225, 391, 335, 432), "card": (25, 450, 695, 1050),
     "btn-teal": (60, 1125, 355, 1220), "btn-gold": (385, 1120, 695, 1220)})

# ---- 文字颜色采样（mockup 坐标 1440x2560） ----
sample("shop", [
    ("back-text", (179, 70, 275, 115), "light"),
    ("subtitle", (422, 250, 794, 288), "dark"),
    ("good-name", (493, 544, 710, 614), "dark"),
    ("good-desc", (493, 666, 934, 710), "dark"),
    ("good-price", (570, 749, 755, 794), "dark"),
    ("buy-text", (1158, 614, 1293, 666), "dark"),
    ("note", (506, 2413, 1011, 2458), "light"),
])
sample("codex", [
    ("panel-title", (416, 614, 1024, 691), "dark"),
    ("hint", (403, 768, 1050, 819), "dark"),
    ("cell-name", (243, 1030, 358, 1075), "dark"),
])
sample("growth", [
    ("back-text", (166, 70, 288, 122), "light"),
    ("subtitle", (435, 141, 1011, 186), "dark"),
    ("entry-title", (454, 659, 730, 723), "dark"),
    ("entry-detail", (454, 768, 845, 845), "dark"),
    ("btn-label", (960, 850, 1060, 900), "light"),
    ("dungeon-label", (280, 2000, 470, 2060), "dark"),
    ("loadout-label", (950, 2000, 1120, 2060), "light"),
    ("footer", (130, 2440, 380, 2490), "light"),
])
sample("ruins", [
    ("title", (237, 384, 442, 467), "dark"),
    ("subtitle", (141, 531, 499, 576), "dark"),
    ("card-name", (224, 653, 410, 710), "dark"),
    ("card-range", (224, 723, 358, 768), "dark"),
    ("card-unlock", (224, 774, 422, 819), "dark"),
    ("card-status", (262, 826, 384, 870), "dark"),
])
sample("prep", [
    ("title", (550, 96, 883, 173), "dark"),
    ("subtitle", (518, 256, 928, 307), "dark"),
    ("skill-name", (122, 627, 275, 685), "light"),
    ("detail-name", (595, 1184, 832, 1261), "dark"),
    ("desc", (275, 1376, 1114, 1427), "dark"),
    ("btn-back", (237, 2131, 499, 2189), "light"),
    ("btn-go", (950, 2125, 1200, 2190), "dark"),
])
print("all done")
