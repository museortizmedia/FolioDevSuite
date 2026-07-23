using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Folio.Editor.Windows.DocManager.DocVariables;

namespace Folio.Editor.Windows
{
    public class DocsManagerWindow : EditorWindow
    {
        [System.Serializable]
        public class DocOrder { public List<string> order; }

        private string docsFolderPath = "Assets/Folio/Resources/DocsManager";
        private List<string> docPaths = new();
        private int selectedIndex = -1;
        private bool isEditing = false;
        private string newDocName = "";

        private Dictionary<string, string> editingBuffers = new();
        private Dictionary<string, bool> isModified = new();

        private Vector2 scrollPos;
        private Vector2 scrollEditorContent;

        private int dragFromIndex = -1;
        private int dragToIndex = -1;

        private string orderPath => Path.Combine(docsFolderPath, "docs_order.json");

        private bool pendingDelete = false;
        private string docToDeletePath = null;

        private const int MAX_SAFE_EDITOR_CHARS = 16000;


        // IMAGENES
        private const string ImagesFolderRelative = "Assets/Folio/Resources/DocsManager/imagenes";
        private string ImagesFolderAbsolute => Path.Combine(Application.dataPath, "Folio/Resources/DocsManager/imagenes");


        // VARIABLES TEMA
        private static bool isLightMode = false; // False = Oscuro, True = Claro
        private GUIStyle markdownEditorStyle;

        // MENU DOC VARIABLES
        private Dictionary<string, bool> showVariablesPanel = new();




        [MenuItem("Window/Folio/🐻 Koda: Docs Manager", false, 30)]
        public static void ShowWindow()
        {
            var window = GetWindow<DocsManagerWindow>("🐻 Koda: Docs Manager");
            window.minSize = new Vector2(500, 950);
            window.RefreshDocList();
        }

