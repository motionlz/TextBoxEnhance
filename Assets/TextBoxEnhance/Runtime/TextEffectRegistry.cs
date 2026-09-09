using System;
using System.Collections.Generic;
using UnityEngine;

namespace TextBoxEnhance
{
    /// <summary>
    /// Maps rich-text tag names onto <see cref="TextEffect"/> instances.
    /// Populated by scanning loaded assemblies for <see cref="TextEffectTagAttribute"/>,
    /// so adding an effect means writing a class and nothing else.
    /// </summary>
    public static class TextEffectRegistry
    {
        private static Dictionary<string, TextEffect> s_Effects;
        private static HashSet<string> s_BuiltInTags;

        /// <summary>Tag names that drive the typewriter rather than the character mesh.</summary>
        internal static readonly HashSet<string> ControlTags =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "speed", "pause" };

        private static Dictionary<string, TextEffect> Effects
        {
            get
            {
                if (s_Effects == null)
                    Rebuild();
                return s_Effects;
            }
        }

        /// <summary>Every registered tag name, sorted. Used by the inspector's cheat sheet.</summary>
        public static IEnumerable<string> TagNames
        {
            get
            {
                var names = new List<string>(Effects.Keys);
                names.Sort(StringComparer.OrdinalIgnoreCase);
                return names;
            }
        }

        public static bool TryGet(string tag, out TextEffect effect)
        {
            return Effects.TryGetValue(tag, out effect);
        }

        /// <summary>True for any tag this package handles, effect or typewriter control.</summary>
        public static bool IsKnownTag(string tag)
        {
            return ControlTags.Contains(tag) || Effects.ContainsKey(tag);
        }

        /// <summary>
        /// Registers an effect at runtime, overriding any built-in of the same name.
        /// Only needed for effects that cannot carry the attribute, e.g. ones built
        /// from data at runtime.
        /// </summary>
        public static void Register(string tag, TextEffect effect)
        {
            if (string.IsNullOrEmpty(tag) || effect == null)
                throw new ArgumentException("A text effect needs a non-empty tag and a non-null effect.");

            Effects[tag] = effect;
        }

        /// <summary>True if a tag is claimed by a code effect carrying the attribute.</summary>
        public static bool IsBuiltIn(string tag)
        {
            if (s_Effects == null)
                Rebuild();

            return !string.IsNullOrEmpty(tag) && s_BuiltInTags.Contains(tag);
        }

        /// <summary>
        /// Throws away every registration, including ones added by
        /// <see cref="Register"/>. The next lookup rediscovers the code effects.
        /// Asset-backed effects have to be put back by whoever owns them.
        /// </summary>
        public static void Reset()
        {
            s_Effects = null;
            s_BuiltInTags = null;
        }

        private static void Rebuild()
        {
            s_Effects = new Dictionary<string, TextEffect>(StringComparer.OrdinalIgnoreCase);
            s_BuiltInTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException e)
                {
                    // A half-loaded assembly still tells us about the types it did load.
                    types = e.Types;
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type == null || type.IsAbstract || !typeof(TextEffect).IsAssignableFrom(type))
                        continue;

                    var attribute = (TextEffectTagAttribute)Attribute.GetCustomAttribute(type, typeof(TextEffectTagAttribute));
                    if (attribute == null || attribute.Tags == null || attribute.Tags.Length == 0)
                        continue;

                    TextEffect instance;
                    try
                    {
                        instance = (TextEffect)Activator.CreateInstance(type);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[TextBoxEnhance] Could not instantiate effect '{type.FullName}'. " +
                                       $"Effects need a public parameterless constructor. {e.Message}");
                        continue;
                    }

                    foreach (string tag in attribute.Tags)
                    {
                        if (string.IsNullOrEmpty(tag))
                            continue;

                        if (s_Effects.TryGetValue(tag, out TextEffect existing))
                        {
                            Debug.LogWarning($"[TextBoxEnhance] Tag '{tag}' is claimed by both " +
                                             $"'{existing.GetType().FullName}' and '{type.FullName}'. Keeping the first.");
                            continue;
                        }

                        s_Effects.Add(tag, instance);
                        s_BuiltInTags.Add(tag);
                    }
                }
            }
        }
    }
}
