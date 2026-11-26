# -*- coding: utf-8 -*-
"""放大指定区域并画细网格，用于精确量取坐标。"""
import sys
from PIL import Image, ImageDraw

src = 'ResourceCache/CommonUI20261123-r01/raw/atlas-20261123.webp'
x1, y1, x2, y2 = map(int, sys.argv[1:5])
scale = int(sys.argv[5]) if len(sys.argv) > 5 else 3
out = sys.argv[6] if len(sys.argv) > 6 else 'Tools/qa/_zoom.png'

im = Image.open(src).convert('RGBA')
bg = Image.new('RGBA', im.size, (24, 28, 34, 255))
bg.alpha_composite(im)
crop = bg.crop((x1, y1, x2, y2)).resize(((x2 - x1) * scale, (y2 - y1) * scale), Image.NEAREST)
d = ImageDraw.Draw(crop)
for x in range(x1, x2 + 1, 10):
    px = (x - x1) * scale
    col = (255, 90, 90, 140) if x % 50 == 0 else (110, 110, 110, 70)
    d.line([(px, 0), (px, crop.height)], fill=col)
    if x % 50 == 0:
        d.text((px + 1, 1), str(x), fill=(255, 255, 0, 255))
for y in range(y1, y2 + 1, 10):
    py = (y - y1) * scale
    col = (255, 90, 90, 140) if y % 50 == 0 else (110, 110, 110, 70)
    d.line([(0, py), (crop.width, py)], fill=col)
    if y % 50 == 0:
        d.text((1, py + 1), str(y), fill=(255, 255, 0, 255))
crop.convert('RGB').save(out)
print('saved', out, crop.size)
