from PIL import Image
import os

for sys in ["shop", "codex", "growth", "ruins", "prep"]:
    d = f"ResourceCache/{sys}-20261125/processed"
    names = sorted(n for n in os.listdir(d) if n.endswith(".png"))
    tiles = [Image.open(os.path.join(d, n)).convert("RGBA") for n in names]
    cw = max(t.width for t in tiles) + 8
    ch = max(t.height for t in tiles) + 8
    cols = 4
    rows = (len(tiles) + cols - 1) // cols
    m = Image.new("RGB", (cw * cols, ch * rows), (40, 44, 48))
    for i, t in enumerate(tiles):
        m.paste(t, (i % cols * cw + 4, i // cols * ch + 4), t)
    m.save(d + "/_review.png")
    print(sys, len(tiles), "tiles", m.size)
