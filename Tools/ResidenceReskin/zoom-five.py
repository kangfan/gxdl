from PIL import Image, ImageDraw

def zoom(src, box, name, step=20):
    im = Image.open(src).convert("RGB")
    z = im.crop(box)
    s = 2
    z = z.resize((z.width * s, z.height * s), Image.LANCZOS)
    d = ImageDraw.Draw(z)
    for x in range(0, box[2] - box[0], step):
        d.line([(x * s, 0), (x * s, 14)], fill=(255, 0, 0))
        if x % 50 == 0:
            d.text((x * s + 2, 14), str(box[0] + x), fill=(255, 0, 0))
    for y in range(0, box[3] - box[1], step):
        d.line([(0, y * s), (14, y * s)], fill=(255, 0, 0))
        if y % 50 == 0:
            d.text((16, y * s), str(box[1] + y), fill=(255, 0, 0))
    z.save("ResourceCache/" + name)

zoom("ResourceCache/shop-20261125/raw/atlas.png", (440, 0, 720, 260), "shop-20261125/_z-top.png")
zoom("ResourceCache/shop-20261125/raw/atlas.png", (440, 1130, 720, 1240), "shop-20261125/_z-buy.png", 10)
zoom("ResourceCache/shop-20261125/raw/atlas.png", (440, 150, 720, 260), "shop-20261125/_z-sub.png", 10)
zoom("ResourceCache/shop-20261125/raw/atlas.png", (0, 1060, 460, 1145), "shop-20261125/_z-foot.png", 10)
zoom("ResourceCache/ruins-20261125/raw/atlas.png", (340, 170, 720, 960), "ruins-20261125/_z-right.png")
print("done")
