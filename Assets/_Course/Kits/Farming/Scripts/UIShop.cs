using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.Farming
{
    /// <summary>
    /// A shop. Open lists the items for sale, greyed out when the player can't
    /// afford them, and everything in the player's bag the shop will buy, with
    /// buttons to sell one or all. The player can't move while it's open, and
    /// the day's clock stops.
    ///
    /// Add it to the same GameObject as a UI Document using FarmHUD, and call
    /// Open from an Interact Zone at the shop counter.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Farming Kit/UI Shop")]
    public class UIShop : MonoBehaviour
    {
        [Tooltip("The shop's name, at the top.")]
        [SerializeField] string shopName = "Shop";

        [Tooltip("What the shop sells, in order. Each needs a Buy Price.")]
        [SerializeField] List<ItemData> forSale = new List<ItemData>();

        [Tooltip("The number variable for money.")]
        [SerializeField] IntVariable money;

        [Tooltip("The player's Inventory.")]
        [SerializeField] Inventory inventory;

        [Tooltip("Optional. Found in the project if empty.")]
        [SerializeField] InputReader input;

        VisualElement panel;
        Label title;
        VisualElement buyList;
        VisualElement sellList;
        Button closeButton;

        /// <summary>True while the shop is open.</summary>
        public bool IsOpen { get; private set; }

        void Awake()
        {
            if (input == null)
            {
                input = InputReader.FindInProject();
            }
        }

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            panel = root?.Q<VisualElement>("shop-panel");
            title = root?.Q<Label>("shop-title");
            buyList = root?.Q<VisualElement>("buy-list");
            sellList = root?.Q<VisualElement>("sell-list");
            closeButton = root?.Q<Button>("shop-close-button");

            if (panel == null || buyList == null || sellList == null)
            {
                Debug.LogWarning($"UI Shop on \"{name}\": the UI Document needs elements named \"shop-panel\", \"buy-list\" and \"sell-list\". FarmHUD has them.", this);
                return;
            }

            if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
            if (closeButton != null) closeButton.clicked += Close;
            if (money != null) money.Changed += OnMoneyChanged;
            if (inventory != null) inventory.Changed += Rebuild;

            panel.style.display = DisplayStyle.None;
        }

        void OnDisable()
        {
            if (closeButton != null) closeButton.clicked -= Close;
            if (money != null) money.Changed -= OnMoneyChanged;
            if (inventory != null) inventory.Changed -= Rebuild;
        }

        /// <summary>Opens the shop. Wire the shop counter's Interact Zone to this.</summary>
        public void Open()
        {
            if (panel == null || IsOpen)
            {
                return;
            }

            IsOpen = true;
            panel.style.display = DisplayStyle.Flex;

            if (title != null)
            {
                title.text = shopName;
            }

            if (input != null)
            {
                input.DisablePlayerInput();
            }

            Rebuild();
            closeButton?.Focus();
        }

        /// <summary>Closes the shop.</summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            panel.style.display = DisplayStyle.None;

            if (input != null)
            {
                input.EnablePlayerInput();
            }
        }

        void OnMoneyChanged(int value)
        {
            Rebuild();
        }

        void Rebuild()
        {
            if (!IsOpen)
            {
                return;
            }

            buyList.Clear();

            foreach (ItemData item in forSale)
            {
                if (item == null || item.BuyPrice <= 0)
                {
                    continue;
                }

                ItemData captured = item;
                VisualElement row = Row(item, $"${item.BuyPrice}");
                Button buy = new Button(() => Buy(captured)) { text = "Buy" };
                buy.AddToClassList("shop-button");
                buy.SetEnabled(money == null || money.Value >= item.BuyPrice);
                row.Add(buy);
                buyList.Add(row);
            }

            sellList.Clear();

            if (inventory == null)
            {
                return;
            }

            HashSet<ItemData> listed = new HashSet<ItemData>();

            foreach (Inventory.Slot slot in inventory.Slots)
            {
                if (slot.IsEmpty || slot.item.SellPrice <= 0 || !listed.Add(slot.item))
                {
                    continue;
                }

                ItemData captured = slot.item;
                int have = inventory.CountOf(captured);
                VisualElement row = Row(captured, $"{have} x ${captured.SellPrice}");

                Button sellOne = new Button(() => Sell(captured, 1)) { text = "Sell 1" };
                sellOne.AddToClassList("shop-button");
                row.Add(sellOne);

                Button sellAll = new Button(() => Sell(captured, inventory.CountOf(captured))) { text = "All" };
                sellAll.AddToClassList("shop-button");
                row.Add(sellAll);

                sellList.Add(row);
            }

            if (listed.Count == 0)
            {
                Label nothing = new Label("Nothing to sell yet.");
                nothing.AddToClassList("shop-empty");
                sellList.Add(nothing);
            }
        }

        static VisualElement Row(ItemData item, string price)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("shop-row");
            row.tooltip = item.Description;

            VisualElement icon = new VisualElement();
            icon.AddToClassList("shop-icon");

            if (item.Icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(item.Icon);
            }

            row.Add(icon);

            Label itemName = new Label(item.DisplayName);
            itemName.AddToClassList("shop-name");
            row.Add(itemName);

            Label cost = new Label(price);
            cost.AddToClassList("shop-price");
            row.Add(cost);

            return row;
        }

        void Buy(ItemData item)
        {
            if (money != null && money.Value < item.BuyPrice)
            {
                return;
            }

            if (inventory == null || !inventory.HasRoomFor(item))
            {
                FarmMessage.Say("Your bag is full.");
                return;
            }

            if (money != null)
            {
                money.Add(-item.BuyPrice);
            }

            inventory.Add(item);
        }

        void Sell(ItemData item, int count)
        {
            if (inventory == null || count <= 0 || !inventory.Remove(item, count))
            {
                return;
            }

            if (money != null)
            {
                money.Add(item.SellPrice * count);
            }
        }
    }
}
