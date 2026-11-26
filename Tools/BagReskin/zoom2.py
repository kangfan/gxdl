from PIL import Image, ImageDraw

OUT = "ResourceCache/bag-20261125/processed"

def zoom2(src, box, name, scale=2):
    im = Image.open(f"ResourceCache/bag-20261125/raw/{src}.png").convert("RGB").crop(box)
    im = im.resize((im.width * scale, im.height * scale), Image.LANCZOS)
    d = ImageDraw.Draw(im)
    x0, y0 = box[0], box[1]
    gx = (x0 // 20) * 20 + 20
    while gx < box[2]:
        px = (gx - x0) * scale
        major = gx % 100 == 0
        d.line([(px, 0), (px, 14 if major else 6)], fill=(255, 0, 0), width=1)
        if major:
            d.text((px + 1, 1), str(gx), fill=(255, 0, 0))
    gy = (y0 // 20) * 20 + 20
    while gy < box[3]:
        py = (gy - y0) * scale
        major = gy % 100 == 0
        d.line([(0, py), (14 if major else 6, py)], fill=(255, 0, 0), width=1)
        if major:
            d.text((1, py + 1), str(gy), fill=(255, 0, 0))
    im.save(f"{OUT}/_z-{name}.png")

zoom2("bag", (60, 480, 760, 700), "title")          # ribbon + panel top-left
zoom2("bag", (1180, 480, 1440, 700), "close")       # close X
zoom2("bag", (500, 660, 980, 790), "header")        # section header
zoom2("bag", (140, 740, 480, 1010), "slot")         # first slot card + badge
zoom2("bag", (1330, 740, 1440, 1660), "rightedge")  # panel right edge + col4
zoom2("bag", (60, 1700, 760, 1960), "footer")       # bottom divider + note
zoom2("detail", (300, 640, 980, 950), "d-top")      # detail panel top + iconbox
zoom2("detail", (330, 930, 980, 1300), "d-mid")     # headers + stats
zoom2("detail", (330, 1300, 980, 1640), "d-bot")    # description + buttons
print("ok")
