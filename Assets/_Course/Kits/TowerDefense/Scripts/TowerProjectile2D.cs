using UnityEngine;

namespace CollinGame.Kits.TowerDefense
{
    /// <summary>
    /// A tower's arrow, cannonball or bolt. It flies to its target (following it
    /// as it moves), then hits. If the target is defeated first, it flies on to
    /// where the target was.
    ///
    /// Setup: a small sprite pointing right. No collider or Rigidbody is needed.
    /// </summary>
    [AddComponentMenu("Collin Game/Tower Defense Kit/Tower Projectile 2D")]
    public class TowerProjectile2D : MonoBehaviour
    {
        [Tooltip("Optional. A prefab created where it hits, such as an explosion for a cannon.")]
        [SerializeField] GameObject hitEffect;

        TowerData data;
        PathEnemy2D target;
        Vector2 lastTargetPosition;
        float speed;
        bool launched;

        /// <summary>Sends the projectile at a target. Towers call this.</summary>
        public void Launch(TowerData towerData, PathEnemy2D enemy, float projectileSpeed)
        {
            data = towerData;
            target = enemy;
            speed = projectileSpeed;
            lastTargetPosition = enemy != null ? (Vector2)enemy.transform.position : (Vector2)transform.position;
            launched = true;
        }

        void Update()
        {
            if (!launched)
            {
                return;
            }

            if (target != null && target.isActiveAndEnabled && !target.Health.IsDead)
            {
                lastTargetPosition = target.transform.position;
            }
            else
            {
                target = null;
            }

            Vector2 position = transform.position;
            Vector2 toTarget = lastTargetPosition - position;
            float step = speed * Time.deltaTime;

            if (toTarget.magnitude <= step)
            {
                transform.position = lastTargetPosition;
                Arrive();
                return;
            }

            Vector2 direction = toTarget.normalized;
            transform.position = position + direction * step;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        void Arrive()
        {
            if (data != null)
            {
                data.HitAt(target, lastTargetPosition);
            }

            if (hitEffect != null)
            {
                Instantiate(hitEffect, lastTargetPosition, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
