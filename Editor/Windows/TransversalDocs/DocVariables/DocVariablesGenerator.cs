using UnityEditor;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Folio.Editor.Windows.DocManager.DocVariables;

public static class DocVariablesGenerator
{
    public static void GenerateRuntimeClass(List<DocVariable> vars)
    {
        string folder = "Assets/Folio/Generated";
        string path = folder + "/DocVariables.cs";

        // Crear carpeta si no existe
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        System.Text.StringBuilder sb = new();

        sb.AppendLine("// ==================================================");
        sb.AppendLine("// Folio: Dev Suie. Indie Project Maanger Suite");
        sb.AppendLine("// AUTO-GENERATED FILE – DO NOT EDIT MANUALLY");
        sb.AppendLine("// Created by MuseOrtiz (museortiz@gmail.com)");
        sb.AppendLine("// ==================================================");
        sb.AppendLine();
        sb.AppendLine("public static class DocVariables");
        sb.AppendLine("{");

        foreach (var v in vars)
        {
            string type = v.type switch
            {
                DocVariableType.String => "string",
                DocVariableType.Int => "int",
                DocVariableType.Float => "float",
                DocVariableType.Bool => "bool",
                _ => "string"
            };

            string value = v.type switch
            {
                DocVariableType.String => $"\"{v.stringValue}\"",
                DocVariableType.Int => v.intValue.ToString(),
                DocVariableType.Float => $"{v.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}f",
                DocVariableType.Bool => v.boolValue.ToString().ToLower(),
                _ => "\"\""
            };


            sb.AppendLine($"    public const {type} {v.name} = {value};");
        }

        sb.AppendLine("}");

        File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();
    }
}