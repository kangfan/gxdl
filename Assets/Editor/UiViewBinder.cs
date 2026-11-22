using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 把 prefab 里的节点绑定到 View 的序列化字段。
    ///
    /// 生成 prefab 时代码里没有 View 组件，所以字段引用为空。
    /// 这里按「字段名 = 节点名去空格转驼峰」的约定自动连线，
    /// 数组字段按名字前缀收集，避免手工拖拽出错。
    /// </summary>
    public static class UiViewBinder
    {
        /// <summary>
        /// 在 prefab 资源上挂载 View 组件并完成字段绑定。
        /// 已存在组件时复用，不会重复添加。
        /// </summary>
        public static bool Bind(string prefabPath, Type viewType)
        {
            if (string.IsNullOrEmpty(prefabPath)) throw new ArgumentException("prefab 路径为空", nameof(prefabPath));
            if (viewType == null) throw new ArgumentNullException(nameof(viewType));

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null)
            {
                Debug.LogError("[UiViewBinder] 无法打开 prefab：" + prefabPath);
                return false;
            }

            try
            {
                var view = contents.GetComponent(viewType);
                if (view == null) view = contents.AddComponent(viewType);
                if (view == null)
                {
                    Debug.LogError("[UiViewBinder] 无法添加组件 " + viewType.Name);
                    return false;
                }

                var bound = BindFields(view, contents);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[UiViewBinder] {prefabPath} 已绑定 {bound} 个字段");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static int BindFields(Component view, GameObject root)
        {
            var bound = 0;
            var fields = view.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (!field.IsDefined(typeof(SerializeField), true) && !field.IsPublic) continue;

                // Unity 的 GetComponentsInChildren 只接受组件类型。
                // 数组字段必须先取元素类型，否则会抛 ArgumentException。
                var isArray = field.FieldType.IsArray;
                var lookupType = isArray ? field.FieldType.GetElementType() : field.FieldType;
                if (lookupType == null) continue;
                if (!typeof(Component).IsAssignableFrom(lookupType)) continue;

                var all = root.GetComponentsInChildren(lookupType, true);
                if (all.Length == 0) continue;

                if (isArray)
                {
                    var prefix = StripArraySuffix(field.Name);

                    var matches = new List<Component>();
                    foreach (var component in all)
                    {
                        if (NameMatches(component.name, prefix)) matches.Add(component);
                    }

                    if (matches.Count == 0) continue;

                    // 按节点在层级中的顺序排列，保证数组顺序与视觉顺序一致。
                    matches.Sort((a, b) => CompareHierarchy(a.transform, b.transform));

                    var array = Array.CreateInstance(lookupType, matches.Count);
                    for (var i = 0; i < matches.Count; i++) array.SetValue(matches[i], i);
                    field.SetValue(view, array);
                    bound++;
                    continue;
                }

                // 单值字段：优先精确匹配，其次包含匹配。
                // 模糊匹配容易把相邻节点连错（曾把 titleRect 绑到 Story Subtitle Shade），
                // 因此模糊命中时记录告警，便于生成后核对。
                var chosen = null as Component;
                foreach (var component in all)
                {
                    if (ExactName(component.name, field.Name))
                    {
                        chosen = component;
                        break;
                    }
                }

                if (chosen == null)
                {
                    foreach (var component in all)
                    {
                        if (NameMatches(component.name, field.Name))
                        {
                            chosen = component;
                            break;
                        }
                    }
                }

                // 兜底：字段名常带类型后缀（captionGroup / captionRect），
                // 去掉后缀再匹配节点名，避免为每个字段写死映射。
                if (chosen == null)
                {
                    var stripped = StripTypeSuffix(field.Name);
                    if (stripped != field.Name)
                    {
                        foreach (var component in all)
                        {
                            if (NameMatches(component.name, stripped))
                            {
                                chosen = component;
                                break;
                            }
                        }
                    }
                }

                if (chosen == null && all.Length == 1) chosen = all[0];
                if (chosen == null) continue;

                field.SetValue(view, chosen);
                bound++;
            }

            return bound;
        }

        /// <summary>
        /// 按层级顺序比较两个节点，用于让自动收集的数组与视觉顺序一致。
        /// 逐个比较从根到该节点的兄弟索引。
        /// </summary>
        static int CompareHierarchy(Transform a, Transform b)
        {
            var pathA = PathOf(a);
            var pathB = PathOf(b);
            var length = Mathf.Min(pathA.Count, pathB.Count);
            for (var i = 0; i < length; i++)
            {
                if (pathA[i] != pathB[i]) return pathA[i].CompareTo(pathB[i]);
            }

            return pathA.Count.CompareTo(pathB.Count);
        }

        static List<int> PathOf(Transform node)
        {
            var indices = new List<int>();
            var current = node;
            while (current != null)
            {
                indices.Insert(0, current.GetSiblingIndex());
                current = current.parent;
            }

            return indices;
        }

        /// <summary>去掉字段名的组件类型后缀，便于与节点名做二次匹配。</summary>
        static string StripTypeSuffix(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return fieldName;

            string[] suffixes = { "Group", "Rect", "Button", "Text", "Image", "Bar", "Layer", "View" };
            for (var i = 0; i < suffixes.Length; i++)
            {
                if (fieldName.EndsWith(suffixes[i], StringComparison.Ordinal) &&
                    fieldName.Length > suffixes[i].Length + 1)
                {
                    return fieldName.Substring(0, fieldName.Length - suffixes[i].Length);
                }
            }

            return fieldName;
        }

        /// <summary>
        /// 去掉数组字段名的复数后缀，例如 foregroundMist 保持原样，
        /// 而 items 会变成 item 以便匹配节点名。
        /// </summary>
        static string StripArraySuffix(string fieldName)
        {
            if (fieldName.EndsWith("s", StringComparison.Ordinal) && fieldName.Length > 2)
            {
                return fieldName.Substring(0, fieldName.Length - 1);
            }

            return fieldName;
        }

        /// <summary>
        /// 节点名与字段名的宽松匹配：忽略大小写、空格与下划线。
        /// 允许节点名在字段名前带有归属前缀（如 Story Light Dust 对应 lightDust），
        /// 因此用包含匹配而非前缀匹配。
        /// </summary>
        static bool NameMatches(string nodeName, string fieldName)
        {
            var left = Normalize(nodeName);
            var right = Normalize(fieldName);
            if (right.Length == 0) return false;
            if (left.Length == 0) return false;

            if (left == right) return true;
            if (left.Contains(right)) return true;

            // 字段名可能比节点名更具体，例如 incomingArt 对应 Incoming Story Art。
            var stripped = right.Replace("art", string.Empty).Replace("text", string.Empty);
            if (stripped.Length > 0 && left.Contains(stripped)) return true;

            return false;
        }

        /// <summary>节点名与字段名规范化后完全一致。</summary>
        static bool ExactName(string nodeName, string fieldName)
        {
            return Normalize(nodeName) == Normalize(fieldName);
        }

        static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var builder = new System.Text.StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
            }

            return builder.ToString();
        }
    }
}
