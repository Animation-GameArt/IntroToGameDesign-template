using System.Collections;
using CollinGame.Core;
using CollinGame.ThreeD;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.Platformer3D
{
    /// <summary>
    /// A platform that shakes when the player stands on it, falls away, and
    /// comes back after a while.
    ///
    /// Setup: a solid collider and a renderer on the same object.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Collin Game/3D Platformer Kit/Crumbling Platform 3D")]
    public class CrumblingPlatform3D : MonoBehaviour
    {
        [Tooltip("Seconds it holds the player before falling.")]
        [SerializeField, Min(0f)] float holdTime = 0.6f;

        [Tooltip("Seconds until it comes back. 0 means never.")]
        [SerializeField, Min(0f)] float returnTime = 3f;

        [Tooltip("How far it shakes while about to fall.")]
        [SerializeField, Min(0f)] float shake = 0.05f;

        [SerializeField] UnityEvent onCrumble;
        [SerializeField] UnityEvent onReturn;

        Collider solid;
        Renderer[] renderers;
        Vector3 home;
        bool crumbling;

        void Awake()
        {
            solid = GetComponent<Collider>();
            renderers = GetComponentsInChildren<Renderer>();
            home = transform.position;
        }

        void Update()
        {
            if (!crumbling && !PauseController.GamePaused && PlayerOnTop())
            {
                StartCoroutine(Crumble());
            }
        }

        bool PlayerOnTop()
        {
            Bounds bounds = solid.bounds;
            Vector3 center = new Vector3(bounds.center.x, bounds.max.y + 0.1f, bounds.center.z);
            Vector3 half = new Vector3(bounds.extents.x * 0.95f, 0.1f, bounds.extents.z * 0.95f);

            foreach (Collider hit in Physics.OverlapBox(center, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                PlayerController3D player = hit.GetComponentInParent<PlayerController3D>();

                if (player != null && player.IsGrounded)
                {
                    return true;
                }
            }

            return false;
        }

        IEnumerator Crumble()
        {
            crumbling = true;
            float end = Time.time + holdTime;

            while (Time.time < end)
            {
                transform.position = home + Random.insideUnitSphere * shake;
                yield return null;
            }

            transform.position = home;
            SetPresent(false);
            onCrumble?.Invoke();

            if (returnTime <= 0f)
            {
                yield break;
            }

            yield return new WaitForSeconds(returnTime);
            SetPresent(true);
            onReturn?.Invoke();
            crumbling = false;
        }

        void SetPresent(bool present)
        {
            solid.enabled = present;

            foreach (Renderer shown in renderers)
            {
                shown.enabled = present;
            }
        }
    }
}
