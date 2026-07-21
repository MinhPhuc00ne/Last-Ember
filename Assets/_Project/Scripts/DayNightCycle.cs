using UnityEngine;

namespace Antigravity
{
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Cycle Settings")]
        [Tooltip("Duration of a full day-night cycle in seconds.")]
        public float dayDurationInSeconds = 60f;
        
        public float timeOfDay = 0.35f; // Start in the morning/daytime by default
        
        [Tooltip("Locks the day-night cycle permanently at daytime.")]
        public bool isPermanentlyDay = true;

        [Tooltip("Locks the day-night cycle permanently at midnight.")]
        public bool isPermanentlyNight = false;

        [Header("Lighting References")]
        public Light sunLight;
        public Light moonLight;

        [Header("Atmosphere Settings (Auto-initialized if empty)")]
        public Gradient sunColor;
        public Gradient moonColor;
        public Gradient ambientColor;
        public Gradient fogColor;
        public AnimationCurve fogDensityCurve;
        
        public float maxFogDensity = 0.09f;
        public float minFogDensity = 0.015f;

        private float _sunBaseIntensity = 1.0f;
        private float _moonBaseIntensity = 0.08f;
        private Material _defaultSkybox;
        private CameraClearFlags _defaultCameraClearFlags = CameraClearFlags.Skybox;
        private Color _defaultCameraBackgroundColor;

        void Start()
        {
            _defaultSkybox = RenderSettings.skybox;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                _defaultCameraClearFlags = mainCam.clearFlags;
                _defaultCameraBackgroundColor = mainCam.backgroundColor;
            }

            if (sunLight != null) _sunBaseIntensity = sunLight.intensity;
            if (moonLight != null) _moonBaseIntensity = moonLight.intensity;

