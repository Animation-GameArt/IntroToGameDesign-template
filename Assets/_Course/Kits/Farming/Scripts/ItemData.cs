using UnityEngine;

namespace CollinGame.Kits.Farming
{
    /// <summary>What happens when the player uses an item on the ground.</summary>
    public enum ItemUse
    {
        /// <summary>Nothing. Crops and other things to sell.</summary>
        Nothing,
        /// <summary>A hoe: turns farmland into soil that can be planted.</summary>
        Till,
        /// <summary>A watering can: waters soil, so crops on it grow overnight.</summary>
        Water,
        /// <summary>Seeds: plants this item's crop in tilled soil.</summary>
        Plant,
        /// <summary>A scythe or shovel: removes a crop, and untills the soil.</summary>
        Clear
    }

    /// <summary>
    /// Anything that goes in the inventory: a tool, seeds, or a crop to sell.
    /// Create one with Create > Collin Game > Farming Kit > Item.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Farming Kit/Item", fileName = "New Item")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("The name the player sees.")]
        [SerializeField] string displayName = "Item";

        [Tooltip("The picture in the hotbar and the shop.")]
        [SerializeField] Sprite icon;

        [Tooltip("Optional. A sentence for the shop.")]
        [SerializeField, TextArea(1, 3)] string description;

        [Header("Using it")]

        [Tooltip("What happens when the player uses this item on the ground.")]
        [SerializeField] ItemUse use = ItemUse.Nothing;

        [Tooltip("For seeds: the crop they grow.")]
        [SerializeField] CropData crop;

        [Tooltip("Ticked: one is used up each time, like seeds. Unticked: it lasts forever, like a hoe.")]
        [SerializeField] bool usedUp;

        [Header("Shop")]

        [Tooltip("The price in the shop. 0 means the shop doesn't sell it.")]
        [SerializeField, Min(0)] int buyPrice;

        [Tooltip("What the shop pays for one. 0 means it can't be sold.")]
        [SerializeField, Min(0)] int sellPrice;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public string Description => description;
        public ItemUse Use => use;
        public CropData Crop => crop;
        public bool UsedUp => usedUp;
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;
    }
}
