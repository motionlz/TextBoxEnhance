using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TextBoxEnhance
{
    /// <summary>
    /// Drives a TextMeshPro label: reveals it one character at a time and animates
    /// individual characters through rich-text tags such as
    /// <c>&lt;wave&gt;</c>, <c>&lt;shake&gt;</c> and <c>&lt;rainbow&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Works with both <see cref="TextMeshProUGUI"/> and <see cref="TextMeshPro"/>. The
    /// component never regenerates the text mesh itself: it restores TMP's own geometry
    /// each frame and writes offsets on top, so TMP stays in charge of layout.
    /// </remarks>
    [AddComponentMenu("TextBox Enhance/Animated Text Box")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AnimatedTextBox : MonoBehaviour
    {
        /// <summary>Raised with the character index each time the typewriter reveals one.</summary>
        [Serializable]
        public sealed class CharacterRevealedEvent : UnityEvent<int> { }

        [Header("Text")]
        [SerializeField, TextArea(3, 10)]
        [Tooltip("Source text. Supports TextMeshPro rich text plus this package's effect tags.")]
        private string m_Text = "Hello, <wave>world</wave>!";

        [SerializeField]
        [Tooltip("Label to drive. Defaults to a TMP_Text on this GameObject.")]
        private TMP_Text m_Target;

        [Header("Typewriter")]
        [SerializeField]
        [Tooltip("Reveal the text one character at a time instead of showing it all at once.")]
        private bool m_Typewriter = true;

        [SerializeField, Min(0f)]
        [Tooltip("Characters revealed per second. <speed> tags scale this.")]
        private float m_CharactersPerSecond = 30f;

        [SerializeField]
        [Tooltip("Start revealing as soon as the component is enabled.")]
        private bool m_PlayOnEnable = true;

        [Header("Character entrance")]
        [SerializeField]
        private RevealStyle m_RevealStyle = RevealStyle.Fade;

        [SerializeField, Min(0f)]
        [Tooltip("Seconds each character takes to finish its entrance.")]
        private float m_RevealDuration = 0.18f;

        [SerializeField]
        [Tooltip("Shapes the entrance over its duration.")]
        private AnimationCurve m_RevealEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Travel distance for the sliding entrances, in em (1 = one font size).")]
        private float m_RevealDistance = 0.5f;

        [SerializeField, Min(0f)]
        [Tooltip("Full turns for the Spin entrance.")]
        private float m_RevealSpins = 1f;

        [Header("Timing")]
        [SerializeField]
        [Tooltip("Ignore Time.timeScale, so text keeps moving while the game is paused.")]
        private bool m_UseUnscaledTime = true;

        [Header("Events")]
        public UnityEvent OnRevealStarted = new UnityEvent();
        public CharacterRevealedEvent OnCharacterRevealed = new CharacterRevealedEvent();
        public UnityEvent OnRevealCompleted = new UnityEvent();

        private readonly ParsedText m_Parsed = new ParsedText();
        private TMP_MeshInfo[] m_CachedMeshInfo;
        private float[] m_CharRevealStart;

        private float m_Time;
        private float m_LastTimeSample;
        private int m_RevealedCount;
        private float m_CharAccumulator;
        private float m_PauseTimer;
        private int m_PauseCursor;
        private bool m_IsRevealing;
        private bool m_CompletedFired;
        private bool m_TextDirty = true;
        private bool m_RestartPending;
        private bool m_GeometryDirty = true;
        private bool m_Subscribed;

        /// <summary>
        /// Source text, tags included. Assigning re-parses and restarts the reveal.
        /// </summary>
        public string Text
        {
            get => m_Text;
            set
            {
                m_Text = value ?? string.Empty;
                m_TextDirty = true;
                if (isActiveAndEnabled)
                    Rebuild(restart: true);
            }
        }

        /// <summary>The label this box drives.</summary>
        public TMP_Text Target => m_Target;

        /// <summary>True while characters are still being revealed.</summary>
        public bool IsRevealing => m_IsRevealing;

        /// <summary>Characters revealed so far.</summary>
        public int RevealedCharacters => m_RevealedCount;

        /// <summary>Characters in the text once tags are stripped.</summary>
        public int TotalCharacters => m_Target != null && m_Target.textInfo != null ? m_Target.textInfo.characterCount : 0;

        /// <summary>Characters revealed per second before <c>&lt;speed&gt;</c> tags are applied.</summary>
        public float CharactersPerSecond
        {
            get => m_CharactersPerSecond;
            set => m_CharactersPerSecond = Mathf.Max(0f, value);
        }

        private void Reset()
        {
            m_Target = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (m_Target == null)
                m_Target = GetComponent<TMP_Text>();

            if (m_Target == null)
            {
                Debug.LogError($"[TextBoxEnhance] '{name}' has no TMP_Text to drive.", this);
                enabled = false;
                return;
            }

            if (!m_Subscribed)
            {
                TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
                m_Subscribed = true;
            }

            m_LastTimeSample = SampleTime();
            m_TextDirty = true;
            Rebuild(restart: m_PlayOnEnable || !Application.isPlaying);
        }

        private void OnDisable()
        {
            if (m_Subscribed)
            {
                TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
                m_Subscribed = false;
            }
        }

        private void OnValidate()
        {
            m_CharactersPerSecond = Mathf.Max(0f, m_CharactersPerSecond);
            m_RevealDuration = Mathf.Max(0f, m_RevealDuration);

            // Rebuilding here would touch the mesh and raise events while Unity is
            // still deserializing. Flag it instead and let the next frame do the work.
            m_TextDirty = true;
            m_RestartPending = true;
        }

        /// <summary>Restarts the reveal from the first character.</summary>
        public void Play()
        {
            Rebuild(restart: true);
        }

        /// <summary>Replaces the text and starts revealing it.</summary>
        public void Play(string text)
        {
            m_Text = text ?? string.Empty;
            m_TextDirty = true;
            Rebuild(restart: true);
        }

        /// <summary>Reveals everything immediately, skipping the remaining typewriter time.</summary>
        public void SkipToEnd()
        {
            EnsureBuilt();

            int total = TotalCharacters;
            for (int i = m_RevealedCount; i < total; i++)
                RevealCharacter(i, m_Time - m_RevealDuration);

            m_RevealedCount = total;
            m_PauseTimer = 0f;
            m_PauseCursor = m_Parsed.Pauses.Count;
            m_IsRevealing = false;
            m_GeometryDirty = true;
            FireCompletedOnce();
        }

        /// <summary>Hides every character and stops, ready for another <see cref="Play()"/>.</summary>
        public void Rewind()
        {
            EnsureBuilt();
            ResetRevealState();
            m_IsRevealing = false;
            m_GeometryDirty = true;
        }

        /// <summary>Holds the typewriter where it is. Effects keep animating.</summary>
        public void Pause()
        {
            m_IsRevealing = false;
        }

        /// <summary>Resumes a paused typewriter.</summary>
        public void Resume()
        {
            if (m_RevealedCount < TotalCharacters)
                m_IsRevealing = true;
        }

        private void EnsureBuilt()
        {
            if (!m_TextDirty && !m_RestartPending && m_CachedMeshInfo != null)
                return;

            bool restart = m_RestartPending;
            m_RestartPending = false;
            Rebuild(restart);
        }

        /// <summary>Re-parses the source text, pushes it into TMP and re-caches its geometry.</summary>
        private void Rebuild(bool restart)
        {
            if (m_Target == null)
                return;

            if (m_TextDirty)
            {
                TextEffectParser.Parse(m_Text, m_Parsed);
                m_Target.text = m_Parsed.Text;
                m_TextDirty = false;
            }

            m_Target.ForceMeshUpdate();
            CacheGeometry();

            if (restart)
                ResetRevealState();

            m_GeometryDirty = true;
        }

        private void CacheGeometry()
        {
            TMP_TextInfo textInfo = m_Target.textInfo;
            if (textInfo == null)
                return;

            m_CachedMeshInfo = textInfo.CopyMeshInfoVertexData();

            int count = textInfo.characterCount;
            if (m_CharRevealStart == null || m_CharRevealStart.Length < count)
            {
                var grown = new float[Mathf.Max(count, 16)];
                for (int i = 0; i < grown.Length; i++)
                    grown[i] = float.NaN;

                if (m_CharRevealStart != null)
                    Array.Copy(m_CharRevealStart, grown, m_CharRevealStart.Length);

                m_CharRevealStart = grown;
            }
        }

        /// <summary>
        /// TextMeshPro rebuilt its mesh (a font swap, a layout change, an external
        /// <c>text</c> assignment); our cached copy of the untouched geometry is stale.
        /// </summary>
        private void OnTextChanged(UnityEngine.Object changed)
        {
            if (!ReferenceEquals(changed, m_Target))
                return;

            CacheGeometry();
            m_GeometryDirty = true;
        }

        private void ResetRevealState()
        {
            m_Time = 0f;
            m_LastTimeSample = SampleTime();
            m_RevealedCount = 0;
            m_CharAccumulator = 0f;
            m_PauseTimer = 0f;
            m_PauseCursor = 0;
            m_CompletedFired = false;
            m_LastRevealStart = float.NegativeInfinity;

            if (m_CharRevealStart != null)
            {
                for (int i = 0; i < m_CharRevealStart.Length; i++)
                    m_CharRevealStart[i] = float.NaN;
            }

            int total = TotalCharacters;
            bool useTypewriter = m_Typewriter && m_CharactersPerSecond > 0f;

            if (useTypewriter && total > 0)
            {
                m_IsRevealing = true;
                RaiseRevealStarted();
            }
            else
            {
                for (int i = 0; i < total; i++)
                    RevealCharacter(i, m_Time - m_RevealDuration);

                m_RevealedCount = total;
                m_IsRevealing = false;
                FireCompletedOnce();
            }
        }

        private void RevealCharacter(int index, float startTime)
        {
            if (m_CharRevealStart == null || index < 0 || index >= m_CharRevealStart.Length)
                return;

            m_CharRevealStart[index] = startTime;
            m_LastRevealStart = Mathf.Max(m_LastRevealStart, startTime);
        }

        private void FireCompletedOnce()
        {
            if (m_CompletedFired)
                return;

            m_CompletedFired = true;
            RaiseRevealCompleted();
        }

        // Under [ExecuteAlways] these would otherwise fire while the user is merely
        // editing the scene, letting listener code change the project by accident.
        private void RaiseRevealStarted()
        {
            if (Application.isPlaying)
                OnRevealStarted?.Invoke();
        }

        private void RaiseCharacterRevealed(int index)
        {
            if (Application.isPlaying)
                OnCharacterRevealed?.Invoke(index);
        }

        private void RaiseRevealCompleted()
        {
            if (Application.isPlaying)
                OnRevealCompleted?.Invoke();
        }

        private float SampleTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return Time.realtimeSinceStartup;
#endif
            return m_UseUnscaledTime ? Time.unscaledTime : Time.time;
        }

        /// <summary>Reveal speed at a character, folding in every <c>&lt;speed&gt;</c> range covering it.</summary>
        private float SpeedMultiplierAt(int charIndex)
        {
            float multiplier = 1f;
            List<SpeedRange> speeds = m_Parsed.Speeds;

            for (int i = 0; i < speeds.Count; i++)
            {
                SpeedRange range = speeds[i];
                if (charIndex >= range.Start && charIndex < range.End)
                    multiplier *= range.Multiplier;
            }

            return multiplier;
        }

        private float m_LastRevealStart = float.NegativeInfinity;
        private bool m_WasAnimating;

        private void LateUpdate()
        {
            if (m_Target == null)
                return;

            float now = SampleTime();
            // Clamped so a long editor stall or a loading hitch does not fast-forward the reveal.
            float deltaTime = Mathf.Clamp(now - m_LastTimeSample, 0f, 0.25f);
            m_LastTimeSample = now;
            m_Time += deltaTime;

            EnsureBuilt();

            if (m_IsRevealing)
                AdvanceReveal(deltaTime);

            if (!m_CompletedFired && !m_IsRevealing && m_RevealedCount >= TotalCharacters && TotalCharacters > 0
                && m_Time >= m_LastRevealStart + m_RevealDuration)
                FireCompletedOnce();

            ApplyToMesh(deltaTime);
        }

        private void AdvanceReveal(float deltaTime)
        {
            int total = TotalCharacters;
            float budget = deltaTime;

            while (budget > 0f && m_RevealedCount < total)
            {
                if (m_PauseTimer > 0f)
                {
                    float used = Mathf.Min(m_PauseTimer, budget);
                    m_PauseTimer -= used;
                    budget -= used;
                    if (m_PauseTimer > 0f)
                        return;
                }

                if (TryStartPauseAt(m_RevealedCount))
                    continue;

                float charsPerSecond = m_CharactersPerSecond * SpeedMultiplierAt(m_RevealedCount);
                if (charsPerSecond <= 0f)
                    return;

                float timeToNext = (1f - m_CharAccumulator) / charsPerSecond;
                if (timeToNext > budget)
                {
                    m_CharAccumulator += budget * charsPerSecond;
                    return;
                }

                budget -= timeToNext;
                m_CharAccumulator = 0f;

                // Backdate the entrance by the leftover budget so fast text does not
                // reveal a whole frame's worth of characters in perfect lockstep.
                RevealCharacter(m_RevealedCount, m_Time - budget);
                RaiseCharacterRevealed(m_RevealedCount);
                m_RevealedCount++;
            }

            if (m_RevealedCount >= total)
                m_IsRevealing = false;
        }

        /// <summary>Starts the hold requested by a <c>&lt;pause&gt;</c> sitting at this character.</summary>
        private bool TryStartPauseAt(int charIndex)
        {
            List<PauseMarker> pauses = m_Parsed.Pauses;

            while (m_PauseCursor < pauses.Count && pauses[m_PauseCursor].CharIndex < charIndex)
                m_PauseCursor++;

            if (m_PauseCursor >= pauses.Count || pauses[m_PauseCursor].CharIndex != charIndex)
                return false;

            m_PauseTimer = pauses[m_PauseCursor].Seconds;
            m_PauseCursor++;
            return true;
        }

        /// <summary>How far a character is through its entrance, 0 while still hidden.</summary>
        private float RevealProgress(int charIndex)
        {
            if (m_CharRevealStart == null || charIndex >= m_CharRevealStart.Length)
                return 1f;

            float start = m_CharRevealStart[charIndex];
            if (float.IsNaN(start))
                return 0f;

            if (m_RevealDuration <= 0f)
                return 1f;

            return Mathf.Clamp01((m_Time - start) / m_RevealDuration);
        }

        private void ApplyToMesh(float deltaTime)
        {
            TMP_TextInfo textInfo = m_Target.textInfo;
            if (textInfo == null || m_CachedMeshInfo == null)
                return;

            int charCount = textInfo.characterCount;
            bool entranceRunning = m_RevealDuration > 0f && m_Time < m_LastRevealStart + m_RevealDuration;
            bool animating = charCount > 0
                             && (m_Parsed.Effects.Count > 0 || m_IsRevealing || entranceRunning
                                 || m_RevealedCount < charCount);

            // One extra pass after the last animating frame settles the final values.
            if (!animating && !m_GeometryDirty && !m_WasAnimating)
                return;

            m_WasAnimating = animating;
            m_GeometryDirty = false;

            if (!RestoreOriginalGeometry(textInfo))
                return;

            List<EffectRange> effects = m_Parsed.Effects;

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

                float reveal = RevealProgress(c);
                if (reveal <= 0f)
                {
                    HideCharacter(colors, vertexIndex);
                    continue;
                }

                CharacterMod mod = CharacterMod.Identity;
                float eased = m_RevealEase != null && m_RevealEase.length > 0 ? m_RevealEase.Evaluate(reveal) : reveal;
                RevealAnimator.Apply(m_RevealStyle, eased, m_RevealDistance, m_RevealSpins, ref mod);

                for (int e = 0; e < effects.Count; e++)
                {
                    EffectRange range = effects[e];
                    if (c < range.Start || c >= range.End)
                        continue;

                    var context = new TextEffectContext(textInfo, c, c - range.Start, range.End - range.Start,
                        m_Time, deltaTime, reveal);
                    range.Effect.Apply(in context, range.Parameters, ref mod);
                }

                BakeCharacter(vertices, colors, vertexIndex, character, ref mod);
            }

            m_Target.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        /// <summary>
        /// Copies TextMeshPro's untouched geometry back over the working arrays, so each
        /// frame's offsets are applied to the layout rather than to last frame's result.
        /// </summary>
        private bool RestoreOriginalGeometry(TMP_TextInfo textInfo)
        {
            int materialCount = textInfo.materialCount;
            if (m_CachedMeshInfo.Length < materialCount)
            {
                CacheGeometry();
                return false;
            }

            for (int m = 0; m < materialCount; m++)
            {
                Vector3[] sourceVertices = m_CachedMeshInfo[m].vertices;
                Vector3[] targetVertices = textInfo.meshInfo[m].vertices;
                Color32[] sourceColors = m_CachedMeshInfo[m].colors32;
                Color32[] targetColors = textInfo.meshInfo[m].colors32;

                if (sourceVertices == null || targetVertices == null
                    || sourceVertices.Length != targetVertices.Length
                    || sourceColors == null || targetColors == null
                    || sourceColors.Length != targetColors.Length)
                {
                    // TMP re-laid the text out between our cache and now; take a fresh copy.
                    CacheGeometry();
                    return false;
                }

                Array.Copy(sourceVertices, targetVertices, sourceVertices.Length);
                Array.Copy(sourceColors, targetColors, sourceColors.Length);
            }

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
