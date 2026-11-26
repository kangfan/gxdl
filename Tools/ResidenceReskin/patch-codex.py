import io

p = 'Assets/Editor/CodexPrefabSource.cs'
s = io.open(p, encoding='utf-8').read()
s = s.replace('panel.gameObject.AddComponent<Image>().color = ScreenPrefabKit.Panel;',
              'panel.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.94f, 0.88f, 0.98f);')
s = s.replace('builder.TextNode("Tier Table", "Title", 35f, 20f, 630f, 48f, string.Empty, 30, ScreenPrefabKit.Gold);',
              'builder.TextNode("Tier Table", "Title", 35f, 20f, 630f, 48f, string.Empty, 30, TitleInk);')
s = s.replace('builder.TextNode("Tier Table", "Note", 45f, 88f, 590f, 24f, string.Empty, 18, ScreenPrefabKit.Cream);',
              'builder.TextNode("Tier Table", "Note", 45f, 88f, 590f, 24f, string.Empty, 18, HintInk);')
s = s.replace('''builder.TextNode("Tier Table", "Header " + c, xs[c], 116f, widths[c], 36f, headers[c], 23,
                    ScreenPrefabKit.Cream, c == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);''',
              '''builder.TextNode("Tier Table", "Header " + c, xs[c], 116f, widths[c], 36f, headers[c], 23,
                    HintInk, c == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);''')
s = s.replace('builder.TextNode(path, "Label", 15f, 0f, 175f, RowHeight, string.Empty, 23, ScreenPrefabKit.Cream);',
              'builder.TextNode(path, "Label", 15f, 0f, 175f, RowHeight, string.Empty, 23, CellNameInk);')
s = s.replace('''builder.TextNode(path, "Attack", 205f, 0f, 125f, RowHeight, string.Empty, 23,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleRight);''',
              '''builder.TextNode(path, "Attack", 205f, 0f, 125f, RowHeight, string.Empty, 23,
                    CellNameInk, TextAnchor.MiddleRight);''')
s = s.replace('''builder.TextNode(path, "Hp", 345f, 0f, 125f, RowHeight, string.Empty, 23,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleRight);''',
              '''builder.TextNode(path, "Hp", 345f, 0f, 125f, RowHeight, string.Empty, 23,
                    CellNameInk, TextAnchor.MiddleRight);''')
s = s.replace('''builder.TextNode(path, "Defense", 485f, 0f, 105f, RowHeight, string.Empty, 23,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleRight);''',
              '''builder.TextNode(path, "Defense", 485f, 0f, 105f, RowHeight, string.Empty, 23,
                    CellNameInk, TextAnchor.MiddleRight);''')
s = s.replace('''builder.TextNode("Tier Table/Close", "Label", 0f, 0f, 320f, 60f, "返回图鉴", 26,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);''',
              '''builder.TextNode("Tier Table/Close", "Label", 0f, 0f, 320f, 60f, "返回图鉴", 26,
                    CloseInk, TextAnchor.MiddleCenter);''')
s = s.replace('static Sprite Skin(string key)',
              'static readonly Color CloseInk = FromHex(0x6E4208);\n\n        static Sprite Skin(string key)')
io.open(p, 'w', encoding='utf-8').write(s)
print('ok')
