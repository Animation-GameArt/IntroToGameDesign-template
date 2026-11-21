using UnityEngine;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// Keeps an enemy asleep until it scrolls into view, then wakes it. Once it
    /// has been on screen and leaves again, it's removed. For scrolling levels:
    /// place enemies along the level, and each one starts moving and shooting
    /// as the camera reaches it.
    ///
    /// Setup: add to the enemy. Its Enemy Mover 2D and Weapon Shooter 2D are
    /// switched off until it wakes.
    /// </summary>
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Wake On Screen 2D")]
    public class WakeOnScreen2D : MonoBehaviour
    {
        [Tooltip("Wakes this far outside the screen's edge, in units, so it can fly in.")]
        [SerializeField, Min(0f)] float wakeMargin = 1f;

        [Tooltip("Removed once it's this far outside the screen after waking, in units.")]
        [SerializeField, Min(0f)] float removeMargin = 2f;

        [Tooltip("Optional. Scripts to keep switched off until it wakes. Leave empty for the Enemy Mover 2D and Weapon Shooter 2D on this object.")]
        [SerializeField] Behaviour[] wakeUp = new Behaviour[0];

        Camera mainCamera;
        Rigidbody2D body;
        bool awake;

        /// <summary>True once the enemy has come into view.</summary>
        public bool IsAwake => awake;

        void Start()
        {
            mainCamera = Camera.main;
            body = GetComponent<Rigidbody2D>();

            if (wakeUp == null || wakeUp.Length == 0)
            {
                wakeUp = new Behaviour[] { GetComponent<EnemyMover2D>(), GetComponent<WeaponShooter2D>() };
            }

            SetAwake(false);
        }

        void Update()
        {
            if (mainCamera == null)
            {
                return;
            }

            if (!awake)
            {
                if (OnScreen(wakeMargin))
                {
                    SetAwake(true);
                }
            }
            else if (!OnScreen(removeMargin))
            {
                Destroy(gameObject);
            }
        }

        bool OnScreen(float margin)
        {
            float halfHeight = mainCamera.orthographicSize + margin;
            float halfWidth = mainCamera.orthographicSize * mainCamera.aspect + margin;
            Vector2 offset = (Vector2)(transform.position - mainCamera.transform.position);
            return Mathf.Abs(offset.x) <= halfWidth && Mathf.Abs(offset.y) <= halfHeight;
        }

        void SetAwake(bool value)
        {
            awake = value;

            foreach (Behaviour behaviour in wakeUp)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = value;
                }
            }

            if (!value && body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
