using UnityEngine;
using UnityEngine.InputSystem;

namespace Antigravity
{
    public class LampController : MonoBehaviour
    {
        public Light lampLight;

        [Header("Oil Settings")]
        public float maxOil = 100f;
        public float currentOil = 100f;
        public float burnRate = 1.0f; // 1% per second (lasts 100 seconds)

        private bool _isOn = false;
        private float _targetIntensity;
        private float _flickerTimer;

        public bool IsOn => _isOn;

        private void Start()
        {
            if (lampLight == null)
            {
                lampLight = GetComponentInChildren<Light>();
            }
            if (lampLight != null)
            {
                _targetIntensity = lampLight.intensity;
                lampLight.enabled = false; // Off by default
            }
        }

        private void Update()
        {
            if (lampLight == null) return;

            // Toggle with F key
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                _isOn = !_isOn;
                lampLight.enabled = _isOn && currentOil > 0f;
            }

            // Burn oil if on
            if (_isOn && currentOil > 0f)
            {
                currentOil -= burnRate * Time.deltaTime;
                if (currentOil <= 0f)
                {
                    currentOil = 0f;
                    lampLight.enabled = false;
                }
            }

            // Keep light disabled if out of oil
            if (currentOil <= 0f && lampLight.enabled)
            {
                lampLight.enabled = false;
            }

            // Flicker effect for a vintage kerosene flame look
            if (lampLight.enabled)
            {
                _flickerTimer -= Time.deltaTime;
                if (_flickerTimer <= 0f)
                {
                    lampLight.intensity = _targetIntensity * Random.Range(0.85f, 1.15f);
                    _flickerTimer = Random.Range(0.05f, 0.25f);
                }
            }
        }
    }
}
