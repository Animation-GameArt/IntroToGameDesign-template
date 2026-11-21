using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// A brick that breaks when the ball hits it, as in Breakout. Tough bricks
    /// take several hits, and can change color with each hit.
    ///
    /// Setup: a sprite with a Box Collider 2D (not a trigger). Add a Counted
    /// Object as well, so the level knows when every brick is gone.
    /// </summary>
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Brick 2D")]
    public class Brick2D : MonoBehaviour
    {
        [Tooltip("How many hits it takes to break.")]
        [SerializeField, Min(1)] int hitsToBreak = 1;

        [Tooltip("Optional. The sprite's color for each number of hits left: element 0 is the last hit, element 1 the one before. Leave empty to keep the color.")]
        [SerializeField] Color[] colorByHitsLeft = new Color[0];

        [Tooltip("Optional. Objects with this tag break bricks too, such as bullets. The ball always does.")]
        [SerializeField] string alsoBrokenByTag = "";

        [Header("When broken")]

        [Tooltip("Optional. A number to add to, such as the score.")]
        [SerializeField] IntVariable addTo;

        [Tooltip("How much to add.")]
        [SerializeField] int amount = 10;

        [Tooltip("Optional. A prefab to create where the brick was, such as a burst of pieces or a power-up.")]
        [SerializeField] GameObject spawnWhenBroken;

        [Header("Events")]

        [Tooltip("Runs on every hit that doesn't break it.")]
        [SerializeField] UnityEvent onHit;

        [Tooltip("Runs when it breaks. Use it for a sound.")]
        [SerializeField] UnityEvent onBroken;

        SpriteRenderer sprite;
        int hitsLeft;

        /// <summary>How many more hits it takes to break.</summary>
        public int HitsLeft => hitsLeft;

        void Awake()
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable()
        {
            hitsLeft = hitsToBreak;
            ShowHitsLeft();
        }

        void OnCollisionEnter2D(Collision2D collision) { Touched(collision.collider, TouchUtility.OwnerOf(collision)); }
        void OnTriggerEnter2D(Collider2D other) { Touched(other, TouchUtility.OwnerOf(other)); }

        void Touched(Collider2D other, GameObject owner)
        {
            bool isBall = other.GetComponentInParent<Ball2D>() != null;
            bool isTagged = !string.IsNullOrEmpty(alsoBrokenByTag) && owner != null && TouchUtility.HasTag(owner, alsoBrokenByTag);

            if (isBall || isTagged)
            {
                Hit();
            }
        }

        /// <summary>Takes one hit, and breaks if no hits are left.</summary>
        public void Hit()
        {
            if (hitsLeft <= 0)
            {
                return;
            }

            hitsLeft--;

            if (hitsLeft > 0)
            {
                ShowHitsLeft();
                onHit?.Invoke();
                return;
            }

            Break();
        }

        /// <summary>Breaks the brick at once.</summary>
        public void Break()
        {
            hitsLeft = 0;

            if (addTo != null)
            {
                addTo.Add(amount);
            }

            if (spawnWhenBroken != null)
            {
                Instantiate(spawnWhenBroken, transform.position, Quaternion.identity);
            }

            onBroken?.Invoke();
            gameObject.SetActive(false);
        }

        void ShowHitsLeft()
        {
            int index = hitsLeft - 1;

            if (sprite != null && colorByHitsLeft != null && index >= 0 && index < colorByHitsLeft.Length)
            {
                sprite.color = colorByHitsLeft[index];
            }
        }
    }
}
