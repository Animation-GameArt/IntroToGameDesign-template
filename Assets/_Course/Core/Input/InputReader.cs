using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CollinGame.Core
{
    /// <summary>
    /// The one place that game scripts get player input from.
    ///
    /// The Input Reader is an asset. It reads the project's input actions
    /// (Move, Jump, Attack and so on) and offers them in two simple ways:
    ///
    ///   Values you can read at any time:   input.Move, input.JumpHeld
    ///   Events you can listen to:          input.JumpPressed += OnJump;
    ///
    /// Because scripts ask the Input Reader, and not the keyboard or gamepad,
    /// the same script works with every device, and controls can be changed
    /// in one place.
    ///
    /// The course project already has one, in Assets/_Course/Core/Input.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Input/Input Reader", fileName = "InputReader")]
    public class InputReader : ScriptableObject
    {
        const string PlayerMapName = "Player";

        // Every Input Reader asset that is currently loaded, so they can all
        // be disconnected cleanly when Play mode starts again.
        static readonly List<InputReader> loadedReaders = new List<InputReader>();

        [Tooltip("The input actions to read. Leave empty to use the project's own actions (Project Settings > Input System Package).")]
        [SerializeField] InputActionAsset actions;

        [NonSerialized] bool bound;
        [NonSerialized] bool bindFailed;
        [NonSerialized] InputActionMap playerMap;
        [NonSerialized] InputAction move;
        [NonSerialized] InputAction look;
        [NonSerialized] InputAction jump;
        [NonSerialized] InputAction attack;
        [NonSerialized] InputAction interact;
        [NonSerialized] InputAction crouch;
        [NonSerialized] InputAction sprint;
        [NonSerialized] InputAction previous;
        [NonSerialized] InputAction next;

        Action jumpPressed;
        Action jumpReleased;
        Action attackPressed;
        Action attackReleased;
        Action interactPressed;
        Action interactReleased;
        Action crouchPressed;
        Action crouchReleased;
        Action sprintPressed;
        Action sprintReleased;
        Action previousPressed;
        Action nextPressed;

        // ---------- Values you can read at any time ----------

        /// <summary>The movement direction: x is left/right, y is down/up. Never longer than 1.</summary>
        public Vector2 Move => Bind() && move != null ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;

        /// <summary>The look or aim input, from the mouse or the right stick.</summary>
        public Vector2 Look => Bind() && look != null ? look.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>True while the Jump button is held down.</summary>
        public bool JumpHeld => IsHeld(jump);

        /// <summary>True while the Attack button is held down.</summary>
        public bool AttackHeld => IsHeld(attack);

        /// <summary>True while the Interact button is held down.</summary>
        public bool InteractHeld => IsHeld(interact);

        /// <summary>True while the Crouch button is held down.</summary>
        public bool CrouchHeld => IsHeld(crouch);

        /// <summary>True while the Sprint button is held down.</summary>
        public bool SprintHeld => IsHeld(sprint);

        // ---------- Events you can listen to ----------

        /// <summary>Raised at the moment the Jump button is pressed.</summary>
        public event Action JumpPressed { add { Bind(); jumpPressed += value; } remove { jumpPressed -= value; } }

        /// <summary>Raised at the moment the Jump button is let go.</summary>
        public event Action JumpReleased { add { Bind(); jumpReleased += value; } remove { jumpReleased -= value; } }

        /// <summary>Raised at the moment the Attack button is pressed.</summary>
        public event Action AttackPressed { add { Bind(); attackPressed += value; } remove { attackPressed -= value; } }

        /// <summary>Raised at the moment the Attack button is let go.</summary>
        public event Action AttackReleased { add { Bind(); attackReleased += value; } remove { attackReleased -= value; } }

        /// <summary>Raised at the moment the Interact button is pressed.</summary>
        public event Action InteractPressed { add { Bind(); interactPressed += value; } remove { interactPressed -= value; } }

        /// <summary>Raised at the moment the Interact button is let go.</summary>
        public event Action InteractReleased { add { Bind(); interactReleased += value; } remove { interactReleased -= value; } }

        /// <summary>Raised at the moment the Crouch button is pressed.</summary>
        public event Action CrouchPressed { add { Bind(); crouchPressed += value; } remove { crouchPressed -= value; } }

        /// <summary>Raised at the moment the Crouch button is let go.</summary>
        public event Action CrouchReleased { add { Bind(); crouchReleased += value; } remove { crouchReleased -= value; } }

        /// <summary>Raised at the moment the Sprint button is pressed.</summary>
        public event Action SprintPressed { add { Bind(); sprintPressed += value; } remove { sprintPressed -= value; } }

        /// <summary>Raised at the moment the Sprint button is let go.</summary>
        public event Action SprintReleased { add { Bind(); sprintReleased += value; } remove { sprintReleased -= value; } }

        /// <summary>Raised at the moment the Previous button is pressed (for example, previous weapon).</summary>
        public event Action PreviousPressed { add { Bind(); previousPressed += value; } remove { previousPressed -= value; } }

        /// <summary>Raised at the moment the Next button is pressed (for example, next weapon).</summary>
        public event Action NextPressed { add { Bind(); nextPressed += value; } remove { nextPressed -= value; } }

        // ---------- Turning player input on and off ----------

        /// <summary>
        /// Whether the player's controls are switched on. Switch them off during a
        /// cutscene or a dialogue, and back on afterwards. Menus keep working.
        /// </summary>
        public bool PlayerInputEnabled
        {
            get => Bind() && playerMap.enabled;
            set
            {
                if (!Bind())
                {
                    return;
                }

                if (value)
                {
                    playerMap.Enable();
                }
                else
                {
                    playerMap.Disable();
                }
            }
        }

        /// <summary>Switches the player's controls on.</summary>
        public void EnablePlayerInput()
        {
            PlayerInputEnabled = true;
        }

        /// <summary>Switches the player's controls off.</summary>
        public void DisablePlayerInput()
        {
            PlayerInputEnabled = false;
        }

        // ---------- How it works ----------

        bool IsHeld(InputAction action)
        {
            return Bind() && action != null && action.IsPressed();
        }

        // Connects to the input actions the first time anything asks for input.
        bool Bind()
        {
            if (bound)
            {
                return true;
            }

            if (bindFailed || !Application.isPlaying)
            {
                return false;
            }

            InputActionAsset asset = actions != null ? actions : InputSystem.actions;

            if (asset == null)
            {
                bindFailed = true;
                Debug.LogError($"Input Reader \"{name}\": there are no input actions to read. Set Actions on this asset, or set the project's actions in Project Settings > Input System Package.", this);
                return false;
            }

            playerMap = asset.FindActionMap(PlayerMapName);

            if (playerMap == null)
            {
                bindFailed = true;
                Debug.LogError($"Input Reader \"{name}\": the input actions have no action map named \"{PlayerMapName}\".", this);
                return false;
            }

            move = FindAction("Move", false);
            look = FindAction("Look", false);
            jump = FindAction("Jump", true);
            attack = FindAction("Attack", true);
            interact = FindAction("Interact", true);
            crouch = FindAction("Crouch", true);
            sprint = FindAction("Sprint", true);
            previous = FindAction("Previous", true);
            next = FindAction("Next", true);

            if (!playerMap.enabled)
            {
                playerMap.Enable();
            }

            bound = true;
            return true;
        }

        InputAction FindAction(string actionName, bool isButton)
        {
            InputAction action = playerMap.FindAction(actionName);

            if (action == null)
            {
                Debug.LogWarning($"Input Reader \"{name}\": the \"{PlayerMapName}\" action map has no action named \"{actionName}\", so that input will do nothing.", this);
                return null;
            }

            if (isButton)
            {
                action.started += OnButtonStarted;
                action.canceled += OnButtonCanceled;
            }

            return action;
        }

        void Unbind()
        {
            StopListening(jump);
            StopListening(attack);
            StopListening(interact);
            StopListening(crouch);
            StopListening(sprint);
            StopListening(previous);
            StopListening(next);

            bound = false;
            bindFailed = false;
        }

        void StopListening(InputAction action)
        {
            if (action != null)
            {
                action.started -= OnButtonStarted;
                action.canceled -= OnButtonCanceled;
            }
        }

        // "started" is the moment a button goes down.
        void OnButtonStarted(InputAction.CallbackContext context)
        {
            InputAction action = context.action;

            if (action == jump) jumpPressed?.Invoke();
            else if (action == attack) attackPressed?.Invoke();
            else if (action == interact) interactPressed?.Invoke();
            else if (action == crouch) crouchPressed?.Invoke();
            else if (action == sprint) sprintPressed?.Invoke();
            else if (action == previous) previousPressed?.Invoke();
            else if (action == next) nextPressed?.Invoke();
        }

        // "canceled" is the moment a button comes back up.
        void OnButtonCanceled(InputAction.CallbackContext context)
        {
            InputAction action = context.action;

            if (action == jump) jumpReleased?.Invoke();
            else if (action == attack) attackReleased?.Invoke();
            else if (action == interact) interactReleased?.Invoke();
            else if (action == crouch) crouchReleased?.Invoke();
            else if (action == sprint) sprintReleased?.Invoke();
        }

        void OnEnable()
        {
            if (!loadedReaders.Contains(this))
            {
                loadedReaders.Add(this);
            }
        }

        void OnDisable()
        {
            loadedReaders.Remove(this);
        }

        // Disconnects every Input Reader when Play mode starts, so each one
        // connects fresh. This matters when "Reload Domain" is turned off in
        // the Enter Play Mode settings.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void UnbindAllWhenPlayStarts()
        {
            for (int i = 0; i < loadedReaders.Count; i++)
            {
                if (loadedReaders[i] != null)
                {
                    loadedReaders[i].Unbind();
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor only. Finds an Input Reader asset in the project, so components
        /// can fill in their Input field by themselves when they are added.
        /// </summary>
        public static InputReader FindInProject()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:InputReader");

            if (guids.Length == 0)
            {
                return null;
            }

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<InputReader>(path);
        }
#endif
    }
}
