using System.Collections.Generic;

namespace Folio.Editor.Windows
{
    [System.Serializable]
    public class ModuleData
    {
        public string Id;
        public string ModuleName;
        public bool Completed;
        public List<TaskData> Tasks = new();

        public ModuleData()
        {
            if (string.IsNullOrEmpty(Id))
                Id = System.Guid.NewGuid().ToString();
        }
    }
}