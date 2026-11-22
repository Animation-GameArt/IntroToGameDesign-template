using System;
using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Yarn.Unity;

namespace CollinGame.Kits.VisualNovel
{
    /// <summary>
    /// The visual novel's stage: a background and up to three characters, on
    /// the left, in the center and on the right. The Yarn script controls it
    /// with these commands:
    ///
    ///   &lt;&lt;background Park&gt;&gt;            change the background
    ///   &lt;&lt;show Ren&gt;&gt;                    show a character (their first portrait, default position)
    ///   &lt;&lt;show Ren happy left&gt;&gt;         ...with a mood and a position
    ///   &lt;&lt;mood Ren sad&gt;&gt;                change a character's portrait
    ///   &lt;&lt;hide Ren&gt;&gt;  &lt;&lt;hide_all&gt;&gt;      take characters off the stage
    ///   &lt;&lt;sound Door&gt;&gt;                 play a sound
    ///   &lt;&lt;music Theme&gt;&gt;  &lt;&lt;stop_music&gt;&gt;  play or stop music
    ///   &lt;&lt;raise GoodEnding&gt;&gt;           raise a Void Event Channel, such as to show an ending screen
    ///   &lt;&lt;scene Credits&gt;&gt;               load a scene
    ///
    /// Names in the commands are the names given in this component's lists.
    /// Add it to the same GameObject as the UI Dialogue Presenter.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Visual Novel Kit/Visual Novel Stage")]
    public class VisualNovelStage : MonoBehaviour
    {
        [Serializable]
        public class Background
        {
            public string name = "Park";
            [Tooltip("The picture. If empty, the background is filled with the color.")]
            public Sprite sprite;
            public Color color = new Color(0.15f, 0.17f, 0.25f);
        }

        [Serializable]
        public class NamedEvent
        {
            public string name = "GoodEnding";
            public VoidEventChannel channel;
        }

        [Serializable]
        public class NamedClip
        {
            public string name = "Door";
            public AudioClip clip;
        }

        [Tooltip("Optional. The Dialogue Runner. Found in the scene if empty.")]
        [SerializeField] DialogueRunner runner;

        [Tooltip("Everyone who can appear or speak.")]
        [SerializeField] List<CharacterData> characters = new List<CharacterData>();

        [Tooltip("The backgrounds, by name.")]
        [SerializeField] List<Background> backgrounds = new List<Background>();

        [Tooltip("Optional. The background at the start.")]
        [SerializeField] string startingBackground;

        [Tooltip("Ticked: characters who aren't speaking are darkened.")]
        [SerializeField] bool dimOthers = true;

        [Tooltip("Void Event Channels the script can raise, by name.")]
        [SerializeField] List<NamedEvent> events = new List<NamedEvent>();

        [Tooltip("Sounds the script can play, by name. Played through the Audio Service.")]
        [SerializeField] List<NamedClip> sounds = new List<NamedClip>();

        [Tooltip("Music the script can play, by name.")]
        [SerializeField] List<NamedClip> music = new List<NamedClip>();

        static readonly string[] Commands = { "background", "show", "mood", "hide", "hide_all", "sound", "music", "stop_music", "raise", "scene" };
        static readonly Color Dimmed = new Color(0.5f, 0.5f, 0.55f);

        VisualElement backgroundElement;
        readonly VisualElement[] slots = new VisualElement[3];
        readonly CharacterData[] onStage = new CharacterData[3];

        void Awake()
        {
            if (runner == null)
            {
                runner = FindFirstObjectByType<DialogueRunner>();
            }

            if (runner == null)
            {
                Debug.LogWarning($"Visual Novel Stage on \"{name}\": there is no Dialogue Runner in the scene, so the script's commands won't work.", this);
                return;
            }

            // The names come from the Commands list rather than being written in here, so Yarn
            // Spinner's code generator doesn't make its own (mismatched) handlers for them.
#pragma warning disable YS1020 // Names on purpose not known at compile time (see above).
            runner.AddCommandHandler<string>(Commands[0], SetBackground);
            runner.AddCommandHandler<string, string, string>(Commands[1], Show);
            runner.AddCommandHandler<string, string>(Commands[2], SetMood);
            runner.AddCommandHandler<string>(Commands[3], Hide);
            runner.AddCommandHandler(Commands[4], new Action(HideAll));
            runner.AddCommandHandler<string>(Commands[5], PlaySound);
            runner.AddCommandHandler<string>(Commands[6], PlayMusic);
            runner.AddCommandHandler(Commands[7], new Action(StopMusic));
            runner.AddCommandHandler<string>(Commands[8], Raise);
            runner.AddCommandHandler<string>(Commands[9], LoadScene);
#pragma warning restore YS1020
        }

