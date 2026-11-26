from PIL import Image, ImageDraw

SRC = r"G:/demos/gxdl/Art/洞府/洞府切图图集.png"
OUT = "ResourceCache/residence-atlas"

import os
os.makedirs(OUT + "/raw", exist_ok=True)

im = Image.open(SRC).convert("RGBA")
im.save(OUT + "/raw/residence-atlas.png")

g = im.convert("RGB").copy()
d = ImageDraw.Draw(g)
for x in range(0, im.width, 50):
    d.line([(x, 0), (x, im.height)], fill=(255, 0, 0), width=1)
    d.text((x + 2, 2), str(x), fill=(255, 0, 0))
for y in range(0, im.height, 50):
    d.line([(0, y), (im.width, y)], fill=(255, 0, 0), width=1)
    d.text((2, y + 2), str(y), fill=(255, 0, 0))
g.save(OUT + "/_grid.png")

# zooms of key regions with 20px ticks
def zoom(box, name, step=20):
    z = im.convert("RGB").crop(box)
    z = z.resize((z.width * 2, z.height * 2), Image.LANCZOS)
    d = ImageDraw.Draw(z)
    for x in range(0, box[2] - box[0], step):
        d.line([(x * 2, 0), (x * 2, 12)], fill=(255, 0, 0))
        if x % 100 == 0:
            d.text((x * 2 + 2, 12), str(box[0] + x), fill=(255, 0, 0))
    for y in range(0, box[3] - box[1], step):
        d.line([(0, y * 2), (12, y * 2)], fill=(255, 0, 0))
        if y % 100 == 0:
            d.text((14, y * 2), str(box[1] + y), fill=(255, 0, 0))
    z.save(OUT + "/" + name)

zoom((40, 0, 640, 500), "_z-frame.png")
zoom((620, 170, 1024, 400), "_z-btnstates.png")
zoom((40, 480, 620, 620), "_z-plaque.png")
zoom((620, 400, 1024, 720), "_z-entries.png")
zoom((30, 590, 640, 870), "_z-window.png")
zoom((0, 840, 480, 1010), "_z-icons.png")
print("done")
