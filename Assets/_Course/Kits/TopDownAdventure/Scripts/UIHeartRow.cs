using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.TopDownAdventure
{
    /// <summary>
    /// Shows health as a row of hearts in a UI Document: full hearts for the
    /// health left, empty hearts for the health lost.
    ///
    /// Add it to the same GameObject as the UI Document. The UXML needs an
    /// empty element to hold the hearts, named "hearts" (AdventureHUD has one).
    /// Without heart sprites, the hearts are drawn as colored squares, styled by
    /// the "heart" and "heart--empty" classes in AdventureHUD.uss.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Top-Down Adventure Kit/UI Heart Row")]
    public class UIHeartRow : MonoBehaviour
    {
        [Tooltip("The number variable that holds the current health. Use the same one as the player's Health.")]
        [SerializeField] IntVariable health;

        [Tooltip("How many hearts to show when health is full. Set it to the player's Max Health.")]
        [SerializeField, Min(1)] int maxHearts = 5;

        [Tooltip("The name of the element in the UXML that holds the hearts.")]
        [SerializeField] string containerName = "hearts";

        [Header("Looks")]

        [Tooltip("Optional. The sprite for a full heart.")]
        [SerializeField] Sprite fullHeart;

        [Tooltip("Optional. The sprite for an empty heart.")]
        [SerializeField] Sprite emptyHeart;

        readonly List<VisualElement> hearts = new List<VisualElement>();
        VisualElement container;

        void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document != null ? document.rootVisualElement : null;
            container = root?.Q<VisualElement>(containerName);

            if (container == null)
            {
                Debug.LogWarning($"UI Heart Row on \"{name}\": there is no element named \"{containerName}\" in the UI Document. Use AdventureHUD, or add an empty VisualElement with that name.", this);
                return;
            }

            if (health == null)
            {
                Debug.LogWarning($"UI Heart Row on \"{name}\": Health is empty. Drag in the number variable that holds the player's health.", this);
                return;
            }

            Build();
            health.Changed += Show;
            Show(health.Value);
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Changed -= Show;
            }
        }

        void Build()
        {
            container.Clear();
            hearts.Clear();

            for (int i = 0; i < maxHearts; i++)
            {
                VisualElement heart = new VisualElement { pickingMode = PickingMode.Ignore };
                heart.AddToClassList("heart");
                container.Add(heart);
                hearts.Add(heart);
            }
        }

        void Show(int value)
        {
            for (int i = 0; i < hearts.Count; i++)
            {
                bool full = i < value;
                VisualElement heart = hearts[i];
                heart.EnableInClassList("heart--empty", !full);

                Sprite sprite = full ? fullHeart : emptyHeart;

                if (sprite != null)
                {
                    heart.AddToClassList("heart--sprite");
                    heart.style.backgroundImage = new StyleBackground(sprite);
                }
                else
                {
                    heart.RemoveFromClassList("heart--sprite");
                    heart.style.backgroundImage = StyleKeyword.Null;
                }
            }
        }
    }
}
