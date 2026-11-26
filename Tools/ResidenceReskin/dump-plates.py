import re
yaml = open('Assets/Resources/UI/LoadoutView.prefab', encoding='utf-8').read()
blocks = re.split(r'--- !u!(\d+) &(\d+)\r?\n', yaml)
objs = {}
i = 1
while i + 2 <= len(blocks):
    cid, fid = blocks[i], blocks[i+1]
    body = blocks[i+2] if i+2 < len(blocks) else ''
    objs[fid] = (cid, body)
    i += 3
names, rects = {}, {}
for fid, (cid, body) in objs.items():
    if cid == '1':
        n = re.search(r'm_Name: (.+)', body)
        names[fid] = n.group(1).strip() if n else '?'
    if cid == '224':
        g = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
        ap = re.search(r'm_AnchoredPosition: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        sd = re.search(r'm_SizeDelta: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        pv = re.search(r'm_Pivot: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        rects[fid] = (g.group(1) if g else '?', ap.groups() if ap else None,
                      sd.groups() if sd else None, pv.groups() if pv else None)
for fid, (go, ap, sd, pv) in rects.items():
    n = names.get(go, '?')
    if n in ('Name Plate', 'Name', 'Equipped Flag', 'Circle', 'Skill 0'):
        print(f"{n:16s} pos={ap} size={sd} pivot={pv}")