        private void OnEnable()
        {
            if (!Directory.Exists(docsFolderPath))
            {
                Directory.CreateDirectory(docsFolderPath);
                AssetDatabase.Refresh();
            }

            if (!Directory.Exists(ImagesFolderAbsolute))
            {
                Directory.CreateDirectory(ImagesFolderAbsolute);
                string sourcePath = "Packages/com.folio.devsuite/Resources/DocsManager/imagenes";
                if (!string.IsNullOrEmpty(sourcePath) && Directory.Exists(sourcePath))
                {
                    string[] files = Directory.GetFiles(sourcePath);
                    
                    foreach (string file in files)
                    {
                        if (!file.EndsWith(".meta"))
                        {
                            string fileName = Path.GetFileName(file);
                            string destFile = Path.Combine(ImagesFolderAbsolute, fileName);
                            File.Copy(file, destFile, true);
                        }
                    }
                }
                AssetDatabase.Refresh();
            }

            //Comprobar si hay documentos
            string[] existingDocs = Directory
                .GetFiles(docsFolderPath, "*.md")
                .Where(p => !p.EndsWith(".meta"))
                .ToArray();
            // Si no hay, crear uno de ejemplo
            if (existingDocs.Length == 0)
            {
                string defaultDocPath = Path.Combine(docsFolderPath, "Example.md");
                string defaultContent =
    "# Example: Markdown syntax guide\n\n" +
    "# This is a Heading h1\n" +
    "## This is a Heading h2\n" +
    "### This is a Heading h3\n" +
    "#### This is a Heading h4\n" +
    "##### This is a Heading h5\n" +
    "###### This is a Heading h6\n\n" +
    "## Emphasis\n" +
    "*This text will be italic*\n" +
    "_This will also be italic_\n\n" +
    "**This text will be bold**\n" +
    "__This will also be bold__\n\n" +
    "_You **can** combine them_\n\n" +
    "## Text\n" +
    "Esto es un párrafo normal.\n" +
    "    Esto un páraffo con sangria\n\n\n" +
    "## Lists\n" +
    "### Unordered\n" +
    "* Item 1\n" +
    "* Item 2\n" +
    "* Item 2a\n" +
    "* Item 2b\n" +
    "    * Item 3a\n" +
    "    * Item 3b\n\n" +
    "### Ordered\n" +
    "1. Item 1\n" +
    "2. Item 2\n" +
    "3. Item 3\n" +
    "    1. Item 3a\n" +
    "    2. Item 3b\n\n" +
    "## Tasks\n" +
    "- [ ] Tarea pendiente\n" +
    "- [x] Tarea completada\n" +
    "- [*] Otra tarea completada\n\n" +
    "## Links\n" +
    "You may be using [Markdown Live Preview](https://markdownlivepreview.com/). Open if you want.\n\n" +
    "## Blocks of code\n" +
    "```\n" +
    "let message = 'Hello world';\n" +
    "alert(message);\n" +
    "```\n\n" +
    "## Inline code\n" +
    "This web site is using `markedjs/marked`.\n\n" +
    "---\n\n" +
    "## Blockquotes\n" +
    "> Markdown is a lightweight markup language with plain-text-formatting syntax, created in 2004 by John Gruber with Aaron Swartz.\n\n" +
    ">> Markdown is a lightweight markup language with plain-text-formatting syntax, created in 2004 by John Gruber with Aaron Swartz.\n\n" +
    ">>> Markdown is a lightweight markup language with plain-text-formatting syntax, created in 2004 by John Gruber with Aaron Swartz.\n\n" +
    ">>>> Markdown is a lightweight markup language with plain-text-formatting syntax, created in 2004 by John Gruber with Aaron Swartz.\n\n" +
    ">>>>> Markdown is a lightweight markup language with plain-text-formatting syntax, created in 2004 by John Gruber with Aaron Swartz.\n\n" +
    "## Images\n" +
    "![icono|md](Assets/Folio/Resources/DocsManager/imagenes/icon.png)\n\n" +
    "![icono|85x85](Assets/Folio/Resources/DocsManager/imagenes/icon.png)\n\n" +
    "![icono](Assets/Folio/Resources/DocsManager/imagenes/icon.png)\n\n\n" +
    "## Tables\n\n" +
    "| Left columns | Right columns |\n" +
    "| :--- | :--- |\n" +
    "| left foo | right foo|\n" +
    "| left bar | right bar|\n" +
    "| left baz | right baz|\n";

                File.WriteAllText(defaultDocPath, defaultContent);
                AssetDatabase.Refresh();
            }


            // Cargar todas las variables de los documentos
            DocVariablesDB.LoadAll(docsFolderPath);

            // Carga la preferencia de tema al iniciar
            isLightMode = EditorPrefs.GetBool("DocsManagerWindow.isLightMode", false);
            RefreshDocList();

        }

        #region TEMA
        private GUIStyle GetMarkdownStyle()
        {
            if (markdownEditorStyle == null)
            {
                markdownEditorStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }

            // Definir colores según el modo
            if (isLightMode)
            {
                // Modo Claro
                markdownEditorStyle.normal.textColor = Color.black;
                // Usamos un color de fondo claro pero que no sea blanco puro para no cansar la vista
                markdownEditorStyle.normal.background = GetTexture(new Color(0.95f, 0.95f, 0.95f));
            }
            else
            {
                // Modo Oscuro (Unity por defecto)
                markdownEditorStyle.normal.textColor = Color.white * 0.9f;
                // Usamos un color de fondo oscuro suave
                markdownEditorStyle.normal.background = GetTexture(new Color(0.15f, 0.15f, 0.15f));
            }

            return markdownEditorStyle;
        }

        private static Texture2D GetTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
        #endregion

        #region Archivo
        private void RefreshDocList()
        {
            docPaths.Clear();
            editingBuffers.Clear();
            isModified.Clear();

            if (!Directory.Exists(docsFolderPath))
                return;

            string[] foundFiles = Directory
                .GetFiles(docsFolderPath, "*.md")
                .Where(p => !p.EndsWith(".meta"))
                .ToArray();

            List<string> ordered = null;

            // Intentar cargar orden guardado
            if (File.Exists(orderPath))
            {
                try
                {
                    var json = File.ReadAllText(orderPath);
                    var orderData = JsonUtility.FromJson<DocOrder>(json);

                    if (orderData != null && orderData.order != null)
                        ordered = orderData.order;
                }
                catch { /* ignorar si el JSON está mal */ }
            }

            // Si existe un orden guardado lo aplicamos
            if (ordered != null)
            {
                foreach (string path in ordered)
                {
                    if (foundFiles.Contains(path))
                    {
                        docPaths.Add(path);
                        isModified[path] = false;
                    }
                }

                // Agregar archivos nuevos no incluidos en el JSON
                foreach (string file in foundFiles)
                {
                    if (!docPaths.Contains(file))
                    {
                        docPaths.Add(file);
                        isModified[file] = false;
                    }
                }
            }
            else
            {
                // No existe docs_order.json → usar orden alfabético
                foreach (string path in foundFiles.OrderBy(p => p))
                {
                    docPaths.Add(path);
                    isModified[path] = false;
                }
            }

            selectedIndex = -1;
            isEditing = false;
        }

