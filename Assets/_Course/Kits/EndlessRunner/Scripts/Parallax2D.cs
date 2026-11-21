using UnityEngine;

namespace CollinGame.Kits.EndlessRunner
{
    /// <summary>
    /// Makes a background layer scroll more slowly than the world, so it looks
    /// far away, and repeats it forever. Use three or four layers: far hills
    /// barely move, near bushes move almost with the ground.
    ///
    /// Setup: a sprite whose left and right edges match. Set the Sprite
    /// Renderer's Draw Mode to Tiled and its Width to three times the sprite's
    /// width, so there's always a copy either side.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Collin Game/Endless Runner Kit/Parallax 2D")]
    public class Parallax2D : MonoBehaviour
    {
        [Tooltip("How far away the layer seems. 0 moves with the world (no parallax); 1 moves with the camera (infinitely far). Hills about 0.8, trees about 0.5.")]
        [SerializeField, Range(0f, 1f)] float distance = 0.7f;

        [Tooltip("Ticked: the layer also follows the camera up and down at the same rate.")]
        [SerializeField] bool vertical = false;

        Camera mainCamera;
        Vector2 start;
        float repeatWidth;

        void Start()
        {
            mainCamera = Camera.main;
            start = transform.position;

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            repeatWidth = renderer.sprite != null ? renderer.sprite.bounds.size.x * transform.lossyScale.x : 0f;
        }

        void LateUpdate()
        {
            if (mainCamera == null)
            {
                return;
            }

            Vector2 cameraPosition = mainCamera.transform.position;
            float moved = cameraPosition.x * distance;
            float left = cameraPosition.x * (1f - distance);

            Vector3 position = transform.position;
            position.x = start.x + moved;

            if (vertical)
            {
                position.y = start.y + cameraPosition.y * distance;
            }

            transform.position = position;

            // Jump one sprite width once the camera has passed a whole copy.
            if (repeatWidth > 0f)
            {
                if (left > start.x + repeatWidth)
                {
                    start.x += repeatWidth;
                }
                else if (left < start.x - repeatWidth)
                {
                    start.x -= repeatWidth;
                }
            }
        }
    }
}
