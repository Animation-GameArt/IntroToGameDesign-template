using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Small helpers shared by components that react to being touched,
    /// such as Trigger Zone, Kill Zone, Checkpoint and Damage On Touch.
    /// </summary>
    public static class TouchUtility
    {
        /// <summary>
        /// The object that "owns" a collider: the one with the Rigidbody if there is one.
        /// This way a collider on a child of the player still counts as the player.
        /// </summary>
        public static GameObject OwnerOf(Collider2D other)
        {
            return other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        }

        /// <summary>The object that "owns" a collider: the one with the Rigidbody if there is one.</summary>
        public static GameObject OwnerOf(Collider other)
        {
            return other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        }

        /// <summary>The other object in a 2D collision.</summary>
        public static GameObject OwnerOf(Collision2D collision)
        {
            return collision.rigidbody != null ? collision.rigidbody.gameObject : collision.gameObject;
        }

        /// <summary>The other object in a 3D collision.</summary>
        public static GameObject OwnerOf(Collision collision)
        {
            return collision.rigidbody != null ? collision.rigidbody.gameObject : collision.gameObject;
        }

        /// <summary>True if the object has the tag, or if no tag was asked for.</summary>
        public static bool HasTag(GameObject target, string tag)
        {
            return string.IsNullOrEmpty(tag) || target.CompareTag(tag);
        }
    }
}
