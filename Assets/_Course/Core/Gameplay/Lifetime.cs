using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Removes this object after a number of seconds. Use it on things that
    /// shouldn't stay forever: falling rocks, projectiles, effects.
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Lifetime")]
    public class Lifetime : MonoBehaviour
    {
        /// <summary>How the object is removed.</summary>
        public enum RemoveAction
        {
            /// <summary>The object is hidden and switched off. A Spawner can reuse it.</summary>
            Deactivate,

            /// <summary>The object is removed from the scene.</summary>
            Destroy
        }

        [Tooltip("How long this object lasts, in seconds.")]
        [SerializeField, Min(0f)] float seconds = 5f;

        [Tooltip("How the object is removed when its time is up.")]
        [SerializeField] RemoveAction whenTimeIsUp = RemoveAction.Deactivate;

        float timer;

        void OnEnable()
        {
            timer = 0f;
        }

        void Update()
        {
            timer += Time.deltaTime;

            if (timer < seconds)
            {
                return;
            }

            if (whenTimeIsUp == RemoveAction.Destroy)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
