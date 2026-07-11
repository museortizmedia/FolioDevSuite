using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Folio.Editor;

namespace Folio.Editor.Windows 
{
    public class FolderStructureWindow : EditorWindow
    {
        private FolderNode rootNode;
        private Vector2 scroll;

        // Ruta de usuario (Persistente en el proyecto)
        private const string USER_SAVE_FOLDER = "Assets/Folio/Resources/FolderDesigner/";
        private const string USER_FILE = "user_structure.json";

        // Ruta del paquete (Solo lectura, datos de fábrica)
        private const string DEFAULT_DATA_PATH = "Packages/Folio: DevSuite/Resources/FolderDesigner/default_structure.json";

        [MenuItem("Window/Folio/🦎 Flux: Folder Designer", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<FolderStructureWindow>("🦎 Flux: Folder Designer");
            window.minSize = new Vector2(500, 950);
        }

        private void OnEnable()
        {
            // Cargar automáticamente: si existe el de usuario, lo usa; si no, intenta el de fábrica
            string userPath = Path.Combine(USER_SAVE_FOLDER, USER_FILE);
            if (File.Exists(userPath))
                LoadStructure(userPath);
            else
                LoadDefaultStructure();
        }

        private void OnDisable() => SaveStructure(USER_FILE);

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🧱 Folder Structure Designer", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("💾 Guardar"))
                SaveStructure(USER_FILE);

            if (GUILayout.Button("📂 Cargar Usuario"))
                LoadStructure(Path.Combine(USER_SAVE_FOLDER, USER_FILE));

            if (GUILayout.Button("🔄 Reset Fábrica"))
            {
                if (EditorUtility.DisplayDialog("Reset", "¿Cargar estructura por defecto del paquete?", "Sí", "No"))
                    LoadDefaultStructure();
            }

            if (GUILayout.Button("🧱 Crear Estructura"))
            {
                CreateFoldersRecursive(rootNode, "");
                FolderColorManager.FolderCache.Clear();
                FolderColorManager.UpdateCache(rootNode, "");
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (rootNode == null) rootNode = new FolderNode("Assets");
            DrawNode(rootNode, 0);
            EditorGUILayout.EndScrollView();
        }

        private void LoadDefaultStructure()
        {
            if (File.Exists(DEFAULT_DATA_PATH))
            {
                string json = File.ReadAllText(DEFAULT_DATA_PATH);
                var wrapper = JsonUtility.FromJson<FolderTreeWrapper>(json);
                rootNode = wrapper?.root ?? new FolderNode("Assets");
                Debug.Log("Folio: Estructura de fábrica cargada.");
            }
            else 
            {
                Debug.LogWarning("No se encontró el archivo de fábrica en: " + DEFAULT_DATA_PATH);
                rootNode = new FolderNode("Assets");
            }
        }

        private void SaveStructure(string fileName)
        {
            // Asegurar que la carpeta de usuario existe
            if (!Directory.Exists(USER_SAVE_FOLDER))
                Directory.CreateDirectory(USER_SAVE_FOLDER);

            string json = JsonUtility.ToJson(new FolderTreeWrapper { root = rootNode }, true);
            File.WriteAllText(Path.Combine(USER_SAVE_FOLDER, fileName), json);
            AssetDatabase.Refresh();
        }

        private void LoadStructure(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                var wrapper = JsonUtility.FromJson<FolderTreeWrapper>(File.ReadAllText(fullPath));
                rootNode = wrapper?.root ?? new FolderNode("Assets");
            }
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
                node.Description = EditorGUILayout.TextArea(node.Description, EditorStyles.textArea, GUILayout.Height(60));
                node.FolderColor = EditorGUILayout.ColorField("🎨 Color", node.FolderColor);
                
                string ext = EditorGUILayout.TextField("📄 Exts (sep. coma)", string.Join(",", node.AllowedExtensions));
                node.AllowedExtensions = new List<string>(ext.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
                for(int i = 0; i < node.AllowedExtensions.Count; i++) node.AllowedExtensions[i] = node.AllowedExtensions[i].Trim();

                foreach (var child in node.Children.ToArray()) DrawNode(child, indent + 1);
            }
            EditorGUILayout.EndVertical();
        }

        private void CreateFoldersRecursive(FolderNode node, string parentPath)
        {
            if (node == null) return;
            string currentPath = string.IsNullOrEmpty(parentPath) ? node.Name : Path.Combine(parentPath, node.Name).Replace("\\", "/");
            if (!Directory.Exists(currentPath)) Directory.CreateDirectory(currentPath);
            foreach (var child in node.Children) CreateFoldersRecursive(child, currentPath);
        }

        private bool RemoveNode(FolderNode parent, FolderNode toRemove)
        {
            if (parent.Children.Contains(toRemove)) { parent.Children.Remove(toRemove); return true; }
            foreach (var child in parent.Children) if (RemoveNode(child, toRemove)) return true;
            return false;
        }

        public class FolderTreeWrapper { public FolderNode root; }
    }
}