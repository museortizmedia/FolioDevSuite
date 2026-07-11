using System.Collections.Generic;
using System.IO;
using Folio.Editor.Windows.DocManager.DocVariables;

public static class DocVariablesDB
{
    public static Dictionary<string, List<DocVariable>> Variables { get; private set; }
        = new Dictionary<string, List<DocVariable>>();

    public static void Set(string filePath, List<DocVariable> vars)
    {
        Variables[filePath] = vars;
    }

    public static List<DocVariable> GetAll()
    {
        List<DocVariable> list = new List<DocVariable>();

        foreach (var kvp in Variables)
            list.AddRange(kvp.Value);

        return list;
    }

    public static void LoadAll(string docsFolderPath)
{
    Variables.Clear();

    if (!Directory.Exists(docsFolderPath))
        return;

    string[] files = Directory.GetFiles(docsFolderPath, "*.md", SearchOption.AllDirectories);

    foreach (string file in files)
    {
        string md = File.ReadAllText(file);
        var vars = DocVariable.ExtractVariables(md);

        Set(file, vars);
    }
}

}