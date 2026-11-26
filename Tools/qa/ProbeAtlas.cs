using UnityEditor;
using UnityEngine;

var p = "Assets/_tmp_atlas.webp";
var obj = AssetDatabase.LoadAssetAtPath<Object>(p);
Debug.Log("[Probe] obj=" + (obj == null ? "null" : obj.GetType().Name));
var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
Debug.Log("[Probe] tex=" + (tex == null ? "null" : tex.width + "x" + tex.height));
var imp = AssetImporter.GetAtPath(p);
Debug.Log("[Probe] imp=" + (imp == null ? "null" : imp.GetType().Name));
Debug.Log("[Probe] guid=" + AssetDatabase.AssetPathToGUID(p));
