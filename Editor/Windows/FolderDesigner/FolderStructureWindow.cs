using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Folio.Editor;

namespace Folio.Editor.Windows {
    public class FolderStructureWindow : EditorWindow
    {
        private FolderNode rootNode;
        private Vector2 scroll;
        private const string SAVE_FOLDER = "Assets/Folio/Resources/FolderDesigner/";
        private const string DEFAULT_FILE = "default_structure.json";

        [MenuItem("Window/Folio/🦎 Flux: Folder Designer", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<FolderStructureWindow>("🦎 Flux: Folder Designer");
            window.minSize = new Vector2(500, 950);
        }

        private void OnEnable()
        {
            if (!Directory.Exists(SAVE_FOLDER))
                Directory.CreateDirectory(SAVE_FOLDER);

            LoadStructure(DEFAULT_FILE);
        }

        private void OnDisable()
        {
            SaveStructure(DEFAULT_FILE);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🧱 Folder Structure Designer", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💾 Guardar diseño"))
                SaveStructure(DEFAULT_FILE);

            if (GUILayout.Button("📂 Cargar diseño"))
                LoadStructure(DEFAULT_FILE);

            if (GUILayout.Button("🧱 Crear estructura"))
            {
                CreateFoldersRecursive(rootNode, "");
                FolderColorManager.FolderCache.Clear();
                FolderColorManager.UpdateCache(rootNode, "");
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (rootNode == null)
            {
                rootNode = new FolderNode("Assets");
            }
            DrawNode(rootNode, 0);
            EditorGUILayout.EndScrollView();
        }

        private void DrawNode(FolderNode node, int indent)
        {
            if (node == null) return;

            EditorGUI.indentLevel = indent;
            Color originalColor = GUI.color;
            GUI.color = node.FolderColor;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.color = originalColor;

            EditorGUILayout.BeginHorizontal();
            node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, node.Name, true);
            if (GUILayout.Button("+", GUILayout.Width(25))) node.Children.Add(new FolderNode("NewFolder"));
            if (node != rootNode && GUILayout.Button("🗑", GUILayout.Width(25)))
            {
                RemoveNode(rootNode, node);
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (node.IsExpanded)
            {
                node.Name = EditorGUILayout.TextField("📁 Nombre", node.Name);
                EditorGUILayout.LabelField("📝 Descripción:");
                node.Description = EditorGUILayout.TextArea(node.Description, EditorStyles.textArea, GUILayout.Height(60));
                node.FolderColor = EditorGUILayout.ColorField("🎨 Color", node.FolderColor);
                
                string extensionInput = string.Join(",", node.AllowedExtensions);
                string newExtInput = EditorGUILayout.TextField("📄 Exts (sep. por coma)", extensionInput);
                
                if (newExtInput != extensionInput)
                {
                    node.AllowedExtensions = new List<string>(newExtInput.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
                    for(int i = 0; i < node.AllowedExtensions.Count; i++) node.AllowedExtensions[i] = node.AllowedExtensions[i].Trim();
                }

                foreach (var child in node.Children.ToArray()) DrawNode(child, indent + 1);
            }
            EditorGUILayout.EndVertical();
        }

        private void CreateFoldersRecursive(FolderNode node, string parentPath)
        {
            if (node == null) return;
            string currentPath = (string.IsNullOrEmpty(parentPath)) ? node.Name : Path.Combine(parentPath, node.Name).Replace("\\", "/");
            if (!Directory.Exists(currentPath)) Directory.CreateDirectory(currentPath);
            foreach (var child in node.Children) CreateFoldersRecursive(child, currentPath);
        }

        private bool RemoveNode(FolderNode parent, FolderNode toRemove)
        {
            if (parent.Children.Contains(toRemove)) { parent.Children.Remove(toRemove); return true; }
            foreach (var child in parent.Children) if (RemoveNode(child, toRemove)) return true;
            return false;
        }

        private void SaveStructure(string fileName)
        {
            string json = JsonUtility.ToJson(new FolderTreeWrapper { root = rootNode }, true);
            File.WriteAllText(Path.Combine(SAVE_FOLDER, fileName), json);
            AssetDatabase.Refresh();
        }

        private void LoadStructure(string fileName)
        {
            string path = Path.Combine(SAVE_FOLDER, fileName);
            if (File.Exists(path))
            {
                var wrapper = JsonUtility.FromJson<FolderTreeWrapper>(File.ReadAllText(path));
                rootNode = wrapper?.root ?? new FolderNode("Assets");
            }
            else { rootNode = new FolderNode("Assets"); }
        }

        public class FolderTreeWrapper 
        { 
            public FolderNode root; 
        }
    }
}