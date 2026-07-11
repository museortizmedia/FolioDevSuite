using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Folio.Editor.Windows
{
    public class TaskManagerWindow : EditorWindow
    {
        private const string SAVE_PATH = "Assets/Folio/Resources/TaskManager/task_database.json";
        private const string DEFAULT_PATH = "Packages/com.folio.devsuite/Resources/TaskManager/task_database.json";

        private TaskDatabase taskDatabase = new();
        private Vector2 scroll;
        private bool isDirty = false;

        private Dictionary<string, bool> moduleFoldouts = new Dictionary<string, bool>();

        [MenuItem("Window/Folio/🐝 Nexo: Task Manager", false, 40)]
        public static void ShowWindow()
        {
            var window = GetWindow<TaskManagerWindow>("🐝 Nexo: Task Manager");
            window.minSize = new Vector2(500, 950);
            window.LoadData();
        }

        private void OnEnable() => LoadData();

        private void OnFocus()
        {
            if (EditorApplication.isCompiling) return;

            if (isDirty)
            {
                if (EditorUtility.DisplayDialog("Cambios no guardados",
                    "Hay cambios sin guardar. ¿Deseas descartar los cambios y recargar?", "Sí", "No"))
                {
                    LoadData();
                    isDirty = false;
                }
            }
            else
            {
                LoadData();
            }
        }

        private void OnDisable()
        {
            if (EditorApplication.isCompiling) return;

            if (isDirty)
            {
                if (EditorUtility.DisplayDialog("Cambios no guardados",
                    "¿Deseas guardar los cambios antes de salir?", "Sí", "No"))
                {
                    SaveData();
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("🧩 Task Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawUserList();
            EditorGUILayout.Space();
            DrawModules();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("🔄 Actualizar"))
            {
                if (isDirty)
                {
                    if (!EditorUtility.DisplayDialog(
                        "Cambios no guardados",
                        "Hay cambios sin guardar. ¿Deseas descartar los cambios y actualizar?",
                        "Sí", "No"))
                        return;
                }

                LoadData();
                isDirty = false;
            }
            if (GUILayout.Button("💾 Guardar Cambios", GUILayout.Height(30)))
            {
                SaveData();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawUserList()
        {
            EditorGUILayout.LabelField("👥 Usuarios del Proyecto", EditorStyles.boldLabel);

            for (int i = 0; i < taskDatabase.Users.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

                string newUser = EditorGUILayout.TextField(taskDatabase.Users[i], GUILayout.ExpandWidth(true));
                if (newUser != taskDatabase.Users[i]) isDirty = true;
                taskDatabase.Users[i] = newUser;

                if (GUILayout.Button("❌", GUILayout.Width(30)))
                {
                    taskDatabase.Users.RemoveAt(i);
                    isDirty = true;
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("➕ Agregar Usuario", GUILayout.ExpandWidth(true)))
            {
                taskDatabase.Users.Add("Nuevo Usuario");
                isDirty = true;
            }

            EditorGUILayout.Space(10);
        }

        private void DrawModules()
        {
            EditorGUILayout.LabelField("📦 Módulos del Proyecto", EditorStyles.boldLabel);
            List<ModuleData> modulesToRemove = new();

            for (int i = 0; i < taskDatabase.Modules.Count; i++)
            {
                var module = taskDatabase.Modules[i];

                // Asegurar ID único del módulo
                if (string.IsNullOrEmpty(module.Id))
                    module.Id = System.Guid.NewGuid().ToString();

                // Crear foldout si no existe
                if (!moduleFoldouts.ContainsKey(module.Id))
                    moduleFoldouts[module.Id] = true;

                EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));

                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                moduleFoldouts[module.Id] = EditorGUILayout.Foldout(
                    moduleFoldouts[module.Id],
                    "📂 " + module.ModuleName,
                    true
                );

                if (GUILayout.Button("❌", GUILayout.Width(30)))
                    modulesToRemove.Add(module);

                EditorGUILayout.EndHorizontal();

                // Si está expandido, mostrar contenido
                if (moduleFoldouts[module.Id])
                {
                    GUILayout.Space(10);
                    float oldLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 110f;
                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                    string newModuleName = EditorGUILayout.TextField(
                        "Nombre del Módulo",
                        module.ModuleName,
                        GUILayout.ExpandWidth(true)
                    );
                    if (newModuleName != module.ModuleName) isDirty = true;
                    module.ModuleName = newModuleName;

                    EditorGUILayout.LabelField("📝 Tareas:", EditorStyles.boldLabel);
                    EditorGUILayout.Space(10);

                    // LISTA DE TAREAS
                    for (int j = 0; j < module.Tasks.Count; j++)
                    {
                        var task = module.Tasks[j];

                        EditorGUILayout.BeginVertical("HelpBox", GUILayout.ExpandWidth(true));

                        string newTitle = EditorGUILayout.TextField(
                            "📄 Título",
                            task.Title,
                            GUILayout.ExpandWidth(true)
                        );
                        if (newTitle != task.Title) isDirty = true;
                        task.Title = newTitle;


                        GUIStyle textAreaStyle = GUI.skin.textArea;
                        float width = EditorGUIUtility.currentViewWidth - 50f;
                        float height = textAreaStyle.CalcHeight(new GUIContent(task.Description), width);
                        height = Mathf.Max(60f, height);
                        string newDesc = EditorGUILayout.TextArea(
                            task.Description,
                            textAreaStyle,
                            GUILayout.Width(width),
                            GUILayout.Height(height)
                        );
                        if (newDesc != task.Description)
                            isDirty = true;

                        task.Description = newDesc;


                        TaskState newState = (TaskState)EditorGUILayout.EnumPopup(
                            "🟣 Estado",
                            task.State,
                            GUILayout.ExpandWidth(true)
                        );
                        if (newState != task.State) isDirty = true;
                        task.State = newState;

                        int newProgress = (int)EditorGUILayout.Slider(
                            "🔵 Progreso",
                            task.Progress,
                            0,
                            100,
                            GUILayout.ExpandWidth(true)
                        );
                        if (newProgress != task.Progress) isDirty = true;
                        task.Progress = newProgress;

                        int userIndex = Mathf.Max(0, taskDatabase.Users.IndexOf(task.AssignedTo));
                        int newUserIndex = EditorGUILayout.Popup(
                            "👤 Encargado",
                            userIndex,
                            taskDatabase.Users.ToArray(),
                            GUILayout.ExpandWidth(true)
                        );

                        string newAssigned =
                            taskDatabase.Users.Count > newUserIndex
                            ? taskDatabase.Users[newUserIndex]
                            : "";

                        if (newAssigned != task.AssignedTo) isDirty = true;
                        task.AssignedTo = newAssigned;

                        if (GUILayout.Button("❌ Eliminar Tarea", GUILayout.ExpandWidth(true)))
                        {
                            module.Tasks.RemoveAt(j);
                            isDirty = true;
                            j--;
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                        GUILayout.Space(20);
                    }

                    if (GUILayout.Button("➕ Agregar Tarea", GUILayout.ExpandWidth(true)))
                    {
                        module.Tasks.Add(new TaskData
                        {
                            Title = "Nueva Tarea",
                            Description = "",
                            State = TaskState.ToDo
                        });

                        moduleFoldouts[module.Id] = true;
                        isDirty = true;
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
            }

            // Eliminar módulos marcados
            foreach (var m in modulesToRemove)
            {
                taskDatabase.Modules.Remove(m);
                isDirty = true;
                moduleFoldouts.Remove(m.Id);
            }

            if (GUILayout.Button("➕ Agregar Módulo", GUILayout.ExpandWidth(true)))
            {
                var newModule = new ModuleData { ModuleName = "Nuevo Módulo" };
                taskDatabase.Modules.Add(newModule);

                moduleFoldouts[newModule.Id] = true;
                isDirty = true;
            }

            EditorGUILayout.Space(10);
        }

        private void SaveData()
        {
            string directory = Path.GetDirectoryName(SAVE_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            string json = JsonUtility.ToJson(taskDatabase, true);
            File.WriteAllText(SAVE_PATH, json);

            Debug.Log("<color=yellow>[Folio-Nexo:]</color> ¡Tareas Guardadas!");

            AssetDatabase.Refresh();
            isDirty = false;
        }

        private void LoadData()
        {
            var previousStates = new Dictionary<string, bool>(moduleFoldouts);

            if (!File.Exists(SAVE_PATH))
            {
                if (File.Exists(DEFAULT_PATH))
                {
                    string directory = Path.GetDirectoryName(SAVE_PATH);
                    if (!Directory.Exists(directory)){ Directory.CreateDirectory(directory); }

                    File.Copy(DEFAULT_PATH, SAVE_PATH, true);
                    AssetDatabase.Refresh();
                    
                    Debug.Log("<color=yellow>[🐝 Folio-Nexo:]</color> Configuración inicial copiada desde el paquete a Assets.");
                }
                else
                {
                    Debug.LogError("<color=yellow>[🐝 Folio-Nexo:]</color> No se encontró el archivo de fábrica en: " + DEFAULT_PATH);
                }
            }

            if (File.Exists(SAVE_PATH))
            {
                string json = File.ReadAllText(SAVE_PATH);
                taskDatabase = JsonUtility.FromJson<TaskDatabase>(json);
                Debug.Log("<color=yellow>[🐝 Folio-Nexo:]</color> Base de datos cargada desde: " + SAVE_PATH);
            }
            else
            {
                taskDatabase = new TaskDatabase();
                Debug.LogWarning("<color=yellow>[🐝 Folio-Nexo:]</color> No se pudo cargar ni copiar la base de datos. Iniciando vacía.");
            }

            isDirty = false;

            // Restaurar foldouts
            moduleFoldouts.Clear();
            foreach (var module in taskDatabase.Modules)
            {
                if (string.IsNullOrEmpty(module.Id))
                    module.Id = System.Guid.NewGuid().ToString();

                if (previousStates.TryGetValue(module.Id, out bool state))
                    moduleFoldouts[module.Id] = state;
                else
                    moduleFoldouts[module.Id] = false;
            }
        }
    }
}