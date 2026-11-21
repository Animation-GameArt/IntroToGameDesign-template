using UnityEngine;

namespace CollinGame.Kits.Farming
{
    /// <summary>
    /// A crop: how it looks as it grows, how long it takes, and what it gives.
    /// A crop grows one day for every night it was watered.
    /// Create one with Create > Collin Game > Farming Kit > Crop.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Farming Kit/Crop", fileName = "New Crop")]
    public class CropData : ScriptableObject
    {
        [Tooltip("The name the player sees.")]
        [SerializeField] string displayName = "Crop";

        [Tooltip("A sprite for each stage, from just planted to ready to harvest. The last one is the ripe crop.")]
        [SerializeField] Sprite[] stages = new Sprite[2];

        [Tooltip("Watered days to reach the next stage.")]
        [SerializeField, Min(1)] int daysPerStage = 2;

        [Header("Harvest")]

        [Tooltip("The item the player gets when harvesting.")]
        [SerializeField] ItemData produce;

        [Tooltip("How many the player gets.")]
        [SerializeField, Min(1)] int amount = 1;

        [Tooltip("0: the crop is gone after harvesting. More: it grows back, ripe again after this many watered days.")]
        [SerializeField, Min(0)] int regrowDays;

        [Header("Neglect")]

        [Tooltip("The crop dies after this many days in a row without water. 0 means it never dies; it just waits.")]
        [SerializeField, Min(0)] int diesAfterDryDays;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public int StageCount => stages != null ? stages.Length : 0;
        public int DaysPerStage => daysPerStage;
        public ItemData Produce => produce;
        public int Amount => amount;
        public int RegrowDays => regrowDays;
        public int DiesAfterDryDays => diesAfterDryDays;

        /// <summary>Watered days from planting to ripe.</summary>
        public int DaysToRipe => daysPerStage * Mathf.Max(0, StageCount - 1);

        /// <summary>The stage for a number of watered days.</summary>
        public int StageFor(int grownDays)
        {
            return Mathf.Clamp(grownDays / daysPerStage, 0, Mathf.Max(0, StageCount - 1));
        }

        public Sprite SpriteFor(int grownDays)
        {
            return StageCount > 0 ? stages[StageFor(grownDays)] : null;
        }

        void OnValidate()
        {
            if (stages == null || stages.Length < 2)
            {
                System.Array.Resize(ref stages, 2);
            }
        }
    }
}
