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
        private List<FolderNode> nodesToRemove = new List<FolderNode>();

        private const string USER_SAVE_FOLDER = "Assets/Folio/Resources/FolderDesigner/";
        private const string USER_FILE = "user_structure.json";

        private const string DEFAULT_DATA_PATH = "Packages/com.folio.devsuite/Resources/FolderDesigner/default_structure.json";

        [MenuItem("Window/Folio/🦎 Flux: Folder Designer", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<FolderStructureWindow>("🦎 Flux: Folder Designer");
            window.minSize = new Vector2(500, 950);
        }

        private void OnEnable()
        {
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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🧱 Folder Structure Designer", EditorStyles.boldLabel);
            if (GUILayout.Button("🔄"))
            {
                if (EditorUtility.DisplayDialog("Reset", "¿Cargar estructura por defecto del paquete?", "Sí", "No"))
                    LoadDefaultStructure();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💾 Guardar")) SaveStructure(USER_FILE);
            if (GUILayout.Button("📂 Cargar")) LoadStructure(Path.Combine(USER_SAVE_FOLDER, USER_FILE));
            if (GUILayout.Button("🧱 Crear Estructura"))
            {
                CreateFoldersRecursive(rootNode, "");
                FolderColorManager.FolderCache.Clear();
                FolderColorManager.UpdateCache(rootNode, "");
                AssetDatabase.Refresh();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            
            nodesToRemove.Clear();
            
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (rootNode == null) rootNode = new FolderNode("Assets");
            DrawNode(rootNode, 0);
            EditorGUILayout.EndScrollView();

            foreach (var node in nodesToRemove)
            {
                RemoveNode(rootNode, node);
            }
        }

        private void LoadFromFile(string path, string successMessage)
        {
            string json = File.ReadAllText(path);
            var wrapper = JsonUtility.FromJson<FolderTreeWrapper>(json);
            rootNode = wrapper?.root ?? new FolderNode("Assets");
            Debug.Log(successMessage);
        }

        private string GetDefaultDataPath()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(FolderStructureWindow).Assembly);
            return package != null 
                ? Path.Combine(package.resolvedPath, "Packages/com.folio.devsuite/Resources/FolderDesigner/default_structure.json") 
                : string.Empty;
        }

        private void LoadDefaultStructure()
        {
            string factoryPath = GetDefaultDataPath(); 
            string userFallbackPath = Path.Combine(USER_SAVE_FOLDER, "default_structure.json");
            if (File.Exists(factoryPath))
            {
                LoadFromFile(factoryPath, "<color=#00FF00>[🦎 Folio-Flux:]</color> Estructura de fábrica cargada desde el paquete.");
            }
            else if (File.Exists(userFallbackPath))
            {
                LoadFromFile(userFallbackPath, "<color=#00FF00>[🦎 Folio-Flux:]</color> Estructura de fábrica no encontrada en paquete, cargada desde carpeta de usuario.");
            }
            else
            {
                Debug.LogError($"No se pudo encontrar default_structure.json en ninguna ruta: {factoryPath} ni {userFallbackPath}");
                rootNode = new FolderNode("Assets");
            }
        }

        private void SaveStructure(string fileName)
        {
            // Asegurar que la carpeta de usuario existe
            if (!Directory.Exists(USER_SAVE_FOLDER))
            {
                Directory.CreateDirectory(USER_SAVE_FOLDER);
                Debug.Log($"Folio: Carpeta creada en {USER_SAVE_FOLDER}");
            }

            string fullPath = Path.Combine(USER_SAVE_FOLDER, fileName);
            string json = JsonUtility.ToJson(new FolderTreeWrapper { root = rootNode }, true);
            
            File.WriteAllText(fullPath, json);
            
            // Feedback visual en consola
            Debug.Log($"<color=#00FF00>[🦎 Folio-Flux:]</color> Estructura de carpetas guardada en: {fullPath}");
            
            AssetDatabase.Refresh();
        }

        private void LoadStructure(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                try 
                {
                    var wrapper = JsonUtility.FromJson<FolderTreeWrapper>(File.ReadAllText(fullPath));
                    rootNode = wrapper?.root ?? new FolderNode("Assets");
                    Debug.Log($"<color=#00FF00>[🦎 Folio-Flux:]</color> Estructura cargada correctamente desde: {fullPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"<color=#00FF00>[🦎 Folio-Flux:]</color> Error al parsear el JSON en {fullPath}. Detalles: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"<color=#00FF00>[🦎 Folio-Flux:]</color> No se encontró archivo en {fullPath}. Iniciando estructura vacía.");
                rootNode = new FolderNode("Assets");
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
            
            if (GUILayout.Button("+", GUILayout.Width(25))) 
            {
                node.Children.Add(new FolderNode("NewFolder"));
            }
            
            if (node != rootNode && GUILayout.Button("🗑", GUILayout.Width(25)))
            {
                nodesToRemove.Add(node);
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