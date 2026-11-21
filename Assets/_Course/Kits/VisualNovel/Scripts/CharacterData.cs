using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Kits.VisualNovel
{
    /// <summary>Where a character stands on the stage.</summary>
    public enum StagePosition
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// A character in the story: their name as written in the Yarn script
    /// ("Ren: Hello!"), the color of their name, and a portrait for each mood.
    /// Create one with Create > Collin Game > Visual Novel Kit > Character.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Visual Novel Kit/Character", fileName = "New Character")]
    public class CharacterData : ScriptableObject
    {
        [Serializable]
        public class Portrait
        {
            [Tooltip("The mood's name, used in <<show Ren happy>> and <<mood Ren happy>>.")]
            public string mood = "neutral";

            public Sprite sprite;
        }

        [Tooltip("The name exactly as written before the colon in the Yarn script.")]
        [SerializeField] string characterName = "Name";

        [Tooltip("The color of the name above their lines.")]
        [SerializeField] Color nameColor = new Color(1f, 0.84f, 0.4f);

        [Tooltip("A picture for each mood. The first is used when no mood is given.")]
        [SerializeField] List<Portrait> portraits = new List<Portrait>();

        [Tooltip("Where they stand when <<show>> doesn't say.")]
        [SerializeField] StagePosition defaultPosition = StagePosition.Center;

        public string CharacterName => string.IsNullOrEmpty(characterName) ? name : characterName;
        public Color NameColor => nameColor;
        public StagePosition DefaultPosition => defaultPosition;

        /// <summary>True if a name from the script means this character.</summary>
        public bool Matches(string other)
        {
            return !string.IsNullOrEmpty(other)
                && (string.Equals(other.Trim(), CharacterName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(other.Trim(), name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The portrait for a mood, or the first portrait if there's no such mood.</summary>
        public Sprite PortraitFor(string mood)
        {
            if (portraits.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(mood))
            {
                foreach (Portrait portrait in portraits)
                {
                    if (string.Equals(portrait.mood, mood, StringComparison.OrdinalIgnoreCase))
                    {
                        return portrait.sprite;
                    }
                }

                Debug.LogWarning($"Character \"{CharacterName}\" has no mood called \"{mood}\", so the first portrait is shown.", this);
            }

            return portraits[0].sprite;
        }
    }
}
