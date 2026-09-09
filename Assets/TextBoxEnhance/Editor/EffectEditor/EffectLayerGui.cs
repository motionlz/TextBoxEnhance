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
        private static EditorUi.RowAction s_PendingAction;
        private static int s_PendingRow = -1;
        private static int s_CurrentRow = -1;

        /// <summary>
        /// Draws one layer as a collapsible row. Collapsed, the header alone says what
        /// the layer does, which is what makes a five layer effect readable at a glance.
        /// </summary>
        public static void Draw(SerializedProperty layer, int index, bool advanced)
        {
            SerializedProperty channel = layer.FindPropertyRelative("Channel");
            SerializedProperty motion = layer.FindPropertyRelative("Motion");

            s_CurrentRow = index;

            var header = new GUIContent($"{index + 1}.   {Summary(layer, channel, motion)}");
            layer.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                layer.isExpanded, header, null, ShowRowMenu);

            if (!layer.isExpanded)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(channel, new GUIContent("Moves"));
                if (EditorGUI.EndChangeCheck())
                    Recentre(layer, (EffectChannel)channel.enumValueIndex);

                EditorGUILayout.PropertyField(motion, new GUIContent("How"));

                SerializedProperty target = layer.FindPropertyRelative("Target");
                EditorGUILayout.PropertyField(target, new GUIContent("Applies to",
                    "Which part of a letter moves. Only does anything in scripts that write " +
                    "marks above or below a base, such as Thai."));

                if ((LayerTarget)target.enumValueIndex != LayerTarget.WholeLetter)
                {
                    EditorGUILayout.HelpBox(
                        "Splitting a letter is for a single word, a title or a damage number. " +
                        "In running text a tone mark that drifts off its consonant reads as " +
                        "broken rather than as style, and one that drifts towards the next " +
                        "consonant can be read as belonging to that one.",
                        MessageType.Info);
                }

                var channelValue = (EffectChannel)channel.enumValueIndex;
                var motionValue = (EffectMotion)motion.enumValueIndex;

                if (channelValue == EffectChannel.Colour)
                    DrawColour(layer, advanced);
                else
                    DrawRange(layer, channelValue, motionValue, advanced);

                DrawTiming(layer, motionValue, advanced);

                if (advanced)
                {
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("Timebase"),
                        new GUIContent("Driven by"));
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>The menu behind the header's dots: everything that moves or copies a row.</summary>
        private static void ShowRowMenu(Rect rect)
        {
            int row = s_CurrentRow;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Move up"), false, () => Remember(row, EditorUi.RowAction.MoveUp));
            menu.AddItem(new GUIContent("Move down"), false, () => Remember(row, EditorUi.RowAction.MoveDown));
            menu.AddItem(new GUIContent("Duplicate"), false, () => Remember(row, EditorUi.RowAction.Duplicate));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Remove"), false, () => Remember(row, EditorUi.RowAction.Remove));
            menu.DropDown(rect);
        }

        private static void Remember(int row, EditorUi.RowAction action)
        {
            s_PendingRow = row;
            s_PendingAction = action;
        }

        /// <summary>
        /// The row a menu choice was made on, and what was chosen. Reading it clears it,
        /// because a GenericMenu fires its callback long after the GUI pass that opened
        /// it -- there is no way to return the answer from Draw.
        /// </summary>
        public static bool TryTakePendingAction(out int row, out EditorUi.RowAction action)
        {
            row = s_PendingRow;
            action = s_PendingAction;

            bool any = action != EditorUi.RowAction.None && row >= 0;
            s_PendingAction = EditorUi.RowAction.None;
            s_PendingRow = -1;
            return any;
        }

        /// <summary>
        /// What the layer does, in one line: what it moves, how, and how far. This is
        /// the whole of a collapsed row, so it has to answer "what is this layer for"
        /// without being opened.
        /// </summary>
        private static string Summary(SerializedProperty layer, SerializedProperty channel,
            SerializedProperty motion)
        {
            var channelValue = (EffectChannel)channel.enumValueIndex;
            var motionValue = (EffectMotion)motion.enumValueIndex;
            var target = (LayerTarget)layer.FindPropertyRelative("Target").enumValueIndex;

            string line = ChannelName(channelValue) + "   ·   " + motionValue.ToString().ToLowerInvariant();

            string amount = AmountSummary(layer, channelValue);
            if (!string.IsNullOrEmpty(amount))
                line += "   ·   " + amount;

            if (target == LayerTarget.MarksOnly)
                line += "   ·   marks only";
            else if (target == LayerTarget.BaseLetterOnly)
                line += "   ·   base only";

            return line;
        }

        /// <summary>Plain names for the channels, rather than the field names.</summary>
        private static string ChannelName(EffectChannel channel)
        {
            switch (channel)
            {
                case EffectChannel.OffsetX: return "Sideways";
                case EffectChannel.OffsetY: return "Up and down";
                case EffectChannel.Rotation: return "Rotation";
                case EffectChannel.ScaleX: return "Width";
                case EffectChannel.ScaleY: return "Height";
                case EffectChannel.Scale: return "Size";
                case EffectChannel.Alpha: return "Opacity";
                default: return "Colour";
            }
        }

        private static string AmountSummary(SerializedProperty layer, EffectChannel channel)
        {
            float min = layer.FindPropertyRelative("Min").floatValue;
            float max = layer.FindPropertyRelative("Max").floatValue;

            switch (channel)
            {
                case EffectChannel.Alpha:
                    return $"{min:0.##} to {max:0.##}";

                case EffectChannel.Colour:
                    return ((ColourMode)layer.FindPropertyRelative("ColourMode").enumValueIndex)
                        .ToString().ToLowerInvariant();

                case EffectChannel.Rotation:
                    return $"{(max - min) * 0.5f:0.#} deg";

                default:
                    return $"{(max - min) * 0.5f:0.###} em";
            }
        }

        /// <summary>
        /// Presents Min/Max as a single "amount" for the channels people think of
        /// symmetrically, keeping the resting position out of the way in Advanced.
        /// </summary>
        private static void DrawRange(SerializedProperty layer, EffectChannel channel,
            EffectMotion motion, bool advanced)
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

            // Rescues a scale layer saved before the resting value followed the channel.
            // A scale of zero renders nothing at all, which looks like a broken effect
            // rather than a setting that wants changing. Left alone once flipping is on:
            // resting at or below zero is a deliberate choice at that point.
            if (EffectLimits.IsScale(channel) && centre <= 0f
                && !layer.FindPropertyRelative("AllowFlip").boolValue)
                centre = 1f;

            reach = CurvedSlider.Draw(AmountLabel(channel), reach, 0f, EffectLimits.Amount(channel, motion));

            if (EffectLimits.IsScale(channel))
            {
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("AllowFlip"),
                    new GUIContent("Allow flipping",
                        "Lets the character turn inside out when the scale passes zero."));
            }

            if (advanced)
            {
                centre = EditorGUILayout.FloatField(new GUIContent("Resting value",
                    "Where the character sits when the motion is halfway."), centre);
            }

            min.floatValue = centre - reach;
            max.floatValue = centre + reach;
        }

        /// <summary>
        /// Moves the layer's range onto the new channel's neutral, keeping how far it
        /// travels. Without this, switching an offset layer to scale leaves it resting
        /// at zero -- and a character scaled to zero is a character you cannot see.
        /// </summary>
        private static void Recentre(SerializedProperty layer, EffectChannel channel)
        {
            SerializedProperty min = layer.FindPropertyRelative("Min");
            SerializedProperty max = layer.FindPropertyRelative("Max");

            if (channel == EffectChannel.Alpha || channel == EffectChannel.Colour)
            {
                min.floatValue = 0f;
                max.floatValue = 1f;
                return;
            }

            float reach = Mathf.Abs(max.floatValue - min.floatValue) * 0.5f;
            float neutral = EffectLimits.Neutral(channel);

            min.floatValue = neutral - reach;
            max.floatValue = neutral + reach;
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
                CurvedSlider.Draw(layer.FindPropertyRelative("Speed"),
                    new GUIContent(SpeedLabel(motion), "Times per second."),
                    0f, EffectLimits.Speed(motion));
            }

            if (UsesSpread(motion))
            {
                // The only signed control here: which way a wave travels is a real
                // choice, where a negative speed would just mirror a symmetric motion
                // onto itself. Zero sits at the middle and the fine control radiates
                // out from it in both directions.
                CurvedSlider.Draw(layer.FindPropertyRelative("Spread"),
                    new GUIContent("Offset per letter",
                        "Delays each letter behind the one before it. This is what makes a wave travel. " +
                        "Negative sends it the other way."),
                    -EffectLimits.Spread, EffectLimits.Spread);
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
