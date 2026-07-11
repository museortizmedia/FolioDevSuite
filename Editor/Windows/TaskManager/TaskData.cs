using UnityEngine;

namespace Folio.Editor.Windows {
    [System.Serializable]
    public class TaskData
    {
        public string Title;
        public string Description;
        public TaskState State;
        public string AssignedTo;
        [Range(0,100)] public int Progress;
    }

    public enum TaskState
    {
        ToDo,
        InProgress,
        Done
    }
}