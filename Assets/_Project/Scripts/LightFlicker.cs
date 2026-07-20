using UnityEngine;

namespace Antigravity
{
    public class LightFlicker : MonoBehaviour
    {
        private Light _light;
        
        [Header("Flicker Settings")]
        public float minScale = 0.5f;
        public float maxScale = 1.1f;
        [Range(0.01f, 0.5f)]
        public float changeInterval = 0.06f;

        private float _timer;
        private float _baseIntensity;

        void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null)
            {
                _baseIntensity = _light.intensity;
            }
        }

        void Update()
        {
            if (_light == null) return;

            _timer += Time.deltaTime;
            if (_timer >= changeInterval)
            {
                _timer = 0f;
                _light.intensity = _baseIntensity * Random.Range(minScale, maxScale);
            }
        }
    }
}
