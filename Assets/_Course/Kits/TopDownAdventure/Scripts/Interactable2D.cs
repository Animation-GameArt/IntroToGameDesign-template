using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TopDownAdventure
{
    /// <summary>
    /// Something the player can use by standing next to it and pressing
    /// Interact: a sign, a person to talk to, a chest, a lever.
    ///
    /// It can show lines of text, one per press, and run events. While the
    /// player is close, an optional prompt (such as a small "E" sprite) is shown.
    ///
    /// Setup: give it a 2D collider with Is Trigger ticked, a little bigger than
    /// the object, for the area the player has to stand in. For a sign the player
    /// can't walk through, add a second, solid collider on a child.
    /// </summary>
    [AddComponentMenu("Collin Game/Top-Down Adventure Kit/Interactable 2D")]
    public class Interactable2D : MonoBehaviour
    {
        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("Only objects with this tag can use it.")]
        [SerializeField] string userTag = "Player";

        [Header("Text")]

        [Tooltip("Optional. A text variable to write the lines to, for a label to show.")]
        [SerializeField] StringVariable messageText;

        [Tooltip("The lines to show, one per press. After the last line, the text is cleared.")]
        [SerializeField, TextArea(1, 3)] string[] lines = { "Hello!" };

        [Header("Use")]

        [Tooltip("Ticked: it can only be used once, like a chest. Its lines can still be read.")]
        [SerializeField] bool onlyOnce;

        [Tooltip("Optional. Shown while the player is close enough to use it, such as a small key icon.")]
        [SerializeField] GameObject prompt;

        [Header("Events")]

        [Tooltip("Runs when the player uses it. With lines, runs when the first line is shown.")]
        [SerializeField] UnityEvent onUsed;

        [Tooltip("Runs when the last line has been read.")]
        [SerializeField] UnityEvent onFinished;

        bool userInRange;
        bool used;
        int nextLine;

        /// <summary>True while the player is close enough to use it.</summary>
        public bool UserInRange => userInRange;

        void Awake()
        {
            if (input == null)
            {
                Debug.LogError($"Interactable 2D on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
            }

            ShowPrompt(false);
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.InteractPressed += OnInteractPressed;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.InteractPressed -= OnInteractPressed;
            }

            if (userInRange)
            {
                userInRange = false;
                Close();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (TouchUtility.HasTag(TouchUtility.OwnerOf(other), userTag))
            {
                userInRange = true;
                ShowPrompt(true);
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (TouchUtility.HasTag(TouchUtility.OwnerOf(other), userTag))
            {
                userInRange = false;
                Close();
            }
        }

        void OnInteractPressed()
        {
            if (userInRange && !PauseController.GamePaused)
            {
                Use();
            }
        }

        /// <summary>Uses it now: shows the next line, and runs On Used the first time.</summary>
        public void Use()
        {
            bool hasLines = lines != null && lines.Length > 0;

            if (nextLine == 0 && (!onlyOnce || !used))
            {
                used = true;
                onUsed?.Invoke();
            }

            if (!hasLines)
            {
                return;
            }

            if (nextLine < lines.Length)
            {
                Say(lines[nextLine]);
                nextLine++;
            }
            else
            {
                Close();
                onFinished?.Invoke();
            }
        }

        void Close()
        {
            nextLine = 0;
            Say(string.Empty);
            ShowPrompt(userInRange);
        }

        void Say(string text)
        {
            if (messageText != null)
            {
                messageText.SetValue(text);
            }
        }

        void ShowPrompt(bool show)
        {
            if (prompt != null)
            {
                prompt.SetActive(show && (!onlyOnce || !used || (lines != null && lines.Length > 0)));
            }
        }

#if UNITY_EDITOR
        // Runs when the component is first added: fills in the Input field for you.
        void Reset()
        {
            input = InputReader.FindInProject();
        }
#endif
    }
}
