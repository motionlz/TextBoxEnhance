using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TextBoxEnhance.Samples
{
    /// <summary>
    /// Feeds a list of lines to an <see cref="AnimatedTextBox"/> one at a time -- the
    /// smallest useful thing you can build on the component, and a worked example of
    /// its API.
    /// </summary>
    /// <remarks>
    /// Deliberately reads no input of its own: call <see cref="Advance"/> from a UI
    /// Button, an input action, or your own controller so the sample stays independent
    /// of which input system the project uses.
    /// </remarks>
    [AddComponentMenu("TextBox Enhance/Dialogue Sequence")]
    [RequireComponent(typeof(AnimatedTextBox))]
    public sealed class DialogueSequence : MonoBehaviour
    {
        [SerializeField, TextArea(2, 6)]
        [Tooltip("Lines to play through, in order. Effect tags are welcome.")]
        private List<string> m_Lines = new List<string>
        {
            "The <wave>sea</wave> was calm that morning.",
            "<speed=0.5>Then the horizon <shake a=0.08>split open</shake>.</speed><pause=0.5> And everything changed.",
            "<rainbow>Nothing was ever the same again.</rainbow>",
        };

        [SerializeField]
        [Tooltip("First Advance() shows line one, rather than needing a Play() first.")]
        private bool m_StartOnEnable = true;

        [SerializeField]
        [Tooltip("Advancing mid-reveal finishes the current line instead of skipping to the next.")]
        private bool m_FirstAdvanceCompletesLine = true;

        [Space]
        public UnityEvent OnSequenceCompleted = new UnityEvent();

        private AnimatedTextBox m_Box;
        private int m_Index = -1;

        /// <summary>Index of the line currently on screen, or -1 before the first one.</summary>
        public int CurrentLine => m_Index;

        /// <summary>True once the last line has finished revealing.</summary>
        public bool IsFinished => m_Index >= m_Lines.Count - 1 && !m_Box.IsRevealing;

        private void Awake()
        {
            m_Box = GetComponent<AnimatedTextBox>();
        }

        private void OnEnable()
        {
            if (m_StartOnEnable)
                ShowLine(0);
        }

        /// <summary>
        /// Completes the current line if it is still revealing, otherwise moves to the
        /// next one. This is the "click to continue" behaviour players expect.
        /// </summary>
        public void Advance()
        {
            if (m_FirstAdvanceCompletesLine && m_Box.IsRevealing)
            {
                m_Box.SkipToEnd();
                return;
            }

            if (m_Index >= m_Lines.Count - 1)
            {
                OnSequenceCompleted?.Invoke();
                return;
            }

            ShowLine(m_Index + 1);
        }

        /// <summary>Jumps straight to a line by index.</summary>
        public void ShowLine(int index)
        {
            if (m_Lines.Count == 0 || index < 0 || index >= m_Lines.Count)
                return;

            m_Index = index;
            m_Box.Play(m_Lines[index]);
        }

        /// <summary>Returns to the first line.</summary>
        public void Restart()
        {
            ShowLine(0);
        }
    }
}
