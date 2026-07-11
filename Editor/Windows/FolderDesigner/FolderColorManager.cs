using Folio.Editor.Windows;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Folio.Editor
{
    public class FolderMetadata
    {
        public Color Color;
        public string Description;
        public List<string> Extensions;
    }

    [InitializeOnLoad]
    public static class FolderColorManager
    {
        public static Dictionary<string, FolderMetadata> FolderCache = new Dictionary<string, FolderMetadata>();

        static FolderColorManager()
        {
            EditorApplication.delayCall += () => 
            {
                LoadCacheFromDisk();
                EditorApplication.RepaintProjectWindow();
            };
        }

        public static void LoadCacheFromDisk()
        {
            string path = "Assets/Folio/Resources/FolderDesigner/default_structure.json";
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<FolderStructureWindow.FolderTreeWrapper>(json);
                if (wrapper != null) UpdateCache(wrapper.root, "");
            }
        }

        public static void UpdateCache(FolderNode node, string parentPath)
        {
            if (node == null) return;
            string currentPath = (string.IsNullOrEmpty(parentPath)) ? node.Name : (parentPath + "/" + node.Name);
            
            FolderCache[currentPath] = new FolderMetadata
            {
                Color = node.FolderColor,
                Description = node.Description,
                Extensions = new List<string>(node.AllowedExtensions)
            };

            foreach (var child in node.Children) UpdateCache(child, currentPath);
        }
    }
}