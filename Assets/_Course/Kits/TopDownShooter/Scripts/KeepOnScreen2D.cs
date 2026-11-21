using UnityEngine;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// Keeps this object inside the camera's view, such as the player's ship
    /// in a scrolling shooter, or the player in a single-screen arena.
    ///
    /// Uses the main camera, which should be Orthographic.
    /// </summary>
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Keep On Screen 2D")]
    public class KeepOnScreen2D : MonoBehaviour
    {
        [Tooltip("How far inside the screen's edge the object's middle must stay, in units. About half its size.")]
        [SerializeField, Min(0f)] float margin = 0.5f;

        Rigidbody2D body;
        Camera mainCamera;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;

                if (mainCamera == null)
                {
                    return;
                }
            }

            float halfHeight = mainCamera.orthographicSize - margin;
            float halfWidth = mainCamera.orthographicSize * mainCamera.aspect - margin;
            Vector2 center = mainCamera.transform.position;

            Vector2 position = transform.position;
            Vector2 clamped = new Vector2(
                Mathf.Clamp(position.x, center.x - halfWidth, center.x + halfWidth),
                Mathf.Clamp(position.y, center.y - halfHeight, center.y + halfHeight));

            if (clamped == position)
            {
                return;
            }

            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);

            if (body != null)
            {
                body.position = clamped;
            }
        }
    }
}
