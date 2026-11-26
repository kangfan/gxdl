from PIL import Image
import sys

im = Image.open(r"G:/demos/gxdl/Art/洞府/洞府切图图集.png")
print(im.mode, im.size)
if im.mode == "RGBA":
    print("alpha extrema", im.getchannel("A").getextrema())
im2 = im.convert("RGBA")
# sample corners and center
for xy in [(2, 2), (512, 512), (1020, 1020), (700, 200)]:
    print(xy, im2.getpixel(xy))
