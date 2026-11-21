using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.Farming
{
    /// <summary>
    /// A place the player uses by standing in it and pressing Interact (E, or
    /// the gamepad's top face button), or by clicking or tapping the prompt:
    /// a bed to sleep in, a shop counter. While the player is inside, the
    /// prompt (such as "E: Sleep") shows on the UI Farm Screen.
    ///
    /// Give it a 2D collider with Is Trigger ticked, for the area the player
    /// has to stand in.
    /// </summary>
    [AddComponentMenu("Collin Game/Farming Kit/Interact Zone 2D")]
    public class InteractZone2D : MonoBehaviour
    {
        [Tooltip("Optional. Found in the project if empty.")]
        [SerializeField] InputReader input;

        [Tooltip("Only objects with this tag can use it.")]
        [SerializeField] string userTag = "Player";

        [Tooltip("The prompt shown while the player is inside.")]
        [SerializeField] string prompt = "E: Use";

        [Tooltip("Runs when the player uses it, such as Day Cycle > Sleep, or UI Shop > Open.")]
        [SerializeField] UnityEvent onUsed;

        bool inside;

        void Awake()
        {
            if (input == null)
            {
                input = InputReader.FindInProject();
            }
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.InteractPressed += Use;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.InteractPressed -= Use;
            }

            if (inside)
            {
                inside = false;
                FarmMessage.SetPrompt(string.Empty);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (TouchUtility.HasTag(TouchUtility.OwnerOf(other), userTag))
            {
                inside = true;
                FarmMessage.SetPrompt(prompt, Use);
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (inside && TouchUtility.HasTag(TouchUtility.OwnerOf(other), userTag))
            {
                inside = false;
                FarmMessage.SetPrompt(string.Empty);
            }
        }

        /// <summary>Uses it, if the player is inside.</summary>
        public void Use()
        {
            if (!inside || PauseController.GamePaused || (input != null && !input.PlayerInputEnabled))
            {
                return;
            }

            onUsed?.Invoke();

            // Sleeping moves the player out of the zone without a trigger exit, so check again next physics step.
            inside = false;
            FarmMessage.SetPrompt(string.Empty);
            StartCoroutine(Recheck());
        }

        System.Collections.IEnumerator Recheck()
        {
            yield return new WaitForFixedUpdate();
            Collider2D zone = GetComponent<Collider2D>();

            if (zone == null)
            {
                yield break;
            }

            ContactFilter2D filter = ContactFilter2D.noFilter;
            filter.useTriggers = true;
            System.Collections.Generic.List<Collider2D> hits = new System.Collections.Generic.List<Collider2D>();
            zone.Overlap(filter, hits);

            foreach (Collider2D hit in hits)
            {
                if (TouchUtility.HasTag(TouchUtility.OwnerOf(hit), userTag))
                {
                    inside = true;
                    FarmMessage.SetPrompt(prompt, Use);
                    yield break;
                }
            }
        }
    }
}
