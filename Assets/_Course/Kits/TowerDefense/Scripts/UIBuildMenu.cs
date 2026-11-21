using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CollinGame.Kits.TowerDefense
{
    /// <summary>
    /// The build menu. Clicking (or tapping) a Build Spot opens it: on an empty
    /// spot it lists the towers, with their prices; on a tower it offers an
    /// upgrade and a sale. Towers the player can't afford are greyed out.
    /// Clicking empty ground closes it.
    ///
    /// Add it to the same GameObject as a UI Document using TowerHUD (from this
    /// kit's UI folder). The build menu is played with the mouse or a touch screen.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Tower Defense Kit/UI Build Menu")]
    public class UIBuildMenu : MonoBehaviour
    {
        [Tooltip("The towers that can be built, in menu order.")]
        [SerializeField] List<TowerData> towers = new List<TowerData>();

        [Tooltip("The number variable for money.")]
        [SerializeField] IntVariable money;

        [Tooltip("Optional. The wave spawner, so the Start Wave button can show when a wave can start.")]
        [SerializeField] TowerWaveSpawner spawner;

        VisualElement menu;
        Label title;
        VisualElement towerButtons;
        Button upgradeButton;
        Button sellButton;
        Button startWaveButton;
        Camera mainCamera;
        BuildSpot2D selected;

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            mainCamera = Camera.main;

            menu = root?.Q<VisualElement>("build-menu");
            title = root?.Q<Label>("build-title");
            towerButtons = root?.Q<VisualElement>("tower-buttons");
            upgradeButton = root?.Q<Button>("upgrade-button");
            sellButton = root?.Q<Button>("sell-button");
            startWaveButton = root?.Q<Button>("start-wave-button");
            Button closeButton = root?.Q<Button>("close-button");

            if (menu == null || towerButtons == null)
            {
                Debug.LogWarning($"UI Build Menu on \"{name}\": the UI Document needs elements named \"build-menu\" and \"tower-buttons\". TowerHUD has them.", this);
                return;
            }

            if (upgradeButton != null) upgradeButton.clicked += Upgrade;
            if (sellButton != null) sellButton.clicked += Sell;
            if (closeButton != null) closeButton.clicked += Close;
            if (startWaveButton != null && spawner != null) startWaveButton.clicked += spawner.StartNextWave;
            if (money != null) money.Changed += OnMoneyChanged;

            Close();
        }

        void OnDisable()
        {
            if (upgradeButton != null) upgradeButton.clicked -= Upgrade;
            if (sellButton != null) sellButton.clicked -= Sell;
            if (startWaveButton != null && spawner != null) startWaveButton.clicked -= spawner.StartNextWave;
            if (money != null) money.Changed -= OnMoneyChanged;
        }

        void Update()
        {
            if (startWaveButton != null && spawner != null)
            {
                startWaveButton.SetEnabled(!spawner.WaveInProgress && spawner.HasNextWave);
            }

            if (PauseController.GamePaused)
            {
                return;
            }

            Pointer pointer = Pointer.current;

            if (pointer == null || !pointer.press.wasPressedThisFrame || mainCamera == null)
            {
                return;
            }

            Vector2 screen = pointer.position.ReadValue();

            if (PointerOverUI(screen))
            {
                return;
            }

            Vector2 world = mainCamera.ScreenToWorldPoint(screen);
            BuildSpot2D spot = null;

            foreach (Collider2D hit in Physics2D.OverlapPointAll(world))
            {
                spot = hit.GetComponentInParent<BuildSpot2D>();

                if (spot != null)
                {
                    break;
                }
            }

            if (spot != null)
            {
                Open(spot);
            }
            else
            {
                Close();
            }
        }

        // True if the pointer is over a visible, clickable part of any UI Document.
        static bool PointerOverUI(Vector2 screen)
        {
            foreach (UIDocument document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                IPanel panel = document.rootVisualElement?.panel;

                if (panel == null)
                {
                    continue;
                }

                Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
                VisualElement picked = panel.Pick(panelPoint);

                if (picked != null && picked != document.rootVisualElement)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Opens the menu for a spot.</summary>
        public void Open(BuildSpot2D spot)
        {
            if (selected != null && selected.Tower != null)
            {
                selected.Tower.ShowRange(false);
            }

            selected = spot;
            menu.style.display = DisplayStyle.Flex;
            Refresh();
        }

        /// <summary>Closes the menu.</summary>
        public void Close()
        {
            if (selected != null && selected.Tower != null)
            {
                selected.Tower.ShowRange(false);
            }

            selected = null;

            if (menu != null)
            {
                menu.style.display = DisplayStyle.None;
            }
        }

        void OnMoneyChanged(int value)
        {
            if (selected != null)
            {
                Refresh();
            }
        }

        void Refresh()
        {
            towerButtons.Clear();
            int cash = money != null ? money.Value : int.MaxValue;
            Tower2D tower = selected.Tower;

            bool empty = tower == null;
            towerButtons.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;
            SetShown(upgradeButton, !empty);
            SetShown(sellButton, !empty);

            if (empty)
            {
                if (title != null) title.text = "Build";

                foreach (TowerData data in towers)
                {
                    if (data == null)
                    {
                        continue;
                    }

                    TowerData choice = data;
                    Button button = new Button(() => Build(choice)) { text = $"{data.DisplayName}  ${data.Cost}" };
                    button.AddToClassList("tower-button");
                    button.SetEnabled(cash >= data.Cost);
                    towerButtons.Add(button);
                }

                return;
            }

            tower.ShowRange(true);

            if (title != null) title.text = tower.Data.DisplayName;

            TowerData next = tower.Data.UpgradesTo;

            if (upgradeButton != null)
            {
                upgradeButton.text = next != null ? $"Upgrade: {next.DisplayName}  ${next.Cost}" : "Fully upgraded";
                upgradeButton.SetEnabled(next != null && cash >= next.Cost);
            }

            if (sellButton != null)
            {
                sellButton.text = $"Sell  +${selected.SellValue}";
            }
        }

        void Build(TowerData data)
        {
            if (selected != null && selected.Build(data, money))
            {
                Refresh();
            }
        }

        void Upgrade()
        {
            if (selected != null && selected.Upgrade(money))
            {
                Refresh();
            }
        }

        void Sell()
        {
            if (selected != null)
            {
                selected.Sell(money);
                Close();
            }
        }

        static void SetShown(VisualElement element, bool shown)
        {
            if (element != null)
            {
                element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
