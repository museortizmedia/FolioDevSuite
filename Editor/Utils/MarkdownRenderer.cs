using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Folio.Editor.Windows.DocManager.DocVariables;
using UnityEditor;
using UnityEngine;

namespace Folio.Editor.Utils
{
    public static class MarkdownRenderer
    {
        // Códigos de color para Rich Text
        private const string VariableColor = "#00cc00";
        private const string LinkedVariableColor = "#0088ff";

        // Colores base (Ajustados para consistencia)
        private static readonly Color LightModeParagraphColor = new(0.2f, 0.2f, 0.2f); // Gris Oscuro
        private static readonly Color DarkModeParagraphColor = new(0.9f, 0.9f, 0.9f);  // Blanco Suave

        // Colores para encabezados
        private static readonly Color LightModeHeaderBlue = new(0.17f, 0.51f, 1f); // Azul brillante
        private static readonly Color DarkModeHeaderBlue = new(0.45f, 0.68f, 1f);  // Azul suave

        // Colores para encabezados (H4-H6)
        private static readonly Color LightModeHeaderTextColor = Color.black; // Negro para Light Mode
        private static readonly Color DarkModeHeaderTextColor = Color.white * 0.9f; // Blanco/Gris claro para Dark Mode
        private static readonly Color LinkColor = new(0.16f, 0.5f, 0.7f);
        private static bool isInsideCodeBlock = false;

        // Colores para el bloque de código
        private static readonly Color CodeBlockBGColorLight = new(0.9f, 0.9f, 0.9f); // Fondo muy claro
        private static readonly Color CodeBlockBGColorDark = new(0.18f, 0.18f, 0.18f); // Fondo gris oscuro
        private static readonly Color CodeBlockTextColorLight = Color.black;
        private static readonly Color CodeBlockTextColorDark = new(0.9f, 0.9f, 0.9f);
        private static GUIStyle codeBlockStyle = null; // Estilo para el texto dentro del bloque

        // Variables de color para Blockquote
        private static readonly Color BlockquoteBGColorLight = new(0.9f, 0.9f, 0.9f); // Gris claro
        private static readonly Color BlockquoteBGColorDark = new(0.2f, 0.2f, 0.2f); // Gris oscuro
        private static readonly Color BlockquoteLineColorLight = new(0.5f, 0.5f, 0.5f); // Línea vertical gris
        private static readonly Color BlockquoteLineColorDark = new(0.4f, 0.4f, 0.4f); // Línea vertical gris
        private static GUIStyle blockquoteStyle = null;

        // Variables Table
        private static readonly Color lightHeader = new(0.85f, 0.85f, 0.85f);
        private static readonly Color darkHeader = new(0.25f, 0.25f, 0.25f);
        private static readonly Color lightRowA = new(0.95f, 0.95f, 0.95f);
        private static readonly Color lightRowB = new(0.90f, 0.90f, 0.90f);
        private static readonly Color darkRowA = new(0.18f, 0.18f, 0.18f);
        private static readonly Color darkRowB = new(0.22f, 0.22f, 0.22f);


        // Métodos de Renderizado de Markdown
        public static void DrawFormattedMarkdown(string markdown, bool isLightMode = true, List<DocVariable> vars = null)
        {
            if (string.IsNullOrEmpty(markdown)) return;

            // 1. Preprocesamiento (Aplicar valores de las variables)
            if (vars != null)
            {
                markdown = Preprocess(markdown, vars, true);
            }

            // 2. Procesamiento inicial (Resaltado de Variables diferenciando tablas de texto normal)
            string highlightedContent = HighlightVariables(markdown);

            // 3. Renderizado Línea por Línea
            var lines = highlightedContent.Split('\n');
            GUIStyle baseRichStyle = GetBaseRichStyle(isLightMode);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line)) { EditorGUILayout.Space(4); continue; }

                string trimmedLine = line.Trim();

                // Detectar tabla
                if (IsTableRow(trimmedLine))
                {
                    List<string[]> rows = new List<string[]>();
                    rows.Add(ParseTableRow(trimmedLine));

                    int j = i + 1;
                    while (j < lines.Length)
                    {
                        string nextTrimmedLine = lines[j].Trim();
                        if (IsTableRow(nextTrimmedLine))
                        {
                            rows.Add(ParseTableRow(nextTrimmedLine));
                            j++;
                        }
                        else { break; }
                    }

                    DrawTable(rows, isLightMode);

                    i = j - 1;
                    continue;
                }

