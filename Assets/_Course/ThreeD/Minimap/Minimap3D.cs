using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CollinGame.ThreeD
{
    /// <summary>
    /// A top-down map in the corner of the screen, for any 3D game where the player finds
    /// their way around. A second camera looks straight down on the player and draws into a
    /// picture that a UI Document shows. M shows or hides the map; holding Tab shows a big map.
    ///
    /// Setup: add this to any object in the scene (Systems is a good place), with a UI Document
    /// using Minimap.uxml (Sort Order above the HUD, below the pause menu). Put Minimap Marker 3D
    /// on things to show on the map: the player, enemies, keys, the exit. Add a layer called
    /// "Minimap" (Project Settings → Tags and Layers) so the markers show on the map only.
    /// </summary>
    [AddComponentMenu("Collin Game/3D/Minimap 3D")]
    public class Minimap3D : MonoBehaviour
    {
        /// <summary>The layer markers go on: seen by the map camera, hidden from the main camera.</summary>
        public const string MarkerLayerName = "Minimap";

        [Header("What it shows")]
        [Tooltip("What the map is centred on. Empty: the object tagged Player.")]
        [SerializeField] Transform target;

        [Tooltip("How much of the level the small map shows: units from the centre to the top edge.")]
        [SerializeField, Min(1f)] float viewSize = 12f;

        [Tooltip("How much the big map (hold Tab) shows.")]
        [SerializeField, Min(1f)] float bigViewSize = 30f;

        [Tooltip("How high above the target the map camera is. Above the tallest wall or tree, or they'll be cut off.")]
        [SerializeField, Min(1f)] float height = 40f;

        [Tooltip("Ticked: the map turns with the player, so up on the map is the way they face. Unticked: north is always up.")]
        [SerializeField] bool turnWithTarget = false;

        [Tooltip("Which layers the map draws (markers are always drawn). Untick a layer such as Ceiling to see into buildings.")]
        [SerializeField] LayerMask showLayers = ~0;

        [Tooltip("The color where there's nothing to draw.")]
        [SerializeField] Color background = new Color(0.05f, 0.06f, 0.08f, 1f);

        [Tooltip("Which renderer in the URP asset's Renderer List the map camera uses. In the course project, 1 is the Universal (3D) Renderer.")]
        [SerializeField, Min(0)] int rendererIndex = 1;

        [Header("On screen")]
        [Tooltip("A UI Document using Minimap.uxml. Empty: one on this object.")]
        [SerializeField] UIDocument document;

        [Tooltip("The map picture's width and height in pixels. Higher is sharper and slower.")]
        [SerializeField, Range(128, 1024)] int resolution = 512;

        [Tooltip("Shows or hides the map.")]
        [SerializeField] Key toggleKey = Key.M;

        [Tooltip("Hold to show the big map.")]
        [SerializeField] Key bigMapKey = Key.Tab;

        [Tooltip("Show the small map when the scene starts.")]
        [SerializeField] bool visibleAtStart = true;

        Camera mapCamera;
        RenderTexture picture;
        VisualElement frame;
        bool visible;
        bool big;

        /// <summary>The Minimap layer's number, or -1 if the project doesn't have one.</summary>
        public static int MarkerLayer => LayerMask.NameToLayer(MarkerLayerName);

        public bool IsVisible => visible;

        void Awake()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                target = player != null ? player.transform : null;
            }

            if (target == null)
            {
                Debug.LogWarning("Minimap 3D: nothing to follow. Tag the player Player, or drag it into Target.", this);
            }

            picture = new RenderTexture(resolution, resolution, 24) { name = "Minimap Picture" };

            GameObject cameraObject = new GameObject("Minimap Camera");
            mapCamera = cameraObject.AddComponent<Camera>();
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = viewSize;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = background;
            mapCamera.nearClipPlane = 0.3f;
            mapCamera.farClipPlane = height + 50f;
            mapCamera.targetTexture = picture;
            mapCamera.depth = -10f;
            mapCamera.allowHDR = false;
            mapCamera.allowMSAA = false;
            UseRenderer(mapCamera, rendererIndex);

            int markerLayer = MarkerLayer;

            if (markerLayer >= 0)
            {
                mapCamera.cullingMask = showLayers | (1 << markerLayer);
            }
            else
            {
                mapCamera.cullingMask = showLayers;
                Debug.LogWarning($"Minimap 3D: the project has no layer called \"{MarkerLayerName}\", so map markers also show in the game view. Add it in Project Settings → Tags and Layers.", this);
            }
        }

        void Start()
        {
            // The main view never shows the map markers.
            int markerLayer = MarkerLayer;

            if (markerLayer >= 0)
            {
                foreach (Camera camera in Camera.allCameras.Where(c => c != mapCamera && c.targetTexture == null))
                {
                    camera.cullingMask &= ~(1 << markerLayer);
                }
            }

            SetVisible(visibleAtStart);
        }

        void OnEnable()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }

            VisualElement root = document != null ? document.rootVisualElement : null;
            frame = root?.Q("minimap-frame");
            VisualElement view = root?.Q("minimap-view");

            if (frame == null || view == null)
            {
                Debug.LogWarning("Minimap 3D: needs a UI Document using Minimap.uxml (elements named minimap-frame and minimap-view).", this);
                return;
            }

            view.Clear();
            Image image = new Image { image = picture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.flexGrow = 1f;
            view.Add(image);
            SetVisible(visible);
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || Time.timeScale == 0f)
            {
                return;
            }

            if (keyboard[toggleKey].wasPressedThisFrame)
            {
                Toggle();
            }

            bool wantBig = keyboard[bigMapKey].isPressed;

            if (wantBig != big)
            {
                ShowBigMap(wantBig);
            }
        }

        void LateUpdate()
        {
            if (target == null || mapCamera == null)
            {
                return;
            }

            float yaw = turnWithTarget ? target.eulerAngles.y : 0f;
            mapCamera.transform.SetPositionAndRotation(target.position + Vector3.up * height, Quaternion.Euler(90f, yaw, 0f));
        }

        void OnDestroy()
        {
            if (mapCamera != null)
            {
                Destroy(mapCamera.gameObject);
            }

            if (picture != null)
            {
                picture.Release();
                Destroy(picture);
            }
        }

        /// <summary>Shows the small map.</summary>
        public void Show() => SetVisible(true);

        /// <summary>Hides the map.</summary>
        public void Hide() => SetVisible(false);

        /// <summary>Shows the map if it's hidden, and hides it if it's showing.</summary>
        public void Toggle() => SetVisible(!visible);

        /// <summary>True: a big map in the middle of the screen, showing more of the level. False: back to the corner.</summary>
        public void ShowBigMap(bool show)
        {
            big = show;

            if (mapCamera != null)
            {
                mapCamera.orthographicSize = big ? bigViewSize : viewSize;
            }

            frame?.EnableInClassList("minimap-frame--big", big);
            SetVisible(visible || big, keepSetting: true);
        }

        void SetVisible(bool show, bool keepSetting = false)
        {
            if (!keepSetting)
            {
                visible = show;
            }

            bool drawn = show || big;

            if (mapCamera != null)
            {
                mapCamera.enabled = drawn;
            }

            if (frame != null)
            {
                frame.style.display = drawn ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// Sets a camera to one of the URP asset's renderers. Done by name, so this script
        /// doesn't need a reference to URP.
        /// </summary>
        static void UseRenderer(Camera camera, int index)
        {
            Type dataType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData", false))
                .FirstOrDefault(type => type != null);

            if (dataType == null)
            {
                return;
            }

            Component data = camera.GetComponent(dataType);

            if (data == null)
            {
                data = camera.gameObject.AddComponent(dataType);
            }

            dataType.GetMethod("SetRenderer", new[] { typeof(int) })?.Invoke(data, new object[] { index });
        }
    }
}
