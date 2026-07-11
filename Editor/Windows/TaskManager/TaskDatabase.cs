using System.Collections.Generic;

namespace Folio.Editor.Windows {
    [System.Serializable]
    public class TaskDatabase
    {
        public List<string> Users = new();
        public List<ModuleData> Modules = new();
        public string ActiveModuleId;
        public string ProjectNotes = "";
    }
}