using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// A place the player returns to after dying. When an object with a Respawner
    /// touches the checkpoint, its respawn point moves here.
    ///
    /// Setup: this GameObject needs a collider with Is Trigger ticked.
    /// Works in 2D and 3D.
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Checkpoint")]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Only objects with this tag can use the checkpoint. Leave empty to let anything use it.")]
        [SerializeField] string activatorTag = "Player";

        [Tooltip("Optional. The exact place to reappear. Leave empty to use this object's position.")]
        [SerializeField] Transform respawnPoint;

        [Header("When activated")]

        [Tooltip("Optional. A sprite that changes color when the checkpoint is reached.")]
        [SerializeField] SpriteRenderer indicator;

        [Tooltip("The color the sprite changes to.")]
        [SerializeField] Color activeColor = Color.green;

        [Tooltip("Optional. An event to raise, so other objects can react.")]
        [SerializeField] VoidEventChannel raiseOnActivated;

        [Tooltip("Optional. Extra things to do, such as playing a sound.")]
        [SerializeField] UnityEvent onActivated;

        /// <summary>True once the checkpoint has been reached.</summary>
        public bool IsActive { get; private set; }

        void OnTriggerEnter2D(Collider2D other)
        {
            Activate(TouchUtility.OwnerOf(other));
        }

        void OnTriggerEnter(Collider other)
        {
            Activate(TouchUtility.OwnerOf(other));
        }

        void Activate(GameObject visitor)
        {
            if (IsActive || !TouchUtility.HasTag(visitor, activatorTag))
            {
                return;
            }

            Respawner respawner = visitor.GetComponent<Respawner>();

            if (respawner == null)
            {
                Debug.LogWarning($"Checkpoint \"{name}\": \"{visitor.name}\" touched it but has no Respawner component, so the checkpoint can't be saved. Add a Respawner to \"{visitor.name}\".", this);
                return;
            }

            respawner.SetRespawnPoint(respawnPoint != null ? respawnPoint.position : transform.position);
            IsActive = true;

            if (indicator != null)
            {
                indicator.color = activeColor;
            }

            if (raiseOnActivated != null)
            {
                raiseOnActivated.Raise();
            }

            onActivated?.Invoke();
        }
    }
}
