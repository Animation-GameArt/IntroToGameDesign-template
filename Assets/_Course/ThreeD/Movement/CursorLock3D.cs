using CollinGame.Core;
using UnityEngine;

namespace CollinGame.ThreeD
{
    /// <summary>
    /// Hides and locks the mouse pointer while playing, so moving the mouse
    /// turns the camera instead of leaving the window. The pointer comes back
    /// while the game is paused, while the player's controls are off, and
    /// after Unlock is called (such as from a game over screen's event).
    ///
    /// Add it to any object in a 3D scene whose camera follows the mouse.
    /// </summary>
    [AddComponentMenu("Collin Game/3D/Cursor Lock 3D")]
    public class CursorLock3D : MonoBehaviour
    {
        [Tooltip("Optional. Found in the project if empty.")]
        [SerializeField] InputReader input;

        bool unlocked;

        void Awake()
        {
            if (input == null)
            {
                input = InputReader.FindInProject();
            }
        }

        /// <summary>Gives the pointer back for good, such as when a game over screen shows.</summary>
        public void Unlock()
        {
            unlocked = true;
            Apply(false);
        }

        /// <summary>Locks the pointer again after Unlock.</summary>
        public void Lock()
        {
            unlocked = false;
        }

        void Update()
        {
            bool playing = !unlocked && !PauseController.GamePaused && (input == null || input.PlayerInputEnabled);
            Apply(playing);
        }

        void OnDisable()
        {
            Apply(false);
        }

        static void Apply(bool locked)
        {
            CursorLockMode mode = locked ? CursorLockMode.Locked : CursorLockMode.None;

            if (Cursor.lockState != mode)
            {
                Cursor.lockState = mode;
                Cursor.visible = !locked;
            }
        }
    }
}
