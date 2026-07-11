using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace Folio.Editor
{
    public class FolderValidator : AssetPostprocessor
    {
        public static bool IsEnabled = true;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!IsEnabled) return;

            foreach (string path in importedAssets)
            {
                string directory = Path.GetDirectoryName(path).Replace("\\", "/");
                
                if (FolderColorManager.FolderCache.TryGetValue(directory, out FolderMetadata meta))
                {
                    string ext = Path.GetExtension(path).Replace(".", "").ToLower();
                    if (meta.Extensions.Count > 0 && !meta.Extensions.Contains(ext))
                    {
                        Object folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(directory);
                        Debug.LogWarning($"🚫 <color=red>[Folio: DevSuite]</color> Archivo <b>{Path.GetFileName(path)}</b> no permitido en <b>{directory}</b>.", folder);
                    }
                }
            }
        }
    }
}