using System;
using System.Collections.Generic;

namespace Folio.Editor.Windows.DocManager.DocVariables
{
    [System.Serializable]
    public class DocVariable
    {
        public string name;
        public DocVariableType type;
        public string stringValue;
        public int intValue;
        public float floatValue;
        public bool boolValue;

        private const string StartTag = "<!-- DOCVARS";
        private const string EndTag = "DOCVARS -->";

        // -----------------------------
        //   PARSEAR VARIABLES DESDE MD
        // -----------------------------
        public static List<DocVariable> ExtractVariables(string mdText)
        {
            List<DocVariable> vars = new();

            int start = mdText.IndexOf(StartTag);
            int end = mdText.IndexOf(EndTag);

            if (start < 0 || end < 0) return vars;

            string raw = mdText.Substring(start + StartTag.Length, end - (start + StartTag.Length));

            string[] lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Ejemplo:
                // name: score; type: Int; value: 30
                if (!line.Contains("name:")) continue;

                DocVariable var = new DocVariable();
                string[] parts = line.Split(';');

                foreach (string part in parts)
                {
                    string[] kv = part.Split(':', 2);

                    if (kv.Length < 2) continue;

                    string key = kv[0].Trim();
                    string val = kv[1].Trim();

                    switch (key)
                    {
                        case "name": var.name = val; break;
                        case "type": var.type = Enum.Parse<DocVariableType>(val); break;
                        case "value":
                            switch (var.type)
                            {
                                case DocVariableType.String: var.stringValue = val.Trim('"'); break;
                                case DocVariableType.Int: int.TryParse(val, out var.intValue); break;
                                case DocVariableType.Float:
                                    float.TryParse(val.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var.floatValue); 
                                    break;
                                case DocVariableType.Bool: bool.TryParse(val, out var.boolValue); break;
                            }
                            break;
                    }
                }

                vars.Add(var);
            }

            return vars;
        }

        // -----------------------------
        //  GENERAR TEXTO PARA GUARDAR
        // -----------------------------
        public static string InjectVariables(List<DocVariable> vars, string mdText)
        {
            // Remover panel anterior si existe
            int start = mdText.IndexOf(StartTag);
            int end = mdText.IndexOf(EndTag);

            if (start >= 0 && end >= 0)
            {
                mdText = mdText.Remove(start, (end + EndTag.Length) - start);
            }

            mdText = mdText.TrimStart();

            // Construir bloque nuevo
            System.Text.StringBuilder sb = new();
            sb.AppendLine(StartTag);

            foreach (var v in vars)
            {
                sb.Append("name: ").Append(v.name).Append("; ");
                sb.Append("type: ").Append(v.type).Append("; ");
                sb.Append("value: ");

                sb.Append(v.type switch
                {
                    DocVariableType.String => $"\"{v.stringValue}\"",
                    DocVariableType.Int => v.intValue.ToString(),
                    DocVariableType.Float => v.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    DocVariableType.Bool => v.boolValue.ToString().ToLower(),
                    _ => ""
                });

                sb.AppendLine();
            }

            sb.AppendLine(EndTag);
            sb.AppendLine();

            return sb + mdText;
        }
    }

    public enum DocVariableType
    {
        String,
        Int,
        Float,
        Bool
    }
}