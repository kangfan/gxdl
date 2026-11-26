using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

var assetPath = "Assets/_tmp_atlas.webp";
AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
if (importer == null) { Debug.Log("[Slice] importer=null"); return; }

importer.textureType = TextureImporterType.Default;
importer.isReadable = true;
importer.maxTextureSize = 2048;
importer.textureCompression = TextureImporterCompression.Uncompressed;
importer.alphaIsTransparency = true;
importer.mipmapEnabled = false;
importer.SaveAndReimport();

var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
if (tex == null) { Debug.Log("[Slice] tex=null"); return; }
Debug.Log("[Slice] tex " + tex.width + "x" + tex.height);

// 坐标基于预览图 721x1264（左上原点），运行时换算到真实纹理。
float sx = tex.width / 721f, sy = tex.height / 1264f;

var items = new (string name, int x, int y, int w, int h)[]
{
    ("btn-confirm",     18, 117, 212, 38),
    ("btn-cancel",      18, 160, 212, 38),
    ("btn-start",       18, 203, 212, 40),
    ("btn-claimed",     18, 248, 212, 40),
    ("btn-normal",     235, 120, 135, 38),
    ("btn-grey",       235, 164, 135, 38),
    ("btn-small",      235, 208, 135, 32),
    ("btn-icon-chevron",235,248, 135, 38),
    ("toggle-on",      610, 120,  92, 40),
    ("toggle-off",     610, 164,  92, 40),
    ("toggle-checked", 610, 212,  92, 40),
    ("circle-back",    376, 116,  68, 68),
    ("circle-close",   466, 116,  68, 68),
    ("circle-more",    556, 116,  68, 68),
    ("circle-refresh", 376, 186,  68, 68),
    ("circle-next",    466, 186,  68, 68),
    ("circle-help",    556, 186,  68, 68),
};

var root = Directory.GetParent(Application.dataPath).FullName;
var outDir = Path.Combine(root, "ResourceCache/CommonUI20261123-r01/raw/elements");
Directory.CreateDirectory(outDir);

int cellW = 240, cellH = 150, cols = 4;
int rows = (items.Length + cols - 1) / cols;
var sheet = new Texture2D(cellW * cols, cellH * rows, TextureFormat.RGBA32, false);
var bg = new Color32[sheet.width * sheet.height];
for (int i = 0; i < bg.Length; i++) bg[i] = new Color32(28, 32, 38, 255);
sheet.SetPixels32(bg);

var log = new StringBuilder();
for (int i = 0; i < items.Length; i++)
{
    var it = items[i];
    int px = Mathf.RoundToInt(it.x * sx);
    int py = Mathf.RoundToInt(it.y * sy);
    int pw = Mathf.RoundToInt(it.w * sx);
    int ph = Mathf.RoundToInt(it.h * sy);
    int yBottom = tex.height - (py + ph);

    var crop = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
    crop.SetPixels(tex.GetPixels(px, yBottom, pw, ph));
    crop.Apply();
    var png = crop.EncodeToPNG();
    File.WriteAllBytes(Path.Combine(outDir, it.name + ".png"), png);

    // 预览：等比缩放到格子内
    float scale = Mathf.Min((float)(cellW - 20) / pw, (float)(cellH - 20) / ph);
    int dw = Mathf.Max(1, Mathf.RoundToInt(pw * scale));
    int dh = Mathf.Max(1, Mathf.RoundToInt(ph * scale));
    int ox = (i % cols) * cellW + (cellW - dw) / 2;
    int oy = sheet.height - ((i / cols) + 1) * cellH + (cellH - dh) / 2;
    for (int y = 0; y < dh; y++)
    for (int x = 0; x < dw; x++)
    {
        var c = crop.GetPixelBilinear((x + 0.5f) / dw, (y + 0.5f) / dh);
        if (c.a < 0.05f) c = new Color(0.11f, 0.13f, 0.15f, 1f);
        sheet.SetPixel(ox + x, oy + y, c);
    }

    log.AppendLine(it.name + " " + pw + "x" + ph + " @(" + px + "," + py + ")");
    Object.DestroyImmediate(crop);
}

sheet.Apply();
File.WriteAllBytes(Path.Combine(outDir, "_contact-sheet.png"), sheet.EncodeToPNG());
Object.DestroyImmediate(sheet);
Debug.Log("[Slice] 完成 " + items.Length + " 张\n" + log);