            // Initialize gradients programmatically if not set in inspector
            if (sunColor == null || sunColor.colorKeys.Length == 0)
            {
                InitializeGradients();
            }
        }

        void Update()
        {
            // Advance time
            if (isPermanentlyDay)
            {
                timeOfDay = 0.35f; // Lock at bright daytime (10:00 AM)
            }
            else if (isPermanentlyNight)
            {
                timeOfDay = 0f; // Lock at deep midnight
            }
            else if (Application.isPlaying)
            {
                timeOfDay += Time.deltaTime / dayDurationInSeconds;
                if (timeOfDay >= 1f) timeOfDay = 0f;
            }

            UpdateLighting();
        }

        private void UpdateLighting()
        {
            if (sunColor == null) return;

            // Calculate angles: 0 is Midnight, so sun is at -90 degrees (facing straight up)
            // 0.25 is Sunrise, sun is at 0 degrees (horizon)
            // 0.5 is Noon, sun is at 90 degrees (facing straight down)
            // 0.75 is Sunset, sun is at 180 degrees (horizon)
            float sunAngle = (timeOfDay * 360f) - 90f;
            float moonAngle = sunAngle + 180f;

            // Rotate and configure Sun
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, -60f, 0f);
                float sunDot = Vector3.Dot(sunLight.transform.forward, Vector3.down);
                
                // Dim sun intensity smoothly near horizon
                sunLight.intensity = Mathf.Clamp01(sunDot * 3f) * _sunBaseIntensity;
                sunLight.color = sunColor.Evaluate(timeOfDay);
                sunLight.enabled = sunLight.intensity > 0.01f;
            }

            // Rotate and configure Moon
            if (moonLight != null)
            {
                moonLight.transform.rotation = Quaternion.Euler(moonAngle, -60f, 0f);
                float moonDot = Vector3.Dot(moonLight.transform.forward, Vector3.down);
                
                moonLight.intensity = Mathf.Clamp01(moonDot * 3f) * (_moonBaseIntensity * 0.15f);
                moonLight.color = moonColor.Evaluate(timeOfDay);
                moonLight.enabled = moonLight.intensity > 0.001f;
            }

            // Update ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor.Evaluate(timeOfDay);

            // Update fog
            RenderSettings.fog = false;
            RenderSettings.fogColor = fogColor.Evaluate(timeOfDay);
            float fogFactor = fogDensityCurve.Evaluate(timeOfDay);
            RenderSettings.fogDensity = Mathf.Lerp(minFogDensity, maxFogDensity, fogFactor);

            // Remove skybox and clear camera to black if permanently night, otherwise restore them
            if (isPermanentlyNight)
            {
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black;
                RenderSettings.fogColor = Color.black;
                RenderSettings.fogDensity = 0f;

                if (Application.isPlaying)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        mainCam.clearFlags = CameraClearFlags.SolidColor;
                        mainCam.backgroundColor = Color.black;
                    }
                }
            }
            else
            {
                #if UNITY_EDITOR
                if (RenderSettings.skybox == null)
                {
                    RenderSettings.skybox = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
                }
                #else
                if (RenderSettings.skybox == null && _defaultSkybox != null)
                {
                    RenderSettings.skybox = _defaultSkybox;
                }
                #endif

                if (Application.isPlaying)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null && mainCam.clearFlags == CameraClearFlags.SolidColor && mainCam.backgroundColor == Color.black)
                    {
                        mainCam.clearFlags = _defaultCameraClearFlags;
                        mainCam.backgroundColor = _defaultCameraBackgroundColor;
                    }
                }
            }
        }

        private void OnValidate()
        {
            // Call update in editor when slider changes
            UpdateLighting();
        }

        private void Reset()
        {
            InitializeGradients();
        }

        private void InitializeGradients()
        {
            // 1. Sun Color Gradient: Sunrise (orange), Noon (white/yellow), Sunset (red), Night (black)
            sunColor = new Gradient();
            sunColor.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.12f, 0.15f, 0.28f), 0f),    // Night
                    new GradientColorKey(new Color(0.95f, 0.45f, 0.2f), 0.22f),   // Sunrise start
                    new GradientColorKey(new Color(1f, 0.96f, 0.88f), 0.5f),      // Noon
                    new GradientColorKey(new Color(0.9f, 0.3f, 0.15f), 0.78f),    // Sunset end
                    new GradientColorKey(new Color(0.12f, 0.15f, 0.28f), 1f)      // Night
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // 2. Moon Color Gradient: Dark blue/white at night, disabled/dark during day
            moonColor = new Gradient();
            moonColor.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.03f), 0f),     // Night
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.2f),    // Sunrise transition
                    new GradientColorKey(new Color(0f, 0f, 0f), 0.5f),            // Day (off)
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.8f),    // Sunset transition
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.03f), 1f)      // Night
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // 3. Ambient Color: Dark blue (night), soft gold (sunrise), white/yellow (day), orange/red (sunset)
            ambientColor = new Gradient();
            ambientColor.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0f),  // Night
                    new GradientColorKey(new Color(0.5f, 0.45f, 0.4f), 0.22f),    // Sunrise
                    new GradientColorKey(new Color(0.72f, 0.75f, 0.82f), 0.5f),   // Noon
                    new GradientColorKey(new Color(0.55f, 0.4f, 0.35f), 0.78f),   // Sunset
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 1f)   // Night
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // 4. Fog Color: Dark blue-grey (night), foggy orange (sunrise), grey-blue (day), foggy red (sunset)
            fogColor = new Gradient();
            fogColor.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0f, 0f, 0f), 0f),    // Night
                    new GradientColorKey(new Color(0.7f, 0.6f, 0.5f), 0.22f),     // Sunrise
                    new GradientColorKey(new Color(0.82f, 0.85f, 0.88f), 0.5f),   // Noon
                    new GradientColorKey(new Color(0.75f, 0.5f, 0.4f), 0.78f),    // Sunset
                    new GradientColorKey(new Color(0f, 0f, 0f), 1f)     // Night
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // 5. Fog Density Curve: Thicker fog during night and sunrise/sunset, thinner during noon
            fogDensityCurve = new AnimationCurve(
                new Keyframe(0f, 1.0f),    // Midnight (Dense)
                new Keyframe(0.22f, 0.8f), // Sunrise (Dense)
                new Keyframe(0.5f, 0.2f),  // Noon (Thinner)
                new Keyframe(0.78f, 0.8f), // Sunset (Dense)
                new Keyframe(1f, 1.0f)     // Midnight (Dense)
            );
        }
    }
}