                DrawFormattedLine(line, isLightMode, baseRichStyle); // Normal: procesar línea markdown
            }
        }
        public static void DrawFormattedMarkdown(string markdown)
        {
            bool isLightMode = !EditorGUIUtility.isProSkin;
            DrawFormattedMarkdown(markdown, isLightMode, null);
        }

        #region Preprocessing
        public static string Preprocess(string markdown, List<DocVariable> vars, bool keepSyntax = true)
        {
            if (string.IsNullOrEmpty(markdown) || vars == null)
                return markdown;

            // 1. Eliminar bloque DOCVARS si existe
            markdown = Regex.Replace(
                markdown,
                @"<!--\s*DOCVARS[\s\S]*?-->",
                "",
                RegexOptions.Multiline
            ).TrimStart();

            // 2. Procesar cada variable registrada en la lista
            for (int i = 0; i < vars.Count; i++)
            {
                var v = vars[i];

                string value = v.type switch
                {
                    DocVariableType.String => v.stringValue,
                    DocVariableType.Int => v.intValue.ToString(),
                    DocVariableType.Float => v.floatValue.ToString(),
                    DocVariableType.Bool => v.boolValue ? "true" : "false",
                    _ => ""
                };

                // Sustituir por NOMBRE ($$ y $)
                if (!string.IsNullOrEmpty(v.name))
                {
                    // Variables vinculadas ($$)
                    string linkedTag = "$$[" + v.name + "]";
                    string linkedRep = keepSyntax ? "$$[" + value + "]" : value;
                    markdown = markdown.Replace(linkedTag, linkedRep);

                    // Variables locales ($)
                    string localTag = "$[" + v.name + "]";
                    string localRep = keepSyntax ? "$[" + value + "]" : value;
                    markdown = markdown.Replace(localTag, localRep);
                }

                // Sustituir por ÍNDICE 1-based ($$ y $)
                int index = i + 1;

                // Vinculadas ($$) por índice
                string linkedIdxTag = "$$[" + index + "]";
                string linkedIdxRep = keepSyntax ? "$$[" + value + "]" : value;
                markdown = markdown.Replace(linkedIdxTag, linkedIdxRep);

                // Locales ($) por índice
                string localIdxTag = "$[" + index + "]";
                string localIdxRep = keepSyntax ? "$[" + value + "]" : value;
                markdown = markdown.Replace(localIdxTag, localIdxRep);
            }

            return markdown;
        }
        private static string HighlightVariables(string markdown)
        {
            var lines = markdown.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();

                if (IsTableRow(trimmedLine))
                {
                    // EN TABLA: Limpiar sintaxis $[...] y $$[...] dejando solo el valor en texto plano
                    line = Regex.Replace(line, @"\$\$\[(.*?)\]", "$1");
                    line = Regex.Replace(line, @"(?<!\$)\$\[(.*?)\]", "$1");
                }
                else
                {
                    // EN LÍNEA NORMAL: Mostrar solo el valor formateado/resaltado sin corchetes
                    // 2.1. Variables Vinculadas $$[...] en AZUL
                    line = Regex.Replace(
                        line,
                        @"\$\$\[(.*?)\]",
                        match => $"<color={LinkedVariableColor}>{match.Groups[1].Value}</color>",
                        RegexOptions.Singleline
                    );

                    // 2.2. Variables Mutables $[...] en VERDE
                    line = Regex.Replace(
                        line,
                        @"(?<!\$)\$\[(.*?)\]",
                        match => $"<color={VariableColor}>{match.Groups[1].Value}</color>",
                        RegexOptions.Singleline
                    );
                }

                lines[i] = line;
            }

            return string.Join("\n", lines);
        }
        #endregion
        
        #region Processing
        // Analiza las lineas buscando claves Markdown para renderizarlas con el estilo correspondiente
        private static void DrawFormattedLine(string line, bool isLightMode, GUIStyle baseRichStyle)
        {
            // --- FENCE CODE BLOCK ---
            if (line.Trim().StartsWith("```"))
            {
                // Alternar el estado. Si estamos dentro, salimos; si estamos fuera, entramos.
                isInsideCodeBlock = !isInsideCodeBlock;
                // Agrega un poco de espacio antes o después de un bloque
                EditorGUILayout.Space(isInsideCodeBlock ? 2 : 8);
                return;
            }

            if (isInsideCodeBlock) // Si estamos dentro de un bloque de código
            {
                // Usar un GUILayout.TextArea para un estilo de entrada/fondo
                GUIStyle codeStyle = GetCodeBlockStyle(isLightMode);
                // Usa `line` (sin trim) para preservar la indentación, si es importante
                GUILayout.TextArea(line, codeStyle);
                return;
            }

            // --- BLOCKQUOTE ---
            if (line.StartsWith(">"))
            {
                // Contar el nivel de anidamiento
                int level = 0;
                string content = line;
                while (content.StartsWith(">"))
                {
                    level++;
                    // Eliminar el '>' y el espacio que le sigue
                    content = content.Length > 1 ? content.Substring(1).TrimStart() : "";
                }

                // 1. Aplicar formato inline (Negrita, Cursiva, etc.)
                // No necesitamos la URL aquí, así que usamos el descarte '_'.
                string parsedContent = ParseMarkdownInline(content, isLightMode, out _);

                // 2. Obtener el estilo
                GUIStyle quoteStyle = GetBlockquoteStyle(isLightMode);

                // 3. Dibujar el Blockquote usando la nueva función
                DrawBlockquote(parsedContent, quoteStyle, isLightMode, level);

                return;
            }

            // --- Línea horizontal (---) ---
            if (line == "---")
            {
                DrawHorizontalRule(isLightMode);
                return;
            }

            // --- Encabezados (#, ##, ...) ---
            for (int h = 6; h >= 1; h--)
            {
                string prefix = new string('#', h) + " ";
                if (line.StartsWith(prefix))
                {
                    string content = line[prefix.Length..];
                    GUIStyle headerStyle = GetHeaderStyle(h, isLightMode);

                    EditorGUILayout.LabelField(content, headerStyle);

                    // HR extra para H1
                    if (h == 1)
                    {
                        DrawHorizontalRule(isLightMode, 1f, 6);
                    }
                    return;
                }
            }

            // --- Imágenes ---
            var match = Regex.Match(line, @"!\[(.*?)\]\((.*?)\)");
            if (match.Success)
            {
                string alt = match.Groups[1].Value;
                string path = match.Groups[2].Value;

                // Separar título y tamaño
                string title = alt.Contains("|") ? alt.Split('|')[0] : alt;
                Vector2Int size = GetImageSizeFromTag(alt);

                if (!string.IsNullOrWhiteSpace(title))
                {
                    GUIStyle titleStyle = new(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.UpperLeft,
                        normal = { textColor = isLightMode ? LightModeParagraphColor : DarkModeParagraphColor }
                    };
                    GUILayout.Label(title, titleStyle);
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    GUILayout.Label("(Ruta de imagen vacía)", EditorStyles.helpBox);
                    return;
                }

                path = path.Replace("\\", "/");
                if (!path.StartsWith("Assets/"))
                {
                    GUILayout.Label($"(Ruta inválida: {path})", EditorStyles.helpBox);
                    return;
                }

                Texture2D image = LoadImage(path);

                if (image != null)
                {
                    float availableWidth = EditorGUIUtility.currentViewWidth - 50f;
                    float finalMaxWidth = Mathf.Min(size.x, availableWidth);
                    float aspectRatio = (float)image.width / image.height;
                    float finalMaxHeight = Mathf.Min(size.y, finalMaxWidth / aspectRatio);
                    if (image.width < finalMaxWidth)
                    {
                        finalMaxWidth = image.width;
                        finalMaxHeight = image.height;
                    }
                    GUILayout.Label(image,
                        GUILayout.Width(finalMaxWidth),
                        GUILayout.Height(finalMaxHeight)
                    );
                }
                else
                {
                    GUILayout.Label($"(Imagen no encontrada: {path})", EditorStyles.helpBox);
                }

                return;
            }

            // --- Checkboxes ---
            var taskMatch = Regex.Match(line, @"^[\-*]\s*\[([ x*])\]\s*(.*)");
            if (taskMatch.Success)
            {
                string status = taskMatch.Groups[1].Value.Trim(); // ' ', 'x', o '*'
                string content = taskMatch.Groups[2].Value.TrimStart();
                bool isChecked = status.Length > 0 && status != " ";

                DrawCheckboxLine(content, isChecked, baseRichStyle, isLightMode);
                return;
            }

            // --- Listas No ordenadas y Ordenadas ---
            if (DrawListLine(line, isLightMode, baseRichStyle))
            {
                return;
            }

            // --- Párrafo o Lista y Enlaces ---
            string parsed;
            parsed = ParseMarkdownInline(line, isLightMode, out string url);

            if (!string.IsNullOrEmpty(url))
            {
                Match linkMatch = Regex.Match(line, @"(.*?)\[(.+?)\]\((http[s]?://[^\s\)]+)\)(.*)");

                if (linkMatch.Success)
                {
                    string textBefore = linkMatch.Groups[1].Value;
                    string label = linkMatch.Groups[2].Value;
                    string urlToOpen = linkMatch.Groups[3].Value;
                    string textAfter = linkMatch.Groups[4].Value;

                    // 1. Obtener estilos sin margen/padding
                    GUIStyle textElementStyle = GetZeroMarginStyle(baseRichStyle);
                    GUIStyle linkButtonStyle = GetLinkButtonStyle();

                    // 2. Iniciar Layout Horizontal
                    float requiredHeight = baseRichStyle.CalcHeight(new GUIContent("A"), EditorGUIUtility.currentViewWidth);
                    EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(requiredHeight));

                    // 3. Dibujar el texto normal antes del enlace
                    if (!string.IsNullOrEmpty(textBefore))
                    {
                        GUILayout.Label(
                            ParseMarkdownInline(textBefore, isLightMode, out _),
                            textElementStyle,
                            GUILayout.ExpandWidth(false) // No expandir
                        );
                    }

                    // 4. Dibujar el botón (Solo el label del enlace)
                    if (GUILayout.Button($"<u>{label}</u>", linkButtonStyle, GUILayout.ExpandWidth(false)))
                    {
                        Application.OpenURL(urlToOpen);
                    }

                    // 5. Dibujar el texto normal después del enlace
                    if (!string.IsNullOrEmpty(textAfter))
                    {
                        GUILayout.Label(ParseMarkdownInline(textAfter, isLightMode, out _), textElementStyle);
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.EndHorizontal();
                    return;
                }

                EditorGUILayout.LabelField(parsed, baseRichStyle);
            }
            else
            {
                // Renderizado estándar de párrafo o lista
                EditorGUILayout.LabelField(parsed, baseRichStyle);
            }

        }

        // Lsitas
        private static bool DrawListLine(string line, bool isLightMode, GUIStyle baseRichStyle)
        {
            // 1. Regex para detectar cualquier marcador de lista y capturar la sangría
            // Usamos (\s*) para capturar los espacios iniciales
            var listMatch = Regex.Match(line, @"^(\s*)([\*\-]|(\d+\.))\s+(.*)");

            if (!listMatch.Success)
            {
                // Si la línea está vacía (solo espacios o nada), la omitimos.
                if (string.IsNullOrWhiteSpace(line))
                {
                    EditorGUILayout.Space(2);
                }
                return false;
            }

            string indentation = listMatch.Groups[1].Value;
            string marker = listMatch.Groups[2].Value;
            string content = listMatch.Groups[4].Value;

            // 1. CÁLCULO DEL NIVEL DE SANGRÍA
            // Si usaste 4 espacios para anidar (como en el ejemplo de Markdown):
            int indentLevel = indentation.Length / 4;

            // Fijo: 20px por nivel de sangría. Esto controla la posición horizontal.
            float indentWidth = indentLevel * 20f; 

            // 2. MODIFICAR MARCADOR Y APLICAR NEGRITA
            string displayMarker = marker switch
            {
                "*" or "-" => "•", // Viñeta para listas no ordenadas
                _ => marker,       // Marcador original (ej: 1., 2.) para listas ordenadas
            };

            // Aplicar negrita al marcador.
            displayMarker = $"<b>{displayMarker}</b>";

            // 3. APLICAR FORMATO INLINE AL CONTENIDO
            string parsedContent = ParseMarkdownInline(content, isLightMode, out _);
            
            // Estilo para el texto. Clonamos el estilo base y ajustamos el padding.
            // Para las listas anidadas, es mejor usar un estilo que no tenga margen horizontal
            // para que el GUILayout.Space tome el control total.
            GUIStyle contentStyle = new(baseRichStyle)
            {
                padding = new RectOffset(0, baseRichStyle.padding.right, baseRichStyle.padding.top, baseRichStyle.padding.bottom),
                margin = new RectOffset(0, 0, baseRichStyle.margin.top, baseRichStyle.margin.bottom),
            };

            // 4. DIBUJAR EL ELEMENTO CON SANGRÍA
            EditorGUILayout.BeginHorizontal();

            // A) Aplicar la sangría horizontal calculada. Esto empuja todo el contenido.
            if (indentWidth > 0)
            {
                GUILayout.Space(indentWidth);
            }

            // B) Dibujar el Marcador (viñeta/número en negrita)
            // Usamos GUILayout.ExpandWidth(false) para que solo ocupe el espacio necesario.
            GUILayout.Label($"{displayMarker} ", contentStyle, GUILayout.ExpandWidth(false));

            // C) Dibujar el Contenido de la Lista
            // Usamos GUILayout.ExpandWidth(true) para que el texto ocupe el resto del ancho disponible.
            GUILayout.Label(parsedContent, contentStyle, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();

            return true;
        }

        // Estilo para bloques de código
        private static GUIStyle GetCodeBlockStyle(bool isLightMode)
        {
            if (codeBlockStyle == null)
            {
                codeBlockStyle = new GUIStyle(EditorStyles.textField)
                {
                    wordWrap = false, // Es crucial para el código
                    richText = false, // Deshabilitar Rich Text dentro del código
                    padding = new RectOffset(6, 6, 6, 6)
                };
            }

            // El color de fondo y texto debe ser consistente con el tema
            codeBlockStyle.normal.background = MakeBackgroundTexture(1, 1, isLightMode ? CodeBlockBGColorLight : CodeBlockBGColorDark);
            codeBlockStyle.normal.textColor = isLightMode ? CodeBlockTextColorLight : CodeBlockTextColorDark;

            // Altura fija para evitar el auto-ajuste de altura al renderizar
            codeBlockStyle.fixedHeight = 0;

            return codeBlockStyle;
        }

        // Estilo para Blockquote
        private static GUIStyle GetBlockquoteStyle(bool isLightMode)
        {
            if (blockquoteStyle == null)
            {
                blockquoteStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    richText = true,
                    // 5 de padding izquierdo para que el texto NO esté pegado a la barra vertical
                    padding = new RectOffset(5, 5, 5, 5),
                    margin = new RectOffset(0, 0, 4, 4),
                };
            }

            Color bgColor = isLightMode ? BlockquoteBGColorLight : BlockquoteBGColorDark;

            blockquoteStyle.normal.background = MakeBackgroundTexture(1, 1, bgColor);
            blockquoteStyle.normal.textColor = isLightMode ? LightModeParagraphColor : DarkModeParagraphColor;

            return blockquoteStyle;
        }

        // Dibuja un Blockquote con una línea vertical y soporte para anidamiento
        private static void DrawBlockquote(string content, GUIStyle style, bool isLightMode, int level)
        {
            Color lineColor = isLightMode ? BlockquoteLineColorLight : BlockquoteLineColorDark;
            float lineHeightThickness = 3f;
            float lineSpacing = 5f; // Espacio entre la línea vertical y el texto

            // Margen horizontal adicional para anidamiento (>>)
            // Si level es 1, nestedIndent es 0. Si level es 2, es 15, etc.
            float nestedIndent = 15f * (level - 1);

            // El ancho total que la barra y el espacio de separación ocuparán horizontalmente
            float totalBarWidth = lineHeightThickness + lineSpacing;

            // 1. Iniciar el área con el estilo (fondo gris y padding)
            EditorGUILayout.BeginVertical(style);

            // 2. Usar BeginHorizontal para forzar la colocación lado a lado: [Sangría][Barra+Espacio][Texto]
            EditorGUILayout.BeginHorizontal();

            // A) Aplicar Sangría (Horizontal) por Anidamiento
            // Esto empuja todo el contenido de la cita (barra + texto) hacia la derecha
            if (nestedIndent > 0)
            {
                // Corregido: Guía al layout horizontalmente.
                GUILayout.Space(nestedIndent);
            }

            // B) Espacio y Línea Vertical

            // 3.1. Dibujar una caja invisible para obtener la posición exacta y reservar el ancho horizontal
            Rect barSpaceRect = GUILayoutUtility.GetRect(totalBarWidth, 0, GUILayout.Width(totalBarWidth), GUILayout.ExpandHeight(true));

            // 3.2. Dibujar la línea vertical dentro del espacio reservado
            Rect lineRect = new Rect(
                barSpaceRect.xMin,
                barSpaceRect.yMin,
                lineHeightThickness,
                barSpaceRect.height
            );
            EditorGUI.DrawRect(lineRect, lineColor);


            // C) Contenido del Texto
            // 3.3. Dibujar el contenido de la cita en el espacio restante.
            // El 'style' (GetBlockquoteStyle) ya maneja el richText y el color de texto.
            GUILayout.Label(content, style);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(2);
        }

        // Dibuja una línea horizontal como separador
        private static void DrawHorizontalRule(bool isLightMode, float height = 2f, int spacingAfter = 6)
        {
            Rect hrRect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            Color lineColor = isLightMode ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.5f, 0.5f, 0.5f, 0.4f);
            EditorGUI.DrawRect(hrRect, lineColor);
            EditorGUILayout.Space(spacingAfter);
        }

        // Estilo para Encabezados
        private static GUIStyle GetHeaderStyle(int level, bool isLightMode)
        {
            int fontSize;
            Color headerColor;

            // H1, H2, H3 (Color Azul)
            if (level >= 1 && level <= 3)
            {
                headerColor = isLightMode ? LightModeHeaderBlue : DarkModeHeaderBlue;
                fontSize = 18 - (level - 1);
            }
            // H4, H5, H6 (Color de texto de encabezado normal)
            else
            {
                headerColor = isLightMode ? LightModeHeaderTextColor : DarkModeHeaderTextColor;
                fontSize = 15 - (level - 4);
            }

            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = fontSize,
                wordWrap = true,
                normal = { textColor = headerColor }
            };
        }

        // Estilo sin margen/padding para elementos inline (usado en enlaces)
        private static GUIStyle GetZeroMarginStyle(GUIStyle baseStyle)
        {
            // Crea un estilo Label basado en el estilo base, pero anula padding y margin horizontal.
            return new GUIStyle(baseStyle)
            {
                padding = new RectOffset(0, 0, baseStyle.padding.top, baseStyle.padding.bottom),
                margin = new RectOffset(0, 0, baseStyle.margin.top, baseStyle.margin.bottom),
                stretchWidth = false // Crucial: no expandir a menos que sea el último elemento.
            };
        }

        private static GUIStyle GetLinkButtonStyle()
        {
            // Baseamos el estilo en EditorStyles.label que es neutro.
            GUIStyle linkStyle = new GUIStyle(EditorStyles.label)
            {
                // **Aplicar CERO Márgenes/Padding AHORA**
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                richText = true,
                wordWrap = true,
                stretchWidth = false,

                normal = {
                    textColor = LinkColor,
                    background = null
                },
                hover = {
                    textColor = LinkColor * 1.2f,
                    background = null
                },
                active = {
                    textColor = LinkColor * 0.8f,
                    background = null
                }
            };

            return linkStyle;
        }
        // Imagenes
        private static Texture2D LoadImage(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Vector2Int GetImageSizeFromTag(string alt)
        {
            int pipeIndex = alt.IndexOf('|');
            if (pipeIndex < 0)
                return new Vector2Int(300, 300); // default md

            string tag = alt[(pipeIndex + 1)..].Trim().ToLower();

            // --- NUEVA LÓGICA DE ANCHO COMPLETO ---
            if (tag == "full") return new Vector2Int(9999, 9999);

            // --- Tamaños abreviados ---
            if (tag == "sm") return new Vector2Int(150, 150);
            if (tag == "md") return new Vector2Int(300, 300);
            if (tag == "lg") return new Vector2Int(500, 500);

            // --- Formato ancho x alto: 300x200 ---
            if (tag.Contains("x"))
            {
                var parts = tag.Split('x');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int w) &&
                    int.TryParse(parts[1], out int h))
                {
                    return new Vector2Int(
                        Mathf.Clamp(w, 32, 2000),
                        Mathf.Clamp(h, 32, 2000)
                    );
                }
            }

            // --- Solo un número ---
            if (int.TryParse(tag, out int numeric))
            {
                numeric = Mathf.Clamp(numeric, 32, 2000);
                return new Vector2Int(numeric, numeric);
            }

            // Fallback
            return new Vector2Int(300, 300);
        }

        // Tablas
        private static bool IsTableRow(string line)
        {
            return line.StartsWith("|") && line.EndsWith("|");
        }

        private static string[] ParseTableRow(string row)
        {
            return row.Trim('|')
                      .Split('|')
                      .Select(c => c.Trim())
                      .ToArray();
        }

        private static void DrawTable(List<string[]> rows, bool isLightMode)
        {
            if (rows.Count == 0) return;

            // 1. Determinar el número máximo de columnas
            int maxCols = rows.Max(r => r.Length);
            float rightMargin = 50f;

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 4, 4),
                wordWrap = true, // Permite el ajuste de línea en los encabezados
                normal =
                {
                    textColor = isLightMode ? LightModeParagraphColor : DarkModeParagraphColor,
                    background = MakeBackgroundTexture(1, 1, isLightMode ? lightHeader : darkHeader)
                }
            };

            GUIStyle cellBaseStyle = new GUIStyle(EditorStyles.wordWrappedLabel) // wordWrappedLabel para altura automática
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 4, 4),
                // El color de fondo y texto se asigna dentro del bucle.
                normal = { textColor = isLightMode ? LightModeParagraphColor : DarkModeParagraphColor }
            };

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                EditorGUILayout.BeginHorizontal();

                bool isHeader = (r == 0) || (r == 1 && Regex.IsMatch(row[0], @"^[-:]+$")); // Heurística: la primera fila o la línea de alineación son el encabezado.
                bool even = (r % 2 == 0);

                Color rowBGColor;
                if (isHeader)
                {
                    rowBGColor = isLightMode ? lightHeader : darkHeader;
                }
                else
                {
                    rowBGColor = isLightMode
                        ? (even ? lightRowA : lightRowB)
                        : (even ? darkRowA : darkRowB);
                }

                GUI.backgroundColor = rowBGColor; // Aplicar el color de fondo a toda la fila (dibuja la caja)
                float availableWidth = EditorGUIUtility.currentViewWidth - rightMargin - 4f;
                EditorGUILayout.BeginHorizontal("box", GUILayout.MaxWidth(availableWidth));
                GUI.backgroundColor = Color.white; // Restaurar el color de fondo para el contenido

                for (int c = 0; c < maxCols; c++)
                {
                    string cellContent = c < row.Length ? row[c] : "";

                    GUIStyle currentStyle;
                    if (r == 0)
                    {
                        currentStyle = headerStyle;
                    }
                    else
                    {
                        currentStyle = cellBaseStyle;
                        currentStyle.normal.background = MakeBackgroundTexture(1, 1, rowBGColor);
                    }

                    if (r == 1 && Regex.IsMatch(cellContent, @"^[-:]+$")) continue;

                    GUILayout.Label(
                        ParseMarkdownInline(cellContent, isLightMode, out _),
                        currentStyle,
                        GUILayout.ExpandWidth(true),
                        GUILayout.MinWidth(80) // Mínimo de ancho para evitar colapsos
                    );
                }

                EditorGUILayout.EndHorizontal(); // Fin del 'box' (Fila)
                EditorGUILayout.EndHorizontal(); // Fin de la distribución de ancho
            }

            EditorGUILayout.Space(4);
        }

        // Checkboxes
        private static void DrawCheckboxLine(string content, bool isChecked, GUIStyle baseRichStyle, bool isLightMode)
        {
            float checkboxWidth = 18f;
            float textIndent = 2f;

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            // 1. Dibujar la casilla de verificación (Checkbox)
            Rect checkboxRect = GUILayoutUtility.GetRect(checkboxWidth, baseRichStyle.CalcHeight(new GUIContent("A"), 100) + baseRichStyle.padding.vertical, GUILayout.Width(checkboxWidth));

            // Centrar verticalmente el checkbox de 16x16
            float checkboxSize = 16f;
            checkboxRect.yMin += (checkboxRect.height - checkboxSize) / 2;
            checkboxRect.height = checkboxSize;
            checkboxRect.xMin += textIndent;

            EditorGUI.Toggle(checkboxRect, isChecked);

            // 2. Dibujar el contenido de la lista (Texto)
            string parsedContent = ParseMarkdownInline(content, isLightMode, out _);

            // Estilo para el texto. Puedes aplicarle un estilo tachado si está chequeado.
            GUIStyle textStyle = new(baseRichStyle);
            if (isChecked)
            {
                Color dimColor = baseRichStyle.normal.textColor * new Color(1f, 1f, 1f, 0.6f);
                textStyle.normal.textColor = dimColor;
            }

            GUILayout.Label(parsedContent, textStyle, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }
        #endregion

        #region Helpers
        private static GUIStyle GetBaseRichStyle(bool isLightMode)
        {
            Color baseTextColor = isLightMode ? LightModeParagraphColor : DarkModeParagraphColor;

            return new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                normal = { textColor = baseTextColor },
                hover = { textColor = baseTextColor },
                active = { textColor = baseTextColor }
            };
        }

        /// <summary>
        /// Reemplaza todas las instancias de un marcador con las etiquetas Rich Text de apertura y cierre.
        /// </summary>
        private static string ProcessInlineTag(string input, string marker, string openTag, string closeTag)
        {
            string output = input;
            bool toggle = true;
            while (output.Contains(marker))
            {
                int index = output.IndexOf(marker);
                if (index < 0) break;

                string tag = toggle ? openTag : closeTag;
                output = output.Remove(index, marker.Length).Insert(index, tag);
                toggle = !toggle;
            }
            return output;
        }

        public static string ParseMarkdownInline(string input, bool isLightMode, out string foundUrl)
        {
            string parsed = input;
            foundUrl = null;

            // colores
            string inlineCodeText = isLightMode ? ColorUtility.ToHtmlStringRGB(CodeBlockTextColorLight) : ColorUtility.ToHtmlStringRGB(CodeBlockTextColorDark);
            string inlineCodeBlock = !isLightMode ? ColorUtility.ToHtmlStringRGB(CodeBlockBGColorLight) : ColorUtility.ToHtmlStringRGB(CodeBlockBGColorDark);

            // Código en línea
            bool codeToggle = true;
            while (parsed.Contains("`"))
            {
                int index = parsed.IndexOf("`");
                if (index < 0) break;
                string tag = codeToggle ? $"<mark=#{inlineCodeBlock}55><color=#{inlineCodeText}DD>" : "</color></mark>";
                parsed = parsed.Remove(index, 1).Insert(index, tag);
                codeToggle = !codeToggle;
            }

            // Negrita y Cursiva
            parsed = ProcessInlineTag(parsed, "**", "<b>", "</b>");
            parsed = ProcessInlineTag(parsed, "__", "<b>", "</b>");
            parsed = ProcessInlineTag(parsed, "*", "<i>", "</i>");
            parsed = ProcessInlineTag(parsed, "_", "<i>", "</i>");

            // Enlaces
            Match linkMatch = Regex.Match(parsed, @"\[(.+?)\]\((http[s]?://[^\s\)]+)\)");
            if (linkMatch.Success)
            {
                string label = linkMatch.Groups[1].Value;
                string url = linkMatch.Groups[2].Value;

                foundUrl = url;
                string hyperlink = $"<color=#{ColorUtility.ToHtmlStringRGB(LinkColor)}><u>{label}</u></color>";
                parsed = parsed.Replace(linkMatch.Value, hyperlink);
            }

            return parsed;
        }

        // Helper para crear una textura de fondo de un solo color
        private static Texture2D MakeBackgroundTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // Visor Selecionable
        private static GUIStyle GetSelectableLabelStyle(GUIStyle baseStyle)
        {
            GUIStyle selectable = new GUIStyle(baseStyle)
            {
                wordWrap = true,
                richText = true
            };
            return selectable;
        }
        #endregion

    }
}