        void OnDestroy()
        {
            if (runner == null)
            {
                return;
            }

            foreach (string command in Commands)
            {
                runner.RemoveCommandHandler(command);
            }
        }

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            backgroundElement = root?.Q<VisualElement>("background");
            slots[(int)StagePosition.Left] = root?.Q<VisualElement>("character-left");
            slots[(int)StagePosition.Center] = root?.Q<VisualElement>("character-center");
            slots[(int)StagePosition.Right] = root?.Q<VisualElement>("character-right");

            if (backgroundElement == null || slots[0] == null)
            {
                Debug.LogWarning($"Visual Novel Stage on \"{name}\": the UI Document needs elements named \"background\", \"character-left\", \"character-center\" and \"character-right\". The kit's VisualNovel screen has them.", this);
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                SetSlot(i, null, null);
            }
        }

        void Start()
        {
            if (!string.IsNullOrEmpty(startingBackground))
            {
                SetBackground(startingBackground);
            }
        }

        /// <summary>The character with this name, or null.</summary>
        public CharacterData FindCharacter(string characterName)
        {
            foreach (CharacterData character in characters)
            {
                if (character != null && character.Matches(characterName))
                {
                    return character;
                }
            }

            return null;
        }

        /// <summary>Darkens everyone on stage except the speaker. Null or a narrator lights everyone.</summary>
        public void HighlightSpeaker(string speaker)
        {
            CharacterData speaking = string.IsNullOrEmpty(speaker) ? null : FindCharacter(speaker);
            bool speakerOnStage = speaking != null && Array.IndexOf(onStage, speaking) >= 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || onStage[i] == null)
                {
                    continue;
                }