        private void CreateNewDocument()
        {
            if (string.IsNullOrWhiteSpace(newDocName))
            {
                EditorUtility.DisplayDialog("Error", "El nombre no puede estar vacío.", "Ok");
                return;
            }

            string safeName = newDocName.Trim().Replace(" ", "_");
            string path = Path.Combine(docsFolderPath, safeName + ".md");

            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("Error", "Ya existe un documento con ese nombre.", "Ok");
                return;
            }

            File.WriteAllText(path, $"# {newDocName}\n\nNuevo documento creado.");
            RefreshDocList();
            AssetDatabase.Refresh();
            newDocName = "";
        }

        private void ExportDocument(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string dest = EditorUtility.SaveFilePanel(
                "Exportar documento",
                "",
                fileName,
                "md"
            );

            if (!string.IsNullOrEmpty(dest))
            {
                File.Copy(filePath, dest, true);
                EditorUtility.DisplayDialog("Exportado", $"Documento exportado a:\n{dest}", "OK");
            }
        }

        private void ExportAllCombined()
        {
            if (docPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("Sin documentos", "No hay documentos para combinar.", "OK");
                return;
            }

            bool replaceVars = EditorUtility.DisplayDialog(
                "Exportar",
                "¿Deseas reemplazar las variables por sus valores?\n\n" +
                "Sí = Reemplazar variables\n" +
                "No = Conservar variables y unir todos los DOCVARS al final",
                "Sí, reemplazar",
                "No, conservar"
            );

            string dest = EditorUtility.SaveFilePanel(
                "Exportar documento compilado",
                "",
                "Documentos_Combinados.md",
                "md"
            );

            if (string.IsNullOrEmpty(dest))
                return;

            using StreamWriter writer = new StreamWriter(dest, false);

            List<DocVariable> globalVars = new List<DocVariable>();

            foreach (string path in docPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);

                string nameWithSpaces = fileName.Replace("_", " ");
                string nameWithUnderscores = fileName.Replace(" ", "_");

                string content = File.ReadAllText(path).TrimStart();

                // EXTRAER VARIABLES DEL BLOQUE DOCVARS
                var vars = DocVariable.ExtractVariables(content);
                if (vars != null && vars.Count > 0)
                    globalVars.AddRange(vars);

                // ELIMINAR BLOQUE DOCVARS DEL CONTENIDO DEL DOCUMENTO
                content = System.Text.RegularExpressions.Regex.Replace(
                    content,
                    @"<!--\s*DOCVARS[\s\S]*?DOCVARS\s*-->",
                    "",
                    System.Text.RegularExpressions.RegexOptions.Multiline
                ).TrimStart();

                // QUITAR POSIBLES ENCABEZADOS DUPLICADOS
                string[] lines = content.Split('\n');
                if (lines.Length > 0)
                {
                    string firstLine = lines[0].Trim();

                    bool isHeaderToRemove =
                        firstLine.Equals("# " + fileName) ||
                        firstLine.Equals("# " + nameWithSpaces) ||
                        firstLine.Equals("# " + nameWithUnderscores);

                    if (isHeaderToRemove)
                    {
                        content = string.Join("\n", lines, 1, lines.Length - 1).TrimStart();
                    }
                }

                // -------------------------------
                //     SI SE REEMPLAZAN VARIABLES
                // -------------------------------
                if (replaceVars)
                {
                    if (DocVariablesDB.Variables.ContainsKey(path))
                        content = Utils.MarkdownRenderer.Preprocess(content, DocVariablesDB.GetAll(), false);
                }

                // -------------------------------
                //    ESCRIBIR SECCIÓN EN SALIDA
                // -------------------------------
                writer.WriteLine($"# {nameWithSpaces}\n");
                writer.WriteLine(content.Trim());
                writer.WriteLine("\n");
            }

