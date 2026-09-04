using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TextBoxEnhance
{
    /// <summary>
    /// Pulls this package's tags out of a string and records the character ranges they
    /// cover, leaving TextMeshPro's own rich-text tags in place for TMP to handle.
    /// </summary>
    /// <remarks>
    /// Ranges are expressed in TextMeshPro character indices, so the parser has to
    /// predict how many characters TMP will emit: rich-text tags produce none, except
    /// <c>&lt;br&gt;</c> and <c>&lt;sprite&gt;</c> which produce one each.
    /// </remarks>
    public static class TextEffectParser
    {
        private struct OpenTag
        {
            public string Name;
            public TagParams Parameters;
            public int Start;
        }

        private static readonly StringBuilder s_Builder = new StringBuilder(256);
        private static readonly List<OpenTag> s_OpenTags = new List<OpenTag>(8);

        public static void Parse(string source, ParsedText result)
        {
            result.Clear();
            s_Builder.Clear();
            s_OpenTags.Clear();

            if (string.IsNullOrEmpty(source))
                return;

            int charCount = 0;
            int i = 0;

            while (i < source.Length)
            {
                char c = source[i];
                if (c != '<')
                {
                    s_Builder.Append(c);
                    charCount++;
                    i++;
                    continue;
                }

                int close = source.IndexOf('>', i + 1);
                if (close < 0 || !TryReadTagName(source, i + 1, close, out string name, out bool isClosing, out int bodyStart))
                {
                    // Not a tag at all - a stray '<' the author meant literally.
                    s_Builder.Append(c);
                    charCount++;
                    i++;
                    continue;
                }

                // <noparse> hands its contents to TMP verbatim, so nothing inside is a tag.
                if (!isClosing && name.Equals("noparse", StringComparison.OrdinalIgnoreCase))
                {
                    i = CopyNoParseBlock(source, i, close, ref charCount);
                    continue;
                }

                if (!TextEffectRegistry.IsKnownTag(name))
                {
                    // A TextMeshPro tag (or something that looks like one): pass it through
                    // untouched and account for the characters TMP will emit from it.
                    s_Builder.Append(source, i, close - i + 1);
                    if (!isClosing && (name.Equals("br", StringComparison.OrdinalIgnoreCase)
                                       || name.Equals("sprite", StringComparison.OrdinalIgnoreCase)))
                        charCount++;

                    i = close + 1;
                    continue;
                }

                if (isClosing)
                    CloseTag(name, charCount, result);
                else
                    OpenOrApply(name, source, bodyStart, close, charCount, result);

                i = close + 1;
            }

            // Tags the author never closed simply run to the end of the text.
            for (int t = 0; t < s_OpenTags.Count; t++)
                Emit(s_OpenTags[t], charCount, result);
            s_OpenTags.Clear();

            result.Text = s_Builder.ToString();
            s_Builder.Clear();
        }

        /// <summary>
        /// Reads the tag name out of <c>source[start..end)</c>, rejecting anything that
        /// does not look like a rich-text tag so stray '&lt;' characters stay literal.
        /// </summary>
        private static bool TryReadTagName(string source, int start, int end, out string name, out bool isClosing, out int bodyStart)
        {
            name = null;
            isClosing = false;
            bodyStart = end;

            if (start >= end)
                return false;

            if (source[start] == '/')
            {
                isClosing = true;
                start++;
            }

            if (start >= end)
                return false;

            // <#FF8800> is TextMeshPro's colour shorthand. It has no name, but it is a
            // real tag: hand it through as one so it is not counted as visible text.
            if (source[start] == '#')
            {
                name = "#";
                bodyStart = end;
                return true;
            }

            int nameEnd = start;
            while (nameEnd < end && source[nameEnd] != '=' && source[nameEnd] != ' ')
                nameEnd++;

            if (nameEnd == start)
                return false;

            // Every rich-text tag starts with a letter, so requiring one keeps "a <3 b>"
            // literal instead of silently swallowing it and throwing the count off.
            char first = source[start];
            if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z')))
                return false;

            for (int k = start + 1; k < nameEnd; k++)
            {
                char c = source[k];
                bool valid = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                             || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!valid)
                    return false;
            }

            name = source.Substring(start, nameEnd - start);
            bodyStart = nameEnd;
            return true;
        }

        /// <summary>Copies a &lt;noparse&gt; block through verbatim, counting its characters.</summary>
        private static int CopyNoParseBlock(string source, int tagStart, int tagClose, ref int charCount)
        {
            const string closeTag = "</noparse>";
            int contentStart = tagClose + 1;
            int contentEnd = source.IndexOf(closeTag, contentStart, StringComparison.OrdinalIgnoreCase);

            if (contentEnd < 0)
            {
                // Unterminated: everything that follows is literal text.
                s_Builder.Append(source, tagStart, source.Length - tagStart);
                charCount += source.Length - contentStart;
                return source.Length;
            }

            s_Builder.Append(source, tagStart, contentEnd + closeTag.Length - tagStart);
            charCount += contentEnd - contentStart;
            return contentEnd + closeTag.Length;
        }

        private static void OpenOrApply(string name, string source, int bodyStart, int close, int charCount, ParsedText result)
        {
            TagParams parameters = ReadParameters(source, bodyStart, close);

            // <pause> is a moment in time, not a span, so it never goes on the stack.
            if (name.Equals("pause", StringComparison.OrdinalIgnoreCase))
            {
                result.Pauses.Add(new PauseMarker
                {
                    CharIndex = charCount,
                    Seconds = Mathf.Max(0f, parameters.GetPrimaryFloat(0.5f, "t", "time", "seconds")),
                });
                return;
            }

            s_OpenTags.Add(new OpenTag { Name = name, Parameters = parameters, Start = charCount });
        }

        private static void CloseTag(string name, int charCount, ParsedText result)
        {
            for (int t = s_OpenTags.Count - 1; t >= 0; t--)
            {
                if (!s_OpenTags[t].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;

                Emit(s_OpenTags[t], charCount, result);
                s_OpenTags.RemoveAt(t);
                return;
            }

            // A closing tag with no opener is harmless; ignore it rather than log every frame.
        }

        private static void Emit(OpenTag tag, int end, ParsedText result)
        {
            if (end <= tag.Start)
                return;

            if (tag.Name.Equals("speed", StringComparison.OrdinalIgnoreCase))
            {
                result.Speeds.Add(new SpeedRange
                {
                    Start = tag.Start,
                    End = end,
                    Multiplier = Mathf.Max(0.01f, tag.Parameters.GetPrimaryFloat(1f, "s", "speed", "x")),
                });
                return;
            }

            if (TextEffectRegistry.TryGet(tag.Name, out TextEffect effect))
            {
                result.Effects.Add(new EffectRange
                {
                    Effect = effect,
                    Parameters = tag.Parameters,
                    Start = tag.Start,
                    End = end,
                });
            }
        }

        /// <summary>Reads <c>=value</c> and any <c>key=value</c> attributes from a tag body.</summary>
        private static TagParams ReadParameters(string source, int start, int end)
        {
            if (start >= end)
                return TagParams.Empty;

            var parameters = new TagParams();
            int i = start;

            if (source[i] == '=')
            {
                i++;
                parameters.Value = ReadValue(source, ref i, end);
            }

            while (i < end)
            {
                while (i < end && source[i] == ' ')
                    i++;

                int keyStart = i;
                while (i < end && source[i] != '=' && source[i] != ' ')
                    i++;

                if (keyStart == i)
                    break;

                string key = source.Substring(keyStart, i - keyStart);
                if (i < end && source[i] == '=')
                {
                    i++;
                    parameters.Set(key, ReadValue(source, ref i, end));
                }
                else
                {
                    // A bare word is a flag, e.g. <shake smooth>.
                    parameters.Set(key, "true");
                }
            }

            return parameters;
        }

        private static string ReadValue(string source, ref int i, int end)
        {
            if (i < end && (source[i] == '"' || source[i] == '\''))
            {
                char quote = source[i++];
                int valueStart = i;
                while (i < end && source[i] != quote)
                    i++;

                string quoted = source.Substring(valueStart, i - valueStart);
                if (i < end)
                    i++;
                return quoted;
            }

            int plainStart = i;
            while (i < end && source[i] != ' ')
                i++;

            return source.Substring(plainStart, i - plainStart);
        }
    }
}
