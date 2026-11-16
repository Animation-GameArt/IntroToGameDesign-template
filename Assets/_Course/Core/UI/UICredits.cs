using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Core
{
    /// <summary>
    /// Fills a credits screen from text files. Add it to the same GameObject as
    /// the UI Document that uses the Credits screen.
    ///
    /// Credits are kept in plain text files, so they are easy to keep up to date:
    /// add a line every time you add an asset you didn't make. The course's own
    /// credits are in Assets/_Course/Credits, and yours go in Assets/_Game/Credits.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/UI/UI Credits")]
    public class UICredits : MonoBehaviour
    {
        [Tooltip("The label's name in the UXML file.")]
        [SerializeField] string labelName = "credits-label";

        [Tooltip("The text files to show, in order. Put your own game's credits first.")]
        [SerializeField] List<TextAsset> creditFiles = new List<TextAsset>();

        void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document != null ? document.rootVisualElement : null;

            if (root == null)
            {
                Debug.LogWarning($"UI Credits on \"{name}\": the UI Document has no Source Asset.", this);
                return;
            }

            Label label = root.Q<Label>(labelName);

            if (label == null)
            {
                Debug.LogWarning($"UI Credits on \"{name}\": there is no label named \"{labelName}\" in the UI Document.", this);
                return;
            }

            StringBuilder text = new StringBuilder();

            for (int i = 0; i < creditFiles.Count; i++)
            {
                if (creditFiles[i] == null)
                {
                    continue;
                }

                if (text.Length > 0)
                {
                    text.Append("\n\n");
                }

                text.Append(creditFiles[i].text.Trim());
            }

            if (text.Length == 0)
            {
                Debug.LogWarning($"UI Credits on \"{name}\": Credit Files is empty, so the credits screen has nothing to show.", this);
                return;
            }

            // Show the text exactly as written, even if it contains < or >.
            label.enableRichText = false;
            label.text = text.ToString();
        }
    }
}
