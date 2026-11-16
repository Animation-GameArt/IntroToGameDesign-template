using UnityEngine;

namespace CollinGame.TwoD
{
    /// <summary>
    /// Plays a few sprites in a loop, like a flipbook: a spinning coin, a walking
    /// enemy, a waving flag. It is the simplest kind of animation and needs no
    /// Animator.
    /// </summary>
    [AddComponentMenu("Collin Game/2D/Sprite Flipbook")]
    public class SpriteFlipbook : MonoBehaviour
    {
        [Tooltip("The sprites to show, in order.")]
        [SerializeField] Sprite[] frames;

        [Tooltip("How many sprites are shown each second.")]
        [SerializeField, Min(0.1f)] float framesPerSecond = 6f;

        [Tooltip("Optional. The sprite renderer to change. Leave empty to use the one on this object.")]
        [SerializeField] SpriteRenderer target;

        [Tooltip("Ticked: starts at a random point in the loop, so copies don't all move in step.")]
        [SerializeField] bool randomStart = true;

        float time;

        void Awake()
        {
            if (target == null)
            {
                target = GetComponent<SpriteRenderer>();
            }
        }

        void OnEnable()
        {
            time = randomStart && frames != null && frames.Length > 0
                ? Random.Range(0f, frames.Length / framesPerSecond)
                : 0f;
        }

        void Update()
        {
            if (target == null || frames == null || frames.Length == 0)
            {
                return;
            }

            time += Time.deltaTime;

            int index = Mathf.FloorToInt(time * framesPerSecond) % frames.Length;

            if (frames[index] != null)
            {
                target.sprite = frames[index];
            }
        }
    }
}
