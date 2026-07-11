using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Folio.Editor.Windows
{
    public class ProjectManagerWindow : EditorWindow
    {
        // Docs
        private string docsFolderPath = "Assets/Folio/Resources/DocsManager";
        private List<string> docPaths = new();
        private int selectedIndex = -1;
        private bool isEditing = false;
        private string editorContent = "";
        private Vector2 docsScroll;
        private Vector2 docEditorScroll;
        private Vector2 overviewScroll;
        private float docsPanelWidth = 500f;

        private bool draggingSplitter = false;
        private Rect splitterRect;

        private TaskDatabase taskDatabase;
        private string databasePath = "Assets/Folio/Resources/TaskManager/task_database.json";
        private int filterUserIndex = 0;
        private int filterStateIndex = 0;
        private Dictionary<string, bool> moduleFoldouts = new();

        private Vector2 projectNotesScroll;
        // Notas
        private bool isNotesEditing = false;



        [MenuItem("Window/Folio/🦊 Folio: Dashboard Dev Suite", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectManagerWindow>("🦊 Folio: Dev Suite");
            window.minSize = new Vector2(750, 850);
            window.LoadTaskDatabase();
            window.RefreshDocs();
        }

        private void OnFocus()
        {
            LoadTaskDatabase();
            RefreshDocs();
            Repaint();
        }

        private void OnEnable()
        {
            if (!Directory.Exists(docsFolderPath))
            {
                Directory.CreateDirectory(docsFolderPath);
                AssetDatabase.Refresh();
            }

            // Cargar todas las variables de los documentos
            DocVariablesDB.LoadAll(docsFolderPath);

            LoadTaskDatabase();
            filterUserIndex = EditorPrefs.GetInt("PM_FilterUserIndex", 0);
            filterStateIndex = EditorPrefs.GetInt("PM_FilterStateIndex", 0);
            RefreshDocs();
        }

        private void LoadTaskDatabase()
        {
            if (File.Exists(databasePath))
            {
                string json = File.ReadAllText(databasePath);
                taskDatabase = JsonUtility.FromJson<TaskDatabase>(json);
            }
            else
            {
                taskDatabase = new TaskDatabase();
            }

            if (taskDatabase != null)
            {
                foreach (var module in taskDatabase.Modules)
                {
                    if (!moduleFoldouts.ContainsKey(module.ModuleName))
                        moduleFoldouts[module.ModuleName] = false;
                }
            }
        }

        private void SaveTaskDatabase()
        {
            string json = JsonUtility.ToJson(taskDatabase, true);
            File.WriteAllText(databasePath, json);
            AssetDatabase.Refresh();
        }

        private void RefreshDocs()
        {
            docPaths.Clear();

            string orderJson = Path.Combine(docsFolderPath, "docs_order.json");

            List<string> orderedList = null;

            if (File.Exists(orderJson))
            {
                if (File.Exists(orderJson))
                {
                    try
                    {
                        var json = File.ReadAllText(orderJson);
                        var order = JsonUtility.FromJson<DocsManagerWindow.DocOrder>(json);
                        orderedList = order.order;
                    }
                    catch
                    {
                        Debug.LogWarning("docs_order.json está corrupto. Se ignorará.");
                        orderedList = null;
                    }
                }
            }

            string[] files = Directory.GetFiles(docsFolderPath, "*.md")
                                       .Where(f => !f.EndsWith(".meta"))
                                       .ToArray();

            if (orderedList != null)
            {
                // aplicar orden guardado
                foreach (string path in orderedList)
                {
                    if (files.Contains(path))
                        docPaths.Add(path);
                }

                // si hay archivos nuevos, agregarlos al final
                foreach (string f in files)
                {
                    if (!docPaths.Contains(f))
                        docPaths.Add(f);
                }
            }
            else
            {
                // sin orden guardado → orden por defecto (alfabético)
                docPaths = files.OrderBy(f => f).ToList();
            }
        }

        private void ShowContextMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Abrir Flux 🦎"), false, () =>
            {
                FolderStructureWindow.ShowWindow();
            });

            menu.AddItem(new GUIContent("Abrir Nexo 🐝"), false, () =>
            {
                TaskManagerWindow.ShowWindow();
            });

            menu.AddItem(new GUIContent("Abrir Koda 🐻"), false, () =>
            {
                DocsManagerWindow.ShowWindow();
            });

            menu.ShowAsContext();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.ContextClick)
            {
                ShowContextMenu();
            }

            EditorGUILayout.BeginHorizontal();

            DrawTasksPanel();
            HandleSplitter();
            DrawDocsPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTasksPanel()
        {
            #region --- Panel de Tareas y Módulos ---
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField("🧩 Tareas y Módulos", EditorStyles.boldLabel);

            overviewScroll = EditorGUILayout.BeginScrollView(overviewScroll);

            if (taskDatabase == null)
            {
                EditorGUILayout.HelpBox("Base de datos de tareas no cargada.", MessageType.Error);
                return;
            }

            List<string> userOptions = new() { "Todos" };
            userOptions.AddRange(taskDatabase.Users);
            filterUserIndex = EditorGUILayout.Popup("👤 Filtrar por encargado", filterUserIndex, userOptions.ToArray());
            EditorPrefs.SetInt("PM_FilterUserIndex", filterUserIndex);

            List<string> stateOptions = new() { "Todos" };
            stateOptions.AddRange(System.Enum.GetNames(typeof(TaskState)));
            filterStateIndex = EditorGUILayout.Popup("🟣​ Filtrar por estado", filterStateIndex, stateOptions.ToArray());
            EditorPrefs.SetInt("PM_FilterStateIndex", filterStateIndex);

            if (GUILayout.Button("🧹 Limpiar Filtros", GUILayout.Height(25)))
            {
                filterUserIndex = 0;
                filterStateIndex = 0;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("📦 Módulos del Proyecto", EditorStyles.boldLabel);

            int totalTasks = 0;
            int totalCompleted = 0;

            foreach (var module in taskDatabase.Modules.Where(m => !m.Completed))
            {
                if (!moduleFoldouts.ContainsKey(module.ModuleName))
                    moduleFoldouts[module.ModuleName] = true;

                int completed = module.Tasks.Count(t => t.State == TaskState.Done);
                int taskCount = module.Tasks.Count;
                float progressByState = taskCount > 0 ? (float)completed / taskCount : 0f;

                float progressBySlider = taskCount > 0 ? (float)module.Tasks.Average(t => t.Progress) / 100f : 0f;

                float progress = (progressByState + progressBySlider) / 2f;

                Color bgColor = Color.Lerp(new Color(1f, 0.8f, 0.8f), new Color(0.8f, 1f, 0.8f), progress);
                GUI.backgroundColor = bgColor;

                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginHorizontal();
                moduleFoldouts[module.ModuleName] = EditorGUILayout.Foldout(moduleFoldouts[module.ModuleName], $"📂​ {module.ModuleName}", true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{Mathf.RoundToInt(progress * 100)}%", GUILayout.Width(40));
                if (GUILayout.Button("✅ Completar", GUILayout.Width(120)))
                {
                    module.Completed = true;
                    SaveTaskDatabase();
                    break;
                }
                EditorGUILayout.EndHorizontal();
                if (moduleFoldouts[module.ModuleName])
                {
                    foreach (var task in module.Tasks)
                    {
                        if (filterUserIndex > 0 && task.AssignedTo != taskDatabase.Users[filterUserIndex - 1])
                            continue;
                        if (filterStateIndex > 0 && task.State.ToString() != stateOptions[filterStateIndex])
                            continue;

                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField("📄 " + task.Title, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(task.Description, EditorStyles.wordWrappedLabel);
                        EditorGUILayout.LabelField("👤 Encargado: " + (string.IsNullOrEmpty(task.AssignedTo) ? "Nadie" : task.AssignedTo));

                        GUIStyle stateStyle = new(EditorStyles.boldLabel);
                        stateStyle.normal.textColor = task.State switch
                        {
                            TaskState.ToDo => Color.gray,
                            TaskState.InProgress => Color.yellow,
                            TaskState.Done => Color.green,
                            _ => Color.white
                        };
                        EditorGUILayout.LabelField("🟣 Estado", task.State.ToString(), stateStyle);

                        TaskState newState = (TaskState)EditorGUILayout.EnumPopup("Cambiar Estado", task.State);
                        if (newState != task.State)
                        {
                            task.State = newState;
                            SaveTaskDatabase();
                        }


                        if (newState == TaskState.InProgress)
                        {
                            // Cambiar progreso
                            EditorGUI.BeginChangeCheck();
                            int newProgress = EditorGUILayout.IntSlider("🔵 Progreso", task.Progress, 0, 99);
                            if (EditorGUI.EndChangeCheck())
                            {
                                task.Progress = newProgress;
                                SaveTaskDatabase();
                            }

                            // Mostrar barra visual
                            Rect progressRect = GUILayoutUtility.GetRect(18, 18);
                            EditorGUI.ProgressBar(progressRect, task.Progress / 100f, $"{task.Progress}%");
                        }


                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndVertical();

                totalTasks += taskCount;
                totalCompleted += completed;
            }
            #endregion

            #region --- Sección de Módulos Completados ---
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("📦 Módulos Completados", EditorStyles.boldLabel);
            var completedModules = taskDatabase.Modules.Where(m => m.Completed).ToList();
            if (completedModules.Count == 0)
            {
                EditorGUILayout.LabelField("Aún no hay módulos completados.");
            }
            else
            {
                foreach (var completed in completedModules)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Label("✅ " + completed.ModuleName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("↩️ Reabrir Módulo", GUILayout.Width(160)))
                    {
                        completed.Completed = false;
                        SaveTaskDatabase();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            #endregion

            #region --- Sección de Notas Generales del Proyecto ---
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("📝 Notas Generales del Proyecto", EditorStyles.boldLabel);

            GUIStyle textAreaStyle = new GUIStyle(GUI.skin.textArea)
            {
                wordWrap = true
            };

            // Estimación del ancho disponible
            // Restamos el ancho del panel de documentos (docsPanelWidth), el separador (6) y un margen (40)
            float estimatedWidth = position.width - docsPanelWidth - 6 - 40;
            if (estimatedWidth <= 0) estimatedWidth = 400f;

            string currentNotes = taskDatabase.ProjectNotes;
            float fixedHeight = 170f; // Altura fija mínima de la caja

            // Contenedor Visual y de Scroll para las notas
            // Usamos BeginVertical con la caja de estilo para dar el marco visual
            EditorGUILayout.BeginVertical(textAreaStyle, GUILayout.Height(fixedHeight), GUILayout.ExpandWidth(true));

            // INICIO DEL SCROLL VIEW ESPECÍFICO PARA LAS NOTAS (Mantiene el scroll de las notas)
            projectNotesScroll = EditorGUILayout.BeginScrollView(projectNotesScroll);

            if (isNotesEditing)
            {
                // MODO EDICIÓN
                // Calcular la altura que necesita el texto
                float textHeight = textAreaStyle.CalcHeight(new GUIContent(currentNotes), estimatedWidth);

                EditorGUI.BeginChangeCheck();
                string newNotes = EditorGUILayout.TextArea(
                    currentNotes,
                    textAreaStyle,
                    GUILayout.ExpandWidth(true),
                    // La altura mínima es fixedHeight (170) o la altura necesaria si el texto es muy largo
                    GUILayout.Height(Mathf.Max(fixedHeight, textHeight + 20))
                );
                if (EditorGUI.EndChangeCheck())
                {
                    taskDatabase.ProjectNotes = newNotes;
                }
            }
            else
            {
                // MODO VISUALIZACIÓN (SÓLO LECTURA)
                if (string.IsNullOrEmpty(currentNotes))
                {
                    GUIStyle emptyStyle = new(EditorStyles.label) { fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
                    EditorGUILayout.Space(fixedHeight / 2 - 10);
                    GUILayout.Label("Haga clic en 'Editar' para agregar notas.", emptyStyle);
                }
                else
                {
                    // Usar el MarkdownRenderer directamente. El scroll es manejado por projectNotesScroll.
                    Utils.MarkdownRenderer.DrawFormattedMarkdown(currentNotes);
                }
            }

            EditorGUILayout.EndScrollView(); // FIN DEL SCROLL VIEW DE NOTAS
            EditorGUILayout.EndVertical(); // FIN DEL CONTENEDOR VISUAL DE NOTAS

            // Botones de Acción
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.Space(5);

            if (!isNotesEditing)
            {
                if (GUILayout.Button("✏️ Editar", GUILayout.ExpandWidth(true), GUILayout.MinWidth(200), GUILayout.MaxWidth(600), GUILayout.Height(30)))
                {
                    isNotesEditing = true;
                    projectNotesScroll = Vector2.zero; // Resetear scroll al editar
                }
            }
            else
            {
                if (GUILayout.Button("💾 Guardar", GUILayout.ExpandWidth(true), GUILayout.MinWidth(60), GUILayout.MaxWidth(300), GUILayout.Height(30)))
                {
                    SaveTaskDatabase();
                    isNotesEditing = false;
                    Repaint();
                }

                EditorGUILayout.Space(5);

                if (GUILayout.Button("❌ Cancelar", GUILayout.ExpandWidth(true), GUILayout.MinWidth(60), GUILayout.MaxWidth(300), GUILayout.Height(30)))
                {
                    LoadTaskDatabase();
                    isNotesEditing = false;
                    Repaint();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            #endregion

            #region --- Progreso General del Proyecto ---
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("📊 Progreso General del Proyecto", EditorStyles.boldLabel);
            float totalProgress = totalTasks > 0 ? (float)totalCompleted / totalTasks : 0f;
            Rect r = GUILayoutUtility.GetRect(100, 20);
            EditorGUI.ProgressBar(r, totalProgress, $"{Mathf.RoundToInt(totalProgress * 100)}% Completado");

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("🔁 Actualizar Tareas"))
            {
                LoadTaskDatabase();
            }
            if (GUILayout.Button("Abrir Nexo: Task Manager", GUILayout.Height(25)))
            {
                TaskManagerWindow.ShowWindow();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
            #endregion
        }

        private void HandleSplitter()
        {
            GUILayout.Box(GUIContent.none, GUILayout.Width(3), GUILayout.ExpandHeight(true));

            if (Event.current.type == EventType.Repaint)
                splitterRect = GUILayoutUtility.GetLastRect();

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (splitterRect.Contains(Event.current.mousePosition))
                    {
                        draggingSplitter = true;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (draggingSplitter)
                    {
                        docsPanelWidth = Mathf.Clamp(Event.current.mousePosition.x, 250f, 600f);
                        Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    draggingSplitter = false;
                    break;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(0.4f, 0.4f, 0.4f);
            GUI.DrawTexture(splitterRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        private void DrawDocsPanel()
        {
            docPaths ??= new List<string>();

            // Panel de documentos
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(position.width - docsPanelWidth), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("📚 Documentos Transversales", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            docsScroll = EditorGUILayout.BeginScrollView(docsScroll);

            for (int i = 0; i < docPaths.Count; i++)
            {
                if (i < 0 || i >= docPaths.Count) { return; }

                string filePath = docPaths[i];
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(10, 10, 10, 10);
                boxStyle.margin = new RectOffset(0, 0, 5, 10);

                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.BeginHorizontal();

                GUIStyle nameStyle = new(EditorStyles.boldLabel)
                {
                    normal = { textColor = GUI.skin.label.normal.textColor },
                    hover = { textColor = GUI.skin.label.normal.textColor }
                };

                if (GUILayout.Button($"📄 {fileName}", nameStyle))
                {
                    selectedIndex = selectedIndex == i ? -1 : i;
                    isEditing = false;
                }

                GUILayout.FlexibleSpace();

                if (selectedIndex == i && isEditing == false && GUILayout.Button(new GUIContent("✏️", "Editar este documento")))
                {
                    if (File.Exists(filePath))
                    {
                        editorContent = File.ReadAllText(filePath);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Archivo no encontrado",
                            $"El archivo ya no existe:\n{filePath}", "OK");
                        RefreshDocs();
                        return;
                    }
                    isEditing = true;
                }

                if (selectedIndex == i && isEditing && GUILayout.Button(new GUIContent("💾", "Guardar")))
                {
                    if (EditorUtility.DisplayDialog("Guardar", $"¿Quiere guardar '{fileName}'?", "Sí", "Cancelar"))
                    {
                        File.WriteAllText(filePath, editorContent);
                        isEditing = false;
                        AssetDatabase.Refresh();
                    }
                }

                if (selectedIndex == i && isEditing && GUILayout.Button(new GUIContent("❌", "Cancelar")))
                {
                    isEditing = false;
                }

                EditorGUILayout.EndHorizontal();

                if (selectedIndex == i)
                {
                    if (isEditing)
                    {
                        docEditorScroll = EditorGUILayout.BeginScrollView(docEditorScroll, GUILayout.Height(200));

                        editorContent = EditorGUILayout.TextArea(
                            editorContent,
                            GUILayout.ExpandHeight(true),
                            GUILayout.ExpandWidth(true) // Permitimos la expansión completa dentro del ScrollView
                        );

                        EditorGUILayout.EndScrollView();
                    }
                    else
                    {
                        if (!File.Exists(filePath))
                        {
                            EditorGUILayout.HelpBox($"Archivo no encontrado:\n{filePath}", MessageType.Error);
                        }
                        else
                        {
                            string content = Utils.MarkdownRenderer.Preprocess(
                            File.ReadAllText(filePath),
                            DocVariablesDB.Variables[filePath],
                            false
                        );
                            EditorGUILayout.Space(5);
                            Utils.MarkdownRenderer.DrawFormattedMarkdown(content);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("🔄 Recargar Documentos"))
            {
                RefreshDocs();
            }

            if (GUILayout.Button("Abrir Koda: Docs Manager", GUILayout.Height(25)))
            {
                DocsManagerWindow.ShowWindow();
            }

            EditorGUILayout.EndVertical();
        }

    }
}