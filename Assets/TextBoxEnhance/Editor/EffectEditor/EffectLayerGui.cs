using TextBoxEnhance.Data;
using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Draws one <see cref="EffectLayer"/>. Works through SerializedProperty so undo,
    /// dirtying and the built-in gradient and curve editors all come for free.
    /// </summary>
    /// <remarks>
    /// Fields are shown by what the layer is actually doing: a colour layer never shows
    /// a speed it ignores, and a constant never shows a spread. Hiding inapplicable
    /// controls matters more here than in a normal inspector, because the audience is
    /// someone who cannot read the code to find out what is ignored.
    /// </remarks>
    internal static class EffectLayerGui
    {
        /// <summary>Draws the layer's body. Returns true if the layer asked to be removed.</summary>
        public static bool Draw(SerializedProperty layer, int index, bool advanced)
        {
            bool remove = false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                SerializedProperty channel = layer.FindPropertyRelative("Channel");
                SerializedProperty motion = layer.FindPropertyRelative("Motion");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{index + 1}. {Title(channel, motion)}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(64f)))
                        remove = true;
                }

                EditorGUILayout.PropertyField(channel, new GUIContent("Moves"));
                EditorGUILayout.PropertyField(motion, new GUIContent("How"));

                var channelValue = (EffectChannel)channel.enumValueIndex;
                var motionValue = (EffectMotion)motion.enumValueIndex;

                if (channelValue == EffectChannel.Colour)
                    DrawColour(layer, advanced);
                else
                    DrawRange(layer, channelValue, advanced);

                DrawTiming(layer, motionValue, advanced);

                if (advanced)
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("Timebase"), new GUIContent("Driven by"));
            }

            return remove;
        }

        private static string Title(SerializedProperty channel, SerializedProperty motion)
        {
            string channelName = ObjectNames.NicifyVariableName(
                ((EffectChannel)channel.enumValueIndex).ToString());
            string motionName = ((EffectMotion)motion.enumValueIndex).ToString();
            return channelName + "  -  " + motionName;
        }

        /// <summary>
        /// Presents Min/Max as a single "amount" for the channels people think of
        /// symmetrically, keeping the resting position out of the way in Advanced.
        /// </summary>
        private static void DrawRange(SerializedProperty layer, EffectChannel channel, bool advanced)
        {
            SerializedProperty min = layer.FindPropertyRelative("Min");
            SerializedProperty max = layer.FindPropertyRelative("Max");

            if (channel == EffectChannel.Alpha)
            {
                EditorGUILayout.Slider(min, 0f, 1f, new GUIContent("Faintest"));
                EditorGUILayout.Slider(max, 0f, 1f, new GUIContent("Strongest"));
                return;
            }

            float centre = (min.floatValue + max.floatValue) * 0.5f;
            float reach = (max.floatValue - min.floatValue) * 0.5f;

            reach = Mathf.Max(0f, SoftRangeSlider.Draw(AmountLabel(channel), reach, 0f, AmountSoftMax(channel)));

            if (advanced)
            {
                centre = EditorGUILayout.FloatField(new GUIContent("Resting value",
                    "Where the character sits when the motion is halfway."), centre);
            }

            min.floatValue = centre - reach;
            max.floatValue = centre + reach;
        }

        private static GUIContent AmountLabel(EffectChannel channel)
        {
            switch (channel)
            {
                case EffectChannel.Rotation:
                    return new GUIContent("Angle", "Degrees swung either side of upright.");

                case EffectChannel.ScaleX:
                case EffectChannel.ScaleY:
                case EffectChannel.Scale:
                    return new GUIContent("Amount", "How much bigger and smaller it gets. 0.15 is 15 percent.");

                default:
                    return new GUIContent("Distance", "How far it travels, as a fraction of the font size.");
            }
        }

        /// <summary>
        /// Where the slider's track ends. Kept wide on purpose: the track bends rather
        /// than shortens, so the everyday values already sit a quarter to two fifths of
        /// the way along without putting a big value out of reach.
        /// </summary>
        private static float AmountSoftMax(EffectChannel channel)
        {
            return channel == EffectChannel.Rotation ? 180f : 1f;
        }

        private static void DrawColour(SerializedProperty layer, bool advanced)
        {
            SerializedProperty mode = layer.FindPropertyRelative("ColourMode");
            EditorGUILayout.PropertyField(mode, new GUIContent("Colour from"));

            switch ((ColourMode)mode.enumValueIndex)
            {
                case ColourMode.Solid:
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("Colour"), new GUIContent("Colour"));
                    break;

                case ColourMode.Gradient:
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("Gradient"), new GUIContent("Gradient"));
                    break;

                case ColourMode.HueSweep:
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("Saturation"), new GUIContent("Saturation"));
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("Value"), new GUIContent("Brightness"));

                    if (advanced)
                    {
                        EditorGUILayout.PropertyField(layer.FindPropertyRelative("Min"), new GUIContent("Hue from"));
                        EditorGUILayout.PropertyField(layer.FindPropertyRelative("Max"), new GUIContent("Hue to"));
                    }

                    break;
            }

            EditorGUILayout.PropertyField(layer.FindPropertyRelative("Weight"), new GUIContent("Strength"));
        }

        private static void DrawTiming(SerializedProperty layer, EffectMotion motion, bool advanced)
        {
            if (motion == EffectMotion.Constant)
                return;

            if (motion != EffectMotion.Jitter)
            {
                SoftRangeSlider.Draw(layer.FindPropertyRelative("Speed"),
                    new GUIContent(SpeedLabel(motion), "Times per second. Type a negative to run it backwards."),
                    0f, 4f);
            }

            if (UsesSpread(motion))
            {
                SoftRangeSlider.Draw(layer.FindPropertyRelative("Spread"),
                    new GUIContent("Offset per letter",
                        "Delays each letter behind the one before it. This is what makes a wave travel. " +
                        "Negative sends it the other way."),
                    0f, 0.5f);
            }

            if (motion == EffectMotion.Blink)
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("Duty"), new GUIContent("On for"));

            if (motion == EffectMotion.Curve)
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("Curve"), new GUIContent("Shape"));

            if (!advanced)
                return;

            if (UsesSpread(motion))
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("Phase"), new GUIContent("Start at"));

            if (motion == EffectMotion.Shake || motion == EffectMotion.Jitter || motion == EffectMotion.Drift)
            {
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("Salt"),
                    new GUIContent("Randomness", "Change this so two layers do not move in step."));
            }
        }

        private static string SpeedLabel(EffectMotion motion)
        {
            switch (motion)
            {
                case EffectMotion.Shake:
                    return "Rattles per second";

                case EffectMotion.Blink:
                    return "Blinks per second";

                default:
                    return "Speed";
            }
        }

        private static bool UsesSpread(EffectMotion motion)
        {
            switch (motion)
            {
                case EffectMotion.Sine:
                case EffectMotion.Bounce:
                case EffectMotion.Ramp:
                case EffectMotion.Curve:
                    return true;

                default:
                    return false;
            }
        }
    }
}
