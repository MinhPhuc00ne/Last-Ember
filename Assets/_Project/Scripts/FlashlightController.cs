using UnityEngine;
using UnityEngine.InputSystem;

namespace Antigravity
{
    public class FlashlightController : MonoBehaviour
    {
        private Light _light;
        private float _targetIntensity;
        private float _flickerTimer;

        void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null)
            {
                _targetIntensity = _light.intensity;
            }
        }

        void Update()
        {
            if (_light == null) return;

            // Toggle with F key
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                _light.enabled = !_light.enabled;
            }

            // Scary flicker effect
            if (_light.enabled)
            {
                _flickerTimer -= Time.deltaTime;
                if (_flickerTimer <= 0)
                {
                    // Occasional random flicker
                    if (Random.value < 0.12f) 
                    {
                        StartCoroutine(FlickerRoutine());
                    }
                    _flickerTimer = Random.Range(0.6f, 2.5f);
                }
            }
        }

        private System.Collections.IEnumerator FlickerRoutine()
        {
            float original = _targetIntensity;
            
            // Fast double-flicker
            _light.intensity = original * Random.Range(0.1f, 0.4f);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            _light.intensity = original;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            _light.intensity = original * Random.Range(0.0f, 0.2f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.1f));
            _light.intensity = original;
        }
    }
}
