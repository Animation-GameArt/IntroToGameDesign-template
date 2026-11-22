using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.ThreeD
{
    /// <summary>
    /// Shows an object on the Minimap 3D map as a colored dot, square or arrow. The marker
    /// floats above the object, on the Minimap layer, so only the map camera sees it. An
    /// arrow turns with the object, showing which way it faces.
    ///
    /// Setup: add to the player, enemies, pickups, doors: anything worth seeing on the map.
    /// The marker goes when the object is turned off or destroyed (a key that's picked up
    /// disappears from the map).
    /// </summary>
    [AddComponentMenu("Collin Game/3D/Minimap Marker 3D")]
    public class MinimapMarker3D : MonoBehaviour
    {
        public enum MarkerShape
        {
            Dot,
            Square,
            Arrow
        }

        [Tooltip("Dot for things, Square for places, Arrow for things that face a way (the player, guards).")]
        [SerializeField] MarkerShape shape = MarkerShape.Dot;

        [Tooltip("The marker's color.")]
        [SerializeField] Color color = Color.white;

        [Tooltip("How big the marker is on the map, in units across.")]
        [SerializeField, Min(0.1f)] float size = 1.5f;

        [Tooltip("How far above the object the marker floats: above the object's own top, so the object doesn't hide it.")]
        [SerializeField, Min(0f)] float heightAbove = 4f;

        [Tooltip("Draw order among markers: higher draws on top (put the player above everything).")]
        [SerializeField] int order;

        static readonly Dictionary<MarkerShape, Sprite> Sprites = new Dictionary<MarkerShape, Sprite>();

        Transform marker;

        void Awake()
        {
            GameObject go = new GameObject($"{name} (Minimap Marker)");
            marker = go.transform;

            int layer = Minimap3D.MarkerLayer;
            if (layer >= 0)
            {
                go.layer = layer;
            }

            SpriteRenderer drawing = go.AddComponent<SpriteRenderer>();
            drawing.sprite = SpriteFor(shape);
            drawing.color = color;
            drawing.sortingOrder = order;
            drawing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            drawing.receiveShadows = false;
            marker.localScale = new Vector3(size, size, 1f);
            Follow();
        }

        void OnEnable()
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(true);
            }
        }

        void OnDisable()
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (marker != null)
            {
                Destroy(marker.gameObject);
            }
        }

        void LateUpdate() => Follow();

        /// <summary>Changes the marker's color, for example red when a guard spots the player.</summary>
        public void SetColor(Color newColor)
        {
            color = newColor;

            if (marker != null)
            {
                marker.GetComponent<SpriteRenderer>().color = newColor;
            }
        }

        void Follow()
        {
            // Lying flat, facing up; an arrow points the way the object faces.
            float yaw = shape == MarkerShape.Arrow ? transform.eulerAngles.y : 0f;
            marker.SetPositionAndRotation(transform.position + Vector3.up * heightAbove, Quaternion.Euler(90f, yaw, 0f));
        }

        /// <summary>Makes each marker shape once, as a 64-pixel white picture (tinted by the color).</summary>
        static Sprite SpriteFor(MarkerShape markerShape)
        {
            if (Sprites.TryGetValue(markerShape, out Sprite cached) && cached != null)
            {
                return cached;
            }

            const int pixels = 64;
            Texture2D texture = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false) { name = $"Minimap {markerShape}" };
            Color32[] colors = new Color32[pixels * pixels];

            for (int y = 0; y < pixels; y++)
            {
                for (int x = 0; x < pixels; x++)
                {
                    // -1 to 1 across the picture, with up being the way an arrow points.
                    float u = (x + 0.5f) / pixels * 2f - 1f;
                    float v = (y + 0.5f) / pixels * 2f - 1f;
                    bool inside = markerShape switch
                    {
                        MarkerShape.Dot => u * u + v * v <= 0.9f * 0.9f,
                        MarkerShape.Square => Mathf.Abs(u) <= 0.8f && Mathf.Abs(v) <= 0.8f,
                        _ => v >= -0.8f && v <= 0.9f && Mathf.Abs(u) <= (0.9f - v) * 0.5f && !(v < -0.35f && Mathf.Abs(u) < (-0.35f - v) * 1.2f)
                    };
                    colors[y * pixels + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(colors);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, pixels, pixels), new Vector2(0.5f, 0.5f), pixels);
            Sprites[markerShape] = sprite;
            return sprite;
        }
    }
}
