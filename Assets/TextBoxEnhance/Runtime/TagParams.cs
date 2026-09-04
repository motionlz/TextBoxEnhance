using System.Collections.Generic;
using UnityEngine;

namespace TextBoxEnhance
{
    /// <summary>
    /// The attributes written on an effect tag, e.g. <c>&lt;wave a=0.3 f=8&gt;</c>.
    /// Also carries the shorthand value of <c>&lt;wave=0.3&gt;</c> in <see cref="Value"/>.
    /// </summary>
    public sealed class TagParams
    {
        /// <summary>Shared instance handed to effects on tags that carry no attributes.</summary>
        public static readonly TagParams Empty = new TagParams();

        private Dictionary<string, string> m_Values;

        /// <summary>The shorthand value from <c>&lt;tag=value&gt;</c>, or null.</summary>
        public string Value { get; internal set; }

        internal void Set(string key, string value)
        {
            m_Values ??= new Dictionary<string, string>(4, System.StringComparer.OrdinalIgnoreCase);
            m_Values[key] = value;
        }

        public bool Has(string key)
        {
            return m_Values != null && m_Values.ContainsKey(key);
        }

        public string GetString(string key, string fallback = null)
        {
            return m_Values != null && m_Values.TryGetValue(key, out string v) ? v : fallback;
        }

        /// <summary>
        /// Reads a float attribute, accepting any of <paramref name="keys"/> as its name and
        /// falling back to the <c>&lt;tag=value&gt;</c> shorthand when none is present.
        /// </summary>
        public float GetFloat(float fallback, params string[] keys)
        {
            foreach (string key in keys)
            {
                string raw = GetString(key);
                if (raw != null && float.TryParse(raw, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float parsed))
                    return parsed;
            }

            if (Value != null && float.TryParse(Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float shorthand))
                return shorthand;

            return fallback;
        }

        /// <summary>
        /// Reads a colour attribute, falling back to the <c>&lt;tag=value&gt;</c> shorthand.
        /// Accepts named colours ("red"), and hex with or without the leading "#".
        /// </summary>
        public Color GetColor(string key, Color fallback)
        {
            string raw = GetString(key) ?? Value;
            if (string.IsNullOrEmpty(raw))
                return fallback;

            if (ColorUtility.TryParseHtmlString(raw, out Color parsed))
                return parsed;

            return ColorUtility.TryParseHtmlString("#" + raw, out parsed) ? parsed : fallback;
        }

        internal void Reset()
        {
            Value = null;
            m_Values?.Clear();
        }
    }
}