            // ---------------------------------------
            //  SI NO REEMPLAZAN: AGREGAR DOCVARS GLOBAL
            // ---------------------------------------
            if (!replaceVars && globalVars.Count > 0)
            {
                writer.WriteLine("\n<!-- DOCVARS");

                foreach (var v in globalVars)
                {
                    writer.Write("name: " + v.name + "; ");
                    writer.Write("type: " + v.type + "; ");
                    writer.Write("value: ");

                    writer.WriteLine(v.type switch
                    {
                        DocVariableType.String => $"\"{v.stringValue}\"",
                        DocVariableType.Int => v.intValue.ToString(),
                        DocVariableType.Float => v.floatValue.ToString(),
                        DocVariableType.Bool => v.boolValue.ToString().ToLower(),
                        _ => "\"\""
                    });
                }

                writer.WriteLine("DOCVARS -->");
            }

            writer.Close();

            EditorUtility.DisplayDialog("Completado", "Documentos combinados exportados correctamente.", "OK");
        }

        private void HandleDragEvents(int index, Rect dragRect)
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (dragRect.Contains(e.mousePosition))
                    {
                        dragFromIndex = index;
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.StartDrag("DocDrag");
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (dragFromIndex >= 0)
                    {
                        DragAndDrop.objectReferences = new Object[0];
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        e.Use();
                    }
                    break;

                case EventType.DragUpdated:
                    if (dragRect.Contains(e.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        e.Use();
                    }
                    break;

                case EventType.DragPerform:
                    if (dragRect.Contains(e.mousePosition))
                    {
                        dragToIndex = index;
                        DragAndDrop.AcceptDrag();
                        ReorderList();
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    dragFromIndex = -1;
                    dragToIndex = -1;
                    break;
            }
        }

        private void SaveOrder()
        {
            File.WriteAllText(orderPath, JsonUtility.ToJson(new DocOrder { order = docPaths }, true));
        }

        private void ReorderList()
        {
            if (dragFromIndex == -1 || dragToIndex == -1 || dragFromIndex == dragToIndex)
                return;

            string moved = docPaths[dragFromIndex];
            docPaths.RemoveAt(dragFromIndex);

            if (dragToIndex > dragFromIndex)
                dragToIndex--;

            docPaths.Insert(dragToIndex, moved);

            dragFromIndex = -1;
            dragToIndex = -1;

            SaveOrder();
            Repaint();
        }

        private void ImportMarkdownDocument()
        {
            string sourcePath = EditorUtility.OpenFilePanel(
                "Importar documento Markdown",
                "",
                "md"
            );

            if (string.IsNullOrEmpty(sourcePath))
                return;

            if (Path.GetExtension(sourcePath).ToLower() != ".md")
            {
                EditorUtility.DisplayDialog(
                    "Formato no válido",
                    "Solo se permiten archivos Markdown (.md).",
                    "OK"
                );
                return;
            }

            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(docsFolderPath, fileName);

            if (File.Exists(destPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Documento existente",
                    $"El documento '{fileName}' ya existe.\n¿Deseas sobrescribirlo?",
                    "Sobrescribir",
                    "Cancelar"
                );

                if (!overwrite)
                    return;
            }

            File.Copy(sourcePath, destPath, true);

            AssetDatabase.Refresh();
            DocVariablesDB.LoadAll(docsFolderPath);
            RefreshDocList();

            EditorUtility.DisplayDialog(
                "Documento importado",
                $"'{fileName}' se ha importado correctamente.",
                "OK"
            );
        }

        private void OpenDocsFolderInProject()
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(ImagesFolderRelative);

            if (folder == null)
            {
                EditorUtility.DisplayDialog(
                    "Carpeta no encontrada",
                    "La carpeta de imágenes no existe en el proyecto.",
                    "OK"
                );
                return;
            }

            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        private void OpenInExternalIDE(string filePath)
        {
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, 1);
        }
        #endregion
        #region Imagenes
        private void ImportImage()
        {
            string sourcePath = EditorUtility.OpenFilePanel(
                "Seleccionar imagen",
                "",
                "png,jpg,jpeg"
            );

            string ext = Path.GetExtension(sourcePath).ToLower();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            {
                EditorUtility.DisplayDialog("Formato no válido", "Solo se permiten PNG y JPG.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(sourcePath))
                return;

            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(ImagesFolderAbsolute, fileName);

            if (File.Exists(destPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Imagen existente",
                    "Ya existe una imagen con ese nombre. ¿Deseas sobrescribirla?",
                    "Sobrescribir",
                    "Cancelar"
                );

                if (!overwrite)
                    return;
            }

            File.Copy(sourcePath, destPath, true);
            AssetDatabase.Refresh();

            ConfigureImageImporter(Path.Combine(ImagesFolderRelative, fileName));

            EditorGUIUtility.systemCopyBuffer = $"![{fileName.Split(".")[0]}]({ImagesFolderRelative}/{fileName})";

            EditorUtility.DisplayDialog(
                "Imagen importada",
                $"Imagen guardada en:\n{ImagesFolderRelative}/{fileName} y enlace copiado en el portapapeles",
                "OK"
            );
        }

        private void ConfigureImageImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.GUI;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private void OpenImagesFolderInProject()
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(ImagesFolderRelative);

            if (folder == null)
            {
                EditorUtility.DisplayDialog(
                    "Carpeta no encontrada",
                    "La carpeta de imágenes no existe en el proyecto.",
                    "OK"
                );
                return;
            }

            // Obtener el primer asset dentro de la carpeta
            string[] assets = AssetDatabase.FindAssets("", new[] { ImagesFolderRelative });

            if (assets.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                // Si está vacía, seleccionar la carpeta
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
            }
        }

        private void InsertImageIntoCurrentDocument(string filePath)
        {
            if (!editingBuffers.ContainsKey(filePath))
                return;

            string imagePath = EditorUtility.OpenFilePanel(
                "Seleccionar imagen",
                ImagesFolderAbsolute,
                "png,jpg,jpeg"
            );

            if (string.IsNullOrEmpty(imagePath))
                return;

            // Convertir a ruta relativa de Unity
            if (!imagePath.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog(
                    "Ruta inválida",
                    "La imagen debe estar dentro del proyecto.",
                    "OK"
                );
                return;
            }

            string relativePath = "Assets" + imagePath.Substring(Application.dataPath.Length);

            string markdown = $"![imagen]({relativePath})";

            // Insertar al final (seguro y simple)
            editingBuffers[filePath] += "\n\n" + markdown;

            isModified[filePath] = true;
        }
        #endregion

        #region Docs Variables
        private void SaveVariablesToDocument(string filePath)
        {
            var vars = DocVariablesDB.Variables[filePath];

            string md = editingBuffers.ContainsKey(filePath)
                ? editingBuffers[filePath]
                : File.ReadAllText(filePath);

            md = DocVariable.InjectVariables(vars, md);

            File.WriteAllText(filePath, md);

            if (editingBuffers.ContainsKey(filePath))
                editingBuffers[filePath] = md;

            AssetDatabase.Refresh();

            // Clases generadas
            List<DocVariable> allVars = new List<DocVariable>();

            foreach (var kvp in DocVariablesDB.Variables)
            {
                allVars.AddRange(kvp.Value);
            }

            DocVariablesGenerator.GenerateRuntimeClass(allVars);
        }

        private void DrawVariablesPanel(string filePath)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Variables", EditorStyles.boldLabel);

            var vars = DocVariablesDB.Variables[filePath];
            foreach (var v in vars)
            {
                EditorGUILayout.BeginHorizontal();

                v.name = EditorGUILayout.TextField("Name", v.name);

                v.type = (DocVariableType)EditorGUILayout.EnumPopup("Type", v.type);

                switch (v.type)
                {
                    case DocVariableType.String:
                        v.stringValue = EditorGUILayout.TextField("Value", v.stringValue);
                        break;

                    case DocVariableType.Int:
                        v.intValue = EditorGUILayout.IntField("Value", v.intValue);
                        break;

                    case DocVariableType.Float:
                        v.floatValue = EditorGUILayout.FloatField("Value", v.floatValue);
                        break;

                    case DocVariableType.Bool:
                        v.boolValue = EditorGUILayout.Toggle("Value", v.boolValue);
                        break;
                }

                if (GUILayout.Button("❌", GUILayout.Width(30)))
                {
                    vars.Remove(v);
                    break;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("➕ Añadir Variable"))
            {
                vars.Add(new DocVariable { name = "NuevaVariable", type = DocVariableType.String });
            }

            if (GUILayout.Button("💾 Guardar Variables"))
            {
                SaveVariablesToDocument(filePath);
            }

            EditorGUILayout.EndVertical();
        }
        #endregion


        private void OnGUI()
        {
            // OBTENER ESTILO DE TEMA
            GUIStyle editorStyle = GetMarkdownStyle();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("📚 Documentos Transversales", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            // ARCHIVO
            EditorGUILayout.LabelField("Archivo", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // CREAR NUEVO DOCUMENTO
            EditorGUILayout.LabelField("Nuevo documento", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            newDocName = EditorGUILayout.TextField("Nombre", newDocName);
            if (GUILayout.Button("➕ Crear", GUILayout.Width(100)))
            {
                CreateNewDocument();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // IMPORTAR DOCUMENTO
            EditorGUILayout.LabelField("Importaciones", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("📥 Importar documento", "Importar documento existente"), GUILayout.Width(150)))
            {
                ImportMarkdownDocument();
            }
            if (GUILayout.Button(new GUIContent("📂 Ver documentos", "Ver documentos"), GUILayout.Width(150)))
            {
                OpenDocsFolderInProject();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // AJUSTES
            EditorGUILayout.LabelField("Opciones", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            bool newLightMode = EditorGUILayout.ToggleLeft(
                isLightMode ? "Modo Claro" : "Modo Oscuro",
                isLightMode,
                EditorStyles.boldLabel
            );
            if (newLightMode != isLightMode)
            {
                isLightMode = newLightMode;
                EditorPrefs.SetBool("DocsManagerWindow.isLightMode", isLightMode);
                // Forzar la recreación del estilo
                markdownEditorStyle = null;
                Repaint();
            }

            if (GUILayout.Button(new GUIContent("📂", "Ver Imágenes"), GUILayout.Width(30)))
            {
                OpenImagesFolderInProject();
            }

            if (GUILayout.Button(new GUIContent("🖼↑", "Subir imagen"), GUILayout.Width(30)))
            {
                ImportImage();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);


            for (int i = 0; i < docPaths.Count; i++)
            {
                string filePath = docPaths[i];
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 10)
                };

                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.BeginHorizontal();

                // -------------------------
                //     DRAG HANDLE ☰
                // -------------------------
                Rect dragRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
                GUI.Label(dragRect, "☰", EditorStyles.boldLabel);
                HandleDragEvents(i, dragRect);


                // -------------------------
                // BOTÓN PARA ABRIR/CERRAR
                // -------------------------
                Vector2 mousePosition = Event.current.mousePosition;
                GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft
                };
                nameStyle.normal.textColor = GUI.skin.label.normal.textColor;

                string displayName = fileName;
                if (isModified.ContainsKey(filePath) && isModified[filePath])
                    displayName += " ⚠️";

                GUIContent titleContent = new GUIContent($"📄 {displayName}");
                Rect titleRect = GUILayoutUtility.GetRect(titleContent, nameStyle);
                Vector2 textSize = nameStyle.CalcSize(titleContent);
                if (titleRect.Contains(mousePosition))
                {
                    EditorGUIUtility.AddCursorRect(titleRect, MouseCursor.Link);

                    float lineThickness = 1f;
                    Rect underlineRect = new Rect(
                        titleRect.x, 
                        titleRect.yMax - 2,
                        textSize.x,
                        lineThickness
                    );

                    EditorGUI.DrawRect(underlineRect, nameStyle.normal.textColor);
                }
                else
                {
                    nameStyle.fontStyle = FontStyle.Normal;
                }


                if (GUI.Button(titleRect, titleContent, nameStyle))
                {
                    if (selectedIndex != i)
                    {
                        selectedIndex = i;
                        isEditing = false;
                    }
                    else
                    {
                        selectedIndex = -1;
                        isEditing = false;
                    }
                }

                GUILayout.FlexibleSpace();


                // -------------------------
                // BOTONES DE EDICIÓN
                // -------------------------

                if (selectedIndex == i && !isEditing && GUILayout.Button(new GUIContent("✏️", "Editar")))
                {
                    if (File.Exists(filePath))
                    {
                        string loadedText = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

                        // Comprobar si el documento supera el límite seguro de caracteres
                        if (loadedText.Length > MAX_SAFE_EDITOR_CHARS)
                        {
                            bool openExternal = EditorUtility.DisplayDialog(
                                "Documento Extenso",
                                $"Este documento tiene {loadedText.Length:N0} caracteres y puede causar problemas de renderizado en el editor interno de Unity.\n\n" +
                                "¿Deseas abrirlo en tu editor de código / IDE externo configurado?",
                                "Abrir en IDE Externo",
                                "Continuar de todos modos"
                            );

                            if (openExternal)
                            {
                                OpenInExternalIDE(filePath);
                            }
                            else
                            {
                                editingBuffers[filePath] = loadedText.Replace("\r\n", "\n").Replace('“', '"').Replace('”', '"').Replace('’', '\'');
                                isEditing = true;
                            }
                        }
                        else
                        {
                            // Cargar normal para edición interna
                            editingBuffers[filePath] = loadedText.Replace("\r\n", "\n").Replace('“', '"').Replace('”', '"').Replace('’', '\'');
                            isEditing = true;
                        }
                    }
                }

                if (selectedIndex == i && isEditing)
                {
                    if (GUILayout.Button(new GUIContent("💾", "Guardar")))
                    {
                        if (EditorUtility.DisplayDialog("Guardar", $"¿Guardar '{fileName}'?", "Sí", "Cancelar"))
                        {
                            string savedText = editingBuffers[filePath].Replace("\r\n", "\n").Replace('“', '"').Replace('”', '"').Replace('’', '\'');
        
                            File.WriteAllText(filePath, savedText);
                            isModified[filePath] = false;
                            isEditing = false;

                            DocVariablesDB.Set(filePath, DocVariable.ExtractVariables(savedText));
                            
                            AssetDatabase.Refresh();
                        }
                    }

                    if (GUILayout.Button(new GUIContent("❌", "Cancelar")))
                    {
                        editingBuffers[filePath] = File.ReadAllText(filePath);
                        isModified[filePath] = false;
                        isEditing = false;
                    }
                }

                if (GUILayout.Button(new GUIContent("⬇", "Exportar"), GUILayout.Width(30)))
                {
                    ExportDocument(filePath);
                }

                if (GUILayout.Button(new GUIContent("🗑", "Eliminar"), GUILayout.Width(30)))
                {
                    bool hasVariables = DocVariablesDB.Variables.ContainsKey(filePath) && DocVariablesDB.Variables[filePath].Count > 0;

                    string dialogMessage = $"¿Eliminar '{fileName}'?";
                    if (hasVariables)
                    {
                        dialogMessage += $"\n\n⚠️ Este documento contiene variables asociadas que también se eliminarán. " + $"Podrían romperse las referencias en otros documentos.";
                    }

                    if (EditorUtility.DisplayDialog("Eliminar documento", dialogMessage, "Sí, eliminar", "Cancelar"))
                    {
                        pendingDelete = true;
                        docToDeletePath = filePath;
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Doc Variables

                if (!DocVariablesDB.Variables.ContainsKey(filePath))
                {
                    var vars = DocVariable.ExtractVariables(File.ReadAllText(filePath));
                    DocVariablesDB.Set(filePath, vars);
                }


                if (!showVariablesPanel.ContainsKey(filePath)) // Inicializar foldout del doc si no existe
                {
                    showVariablesPanel[filePath] = false;
                }



                // BARRA DE HERRAMIENTAS DE EDICION
                EditorGUILayout.BeginHorizontal();
                showVariablesPanel[filePath] = EditorGUILayout.Foldout(
                    showVariablesPanel[filePath],
                    "🔧 Variables del documento"
                );
                if (selectedIndex == i && isEditing)
                {
                    if (GUILayout.Button(new GUIContent("🖼↑", "Subir imagen"), GUILayout.Width(30)))
                    {
                        ImportImage();
                    }
                    if (GUILayout.Button(new GUIContent("🖼↓", "Insertar imagen"), GUILayout.Width(30)))
                    {
                        InsertImageIntoCurrentDocument(filePath);
                    }

                }
                EditorGUILayout.EndHorizontal();

                if (showVariablesPanel[filePath])
                {
                    DrawVariablesPanel(filePath);
                }

                // Doc Variables

                // -------------------------
                //   PREVIEW / EDITOR
                // -------------------------
                if (selectedIndex == i)
                {
                    // Doc Variables

                    // Doc Variables


                    if (isEditing)
                    {
                        EditorGUILayout.Space(5);

                        // 1. Estilo totalmente neutro y seguro para textos largos
                        GUIStyle areaStyle = new GUIStyle(EditorStyles.textArea)
                        {
                            wordWrap = true,
                            richText = false,
                            fontSize = 12
                        };

                        // Ajustar contraste de texto
                        Color textColor = isLightMode ? Color.black : new Color(0.9f, 0.9f, 0.9f, 1f);
                        areaStyle.normal.textColor = textColor;
                        areaStyle.focused.textColor = textColor;
                        areaStyle.active.textColor = textColor;

                        string currentBuffer = editingBuffers[filePath];

                        // 2. Control de cambios nativo sin colapso de layout
                        EditorGUI.BeginChangeCheck();

                        // ScrollView de tamaño fijo
                        scrollEditorContent = EditorGUILayout.BeginScrollView(scrollEditorContent, GUILayout.Height(350));

                        // TextArea que calcula su altura dinámicamente según el contenido
                        string newBuffer = EditorGUILayout.TextArea(
                            currentBuffer, 
                            areaStyle, 
                            GUILayout.ExpandHeight(true)
                        );

                        EditorGUILayout.EndScrollView();

                        if (EditorGUI.EndChangeCheck())
                        {
                            editingBuffers[filePath] = newBuffer;
                            isModified[filePath] = true;
                        }
                    }
                    else
                    {
                        string content = File.ReadAllText(filePath);

                        EditorGUILayout.Space(5);

                        // Renderiza el Markdown usando el estilo del editor como fondo para la previsualización
                        EditorGUILayout.BeginVertical(editorStyle);

                        // Le pasamos el modo de tema al renderer por si lo necesita
                        Utils.MarkdownRenderer.DrawFormattedMarkdown(content, isLightMode, DocVariablesDB.GetAll());

                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📘 Unir todos y exportar Markdown"))
            {
                ExportAllCombined();
            }

            if (GUILayout.Button("🔄 Actualizar lista de documentos"))
            {
                if (EditorUtility.DisplayDialog("Actualizar", "Esto cancelará todas las ediciones sin guardar. ¿Desea continuar?", "Sí", "Cancelar"))
                {
                    isEditing = false;
                    selectedIndex = -1;
                    editingBuffers.Clear();
                    foreach (var path in docPaths)
                        isModified[path] = false;

                    RefreshDocList();
                    AssetDatabase.Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);


            // Finales
            if (pendingDelete && !string.IsNullOrEmpty(docToDeletePath))
            {
                pendingDelete = false;

                string filePath = docToDeletePath;
                docToDeletePath = null;

                // 1. Eliminar archivo .md del disco
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // 2. Limpiar referencias en diccionarios locales
                editingBuffers.Remove(filePath);
                isModified.Remove(filePath);
                showVariablesPanel.Remove(filePath);

                // 3. Eliminar variables del documento en la DB global
                if (DocVariablesDB.Variables.ContainsKey(filePath))
                {
                    DocVariablesDB.Variables.Remove(filePath);
                }

                // 4. Regenerar el archivo C# (DocVariables.cs) con las variables restantes
                List<DocVariable> remainingVars = DocVariablesDB.GetAll();
                DocVariablesGenerator.GenerateRuntimeClass(remainingVars);

                // 5. Refrescar la lista de documentos y el AssetDatabase de Unity
                RefreshDocList();
                AssetDatabase.Refresh();
            }
        }
    }
}