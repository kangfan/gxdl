src = open("Tools/ResidenceReskin/slice-five.py", encoding="utf-8").read()
funcs = src.split("def slice_sys")[0]
body = open("Tools/ResidenceReskin/slice-five-fix.py", encoding="utf-8").read()
marker = 'from slice_five import slice_sys  # noqa: E402\n'
body = body.replace('"""shop/ruins 坐标修正重切（复用 slice-five 的函数）。"""\nimport sys\nsys.path.insert(0, "Tools/ResidenceReskin")\n' + marker, funcs)
open("Tools/ResidenceReskin/slice-five-fix.py", "w", encoding="utf-8").write(body)
print("patched")
