using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Kits.Farming
{
    /// <summary>
    /// The player's bag: a row of slots, each holding a stack of one item. One
    /// slot is selected; that's the item the Farmer Tools use. The UI Farm
    /// Screen shows it as the hotbar.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Farming Kit/Inventory")]
    public class Inventory : MonoBehaviour
    {
        [Serializable]
        public class Slot
        {
            public ItemData item;
            [Min(0)] public int count = 1;

            public bool IsEmpty => item == null || count <= 0;
        }

        [Tooltip("How many slots the bag has.")]
        [SerializeField, Range(1, 10)] int size = 8;

        [Tooltip("The most of one item in one slot.")]
        [SerializeField, Min(1)] int maxStack = 99;

        [Tooltip("What the player starts with, in slot order: tools first is a good idea.")]
        [SerializeField] List<Slot> startingItems = new List<Slot>();

        readonly List<Slot> slots = new List<Slot>();

        /// <summary>Raised whenever anything in the bag, or the selected slot, changes.</summary>
        public event Action Changed;

        public IReadOnlyList<Slot> Slots => slots;
        public int Size => size;
        public int SelectedIndex { get; private set; }

        /// <summary>The item in the selected slot, or null.</summary>
        public ItemData Selected => slots.Count > 0 && !slots[SelectedIndex].IsEmpty ? slots[SelectedIndex].item : null;

        void Awake()
        {
            slots.Clear();

            for (int i = 0; i < size; i++)
            {
                slots.Add(new Slot { item = null, count = 0 });
            }

            foreach (Slot start in startingItems)
            {
                if (start != null && start.item != null && start.count > 0)
                {
                    Add(start.item, start.count);
                }
            }
        }

        /// <summary>Adds items, filling stacks of the same item first. Returns how many fit.</summary>
        public int Add(ItemData item, int count = 1)
        {
            if (item == null || count <= 0)
            {
                return 0;
            }

            int left = count;

            foreach (Slot slot in slots)
            {
                if (left == 0) break;

                if (slot.item == item && slot.count > 0 && slot.count < maxStack)
                {
                    int put = Mathf.Min(left, maxStack - slot.count);
                    slot.count += put;
                    left -= put;
                }
            }

            foreach (Slot slot in slots)
            {
                if (left == 0) break;

                if (slot.IsEmpty)
                {
                    int put = Mathf.Min(left, maxStack);
                    slot.item = item;
                    slot.count = put;
                    left -= put;
                }
            }

            if (left != count)
            {
                Changed?.Invoke();
            }

            return count - left;
        }

        /// <summary>True if this many would fit in the bag.</summary>
        public bool HasRoomFor(ItemData item, int count = 1)
        {
            int room = 0;

            foreach (Slot slot in slots)
            {
                if (slot.IsEmpty) room += maxStack;
                else if (slot.item == item) room += maxStack - slot.count;
            }

            return room >= count;
        }

        /// <summary>How many of an item the bag holds.</summary>
        public int CountOf(ItemData item)
        {
            int total = 0;

            foreach (Slot slot in slots)
            {
                if (!slot.IsEmpty && slot.item == item)
                {
                    total += slot.count;
                }
            }

            return total;
        }

        /// <summary>Removes items, if there are enough. Takes from the selected slot first.</summary>
        public bool Remove(ItemData item, int count = 1)
        {
            if (item == null || count <= 0 || CountOf(item) < count)
            {
                return false;
            }

            int left = count;
            left = TakeFrom(SelectedIndex, item, left);

            for (int i = 0; i < slots.Count && left > 0; i++)
            {
                left = TakeFrom(i, item, left);
            }

            Changed?.Invoke();
            return true;
        }

        int TakeFrom(int index, ItemData item, int left)
        {
            Slot slot = slots[index];

            if (slot.IsEmpty || slot.item != item)
            {
                return left;
            }

            int take = Mathf.Min(left, slot.count);
            slot.count -= take;

            if (slot.count <= 0)
            {
                slot.item = null;
                slot.count = 0;
            }

            return left - take;
        }

        /// <summary>Selects a slot, counting from 0.</summary>
        public void Select(int index)
        {
            if (slots.Count == 0)
            {
                return;
            }

            SelectedIndex = Mathf.Clamp(index, 0, slots.Count - 1);
            Changed?.Invoke();
        }

        /// <summary>Selects the next slot (1) or the previous one (-1), wrapping around.</summary>
        public void SelectNext(int direction)
        {
            if (slots.Count == 0)
            {
                return;
            }

            Select(((SelectedIndex + direction) % slots.Count + slots.Count) % slots.Count);
        }
    }
}
