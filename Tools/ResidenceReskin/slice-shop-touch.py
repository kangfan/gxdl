"""shop 返回钮残影二次修补 + 副题条裁掉货币栏入侵。"""
import importlib.util
spec = importlib.util.spec_from_file_location("fx", "Tools/ResidenceReskin/slice-five-fix.py")
fx = importlib.util.module_from_spec(spec)
import sys
sys.modules["fx"] = fx
# 只复用函数：手动执行到函数定义为止（避免触发切片）
src = open("Tools/ResidenceReskin/slice-five-fix.py", encoding="utf-8").read()
exec(src.split("def slice_sys")[0])
fx_src = src  # noqa

atlas = Image.open("ResourceCache/shop-20261125/raw/atlas.png").convert("RGBA")
work = atlas.copy()
inpaint(work, (527, 32, 566, 48))
inpaint(work, (528, 40, 564, 52))
back = extract_object(work, (450, 10, 613, 85))
back.save("ResourceCache/shop-20261125/processed/back.png")
print("back", back.size)

sub = extract_object(work, (466, 160, 672, 196))
sub.save("ResourceCache/shop-20261125/processed/subtitle-bar.png")
print("subtitle", sub.size)

# 顺带重切其余 shop 切片（同一份擦除底图）
for name, box in {"title-art": (470, 75, 700, 165), "currency": (498, 198, 648, 252),
                  "card": (18, 430, 702, 592), "buy-gold": (488, 1157, 596, 1242),
                  "buy-blue": (598, 1157, 716, 1242), "footer-bar": (22, 1078, 438, 1132)}.items():
    c = extract_object(work, box)
    c.save(f"ResourceCache/shop-20261125/processed/{name}.png")
    print(name, c.size)
print("shop touch done")
