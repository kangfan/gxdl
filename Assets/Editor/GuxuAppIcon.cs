using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

public static class GuxuAppIcon {
    const string Root = "Assets/Art/AppIcon/";
    const string Evidence = "ResourceCache/AppIcon20261110";
    static readonly string[] Files = { "app-icon.png", "app-icon-adaptive-foreground.png", "app-icon-background.png" };

    [MenuItem("GXDL/Resources/Apply App Icon")]
    public static void Apply() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit play mode before configuring icons.");
        foreach (string name in Files) if (!File.Exists(Root + name)) throw new FileNotFoundException("Run the resource pipeline first.", Root + name);
        Directory.CreateDirectory(Evidence + "/backups");
        string backup = Evidence + "/backups/ProjectSettings.before-app-icon.asset";
        if (!File.Exists(backup)) File.Copy("ProjectSettings/ProjectSettings.asset", backup);
        foreach (string name in Files) {
            string path = Root + name;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = name.Contains("foreground");
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        var master = Load(Files[0]);
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { master });
        Assign(AndroidPlatformIconKind.Legacy, new[] { master });
        Assign(AndroidPlatformIconKind.Round, new[] { master });
        Assign(AndroidPlatformIconKind.Adaptive, AdaptiveLayers());
        AssetDatabase.SaveAssets();
        Verify();
    }

    static Texture2D Load(string name) {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + name);
        if (!texture) throw new InvalidDataException("Missing icon texture: " + name);
        return texture;
    }

    static Texture2D[] AdaptiveLayers() {
        // Verified against this Unity 2022.3 editor's adaptive layer labels: background, foreground.
        return new[] { Load(Files[2]), Load(Files[1]) };
    }

    static void Assign(PlatformIconKind kind, Texture2D[] textures) {
        var slots = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
        if (slots.Length == 0) throw new InvalidOperationException("Android icon slots unavailable: " + kind);
        foreach (var slot in slots) slot.SetTextures(textures);
        PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, kind, slots);
    }

    [MenuItem("GXDL/Resources/Verify App Icon")]
    public static void Verify() {
        int checks = 0;
        Action<bool, string> require = (ok, message) => { checks++; if (!ok) throw new InvalidDataException("App icon: " + message); };
        foreach (string name in Files) {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + name);
            require(importer != null && !importer.mipmapEnabled && importer.npotScale == TextureImporterNPOTScale.None && importer.textureCompression == TextureImporterCompression.Uncompressed, "import settings " + name);
            var pixels = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try {
                require(pixels.LoadImage(File.ReadAllBytes(Root + name)), "PNG decode " + name);
                int size = name == Files[0] ? 512 : 432;
                require(pixels.width == size && pixels.height == size, "size " + name);
                bool transparent = pixels.GetPixels32().Any(p => p.a != 255);
                require(transparent == (name == Files[1]), "alpha " + name);
            } finally { UnityEngine.Object.DestroyImmediate(pixels); }
        }
        var master = Load(Files[0]);
        require(PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown).SingleOrDefault() == master, "default icon assignment");
        var kinds = new[] { AndroidPlatformIconKind.Legacy, AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Adaptive };
        int total = 0;
        foreach (var kind in kinds) {
            var expected = kind == AndroidPlatformIconKind.Adaptive ? AdaptiveLayers() : new[] { master };
            var slots = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
            require(slots.Length > 0, "slot count " + kind);
            foreach (var slot in slots) {
                total++;
                require(slot.GetTextures().SequenceEqual(expected), "assigned layers " + kind + " " + slot.width);
                require(expected.All(t => t.width >= slot.width && t.height >= slot.height), "sufficient resolution " + kind);
            }
        }
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "/unity-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
            status = "PASS", checks, androidSlots = total, checkedAt = DateTimeOffset.Now.ToString("o"),
            assets = Files.Select(f => new { path = Root + f, guid = AssetDatabase.AssetPathToGUID(Root + f) }),
            adaptiveLayerOrder = AdaptiveLayers().Select(AssetDatabase.GetAssetPath), apkBuilt = false
        }, Newtonsoft.Json.Formatting.Indented));
        Debug.Log("[APP ICON] PASS: " + checks + " checks; default + " + total + " Android slots.");
    }
}
