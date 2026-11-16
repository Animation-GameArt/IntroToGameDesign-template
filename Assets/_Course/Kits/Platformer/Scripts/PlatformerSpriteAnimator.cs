using CollinGame.TwoD;
using UnityEngine;

namespace CollinGame.Kits.Platformer
{
    /// <summary>
    /// Changes a platformer character's sprite to match what it is doing:
    /// standing, walking or in the air. It needs no Animator.
    ///
    /// Add it to the same GameObject as the Platformer Controller 2D.
    /// </summary>
    [RequireComponent(typeof(PlatformerController2D))]
    [AddComponentMenu("Collin Game/Platformer Kit/Platformer Sprite Animator")]
    public class PlatformerSpriteAnimator : MonoBehaviour
    {
        [Tooltip("Optional. The sprite renderer to change. Leave empty to use the first one found on this object.")]
        [SerializeField] SpriteRenderer target;

        [Tooltip("The sprite shown while standing still.")]
        [SerializeField] Sprite idle;

        [Tooltip("The sprites shown in a loop while walking.")]
        [SerializeField] Sprite[] walk;

        [Tooltip("The sprite shown while in the air. Leave empty to keep the last walking sprite.")]
        [SerializeField] Sprite inAir;

        [Tooltip("How many walking sprites are shown each second.")]
        [SerializeField, Min(0.1f)] float walkFramesPerSecond = 8f;

        PlatformerController2D controller;
        float walkTime;

        void Awake()
        {
            controller = GetComponent<PlatformerController2D>();

            if (target == null)
            {
                target = GetComponentInChildren<SpriteRenderer>();
            }
        }

        void Update()
        {
            if (target == null)
            {
                return;
            }

            if (!controller.IsGrounded)
            {
                if (inAir != null)
                {
                    target.sprite = inAir;
                }

                return;
            }

            bool walking = Mathf.Abs(controller.MoveInput) > 0.01f && walk != null && walk.Length > 0;

            if (walking)
            {
                walkTime += Time.deltaTime;

                Sprite frame = walk[Mathf.FloorToInt(walkTime * walkFramesPerSecond) % walk.Length];

                if (frame != null)
                {
                    target.sprite = frame;
                }
            }
            else
            {
                walkTime = 0f;

                if (idle != null)
                {
                    target.sprite = idle;
                }
            }
        }
    }
}
