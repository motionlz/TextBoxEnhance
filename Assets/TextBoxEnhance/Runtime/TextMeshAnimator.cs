using System;
using System.Collections.Generic;
using TextBoxEnhance.Data;
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
            return Apply(target, cache, effects, time, deltaTime, revealSource, reveal,
                ClusterMap.OnePerCharacter);
        }

        /// <summary>
        /// Restores the original geometry, lays this frame's animation over it and
        /// uploads the result, treating each cluster in <paramref name="clusters"/> as
        /// one letter.
        /// </summary>
        /// <returns>False if the cache was stale and nothing was drawn.</returns>
        public static bool Apply(TMP_Text target, TMP_MeshInfo[] cache, List<EffectRange> effects,
            float time, float deltaTime, ICharacterRevealSource revealSource, in RevealSettings reveal,
            in ClusterMap clusters)
        {
            if (!Restore(target, cache))
                return false;

            TMP_TextInfo textInfo = target.textInfo;
            int charCount = textInfo.characterCount;

            // TextMeshPro lays a character out at its point size only in orthographic
            // mode; otherwise it shrinks the geometry by ten. TextMeshProUGUI turns that
            // on for itself, a 3D TextMeshPro leaves it off, and characterInfo.pointSize
            // reports the same number either way -- so treating point size as the em
            // makes every effect ten times too strong on 3D text.
            float emScale = target.isOrthographic ? 1f : 0.1f;

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

                // A combining mark takes its phase, its randomness and its pivot from the
                // character it sits on. Give it its own and it drifts off the letter it
                // belongs to, which is what animating Thai one code point at a time looks
                // like.
                int cluster = clusters.ClusterFor(c);
                int anchorIndex = clusters.BaseFor(c);
                bool isMark = anchorIndex != c;

                CharacterMod mod = CharacterMod.Identity;
                RevealAnimator.Apply(reveal.Style, reveal.Shape(progress), reveal.Distance, reveal.Spins, ref mod);

                if (effects != null)
                {
                    for (int e = 0; e < effects.Count; e++)
                    {
                        EffectRange range = effects[e];

                        // Tested against the base so a tag can never take half a letter.
                        if (anchorIndex < range.Start || anchorIndex >= range.End)
                            continue;

                        // <shake marks> rattles the tone marks over a word that holds
                        // still; <shake base> slides the word out from under them.
                        if (range.Target == LayerTarget.BaseLetterOnly && isMark)
                            continue;

                        if (range.Target == LayerTarget.MarksOnly && !isMark)
                            continue;

                        var context = new TextEffectContext(textInfo, cluster, anchorIndex - range.Start,
                            range.End - range.Start, time, deltaTime, progress, c, isMark);
                        range.Effect.Apply(in context, range.Parameters, ref mod);
                    }
                }

                BakeCharacter(textInfo, cache, vertices, colors, vertexIndex, character, anchorIndex, emScale, ref mod);
            }

            target.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
            return true;
        }

        /// <summary>
        /// The horizontal middle of the base character as TextMeshPro laid it out,
        /// falling back to the character's own middle when the base has no geometry --
        /// a mark after a space, and other text nobody meant to write.
        /// </summary>
        private static float AnchorCentreX(TMP_MeshInfo[] cache, TMP_CharacterInfo anchor,
            Vector3[] vertices, int vertexIndex)
        {
            if (anchor.isVisible && cache != null && anchor.materialReferenceIndex < cache.Length)
            {
                Vector3[] original = cache[anchor.materialReferenceIndex].vertices;
                if (original != null && anchor.vertexIndex + 3 < original.Length)
                    return (original[anchor.vertexIndex].x + original[anchor.vertexIndex + 2].x) * 0.5f;
            }

            return (vertices[vertexIndex].x + vertices[vertexIndex + 2].x) * 0.5f;
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
        private static void BakeCharacter(TMP_TextInfo textInfo, TMP_MeshInfo[] cache,
            Vector3[] vertices, Color32[] colors, int vertexIndex,
            TMP_CharacterInfo character, int anchorIndex, float emScale, ref CharacterMod mod)
        {
            TMP_CharacterInfo anchor = textInfo.characterInfo[anchorIndex];

            // Offsets arrive in em, so a 12pt and a 120pt label animate identically.
            float em = (anchor.pointSize > 0f ? anchor.pointSize : 1f) * emScale;

            // Rotate and scale around the base character's baseline centre, which is
            // where a reader expects a letter to pivot -- and, for a combining mark, the
            // only pivot that keeps it sitting on the letter it belongs to.
            //
            // Read from the cache rather than the working arrays: the base character was
            // baked earlier this frame, so its live vertices have already moved.
            var pivot = new Vector3(
                AnchorCentreX(cache, anchor, vertices, vertexIndex),
                anchor.baseLine,
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
