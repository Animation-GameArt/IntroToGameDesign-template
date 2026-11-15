using TMPro;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// A short piece of text in the game world that drifts upward and fades out,
    /// such as "-1" above a character that was just hit.
    ///
    /// You don't add this to a GameObject. Create one from a script with:
    ///   FloatingText.Spawn("-1", transform.position, Color.red);
    /// The Health Popups component does this for you.
    /// </summary>
    [AddComponentMenu("")]
    public class FloatingText : MonoBehaviour
    {
        TextMeshPro label;
        Color color;
        Vector3 startPosition;
        float duration;
        float riseDistance;
        float elapsed;

        /// <summary>Creates a floating text at a position in the world.</summary>
        public static FloatingText Spawn(string text, Vector3 position, Color color, float fontSize = 5f, float duration = 0.8f, float riseDistance = 1f)
        {
            GameObject textObject = new GameObject("Floating Text");
            textObject.transform.position = position;

            TextMeshPro label = textObject.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(12f, 2f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.color = color;
            label.text = text;
            label.sortingOrder = 1000; // Draw in front of sprites.

            FloatingText floating = textObject.AddComponent<FloatingText>();
            floating.label = label;
            floating.color = color;
            floating.startPosition = position;
            floating.duration = Mathf.Max(0.05f, duration);
            floating.riseDistance = riseDistance;
            return floating;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // Rise quickly at first, then slow down.
            float eased = 1f - (1f - progress) * (1f - progress);
            transform.position = startPosition + Vector3.up * (riseDistance * eased);

            // Stay solid for the first half, then fade.
            float alpha = progress < 0.5f ? 1f : 1f - (progress - 0.5f) * 2f;
            label.color = new Color(color.r, color.g, color.b, color.a * alpha);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
