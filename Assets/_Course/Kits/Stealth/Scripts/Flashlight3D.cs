using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.Stealth
{
    /// <summary>
    /// A flashlight. The Attack button (left click, or the gamepad's left face
    /// button) turns it on and off. It drains its battery while on, flickers
    /// when the battery is low, and goes out when it's empty. Battery pickups
    /// call AddBattery.
    ///
    /// While it's on, guards see the player from farther away.
    ///
    /// Setup: add it to a Spot Light that is a child of the camera (first
    /// person) or the player (third person).
    /// </summary>
    [RequireComponent(typeof(Light))]
    [AddComponentMenu("Collin Game/Stealth Kit/Flashlight 3D")]
    public class Flashlight3D : MonoBehaviour
    {
        [Tooltip("Optional. Found in the project if empty.")]
        [SerializeField] InputReader input;

        [Tooltip("Seconds of light from a full battery. 0 means it never runs out.")]
        [SerializeField, Min(0f)] float batterySeconds = 120f;

        [Tooltip("Optional. A number variable for the battery, from 0 to 100, for the HUD.")]
        [SerializeField] FloatVariable batteryPercent;

        [Tooltip("Below this percent, the light flickers.")]
        [SerializeField, Range(0f, 100f)] float flickerBelow = 20f;

        [SerializeField] bool onAtStart;

        [SerializeField] UnityEvent onSwitchedOn;
        [SerializeField] UnityEvent onSwitchedOff;
        [SerializeField] UnityEvent onEmpty;

        Light lamp;
        float fullIntensity;
        float charge = 1f;

        public bool IsOn { get; private set; }

        /// <summary>The battery, from 0 to 1.</summary>
        public float Charge => charge;

        void Awake()
        {
            lamp = GetComponent<Light>();
            fullIntensity = lamp.intensity;

            if (input == null)
            {
                input = InputReader.FindInProject();
            }
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.AttackPressed += Toggle;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.AttackPressed -= Toggle;
            }
        }

        void Start()
        {
            SetOn(onAtStart);
            ShowCharge();
        }

        /// <summary>Turns the flashlight on or off.</summary>
        public void Toggle()
        {
            if (PauseController.GamePaused || (input != null && !input.PlayerInputEnabled))
            {
                return;
            }

            SetOn(!IsOn);
        }

        /// <summary>Adds battery, as a percent, such as 50 for half a battery.</summary>
        public void AddBattery(float percent)
        {
            charge = Mathf.Clamp01(charge + percent / 100f);
            ShowCharge();
        }

        void SetOn(bool on)
        {
            if (on && batterySeconds > 0f && charge <= 0f)
            {
                on = false;
            }

            if (on == IsOn && lamp.enabled == on)
            {
                return;
            }

            IsOn = on;
            lamp.enabled = on;
            lamp.intensity = fullIntensity;

            if (on) onSwitchedOn?.Invoke();
            else onSwitchedOff?.Invoke();
        }

        void Update()
        {
            if (!IsOn || batterySeconds <= 0f || PauseController.GamePaused)
            {
                return;
            }

            charge = Mathf.Max(0f, charge - Time.deltaTime / batterySeconds);
            ShowCharge();

            if (charge <= 0f)
            {
                SetOn(false);
                onEmpty?.Invoke();
                return;
            }

            // Flicker when low.
            bool low = charge * 100f < flickerBelow;
            lamp.intensity = low && Random.value < 0.08f ? fullIntensity * Random.Range(0.1f, 0.6f) : fullIntensity;
        }

        void ShowCharge()
        {
            if (batteryPercent != null)
            {
                batteryPercent.SetValue(Mathf.Round(charge * 100f));
            }
        }
    }
}