                bool lit = !dimOthers || !speakerOnStage || onStage[i] == speaking;
                slots[i].style.unityBackgroundImageTintColor = lit ? Color.white : Dimmed;
            }
        }

        // ---------------------------------------------------------------- Commands

        /// <summary>&lt;&lt;background Name&gt;&gt;</summary>
        public void SetBackground(string backgroundName)
        {
            if (backgroundElement == null)
            {
                return;
            }

            Background found = backgrounds.Find(b => string.Equals(b.name, backgroundName, StringComparison.OrdinalIgnoreCase));

            if (found == null)
            {
                Debug.LogWarning($"Visual Novel Stage: there's no background called \"{backgroundName}\". Add it to Backgrounds.", this);
                return;
            }

            backgroundElement.style.backgroundImage = found.sprite != null ? new StyleBackground(found.sprite) : new StyleBackground(StyleKeyword.None);
            backgroundElement.style.backgroundColor = found.color;
        }

        /// <summary>&lt;&lt;show Name mood position&gt;&gt;. The mood and position are optional.</summary>
        public void Show(string characterName, string mood = "", string position = "")
        {
            CharacterData character = FindOrWarn(characterName);

            if (character == null)
            {
                return;
            }

            int slot = (int)character.DefaultPosition;

            if (!string.IsNullOrEmpty(position))
            {
                if (Enum.TryParse(position, true, out StagePosition parsed))
                {
                    slot = (int)parsed;
                }
                else if (Enum.TryParse(mood, true, out StagePosition swapped))
                {
                    // <<show Ren left happy>> works too.
                    slot = (int)swapped;
                    mood = position;
                }
                else
                {
                    Debug.LogWarning($"Visual Novel Stage: \"{position}\" isn't a position. Use left, center or right.", this);
                }
            }
            else if (Enum.TryParse(mood, true, out StagePosition onlyPosition))
            {
                // <<show Ren left>>: a position and no mood.
                slot = (int)onlyPosition;
                mood = string.Empty;
            }

            // Take them off wherever they were already standing.
            for (int i = 0; i < onStage.Length; i++)
            {
                if (onStage[i] == character && i != slot)
                {
                    SetSlot(i, null, null);
                }
            }

            SetSlot(slot, character, character.PortraitFor(mood));
        }

        /// <summary>&lt;&lt;mood Name mood&gt;&gt;</summary>
        public void SetMood(string characterName, string mood)
        {
            CharacterData character = FindOrWarn(characterName);
            int slot = character != null ? Array.IndexOf(onStage, character) : -1;

            if (slot < 0)
            {
                if (character != null)
                {
                    Show(characterName, mood);
                }

                return;
            }

            SetSlot(slot, character, character.PortraitFor(mood));
        }

        /// <summary>&lt;&lt;hide Name&gt;&gt;</summary>
        public void Hide(string characterName)
        {
            CharacterData character = FindOrWarn(characterName);

            for (int i = 0; i < onStage.Length; i++)
            {
                if (character != null && onStage[i] == character)
                {
                    SetSlot(i, null, null);
                }
            }
        }

        /// <summary>&lt;&lt;hide_all&gt;&gt;</summary>
        public void HideAll()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                SetSlot(i, null, null);
            }
        }

        /// <summary>&lt;&lt;sound Name&gt;&gt;</summary>
        public void PlaySound(string soundName)
        {
            AudioClip clip = FindClip(sounds, soundName);

            if (clip != null && AudioService.Instance != null)
            {
                AudioService.Instance.PlayEffect(clip);
            }
        }

        /// <summary>&lt;&lt;music Name&gt;&gt;</summary>
        public void PlayMusic(string musicName)
        {
            AudioClip clip = FindClip(music, musicName);

            if (clip != null && AudioService.Instance != null)
            {
                AudioService.Instance.PlayMusic(clip);
            }
        }

        /// <summary>&lt;&lt;stop_music&gt;&gt;</summary>
        public void StopMusic()
        {
            if (AudioService.Instance != null)
            {
                AudioService.Instance.StopMusic();
            }
        }

        /// <summary>&lt;&lt;raise Name&gt;&gt;</summary>
        public void Raise(string eventName)
        {
            NamedEvent found = events.Find(e => string.Equals(e.name, eventName, StringComparison.OrdinalIgnoreCase));

            if (found == null || found.channel == null)
            {
                Debug.LogWarning($"Visual Novel Stage: there's no event called \"{eventName}\". Add it to Events.", this);
                return;
            }

            found.channel.Raise();
        }

        /// <summary>&lt;&lt;scene Name&gt;&gt;</summary>
        public void LoadScene(string sceneName)
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        // ---------------------------------------------------------------- Helpers

        CharacterData FindOrWarn(string characterName)
        {
            CharacterData character = FindCharacter(characterName);

            if (character == null)
            {
                Debug.LogWarning($"Visual Novel Stage: there's no character called \"{characterName}\". Add their Character asset to Characters.", this);
            }

            return character;
        }

        AudioClip FindClip(List<NamedClip> list, string clipName)
        {
            NamedClip found = list.Find(c => string.Equals(c.name, clipName, StringComparison.OrdinalIgnoreCase));

            if (found == null || found.clip == null)
            {
                Debug.LogWarning($"Visual Novel Stage: there's no sound or music called \"{clipName}\".", this);
                return null;
            }

            return found.clip;
        }

        void SetSlot(int slot, CharacterData character, Sprite portrait)
        {
            onStage[slot] = character;
            VisualElement element = slots[slot];

            if (element == null)
            {
                return;
            }

            bool showing = character != null && portrait != null;
            element.style.backgroundImage = showing ? new StyleBackground(portrait) : new StyleBackground(StyleKeyword.None);
            element.style.unityBackgroundImageTintColor = Color.white;
            element.EnableInClassList("vn-character--shown", showing);
        }
    }
}
