using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.Management
{
    /// <summary>
    /// A grid of building plots, and a row of building cards to drag onto them.
    /// Dropping a card on an empty plot pays the building's cost and builds it;
    /// the Turn Cycle then applies its effects every turn. Cards the player
    /// can't afford are greyed out. Dragging a building from a plot back onto
    /// the cards removes it.
    ///
    /// Add it to the same GameObject as a UI Document using ManagementHUD. It
    /// fills the elements named "building-palette" and "build-grid". Works with
    /// the mouse and touch screens.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Management Kit/UI Build Grid")]
    public class UIBuildGrid : MonoBehaviour
    {
        [Tooltip("The buildings that can be built, in card order.")]
        [SerializeField] List<BuildingData> buildings = new List<BuildingData>();

        [Tooltip("How many plots there are.")]
        [SerializeField, Min(1)] int plots = 12;

        [Tooltip("Plots per row.")]
        [SerializeField, Min(1)] int plotsPerRow = 4;

        [Tooltip("Optional. Buildings already on the plots when the game starts, from the first plot.")]
        [SerializeField] List<BuildingData> startingBuildings = new List<BuildingData>();

        [Tooltip("Optional. The resources that buildings cost, so cards update when they change.")]
        [SerializeField] List<IntVariable> watchResources = new List<IntVariable>();

        VisualElement root;
        VisualElement palette;
        VisualElement grid;
        readonly List<VisualElement> plotElements = new List<VisualElement>();
        readonly List<BuildingData> placed = new List<BuildingData>();
        readonly List<(VisualElement card, BuildingData data)> cards = new List<(VisualElement, BuildingData)>();

        // Dragging
        VisualElement ghost;
        BuildingData dragging;
        int dragFromPlot = -1;
        int dragPointerId = -1;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            palette = root?.Q<VisualElement>("building-palette");
            grid = root?.Q<VisualElement>("build-grid");

            if (palette == null || grid == null)
            {
                Debug.LogWarning($"UI Build Grid on \"{name}\": the UI Document needs elements named \"building-palette\" and \"build-grid\". ManagementHUD has them.", this);
                return;
            }

            BuildCards();
            BuildPlots();

            foreach (IntVariable resource in watchResources)
            {
                if (resource != null)
                {
                    resource.Changed += OnResourceChanged;
                }
            }

            root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        void OnDisable()
        {
            foreach (IntVariable resource in watchResources)
            {
                if (resource != null)
                {
                    resource.Changed -= OnResourceChanged;
                }
            }

            root?.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            root?.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        void Start()
        {
            // Starting buildings are free, and go on the first plots.
            for (int i = 0; i < startingBuildings.Count && i < plotElements.Count; i++)
            {
                if (startingBuildings[i] != null)
                {
                    Place(i, startingBuildings[i]);
                }
            }
        }

        // ---------------------------------------------------------------- Building the screen

        void BuildCards()
        {
            palette.Clear();
            cards.Clear();

            foreach (BuildingData data in buildings)
            {
                if (data == null)
                {
                    continue;
                }

                VisualElement card = new VisualElement();
                card.AddToClassList("building-card");

                VisualElement icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("building-icon");

                if (data.Icon != null)
                {
                    icon.style.backgroundImage = new StyleBackground(data.Icon);
                }

                card.Add(icon);
                card.Add(Text(data.DisplayName, "building-name"));
                card.Add(Text(data.CostText(), "building-cost"));
                card.tooltip = data.Description;

                BuildingData captured = data;
                card.RegisterCallback<PointerDownEvent>(e => BeginDrag(e, captured, -1));
                palette.Add(card);
                cards.Add((card, data));
            }

            RefreshCards();
        }

        void BuildPlots()
        {
            grid.Clear();
            plotElements.Clear();
            placed.Clear();

            VisualElement row = null;

            for (int i = 0; i < plots; i++)
            {
                if (i % plotsPerRow == 0)
                {
                    row = new VisualElement { pickingMode = PickingMode.Ignore };
                    row.AddToClassList("build-row");
                    grid.Add(row);
                }

                VisualElement plot = new VisualElement();
                plot.AddToClassList("build-plot");
                plot.userData = i;
                int index = i;
                plot.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (placed[index] != null)
                    {
                        BeginDrag(e, placed[index], index);
                    }
                });

                row.Add(plot);
                plotElements.Add(plot);
                placed.Add(null);
            }
        }

        static Label Text(string text, string className)
        {
            Label label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }

        void OnResourceChanged(int value)
        {
            RefreshCards();
        }

        void RefreshCards()
        {
            foreach ((VisualElement card, BuildingData data) in cards)
            {
                card.EnableInClassList("building-card--unaffordable", !data.CanAfford());
            }
        }

        // ---------------------------------------------------------------- Dragging

        void BeginDrag(PointerDownEvent e, BuildingData data, int fromPlot)
        {
            if (dragging != null || PauseController.GamePaused || (TurnCycle.Instance != null && TurnCycle.Instance.IsOver))
            {
                return;
            }

            // New buildings must be affordable; moving one off a plot is always allowed.
            if (fromPlot < 0 && !data.CanAfford())
            {
                return;
            }

            dragging = data;
            dragFromPlot = fromPlot;
            dragPointerId = e.pointerId;

            ghost = new VisualElement { pickingMode = PickingMode.Ignore };
            ghost.AddToClassList("building-ghost");

            if (data.Icon != null)
            {
                ghost.style.backgroundImage = new StyleBackground(data.Icon);
            }

            ghost.Add(Text(data.DisplayName, "building-name"));
            root.Add(ghost);
            MoveGhost(e.position);
            root.CapturePointer(e.pointerId);
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (dragging == null || e.pointerId != dragPointerId)
            {
                return;
            }

            MoveGhost(e.position);

            int over = PlotAt(e.position);

            for (int i = 0; i < plotElements.Count; i++)
            {
                plotElements[i].EnableInClassList("build-plot--target", i == over && placed[i] == null);
            }
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (dragging == null || e.pointerId != dragPointerId)
            {
                return;
            }

            root.ReleasePointer(e.pointerId);
            int target = PlotAt(e.position);
            bool overPalette = palette.worldBound.Contains(e.position);

            if (dragFromPlot < 0 && target >= 0 && placed[target] == null)
            {
                // A new building.
                dragging.PayCost();
                Place(target, dragging);
            }
            else if (dragFromPlot >= 0 && target >= 0 && placed[target] == null)
            {
                // Moving a building to another plot.
                BuildingData moving = placed[dragFromPlot];
                ClearPlot(dragFromPlot);
                SetPlotLooks(target, moving);
                placed[target] = moving;
            }
            else if (dragFromPlot >= 0 && overPalette)
            {
                // Removing a building.
                if (TurnCycle.Instance != null)
                {
                    TurnCycle.Instance.RemoveBuilding(placed[dragFromPlot]);
                }

                ClearPlot(dragFromPlot);
            }

            EndDrag();
        }

        void EndDrag()
        {
            ghost?.RemoveFromHierarchy();
            ghost = null;
            dragging = null;
            dragFromPlot = -1;
            dragPointerId = -1;

            foreach (VisualElement plot in plotElements)
            {
                plot.RemoveFromClassList("build-plot--target");
            }

            RefreshCards();
        }

        void MoveGhost(Vector2 position)
        {
            if (ghost == null)
            {
                return;
            }

            Vector2 local = root.WorldToLocal(position);
            ghost.style.left = local.x - 40f;
            ghost.style.top = local.y - 40f;
        }

        int PlotAt(Vector2 position)
        {
            for (int i = 0; i < plotElements.Count; i++)
            {
                if (plotElements[i].worldBound.Contains(position))
                {
                    return i;
                }
            }

            return -1;
        }

        // ---------------------------------------------------------------- Plots

        void Place(int plot, BuildingData data)
        {
            placed[plot] = data;
            SetPlotLooks(plot, data);

            if (TurnCycle.Instance != null)
            {
                TurnCycle.Instance.AddBuilding(data);
            }
        }

        void ClearPlot(int plot)
        {
            placed[plot] = null;
            SetPlotLooks(plot, null);
        }

        void SetPlotLooks(int plot, BuildingData data)
        {
            VisualElement element = plotElements[plot];
            element.Clear();
            element.EnableInClassList("build-plot--filled", data != null);
            element.style.backgroundImage = data != null && data.Icon != null ? new StyleBackground(data.Icon) : StyleKeyword.Null;

            if (data != null)
            {
                element.Add(Text(data.DisplayName, "building-name"));
            }
        }
    }
}
