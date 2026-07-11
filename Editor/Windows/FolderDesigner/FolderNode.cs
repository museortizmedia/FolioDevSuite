using System;
using System.Collections.Generic;
using UnityEngine;

namespace Folio.Editor.Windows {
    [System.Serializable]
    public class FolderNode
    {
        public string Name;
        public string Description;
        public Color FolderColor = Color.white;
        public List<string> AllowedExtensions = new();
        public bool IsExpanded;

        [SerializeReference]
        public List<FolderNode> Children = new();

        public FolderNode(string name)
        {
            Name = name;
            Description = "";
            Children = new List<FolderNode>();
            IsExpanded = true;
        }
    }
}