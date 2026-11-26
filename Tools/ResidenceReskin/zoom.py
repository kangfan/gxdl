from PIL import Image

RAW = "ResourceCache/residence-20261125/raw"
OUT = "ResourceCache/residence-20261125/processed"

dongfu = Image.open(f"{RAW}/dongfu.png").convert("RGB")
zones = {
    "_z-plaque": (130, 440, 430, 590, 2),
    "_z-gold1": (590, 560, 910, 675, 2),
    "_z-gold2": (590, 760, 910, 875, 2),
    "_z-teal3": (590, 660, 910, 775, 2),
}
for name, (x0, y0, x1, y1, scale) in zones.items():
    im = dongfu.crop((x0, y0, x1, y1))
    im = im.resize((im.width * scale, im.height * scale), Image.LANCZOS)
    d = im.load()
    # thin coordinate ticks every 20 source px
    from PIL import ImageDraw
    dr = ImageDraw.Draw(im)
    for gx in range(x0 // 20 * 20 + 20, x1, 20):
        px = (gx - x0) * scale
        major = gx % 100 == 0
        dr.line([(px, 0), (px, 8 if major else 4)], fill=(255, 0, 0), width=1)
        if major:
            dr.text((px + 1, 1), str(gx), fill=(255, 0, 0))
    im.save(f"{OUT}/{name}.png")
print("ok")
