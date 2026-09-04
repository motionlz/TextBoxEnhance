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
        /// Reads a float attribute written by name, accepting any of
        /// <paramref name="keys"/> as its spelling.
        /// </summary>
        public float GetFloat(float fallback, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (TryParseFloat(GetString(key), out float parsed))
                    return parsed;
            }

            return fallback;
        }

        /// <summary>
        /// Reads the effect's headline attribute, which is also what the
        /// <c>&lt;tag=value&gt;</c> shorthand sets. Only one attribute per effect may use
        /// this, or <c>&lt;wave=0.5&gt;</c> would set every attribute to 0.5 at once.
        /// </summary>
        public float GetPrimaryFloat(float fallback, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (TryParseFloat(GetString(key), out float parsed))
                    return parsed;
            }

            return TryParseFloat(Value, out float shorthand) ? shorthand : fallback;
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            if (!string.IsNullOrEmpty(raw))
            {
                return float.TryParse(raw, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out value);
            }

            value = 0f;
            return false;
        }

        /// <summary>
        /// Reads the effect's headline colour, which the <c>&lt;tag=value&gt;</c> shorthand
        /// also sets. Accepts named colours ("red") and hex with or without the "#".
        /// </summary>
        public Color GetPrimaryColor(string key, Color fallback)
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
