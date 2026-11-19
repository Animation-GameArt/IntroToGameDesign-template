using UnityEngine;
using UnityEngine.InputSystem;

namespace CollinGame.Kits.PhysicsPuzzle
{
    /// <summary>
    /// Reads the mouse or a touch, in world positions. Shared by the kit's
    /// Launcher and Cutter.
    /// </summary>
    public static class PointerUtility
    {
        /// <summary>True on the frame the mouse button or a finger goes down.</summary>
        public static bool PressedThisFrame => Pointer.current != null && Pointer.current.press.wasPressedThisFrame;

        /// <summary>True on the frame the mouse button or a finger comes up.</summary>
        public static bool ReleasedThisFrame => Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;

        /// <summary>True while the mouse button or a finger is down.</summary>
        public static bool IsPressed => Pointer.current != null && Pointer.current.press.isPressed;

        /// <summary>Where the pointer is in the world, on the plane z = 0.</summary>
        public static bool TryGetWorldPosition(out Vector2 position)
        {
            position = Vector2.zero;
            Camera camera = Camera.main;

            if (Pointer.current == null || camera == null)
            {
                return false;
            }

            Vector2 screen = Pointer.current.position.ReadValue();
            Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
            position = world;
            return true;
        }
    }
}
