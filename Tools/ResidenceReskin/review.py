from PIL import Image
import glob

OUT = "ResourceCache/residence-20261125/processed"
names = ["residence-panel", "residence-window", "residence-plaque", "residence-divider",
         "residence-close", "residence-bg",
         "residence-btn-teal-1", "residence-btn-gold-1", "residence-btn-teal-2",
         "residence-btn-teal-3", "residence-btn-teal-4", "residence-btn-gold-2",
         "relic-modal-panel"]
cell_w = 360
imgs = []
for n in names:
    im = Image.open(f"{OUT}/{n}.png").convert("RGB")
    s = min(1.0, cell_w / im.width)
    imgs.append((n, im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)))
cols = 3
rows = (len(imgs) + cols - 1) // cols
row_h = []
for r in range(rows):
    row_h.append(max(im.height for _, im in imgs[r * cols:(r + 1) * cols]) + 8)
sheet = Image.new("RGB", (cols * (cell_w + 8) + 8, sum(row_h) + 8), (40, 44, 48))
y = 8
for r in range(rows):
    for c, (n, im) in enumerate(imgs[r * cols:(r + 1) * cols]):
        sheet.paste(im, (8 + c * (cell_w + 8), y))
    y += row_h[r]
sheet.save(f"{OUT}/_review.png")
print("ok", sheet.size)
