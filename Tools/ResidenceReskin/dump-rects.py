import re
yaml = open('Assets/Resources/UI/LoadoutView.prefab', encoding='utf-8').read()
gos = {}
for m in re.finditer(r'--- !u!1 &(\d+)\nm_Name: (.+)', yaml):
    gos[m.group(1)] = m.group(2)
# fallback: name may be later in block
if len(gos) < 20:
    gos = {}
    for m in re.finditer(r'--- !u!1 &(\d+)\n(.*?)^--- ', yaml, re.S):
        n = re.search(r'm_Name: (.+)', m.group(2))
        if n: gos[m.group(1)] = n.group(1)
trs = {}
for m in re.finditer(r'--- !u!224 &(\d+)\n(.*?)^--- ', yaml, re.S):
    trs[m.group(1)] = m.group(2)
# map rect -> gameobject via RectTransform's m_GameObject
rt2go = {}
for rid, body in trs.items():
    g = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
    if g: rt2go[rid] = g.group(1)
for rid, body in trs.items():
    name = gos.get(rt2go.get(rid, ''), '?')
    if name in ('Label', 'Name', 'Home Title', 'Title', 'Selected Name', 'Auto Detail', 'Equipped Flag'):
        ap = re.search(r'm_AnchoredPosition: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        sd = re.search(r'm_SizeDelta: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        amin = re.search(r'm_AnchorMin: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        print(f"{name:16s} pos={ap.groups() if ap else '?'} size={sd.groups() if sd else '?'} amin={amin.groups() if amin else '?'}")
