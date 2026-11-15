using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollinGame.Core
{
    /// <summary>
    /// Loads scenes with a fade out and a fade in.
    ///
    /// There is always exactly one Scene Loader. The Bootstrapper creates it on the
    /// "[Course Services]" object when the game starts, so you never add it to a scene.
    ///
    /// From your own script:   SceneLoader.Instance.LoadScene("Level2");
    /// Without any code:       add a Scene Changer component to a GameObject.
    ///
    /// A scene can only be loaded if it is in the Scene List (File > Build Profiles).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScreenFader))]
    [AddComponentMenu("Collin Game/Core/Scene Loader")]
    public class SceneLoader : MonoBehaviour
    {
        /// <summary>The one Scene Loader in the game.</summary>
        public static SceneLoader Instance { get; private set; }

        [Tooltip("How long each fade takes, in seconds. 0 switches scenes with no fade.")]
        [SerializeField, Min(0f)] float fadeDuration = 0.35f;

        ScreenFader fader;

        /// <summary>True from the start of the fade out until the fade in has finished.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>How long each fade takes, in seconds.</summary>
        public float FadeDuration
        {
            get => fadeDuration;
            set => fadeDuration = Mathf.Max(0f, value);
        }

        /// <summary>Raised when a scene change begins. The text is the name of the scene being loaded.</summary>
        public event Action<string> LoadStarted;

        /// <summary>Raised when the new scene is loaded and the fade in has finished.</summary>
        public event Action<string> LoadFinished;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Scene Loader: there is already a Scene Loader on [Course Services], so this extra one was removed. You don't need to add a Scene Loader to your scenes.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            fader = GetComponent<ScreenFader>();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Fades out, loads the scene with this name, then fades in.
        /// Returns false, with a message in the Console, if the scene can't be loaded.
        /// </summary>
        public bool LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"Scene Loader: ignored the request to load \"{sceneName}\" because another scene is already loading.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("Scene Loader: no scene name was given. Type the name of the scene to load.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene Loader: can't find a scene named \"{sceneName}\". Check the spelling, and make sure the scene is in the Scene List (File > Build Profiles).");
                return false;
            }

            StartCoroutine(LoadRoutine(sceneName));
            return true;
        }

        /// <summary>Loads the current scene again from the start.</summary>
        public bool ReloadScene()
        {
            return LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Loads the scene that comes after the current one in the Scene List.</summary>
        public bool LoadNextScene()
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;

            if (currentIndex < 0)
            {
                Debug.LogError("Scene Loader: the current scene is not in the Scene List (File > Build Profiles), so there is no next scene to load.");
                return false;
            }

            int nextIndex = currentIndex + 1;

            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("Scene Loader: this is the last scene in the Scene List, so there is no next scene to load.");
                return false;
            }

            string nextName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(nextIndex));
            return LoadScene(nextName);
        }

        /// <summary>Closes the game. In the Unity Editor, it stops Play mode instead.</summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            LoadStarted?.Invoke(sceneName);

            yield return fader.FadeTo(1f, fadeDuration);

            // If the game was paused when the scene change began, un-pause it,
            // so the new scene doesn't start frozen.
            Time.timeScale = 1f;

            AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
            bool loaded = loading != null;

            if (loaded)
            {
                while (!loading.isDone)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogError($"Scene Loader: Unity could not load the scene \"{sceneName}\".");
            }

            // Give the new scene one frame to set itself up before it is shown.
            yield return null;

            yield return fader.FadeTo(0f, fadeDuration);

            IsLoading = false;

            if (loaded)
            {
                LoadFinished?.Invoke(sceneName);
            }
        }
    }
}
