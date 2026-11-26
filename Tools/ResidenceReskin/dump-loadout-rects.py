import re, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
yaml = open('Assets/Resources/UI/LoadoutView.prefab', encoding='utf-8').read()
blocks = re.split(r'--- !u!(\d+) &(\d+)\n', yaml)
# blocks: [pre, classid, id, body, classid, id, body, ...]
objs = {}   # fileID -> (classid, body)
i = 1
while i + 2 < len(blocks) + 1 and i + 2 <= len(blocks):
    cid, fid, body = blocks[i], blocks[i+1], blocks[i+2] if i+2 < len(blocks) else ''
    objs[fid] = (cid, body)
    i += 3
names = {}
rects = {}
for fid, (cid, body) in objs.items():
    if cid == '1':
        n = re.search(r'm_Name: (.+)', body)
        names[fid] = n.group(1).strip() if n else '?'
    if cid == '224':
        g = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
        f = re.search(r'm_Father: \{fileID: (\d+)\}', body)
        ap = re.search(r'm_AnchoredPosition: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        sd = re.search(r'm_SizeDelta: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        amin = re.search(r'm_AnchorMin: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        pv = re.search(r'm_Pivot: \{x: ([-\d.]+), y: ([-\d.]+)\}', body)
        ch = re.search(r'm_Children: (.*?)(?:m_|--- )', body, re.S)
        rects[fid] = dict(go=g.group(1) if g else '?', father=f.group(1) if f else '?',
                          ap=ap.groups() if ap else None, sd=sd.groups() if sd else None,
                          amin=amin.groups() if amin else None, pv=pv.groups() if pv else None)

def path(fid):
    parts = []
    while fid in rects and rects[fid]['father'] in rects:
        parts.append(names.get(rects[fid]['go'], '?'))
        fid = rects[fid]['father']
    if fid in rects:
        parts.append(names.get(rects[fid]['go'], '?'))
    return '/'.join(reversed(parts))

want = ('Header Back', 'Label', 'Name', 'Equipped Flag', 'Auto', 'Equip', 'Title Banner', 'Skill 0', 'Circle', 'Loadout Actions', 'Return', 'Prep Divider')
for fid, r in rects.items():
    p = path(fid)
    last = p.split('/')[-1]
    if last in want and ('Skill Root' in p or 'Header' in p or 'Detail' in p or 'Actions' in p):
        print(f"{p:55s} pos={r['ap']} size={r['sd']} amin={r['amin']} pivot={r['pv']}")
