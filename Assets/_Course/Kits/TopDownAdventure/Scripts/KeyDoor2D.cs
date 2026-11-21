using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TopDownAdventure
{
    /// <summary>
    /// A locked door that opens when the player touches it with enough keys.
    ///
    /// Keys are counted in a number variable. Collecting a key is a Collectible
    /// that adds 1 to the same variable. By default, opening the door uses up
    /// the key, as in most adventure games.
    ///
    /// Setup: give the door a solid 2D collider (not a trigger), so it blocks the
    /// way until it opens. When it opens, the GameObject is switched off.
    /// </summary>
    [AddComponentMenu("Collin Game/Top-Down Adventure Kit/Key Door 2D")]
    public class KeyDoor2D : MonoBehaviour
    {
        [Tooltip("Only objects with this tag can open the door.")]
        [SerializeField] string openerTag = "Player";

        [Tooltip("The number variable that counts the player's keys.")]
        [SerializeField] IntVariable keys;

        [Tooltip("How many keys the door needs. 0 makes it a door that opens when touched, or when Open is called by a switch or an event.")]
        [SerializeField, Min(0)] int keysNeeded = 1;

        [Tooltip("Ticked: opening the door takes the keys away. Unticked: the player keeps them.")]
        [SerializeField] bool useUpKeys = true;

        [Header("Messages")]

        [Tooltip("Optional. A text variable to write a message to when the door is locked, for a label to show.")]
        [SerializeField] StringVariable messageText;

        [Tooltip("The message when the player doesn't have enough keys. {0} is replaced by the number needed.")]
        [SerializeField] string lockedMessage = "Locked. You need {0} key.";

        [Header("Events")]

        [Tooltip("Runs when the door opens. Use it for a sound.")]
        [SerializeField] UnityEvent onOpened;

        [Tooltip("Runs when the player touches the door without enough keys.")]
        [SerializeField] UnityEvent onLocked;

        /// <summary>True once the door has opened.</summary>
        public bool IsOpen { get; private set; }

        void OnCollisionEnter2D(Collision2D collision) { Try(TouchUtility.OwnerOf(collision)); }
        void OnTriggerEnter2D(Collider2D other) { Try(TouchUtility.OwnerOf(other)); }

        void Try(GameObject opener)
        {
            if (IsOpen || !TouchUtility.HasTag(opener, openerTag))
            {
                return;
            }

            int have = keys != null ? keys.Value : 0;

            if (keysNeeded > 0 && (keys == null || have < keysNeeded))
            {
                if (keys == null)
                {
                    Debug.LogWarning($"Key Door 2D on \"{name}\": Keys is empty, so the door can never open. Drag in the number variable that counts keys.", this);
                }

                if (messageText != null)
                {
                    messageText.SetValue(string.Format(lockedMessage, keysNeeded));
                }

                onLocked?.Invoke();
                return;
            }

            if (useUpKeys && keys != null && keysNeeded > 0)
            {
                keys.Add(-keysNeeded);
            }

            Open();
        }

        /// <summary>Opens the door without needing keys. Call it from a switch, a puzzle or an event listener.</summary>
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            onOpened?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
