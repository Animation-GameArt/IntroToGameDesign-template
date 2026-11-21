using CollinGame.Core;
using CollinGame.TwoD;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CollinGame.Kits.Farming
{
    /// <summary>
    /// Lets the player farm. The Attack button (left click, a tap, Enter, or
    /// the gamepad's left face button) uses the selected item on a cell of the
    /// Farm Field: the cell in front of the player, or the cell clicked or
    /// tapped if it's within reach. A ripe crop is harvested whatever is
    /// selected.
    ///
    /// The number keys 1 to 9, the mouse wheel, and the Previous and Next
    /// buttons choose the hotbar slot.
    ///
    /// Add it to the player, with a Top Down Controller 2D and an Inventory.
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    [AddComponentMenu("Collin Game/Farming Kit/Farmer Tools 2D")]
    public class FarmerTools2D : MonoBehaviour
    {
        [Tooltip("Optional. Found in the project if empty.")]
        [SerializeField] InputReader input;

        [Tooltip("Optional. The field. Found in the scene if empty.")]
        [SerializeField] FarmField field;

        [Tooltip("How far from the player, in cells, a clicked or tapped cell can be.")]
        [SerializeField, Min(1f)] float reach = 1.6f;

        [Tooltip("Moves the point the player stands on from the pivot, such as down to the feet.")]
        [SerializeField] Vector2 feetOffset = Vector2.zero;

        [Tooltip("Optional. A sprite moved onto the cell the player will work on, such as a square outline. It shows only over the field.")]
        [SerializeField] Transform cursor;

        [Tooltip("Seconds between uses, so holding the button down doesn't till a whole row.")]
        [SerializeField, Min(0f)] float useCooldown = 0.2f;

        [Header("Energy")]

        [Tooltip("Optional. A number variable for energy. Each use of a tool, seeds or the watering can costs Energy Per Use; with too little, the player must sleep. Add it to the Day Cycle's Reset Each Morning.")]
        [SerializeField] IntVariable energy;

        [Tooltip("Energy each use costs. Harvesting is free.")]
        [SerializeField, Min(0)] int energyPerUse = 1;

        [Header("Sounds and effects")]

        [SerializeField] UnityEvent onTill;
        [SerializeField] UnityEvent onWater;
        [SerializeField] UnityEvent onPlant;
        [SerializeField] UnityEvent onHarvest;
        [SerializeField] UnityEvent onClear;

        [Tooltip("Runs when a use does nothing, such as watering grass.")]
        [SerializeField] UnityEvent onNothing;

        Inventory inventory;
        TopDownController2D mover;
        Camera mainCamera;
        Vector3Int target;
        bool targetValid;
        float nextUse;

        /// <summary>The cell the player will work on next.</summary>
        public Vector3Int TargetCell => target;

        void Awake()
        {
            inventory = GetComponent<Inventory>();
            mover = GetComponent<TopDownController2D>();

            if (input == null)
            {
                input = InputReader.FindInProject();
            }
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.AttackPressed += Use;
                input.PreviousPressed += OnPrevious;
                input.NextPressed += OnNext;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.AttackPressed -= Use;
                input.PreviousPressed -= OnPrevious;
                input.NextPressed -= OnNext;
            }
        }

        void Start()
        {
            mainCamera = Camera.main;

            if (field == null)
            {
                field = FarmField.Instance != null ? FarmField.Instance : FindFirstObjectByType<FarmField>();
            }

            if (field == null)
            {
                Debug.LogWarning($"Farmer Tools 2D on \"{name}\": there is no Farm Field in the scene.", this);
            }
        }

        void Update()
        {
            if (field == null)
            {
                return;
            }

            bool canAct = !PauseController.GamePaused && (input == null || input.PlayerInputEnabled);

            if (canAct)
            {
                ChooseSlotFromKeys();
            }

            // Aim at the cell under the mouse if it's within reach; otherwise in front of the player.
            target = FrontCell();

            Mouse mouse = Mouse.current;

            if (mouse != null && mainCamera != null)
            {
                Vector2 screen = mouse.position.ReadValue();

                if (!FarmMessage.PointerOverUI(screen) && TryPointerCell(screen, out Vector3Int hovered))
                {
                    target = hovered;
                }
            }

            // The cursor only shows over the field.
            targetValid = field.IsFarmland(target) || field.IsTilled(target);

            if (cursor != null)
            {
                cursor.gameObject.SetActive(canAct && targetValid);
                cursor.position = field.CenterOf(target);
            }
        }

        void ChooseSlotFromKeys()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                for (int i = 0; i < 9 && i < inventory.Size; i++)
                {
                    if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                    {
                        inventory.Select(i);
                    }
                }
            }

            Mouse mouse = Mouse.current;

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;

                if (scroll > 0.1f) inventory.SelectNext(-1);
                else if (scroll < -0.1f) inventory.SelectNext(1);
            }
        }

        // The Previous and Next actions are also on the 1 and 2 keys, which already choose slots directly.
        static bool DigitKeyPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.digit1Key.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame);
        }

        void OnPrevious()
        {
            if (!DigitKeyPressed() && !PauseController.GamePaused) inventory.SelectNext(-1);
        }

        void OnNext()
        {
            if (!DigitKeyPressed() && !PauseController.GamePaused) inventory.SelectNext(1);
        }

        // ---------------------------------------------------------------- Aiming

        Vector3Int StandingCell()
        {
            return field.CellAt(transform.position + (Vector3)feetOffset);
        }

        Vector3Int FrontCell()
        {
            Vector2 facing = mover != null ? mover.FacingDirection : Vector2.down;
            Vector3Int step = Mathf.Abs(facing.x) >= Mathf.Abs(facing.y)
                ? new Vector3Int(facing.x >= 0f ? 1 : -1, 0, 0)
                : new Vector3Int(0, facing.y >= 0f ? 1 : -1, 0);
            return StandingCell() + step;
        }

        bool TryPointerCell(Vector2 screen, out Vector3Int cell)
        {
            Vector3 world = mainCamera.ScreenToWorldPoint(screen);
            world.z = 0f;
            cell = field.CellAt(world);
            Vector3 feet = transform.position + (Vector3)feetOffset;
            Vector3 center = field.CenterOf(cell);
            float cellSize = Mathf.Max(0.01f, Vector3.Distance(field.CenterOf(Vector3Int.zero), field.CenterOf(Vector3Int.right)));
            return Vector2.Distance(feet, center) <= reach * cellSize;
        }

        // ---------------------------------------------------------------- Using items

        void Use()
        {
            if (field == null || PauseController.GamePaused || Time.time < nextUse)
            {
                return;
            }

            if (DayCycle.Instance != null && (DayCycle.Instance.IsOver || DayCycle.Instance.WaitingForReport))
            {
                return;
            }

            Vector3Int cell = FrontCell();

            // A click or tap works on the cell under it, if it's within reach.
            Vector2? pointer = PressedPointerPosition();

            if (pointer.HasValue)
            {
                if (FarmMessage.PointerOverUI(pointer.Value))
                {
                    return;
                }

                if (mainCamera != null && TryPointerCell(pointer.Value, out Vector3Int clicked))
                {
                    cell = clicked;
                }
                else if (Touchscreen.current != null && Mouse.current == null)
                {
                    // A tap out of reach on a touch screen does nothing.
                    return;
                }
            }

            nextUse = Time.time + useCooldown;
            UseOn(cell);
        }

        static Vector2? PressedPointerPosition()
        {
            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return mouse.position.ReadValue();
            }

            Touchscreen touch = Touchscreen.current;

            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                return touch.primaryTouch.position.ReadValue();
            }

            return null;
        }

        void UseOn(Vector3Int cell)
        {
            // Ripe crops are harvested whatever is selected.
            if (field.IsRipe(cell))
            {
                CropData crop = field.CropAt(cell);

                if (crop.Produce != null && !inventory.HasRoomFor(crop.Produce, crop.Amount))
                {
                    FarmMessage.Say("Your bag is full.");
                    onNothing?.Invoke();
                    return;
                }

                field.Harvest(cell);

                if (crop.Produce != null)
                {
                    inventory.Add(crop.Produce, crop.Amount);
                }

                onHarvest?.Invoke();
                return;
            }

            ItemData item = inventory.Selected;

            if (item == null || item.Use == ItemUse.Nothing)
            {
                onNothing?.Invoke();
                return;
            }

            if (energy != null && energy.Value < energyPerUse)
            {
                FarmMessage.Say("You're too tired. Go to bed.");
                onNothing?.Invoke();
                return;
            }

            bool worked = false;

            switch (item.Use)
            {
                case ItemUse.Till:
                    worked = field.Till(cell);
                    if (worked) onTill?.Invoke();
                    break;

                case ItemUse.Water:
                    worked = field.Water(cell);
                    if (worked) onWater?.Invoke();
                    break;

                case ItemUse.Plant:
                    if (!field.IsTilled(cell))
                    {
                        FarmMessage.Say("Till the ground first.");
                    }
                    else
                    {
                        worked = field.Plant(cell, item.Crop);
                        if (worked) onPlant?.Invoke();
                    }
                    break;

                case ItemUse.Clear:
                    worked = field.Clear(cell);
                    if (worked) onClear?.Invoke();
                    break;
            }

            if (!worked)
            {
                onNothing?.Invoke();
                return;
            }

            if (item.UsedUp)
            {
                inventory.Remove(item, 1);
            }

            if (energy != null)
            {
                energy.Add(-energyPerUse);
            }
        }
    }
}
