using UnityEngine;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// When this object leaves one edge of the screen, it comes back on the
    /// opposite edge, as in Asteroids. Works with or without a Rigidbody 2D.
    ///
    /// Uses the main camera, which should be Orthographic and not moving.
    /// </summary>
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Screen Wrap 2D")]
    public class ScreenWrap2D : MonoBehaviour
    {
        [Tooltip("How far past the edge the object goes before it wraps, in units. About half the object's size looks right.")]
        [SerializeField, Min(0f)] float margin = 0.5f;

        [Tooltip("Wrap at the left and right edges.")]
        [SerializeField] bool wrapSideways = true;

        [Tooltip("Wrap at the top and bottom edges.")]
        [SerializeField] bool wrapUpAndDown = true;

        Rigidbody2D body;
        Camera mainCamera;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            mainCamera = Camera.main;

            if (mainCamera != null && !mainCamera.orthographic)
            {
                Debug.LogWarning($"Screen Wrap 2D on \"{name}\": the main camera isn't Orthographic, so wrapping may happen in the wrong place.", this);
            }
        }

        void FixedUpdate()
        {
            if (body != null)
            {
                Wrap();
            }
        }

        void LateUpdate()
        {
            if (body == null)
            {
                Wrap();
            }
        }

        void Wrap()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;

                if (mainCamera == null)
                {
                    return;
                }
            }

            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;
            Vector2 center = mainCamera.transform.position;

            Vector2 position = body != null ? body.position : (Vector2)transform.position;
            Vector2 wrapped = position;

            if (wrapSideways)
            {
                float left = center.x - halfWidth - margin;
                float right = center.x + halfWidth + margin;

                if (position.x > right) wrapped.x = left;
                else if (position.x < left) wrapped.x = right;
            }

            if (wrapUpAndDown)
            {
                float bottom = center.y - halfHeight - margin;
                float top = center.y + halfHeight + margin;

                if (position.y > top) wrapped.y = bottom;
                else if (position.y < bottom) wrapped.y = top;
            }

            if (wrapped == position)
            {
                return;
            }

            if (body != null)
            {
                body.position = wrapped;
            }

            transform.position = new Vector3(wrapped.x, wrapped.y, transform.position.z);
        }
    }
}
