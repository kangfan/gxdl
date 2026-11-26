from PIL import Image, ImageFilter, ImageDraw

RAW = "ResourceCache/relic-atlas/raw"
OUT = "ResourceCache/relic-atlas/processed"

atlas = Image.open(f"{RAW}/relic.png").convert("RGBA")


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


save(atlas.crop((12, 18, 1012, 514)), "relic-modal-panel", 1024)
save(circle_alpha(atlas.crop((878, 516, 998, 648))), "relic-close", 128)
save(atlas.crop((628, 644, 1012, 786)), "relic-btn-teal", 256)
save(atlas.crop((628, 812, 1012, 952)), "relic-btn-gold", 256)
print("done")
