using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TextBoxEnhance
{
    /// <summary>
    /// Writes per-character animation into a TextMeshPro mesh. Shared by
    /// <see cref="AnimatedTextBox"/> at runtime and by the effect editor's preview, so
    /// what an author sees while dialling a slider is produced by the same code that
    /// will run in the game.
    /// </summary>
    public static class TextMeshAnimator
    {
        /// <summary>Takes the copy of TextMeshPro's untouched geometry that Apply works from.</summary>
        public static TMP_MeshInfo[] Cache(TMP_Text target)
        {
            return target != null && target.textInfo != null ? target.textInfo.CopyMeshInfoVertexData() : null;
        }

        /// <summary>
        /// Copies the cached geometry back over the working arrays.
        /// </summary>
        /// <returns>
        /// False when the cache no longer matches the laid-out text, which means TMP
        /// re-flowed it and the caller should take a fresh <see cref="Cache"/>.
        /// </returns>
        public static bool Restore(TMP_Text target, TMP_MeshInfo[] cache)
        {
            TMP_TextInfo textInfo = target != null ? target.textInfo : null;
            if (textInfo == null || cache == null)
                return false;

            int materialCount = textInfo.materialCount;
            if (cache.Length < materialCount)
                return false;

            for (int m = 0; m < materialCount; m++)
            {
                Vector3[] sourceVertices = cache[m].vertices;
                Vector3[] targetVertices = textInfo.meshInfo[m].vertices;
                Color32[] sourceColors = cache[m].colors32;
                Color32[] targetColors = textInfo.meshInfo[m].colors32;

                if (sourceVertices == null || targetVertices == null
                    || sourceVertices.Length != targetVertices.Length
                    || sourceColors == null || targetColors == null
                    || sourceColors.Length != targetColors.Length)
                    return false;

                Array.Copy(sourceVertices, targetVertices, sourceVertices.Length);
                Array.Copy(sourceColors, targetColors, sourceColors.Length);
            }

            return true;
        }

        /// <summary>
        /// Restores the original geometry, lays this frame's animation over it and
        /// uploads the result.
        /// </summary>
        /// <returns>False if the cache was stale and nothing was drawn.</returns>
        public static bool Apply(TMP_Text target, TMP_MeshInfo[] cache, List<EffectRange> effects,
            float time, float deltaTime, ICharacterRevealSource revealSource, in RevealSettings reveal)
        {
            if (!Restore(target, cache))
                return false;

            TMP_TextInfo textInfo = target.textInfo;
            int charCount = textInfo.characterCount;

            for (int c = 0; c < charCount; c++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[c];
                if (!character.isVisible)
                    continue;

                int materialIndex = character.materialReferenceIndex;
                int vertexIndex = character.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

                if (vertexIndex + 3 >= vertices.Length)
                    continue;

                float progress = revealSource?.RevealProgressOf(c) ?? 1f;
                if (progress <= 0f)
                {
                    HideCharacter(colors, vertexIndex);
                    continue;
                }

                CharacterMod mod = CharacterMod.Identity;
                RevealAnimator.Apply(reveal.Style, reveal.Shape(progress), reveal.Distance, reveal.Spins, ref mod);

                if (effects != null)
                {
                    for (int e = 0; e < effects.Count; e++)
                    {
                        EffectRange range = effects[e];
                        if (c < range.Start || c >= range.End)
                            continue;

                        var context = new TextEffectContext(textInfo, c, c - range.Start,
                            range.End - range.Start, time, deltaTime, progress);
                        range.Effect.Apply(in context, range.Parameters, ref mod);
                    }
                }

                BakeCharacter(vertices, colors, vertexIndex, character, ref mod);
            }

            target.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
            return true;
        }

        private static void HideCharacter(Color32[] colors, int vertexIndex)
        {
            for (int k = 0; k < 4; k++)
            {
                Color32 color = colors[vertexIndex + k];
                color.a = 0;
                colors[vertexIndex + k] = color;
            }
        }

        /// <summary>Writes one character's accumulated offsets into the TMP vertex arrays.</summary>
        private static void BakeCharacter(Vector3[] vertices, Color32[] colors, int vertexIndex,
            TMP_CharacterInfo character, ref CharacterMod mod)
        {
            // Offsets arrive in em, so a 12pt and a 120pt label animate identically.
            float em = character.pointSize > 0f ? character.pointSize : 1f;

            // Rotate and scale around the character's own baseline centre, which is where
            // a reader expects a letter to pivot.
            var pivot = new Vector3(
                (vertices[vertexIndex].x + vertices[vertexIndex + 2].x) * 0.5f,
                character.baseLine,
                0f);

            var matrix = Matrix4x4.TRS(
                new Vector3(mod.Offset.x * em, mod.Offset.y * em, 0f),
                Quaternion.Euler(0f, 0f, mod.Rotation),
                new Vector3(mod.Scale.x, mod.Scale.y, 1f));

            for (int k = 0; k < 4; k++)
                vertices[vertexIndex + k] = matrix.MultiplyPoint3x4(vertices[vertexIndex + k] - pivot) + pivot;

            for (int k = 0; k < 4; k++)
            {
                Color color = colors[vertexIndex + k];
                color *= mod.ColorMultiply;

                if (mod.ColorOverrideWeight > 0f)
                {
                    // Only the hue is replaced; alpha stays with the typewriter and <fade>.
                    color.r = Mathf.Lerp(color.r, mod.ColorOverride.r, mod.ColorOverrideWeight);
                    color.g = Mathf.Lerp(color.g, mod.ColorOverride.g, mod.ColorOverrideWeight);
                    color.b = Mathf.Lerp(color.b, mod.ColorOverride.b, mod.ColorOverrideWeight);
                }

                colors[vertexIndex + k] = color;
            }
        }
    }
}
