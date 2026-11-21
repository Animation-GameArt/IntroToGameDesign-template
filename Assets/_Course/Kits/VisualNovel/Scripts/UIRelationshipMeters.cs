using System;
using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.VisualNovel
{
    /// <summary>
    /// Bars showing how each character feels about the player, for a dating
    /// sim. Each bar follows an Int Variable (kept in step with the Yarn script
    /// by a Yarn Variable Link).
    ///
    /// Add it to the same GameObject as a UI Document with an element named
    /// "meters"; the kit's VisualNovel screen has one.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Visual Novel Kit/UI Relationship Meters")]
    public class UIRelationshipMeters : MonoBehaviour
    {
        [Serializable]
        public class Meter
        {
            public CharacterData character;
            public IntVariable value;
            [Min(1)] public int max = 10;
        }

        [Tooltip("A bar per character, in order.")]
        [SerializeField] List<Meter> meters = new List<Meter>();

        [Tooltip("Ticked: shows the number beside each bar, such as 6 / 10.")]
        [SerializeField] bool showNumbers = true;

        VisualElement container;
        readonly List<(Meter meter, VisualElement fill, Label number)> rows = new List<(Meter, VisualElement, Label)>();

        void OnEnable()
        {
            container = GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>("meters");

            if (container == null)
            {
                Debug.LogWarning($"UI Relationship Meters on \"{name}\": the UI Document needs an element named \"meters\".", this);
                return;
            }

            Build();

            foreach (Meter meter in meters)
            {
                if (meter.value != null)
                {
                    meter.value.Changed += OnValueChanged;
                }
            }
        }

        void OnDisable()
        {
            foreach (Meter meter in meters)
            {
                if (meter.value != null)
                {
                    meter.value.Changed -= OnValueChanged;
                }
            }
        }

        void Build()
        {
            container.Clear();
            rows.Clear();

            foreach (Meter meter in meters)
            {
                if (meter == null || meter.value == null)
                {
                    continue;
                }

                VisualElement row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("vn-meter");

                Label nameLabel = new Label(meter.character != null ? meter.character.CharacterName : meter.value.name) { pickingMode = PickingMode.Ignore };
                nameLabel.AddToClassList("vn-meter-name");

                if (meter.character != null)
                {
                    nameLabel.style.color = meter.character.NameColor;
                }

                VisualElement bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.AddToClassList("vn-meter-bar");

                VisualElement fill = new VisualElement { pickingMode = PickingMode.Ignore };
                fill.AddToClassList("vn-meter-fill");

                if (meter.character != null)
                {
                    fill.style.backgroundColor = meter.character.NameColor;
                }

                bar.Add(fill);

                Label number = new Label { pickingMode = PickingMode.Ignore };
                number.AddToClassList("vn-meter-number");
                number.style.display = showNumbers ? DisplayStyle.Flex : DisplayStyle.None;

                row.Add(nameLabel);
                row.Add(bar);
                row.Add(number);
                container.Add(row);
                rows.Add((meter, fill, number));
            }

            Refresh();
        }

        void OnValueChanged(int value)
        {
            Refresh();
        }

        void Refresh()
        {
            foreach ((Meter meter, VisualElement fill, Label number) in rows)
            {
                int value = Mathf.Clamp(meter.value.Value, 0, meter.max);
                fill.style.width = Length.Percent(100f * value / meter.max);
                number.text = $"{meter.value.Value} / {meter.max}";
            }
        }
    }
}
