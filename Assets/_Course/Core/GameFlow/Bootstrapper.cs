using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Creates the "[Course Services]" object when the game starts, before the first
    /// scene loads. That object holds the services every scene needs (for now, the
    /// Scene Loader) and it stays alive when scenes change.
    ///
    /// Because this runs by itself, you can press Play from any scene and the
    /// services are always there. You never add this script to a GameObject.
    ///
    /// The services are the Scene Loader (with its Screen Fader) and the Audio Service.
    ///
    /// Optional: if a prefab named "CourseServices" exists in a Resources folder,
    /// it is used instead of an empty object, so its settings can be changed in
    /// the Inspector.
    /// </summary>
    public static class Bootstrapper
    {
        const string ServicesPrefabName = "CourseServices";
        const string ServicesObjectName = "[Course Services]";

        /// <summary>The object that holds the course services. It is never destroyed while the game runs.</summary>
        public static GameObject Services { get; private set; }

        // Clears the reference when Play mode starts, so it still works when
        // "Reload Domain" is turned off in the Enter Play Mode settings.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Services = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateServices()
        {
            if (Services != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(ServicesPrefabName);
            Services = prefab != null ? Object.Instantiate(prefab) : new GameObject();
            Services.name = ServicesObjectName;
            Object.DontDestroyOnLoad(Services);

            // Adding the Scene Loader also adds the Screen Fader it needs.
            if (Services.GetComponent<SceneLoader>() == null)
            {
                Services.AddComponent<SceneLoader>();
            }

            if (Services.GetComponent<AudioService>() == null)
            {
                Services.AddComponent<AudioService>();
            }
        }
    }
}
