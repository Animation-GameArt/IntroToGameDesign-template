using CollinGame.Core;
using CollinGame.TwoD;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// Aims with the mouse or the right stick, separately from moving: the
    /// "twin-stick" controls of Geometry Wars and Enter the Gungeon.
    ///
    ///   Mouse        aims at the pointer.
    ///   Right stick  aims the way the stick points. When it's let go, the aim stays.
    ///   Neither      aims the way the player last moved.
    ///
    /// A Weapon Shooter 2D on the same object fires the way this aims. Drag a
    /// child object, such as a gun sprite pointing right, into Turn To Aim to
    /// show the aim.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Aim 2D")]
    public class Aim2D : MonoBehaviour
    {
        /// <summary>Where the aim comes from.</summary>
        public enum AimWith
        {
            /// <summary>The mouse when it moves, otherwise the right stick.</summary>
            MouseOrStick,

            /// <summary>Always the way the player moves.</summary>
            MoveDirection,

            /// <summary>Always straight ahead, along Fixed Direction. For a scrolling shooter's ship.</summary>
            FixedDirection
        }

        [Tooltip("Where the aim comes from.")]
        [SerializeField] AimWith aimWith = AimWith.MouseOrStick;

        [Tooltip("Fixed Direction only: the way to aim, such as up for a ship flying up the screen.")]
        [SerializeField] Vector2 fixedDirection = Vector2.up;

        [Tooltip("How far the right stick must be pushed before it aims, from 0 to 1.")]
        [SerializeField, Range(0f, 1f)] float stickDeadZone = 0.3f;

        [Header("Looks")]

        [Tooltip("Optional. A child, such as a gun sprite pointing right, turned to point where the player aims.")]
        [SerializeField] Transform turnToAim;

        [Tooltip("Optional. This sprite is flipped to face left or right with the aim.")]
        [SerializeField] SpriteRenderer spriteToFlip;

        TopDownController2D mover;
        Camera mainCamera;
        Vector2 aim = Vector2.right;
        bool usingMouse;

        /// <summary>The direction the player is aiming, one unit long.</summary>
        public Vector2 Direction => aim;

        void Awake()
        {
            mover = GetComponent<TopDownController2D>();
            mainCamera = Camera.main;

            if (aimWith == AimWith.FixedDirection)
            {
                aim = fixedDirection.sqrMagnitude > 0.0001f ? fixedDirection.normalized : Vector2.up;
            }
        }

        void Update()
        {
            if (!PauseController.GamePaused)
            {
                UpdateAim();
            }

            if (turnToAim != null)
            {
                float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                turnToAim.rotation = Quaternion.Euler(0f, 0f, angle);

                // Keep a gun sprite the right way up when aiming left.
                Vector3 scale = turnToAim.localScale;
                scale.y = Mathf.Abs(scale.y) * (aim.x < 0f ? -1f : 1f);
                turnToAim.localScale = scale;
            }

            if (spriteToFlip != null && Mathf.Abs(aim.x) > 0.01f)
            {
                spriteToFlip.flipX = aim.x < 0f;
            }
        }

        void UpdateAim()
        {
            switch (aimWith)
            {
                case AimWith.FixedDirection:
                    aim = fixedDirection.sqrMagnitude > 0.0001f ? fixedDirection.normalized : Vector2.up;
                    return;

                case AimWith.MoveDirection:
                    AimAlongMovement();
                    return;
            }

            // The right stick wins whenever it's pushed.
            Gamepad pad = Gamepad.current;

            if (pad != null)
            {
                Vector2 stick = pad.rightStick.ReadValue();

                if (stick.magnitude >= stickDeadZone)
                {
                    aim = stick.normalized;
                    usingMouse = false;
                    return;
                }
            }

            // The mouse takes over once it moves, and keeps aiming at the pointer while still.
            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0.01f)
            {
                usingMouse = true;
            }

            if (usingMouse && mouse != null && mainCamera != null)
            {
                Vector2 pointer = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                Vector2 offset = pointer - (Vector2)transform.position;

                if (offset.sqrMagnitude > 0.01f)
                {
                    aim = offset.normalized;
                }

                return;
            }

            AimAlongMovement();
        }

        void AimAlongMovement()
        {
            if (mover != null && mover.FacingDirection.sqrMagnitude > 0.0001f)
            {
                aim = mover.FacingDirection.normalized;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Vector2 direction = Application.isPlaying ? aim : (aimWith == AimWith.FixedDirection ? fixedDirection.normalized : Vector2.right);
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * 1.5f);
        }
    }
}
