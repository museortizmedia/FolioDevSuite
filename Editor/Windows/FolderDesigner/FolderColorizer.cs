using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace Folio.Editor
{
    [InitializeOnLoad]
    public class FolderColorizer
    {
        static FolderColorizer()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) return;

            if (FolderColorManager.FolderCache.TryGetValue(path, out FolderMetadata meta))
            {
                Color color = meta.Color;
                EditorGUI.DrawRect(new Rect(selectionRect.x + 2, selectionRect.y + 4, 14, 10), color);
                EditorGUI.DrawRect(new Rect(selectionRect.x + 2, selectionRect.y + 2, 6, 3), color);
                Rect hitRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.height);
                if (hitRect.Contains(Event.current.mousePosition))
                {
                    string tooltipText = $"<b>{Path.GetFileName(path)}</b>\n<i>{meta.Description}</i>";
                    if (meta.Extensions != null && meta.Extensions.Count > 0)
                    {
                        tooltipText += $"\n<color=cyan>Exts:</color> {string.Join(", ", meta.Extensions)}";
                    }
                    GUI.Label(hitRect, new GUIContent("", tooltipText));
                }
            }
        }
    }
}