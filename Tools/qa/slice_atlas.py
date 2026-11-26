# -*- coding: utf-8 -*-
"""按图集分节裁切 UI 元素，输出到 raw/elements/<分类>/。"""
import os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ATLAS = os.path.join(ROOT, 'ResourceCache', 'CommonUI20261123-r01', 'raw', 'atlas-20261123.webp')
OUT = os.path.join(ROOT, 'ResourceCache', 'CommonUI20261123-r01', 'raw', 'elements')


def grid(xs, ys, w, h):
    out = []
    for r, y in enumerate(ys):
        for c, x in enumerate(xs):
            out.append(('item-%02d' % (r * len(xs) + c + 1), (x, y, x + w, y + h)))
    return out


ITEMS = [
    ('通用按钮', [
        ('btn-confirm',       (17, 107, 193, 152)),
        ('btn-cancel',        (17, 158, 193, 203)),
        ('btn-start',         (17, 208, 193, 254)),
        ('btn-claimed',       (17, 257, 193, 294)),
        ('btn-normal',        (213, 107, 343, 152)),
        ('btn-grey',          (213, 158, 343, 203)),
        ('btn-small',         (227, 207, 323, 240)),
        ('btn-icon-chevron',  (227, 247, 333, 286)),
    ]),
    ('开关切换', [
        ('toggle-on',         (610, 129, 699, 169)),
        ('toggle-off',        (610, 184, 699, 224)),
        ('toggle-checked',    (610, 238, 699, 279)),
    ]),
    ('圆形图标按钮', [
        ('circle-back',       (384, 124, 460, 200)),
        ('circle-close',      (462, 124, 538, 200)),
        ('circle-more',       (540, 124, 616, 200)),
        ('circle-refresh',    (384, 205, 460, 281)),
        ('circle-next',       (462, 205, 538, 281)),
        ('circle-help',       (540, 205, 616, 281)),
    ]),
    ('主要面功能图标', grid([50, 110, 170, 230, 290], [338, 412, 486], 58, 58)),
    ('实用类图标', grid([362, 422, 482, 542], [338, 412], 58, 58)),
    ('状态提示图标', grid([565, 610, 655, 695], [330, 400], 42, 42)),
    ('通知与提示', [
        ('toast-success', (315, 608, 520, 632)),
        ('toast-info',    (315, 634, 520, 658)),
        ('toast-reward',  (315, 660, 520, 684)),
        ('toast-warning', (315, 686, 520, 710)),
    ]),
    ('通用弹窗', [
        ('dialog-prompt',  (10, 780, 225, 958)),
        ('dialog-reward',  (235, 780, 450, 958)),
        ('dialog-notice',  (460, 780, 700, 958)),
    ]),
    ('选择框', [
        ('check-on',   (215, 960, 300, 990)),
        ('check-dark', (215, 990, 300, 1020)),
        ('check-dim',  (215, 1020, 300, 1050)),
        ('check-radio',(215, 1050, 300, 1080)),
    ]),
    ('视图切换', [
        ('view-switch', (360, 985, 450, 1030)),
    ]),
    ('状态标贴', [
        ('tag-new',       (640, 958, 712, 988)),
        ('tag-recommend', (640, 988, 712, 1018)),
        ('tag-limited',   (640, 1018, 712, 1048)),
        ('tag-event',     (640, 1048, 712, 1078)),
    ]),
]


def main():
    atlas = Image.open(ATLAS).convert('RGBA')
    total = 0
    for category, items in ITEMS:
        folder = os.path.join(OUT, category)
        os.makedirs(folder, exist_ok=True)
        crops = []
        for name, box in items:
            img = atlas.crop(box)
            img.save(os.path.join(folder, name + '.png'))
            crops.append((name, img))
            total += 1

        cell_w, cell_h = 200, 120
        cols = 4
        rows = (len(crops) + cols - 1) // cols
        sheet = Image.new('RGBA', (cell_w * cols, cell_h * rows), (24, 28, 34, 255))
        draw = ImageDraw.Draw(sheet)
        for i, (label, img) in enumerate(crops):
            scale = min((cell_w - 16) / img.width, (cell_h - 26) / img.height)
            w, h = max(1, int(img.width * scale)), max(1, int(img.height * scale))
            small = img.resize((w, h), Image.LANCZOS)
            x = (i % cols) * cell_w + (cell_w - w) // 2
            y = (i // cols) * cell_h + 16 + (cell_h - 26 - h) // 2
            sheet.alpha_composite(small, (x, y))
            draw.text(((i % cols) * cell_w + 3, (i // cols) * cell_h + 2), label, fill=(255, 220, 120, 255))
        sheet.convert('RGB').save(os.path.join(folder, '_contact.png'))
        print(category, len(crops))
    print('total', total)


if __name__ == '__main__':
    main()
