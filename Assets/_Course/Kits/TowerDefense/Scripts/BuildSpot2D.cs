using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TowerDefense
{
    /// <summary>
    /// A place where a tower can be built. Clicking it opens the build menu;
    /// clicking it again once a tower is built offers to upgrade or sell it.
    ///
    /// Setup: a sprite (such as a stone pad) with a 2D collider for clicking,
    /// placed beside the path.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Collin Game/Tower Defense Kit/Build Spot 2D")]
    public class BuildSpot2D : MonoBehaviour
    {
        [Tooltip("Runs when a tower is built here.")]
        [SerializeField] UnityEvent onBuilt;

        [Tooltip("Runs when the tower here is sold.")]
        [SerializeField] UnityEvent onSold;

        /// <summary>The tower on this spot, or null.</summary>
        public Tower2D Tower { get; private set; }

        /// <summary>Builds a tower here if the money allows. Returns true if it was built.</summary>
        public bool Build(TowerData data, IntVariable money)
        {
            if (Tower != null || data == null || data.Prefab == null || !CanAfford(data.Cost, money))
            {
                return false;
            }

            Spend(data.Cost, money);
            Tower = Instantiate(data.Prefab, transform.position, Quaternion.identity);
            Tower.Setup(data, this, data.Cost);
            onBuilt?.Invoke();
            return true;
        }

        /// <summary>Upgrades the tower here to its next level, if the money allows.</summary>
        public bool Upgrade(IntVariable money)
        {
            if (Tower == null || Tower.Data.UpgradesTo == null)
            {
                return false;
            }

            TowerData next = Tower.Data.UpgradesTo;

            if (!CanAfford(next.Cost, money))
            {
                return false;
            }

            Spend(next.Cost, money);
            int spent = Tower.Spent + next.Cost;

            // A new prefab for the new level, if it has one.
            if (next.Prefab != null)
            {
                Destroy(Tower.gameObject);
                Tower = Instantiate(next.Prefab, transform.position, Quaternion.identity);
            }

            Tower.Setup(next, this, spent);
            return true;
        }

        /// <summary>Sells the tower here, giving back part of what was spent.</summary>
        public void Sell(IntVariable money)
        {
            if (Tower == null)
            {
                return;
            }

            if (money != null)
            {
                money.Add(SellValue);
            }

            Destroy(Tower.gameObject);
            Tower = null;
            onSold?.Invoke();
        }

        /// <summary>What selling the tower here would give back.</summary>
        public int SellValue => Tower != null ? Mathf.FloorToInt(Tower.Spent * Tower.Data.SellShare) : 0;

        static bool CanAfford(int cost, IntVariable money) => money == null || money.Value >= cost;

        static void Spend(int cost, IntVariable money)
        {
            if (money != null)
            {
                money.Add(-cost);
            }
        }
    }
}
