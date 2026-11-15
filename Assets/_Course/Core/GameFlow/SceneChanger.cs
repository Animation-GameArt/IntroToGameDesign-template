using UnityEngine;
using UnityEngine.InputSystem;

namespace CollinGame.Core
{
    /// <summary>
    /// Changes to another scene, with no code needed. Add it to any GameObject and
    /// type the name of the scene to load. The scene change can start in three ways:
    ///
    /// 1. After a number of seconds (Load After Seconds).
    /// 2. When the player uses an input action, such as Jump (Load On Action).
    /// 3. When something else calls Load(), such as a button or another script.
    ///
    /// The scene must be in the Scene List (File > Build Profiles).
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Scene Changer")]
    public class SceneChanger : MonoBehaviour
    {
        [Tooltip("The name of the scene to load, exactly as it appears in the Project window (without .unity).")]
        [SerializeField] string sceneName;

        [Tooltip("Load the scene this many seconds after this object appears. 0 turns the timer off.")]
        [SerializeField, Min(0f)] float loadAfterSeconds = 0f;

        [Tooltip("Optional. Load the scene when the player uses this input action, for example Player/Jump.")]
        [SerializeField] InputActionReference loadOnAction;

        float timer;
        bool loadRequested;

        void OnEnable()
        {
            timer = 0f;
            loadRequested = false;

            if (loadOnAction != null && loadOnAction.action != null)
            {
                loadOnAction.action.performed += OnActionPerformed;

                // The project's input actions are normally switched on already.
                // This makes sure this one is, in case it comes from another asset.
                if (!loadOnAction.action.enabled)
                {
                    loadOnAction.action.Enable();
                }
            }
        }

        void OnDisable()
        {
            if (loadOnAction != null && loadOnAction.action != null)
            {
                loadOnAction.action.performed -= OnActionPerformed;
            }
        }

        void Update()
        {
            if (loadAfterSeconds <= 0f || loadRequested)
            {
                return;
            }

            timer += Time.deltaTime;

            if (timer >= loadAfterSeconds)
            {
                Load();
            }
        }

        void OnActionPerformed(InputAction.CallbackContext context)
        {
            Load();
        }

        /// <summary>Loads the scene named in Scene Name.</summary>
        public void Load()
        {
            if (loadRequested || !LoaderIsReady())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError($"Scene Changer on \"{name}\": Scene Name is empty. Type the name of the scene to load.", this);
                enabled = false; // Stop the timer from repeating this message every frame.
                return;
            }

            loadRequested = SceneLoader.Instance.LoadScene(sceneName);

            if (!loadRequested)
            {
                enabled = false; // The Console explains why. Fix it, then press Play again.
            }
        }

        /// <summary>Loads the current scene again from the start.</summary>
        public void ReloadScene()
        {
            if (!loadRequested && LoaderIsReady())
            {
                loadRequested = SceneLoader.Instance.ReloadScene();
            }
        }

        /// <summary>Loads the scene that comes after this one in the Scene List.</summary>
        public void LoadNextScene()
        {
            if (!loadRequested && LoaderIsReady())
            {
                loadRequested = SceneLoader.Instance.LoadNextScene();
            }
        }

        /// <summary>Closes the game. In the Unity Editor, it stops Play mode instead.</summary>
        public void QuitGame()
        {
            if (LoaderIsReady())
            {
                SceneLoader.Instance.QuitGame();
            }
        }

        bool LoaderIsReady()
        {
            if (SceneLoader.Instance != null)
            {
                return true;
            }

            Debug.LogError($"Scene Changer on \"{name}\": there is no Scene Loader. It is created when the game starts, so this only works in Play mode.", this);
            return false;
        }
    }
}
