using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CollinGame.Kits.Farming
{
    /// <summary>
    /// The field. Any cell painted on the Farmland tilemap can be tilled; tilled
    /// cells can be planted and watered. Every night (from the Day Cycle),
    /// watered crops grow one day and the soil dries out.
    ///
    /// Put it on the Grid that holds the tilemaps. The Farmer Tools call Till,
    /// Water, Plant, Harvest and Clear.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Farming Kit/Farm Field")]
    public class FarmField : MonoBehaviour
    {
        class Plot
        {
            public bool watered;
            public CropData crop;
            public int grownDays;
            public int dryDays;
            public int emptyDays;
            public SpriteRenderer sprite;
        }

        [Header("Tilemaps")]

        [Tooltip("The tilemap marking where the player can farm. Paint the field on it, such as with grass.")]
        [SerializeField] Tilemap farmland;

        [Tooltip("The tilemap the tilled and watered soil is drawn on. Put it above Farmland, with a higher Order in Layer.")]
        [SerializeField] Tilemap soil;

        [Tooltip("The tile for tilled soil.")]
        [SerializeField] TileBase tilledTile;

        [Tooltip("The tile for watered soil, such as the same soil made darker (a Tile asset with a Color).")]
        [SerializeField] TileBase wateredTile;

        [Header("Crops")]

        [Tooltip("The Sorting Layer for crop sprites.")]
        [SerializeField] string cropSortingLayer = "Default";

        [Tooltip("The Order in Layer for crop sprites: above the soil, below the player.")]
        [SerializeField] int cropOrder = 5;

        [Tooltip("Moves crop sprites from the middle of their cell, such as up a little so they stand on the soil.")]
        [SerializeField] Vector2 cropOffset = Vector2.zero;

        [Header("Nights")]

        [Tooltip("Optional. The day cycle. Found in the scene if empty.")]
        [SerializeField] DayCycle dayCycle;

        [Tooltip("Tilled soil with nothing planted turns back into farmland after this many nights. 0 means never.")]
        [SerializeField, Min(0)] int emptySoilLasts = 3;

        [Tooltip("The chance of rain each night, from 0 to 1. Rain waters every tilled cell for the next day.")]
        [SerializeField, Range(0f, 1f)] float rainChance = 0.15f;

        readonly Dictionary<Vector3Int, Plot> plots = new Dictionary<Vector3Int, Plot>();

        /// <summary>The field in the open scene.</summary>
        public static FarmField Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        void Awake()
        {
            Instance = this;

            if (soil == null)
            {
                Debug.LogWarning($"Farm Field on \"{name}\": set the Soil tilemap.", this);
            }
        }

        void OnEnable()
        {
            if (dayCycle == null)
            {
                dayCycle = FindFirstObjectByType<DayCycle>();
            }

            if (dayCycle != null)
            {
                dayCycle.DayEnded += OnNight;
            }
        }

        void OnDisable()
        {
            if (dayCycle != null)
            {
                dayCycle.DayEnded -= OnNight;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ---------------------------------------------------------------- Looking at cells

        /// <summary>The cell at a world position.</summary>
        public Vector3Int CellAt(Vector3 world)
        {
            Tilemap map = soil != null ? soil : farmland;
            return map != null ? map.WorldToCell(world) : Vector3Int.FloorToInt(world);
        }

        /// <summary>The world position of a cell's middle.</summary>
        public Vector3 CenterOf(Vector3Int cell)
        {
            Tilemap map = soil != null ? soil : farmland;
            return map != null ? map.GetCellCenterWorld(cell) : cell + new Vector3(0.5f, 0.5f);
        }

        public bool IsFarmland(Vector3Int cell) => farmland != null && farmland.HasTile(cell);
        public bool IsTilled(Vector3Int cell) => plots.ContainsKey(cell);
        public bool IsWatered(Vector3Int cell) => plots.TryGetValue(cell, out Plot plot) && plot.watered;
        public bool HasCrop(Vector3Int cell) => plots.TryGetValue(cell, out Plot plot) && plot.crop != null;
        public CropData CropAt(Vector3Int cell) => plots.TryGetValue(cell, out Plot plot) ? plot.crop : null;

        /// <summary>True if the cell has a crop ready to harvest.</summary>
        public bool IsRipe(Vector3Int cell)
        {
            return plots.TryGetValue(cell, out Plot plot) && plot.crop != null && plot.grownDays >= plot.crop.DaysToRipe;
        }

        /// <summary>The number of cells with crops planted.</summary>
        public int CropCount
        {
            get
            {
                int count = 0;

                foreach (Plot plot in plots.Values)
                {
                    if (plot.crop != null) count++;
                }

                return count;
            }
        }

        // ---------------------------------------------------------------- Farming

        /// <summary>Turns farmland into tilled soil. Returns false if it can't be tilled.</summary>
        public bool Till(Vector3Int cell)
        {
            if (!IsFarmland(cell) || IsTilled(cell))
            {
                return false;
            }

            plots[cell] = new Plot();
            DrawSoil(cell);
            return true;
        }

        /// <summary>Waters tilled soil. Returns false if there's no soil, or it's already watered.</summary>
        public bool Water(Vector3Int cell)
        {
            if (!plots.TryGetValue(cell, out Plot plot) || plot.watered)
            {
                return false;
            }

            plot.watered = true;
            plot.dryDays = 0;
            DrawSoil(cell);
            return true;
        }

        /// <summary>Plants a crop in empty tilled soil. Returns false if it can't.</summary>
        public bool Plant(Vector3Int cell, CropData crop)
        {
            if (crop == null || !plots.TryGetValue(cell, out Plot plot) || plot.crop != null)
            {
                return false;
            }

            plot.crop = crop;
            plot.grownDays = 0;
            plot.dryDays = 0;
            plot.emptyDays = 0;
            DrawCrop(cell, plot);
            return true;
        }

        /// <summary>Harvests a ripe crop: gives back what it produces, or null. Crops that regrow stay.</summary>
        public CropData Harvest(Vector3Int cell)
        {
            if (!IsRipe(cell))
            {
                return null;
            }

            Plot plot = plots[cell];
            CropData crop = plot.crop;

            if (crop.RegrowDays > 0)
            {
                plot.grownDays = Mathf.Max(0, crop.DaysToRipe - crop.RegrowDays);
            }
            else
            {
                plot.crop = null;
                plot.grownDays = 0;
            }

            DrawCrop(cell, plot);
            return crop;
        }

        /// <summary>Removes whatever is planted, and untills the soil. Returns false if there was nothing.</summary>
        public bool Clear(Vector3Int cell)
        {
            if (!plots.TryGetValue(cell, out Plot plot))
            {
                return false;
            }

            if (plot.sprite != null)
            {
                Destroy(plot.sprite.gameObject);
            }

            plots.Remove(cell);

            if (soil != null)
            {
                soil.SetTile(cell, null);
            }

            return true;
        }

        // ---------------------------------------------------------------- Nights

        void OnNight(int newDay)
        {
            int died = 0;
            int ripened = 0;
            List<Vector3Int> revert = new List<Vector3Int>();

            foreach (KeyValuePair<Vector3Int, Plot> pair in plots)
            {
                Plot plot = pair.Value;

                if (plot.crop != null)
                {
                    bool wasRipe = plot.grownDays >= plot.crop.DaysToRipe;

                    if (plot.watered)
                    {
                        plot.grownDays = Mathf.Min(plot.grownDays + 1, plot.crop.DaysToRipe);
                        plot.dryDays = 0;
                    }
                    else if (!wasRipe)
                    {
                        plot.dryDays++;

                        if (plot.crop.DiesAfterDryDays > 0 && plot.dryDays >= plot.crop.DiesAfterDryDays)
                        {
                            plot.crop = null;
                            plot.grownDays = 0;
                            died++;
                        }
                    }

                    if (plot.crop != null && !wasRipe && plot.grownDays >= plot.crop.DaysToRipe)
                    {
                        ripened++;
                    }
                }
                else if (!plot.watered)
                {
                    plot.emptyDays++;

                    if (emptySoilLasts > 0 && plot.emptyDays >= emptySoilLasts)
                    {
                        revert.Add(pair.Key);
                    }
                }

                plot.watered = false;
            }

            foreach (Vector3Int cell in revert)
            {
                Clear(cell);
            }

            bool rained = plots.Count > 0 && Random.value < rainChance;

            foreach (KeyValuePair<Vector3Int, Plot> pair in plots)
            {
                pair.Value.watered = rained;
                DrawSoil(pair.Key);
                DrawCrop(pair.Key, pair.Value);
            }

            if (dayCycle != null)
            {
                if (ripened > 0) dayCycle.AddNote(ripened == 1 ? "A crop is ready to harvest." : $"{ripened} crops are ready to harvest.");
                if (died > 0) dayCycle.AddNote(died == 1 ? "A crop died without water." : $"{died} crops died without water.");
                if (revert.Count > 0) dayCycle.AddNote("Some empty soil grew over again.");
                if (rained) dayCycle.AddNote("It rained in the night, so the field is watered today.");
            }
        }

        // ---------------------------------------------------------------- Drawing

        void DrawSoil(Vector3Int cell)
        {
            if (soil == null || !plots.TryGetValue(cell, out Plot plot))
            {
                return;
            }

            TileBase tile = plot.watered && wateredTile != null ? wateredTile : tilledTile;
            soil.SetTile(cell, tile);
        }

        void DrawCrop(Vector3Int cell, Plot plot)
        {
            if (plot.crop == null)
            {
                if (plot.sprite != null)
                {
                    Destroy(plot.sprite.gameObject);
                    plot.sprite = null;
                }

                return;
            }

            if (plot.sprite == null)
            {
                GameObject go = new GameObject($"Crop {cell.x},{cell.y}");
                go.transform.SetParent(transform, false);
                go.transform.position = CenterOf(cell) + (Vector3)cropOffset;
                plot.sprite = go.AddComponent<SpriteRenderer>();
                plot.sprite.sortingLayerName = cropSortingLayer;
                plot.sprite.sortingOrder = cropOrder;
            }

            plot.sprite.gameObject.name = $"{plot.crop.DisplayName} {cell.x},{cell.y}";
            plot.sprite.sprite = plot.crop.SpriteFor(plot.grownDays);
        }
    }
}
