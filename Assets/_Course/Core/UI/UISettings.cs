using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Core
{
    /// <summary>
    /// Runs the Settings screen: music and sound effects volume, and full screen.
    /// Changes take effect at once and are saved on this computer.
    ///
    /// Add it to the same GameObject as the UI Document that uses the Settings screen.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/UI/UI Settings")]
    public class UISettings : MonoBehaviour
    {
        [Tooltip("The music volume slider's name in the UXML file.")]
        [SerializeField] string musicSliderName = "music-slider";

        [Tooltip("The sound effects volume slider's name in the UXML file.")]
        [SerializeField] string effectsSliderName = "effects-slider";

        [Tooltip("The full screen toggle's name in the UXML file.")]
        [SerializeField] string fullScreenToggleName = "fullscreen-toggle";

        [Tooltip("Optional. A sound to play when the sound effects volume changes, so the player can hear the new level.")]
        [SerializeField] SoundEffect previewSound;

        Slider musicSlider;
        Slider effectsSlider;
        Toggle fullScreenToggle;

        void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document != null ? document.rootVisualElement : null;

            if (root == null)
            {
                Debug.LogWarning($"UI Settings on \"{name}\": the UI Document has no Source Asset.", this);
                return;
            }

            AudioService audio = AudioService.Instance;

            musicSlider = root.Q<Slider>(musicSliderName);
            effectsSlider = root.Q<Slider>(effectsSliderName);
            fullScreenToggle = root.Q<Toggle>(fullScreenToggleName);

            if (musicSlider != null)
            {
                if (audio != null)
                {
                    musicSlider.SetValueWithoutNotify(audio.MusicVolume);
                }

                musicSlider.RegisterValueChangedCallback(OnMusicChanged);
            }

            if (effectsSlider != null)
            {
                if (audio != null)
                {
                    effectsSlider.SetValueWithoutNotify(audio.EffectsVolume);
                }

                effectsSlider.RegisterValueChangedCallback(OnEffectsChanged);
            }

            if (fullScreenToggle != null)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // Browsers decide full screen themselves, so hide the option in Web builds.
                fullScreenToggle.style.display = DisplayStyle.None;
#else
                fullScreenToggle.SetValueWithoutNotify(Screen.fullScreen);
                fullScreenToggle.RegisterValueChangedCallback(OnFullScreenChanged);
#endif
            }
        }

        void OnDisable()
        {
            if (musicSlider != null)
            {
                musicSlider.UnregisterValueChangedCallback(OnMusicChanged);
            }

            if (effectsSlider != null)
            {
                effectsSlider.UnregisterValueChangedCallback(OnEffectsChanged);
            }

            if (fullScreenToggle != null)
            {
                fullScreenToggle.UnregisterValueChangedCallback(OnFullScreenChanged);
            }

            PlayerPrefs.Save();
        }

        void OnMusicChanged(ChangeEvent<float> change)
        {
            if (AudioService.Instance != null)
            {
                AudioService.Instance.MusicVolume = change.newValue;
            }
        }

        void OnEffectsChanged(ChangeEvent<float> change)
        {
            if (AudioService.Instance != null)
            {
                AudioService.Instance.EffectsVolume = change.newValue;
            }

            if (previewSound != null)
            {
                previewSound.Play();
            }
        }

        void OnFullScreenChanged(ChangeEvent<bool> change)
        {
            Screen.fullScreen = change.newValue;
        }
    }
}
