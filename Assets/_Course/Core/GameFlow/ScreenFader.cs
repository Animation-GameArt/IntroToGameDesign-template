using System.Collections;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Covers the whole screen with a solid color that can fade in and out.
    /// The Scene Loader uses it to hide the moment when one scene is swapped for another.
    ///
    /// It draws with Unity's built-in OnGUI method so that it needs no extra
    /// assets and always appears in front of the game.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Core/Screen Fader")]
    public class ScreenFader : MonoBehaviour
    {
        // The longest step, in seconds, that one frame may add to a fade.
        // Without it, a slow frame right after a scene loads would make the fade jump.
        const float MaxStep = 0.05f;

        [Tooltip("The color the screen fades to.")]
        [SerializeField] Color color = Color.black;

        float alpha;

        /// <summary>How much the screen is covered: 0 is clear, 1 is fully covered.</summary>
        public float Alpha => alpha;

        /// <summary>The color the screen fades to.</summary>
        public Color FadeColor
        {
            get => color;
            set => color = value;
        }

        void Awake()
        {
            // Nothing to draw yet, so switch off until a fade starts.
            enabled = false;
        }

        /// <summary>
        /// Fades the cover to the target amount (0 = clear, 1 = fully covered).
        /// Use it inside a coroutine: yield return fader.FadeTo(1f, 0.5f);
        /// It ignores Time.timeScale, so it still works while the game is paused.
        /// </summary>
        public IEnumerator FadeTo(float target, float duration)
        {
            target = Mathf.Clamp01(target);
            enabled = true;

            float start = alpha;
            float elapsed = 0f;

            while (duration > 0f && elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStep);
                alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            alpha = target;

            // Stop drawing once the screen is clear again.
            enabled = alpha > 0f;
        }

        void OnGUI()
        {
            if (alpha <= 0f || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            GUI.depth = -1000; // Draw in front of any other OnGUI drawing.
            GUI.color = new Color(color.r, color.g, color.b, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }
}
