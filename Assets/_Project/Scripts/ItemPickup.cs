using UnityEngine;

namespace Antigravity
{
    public class ItemPickup : MonoBehaviour
    {
        [Header("Item Configuration")]
        public ItemData itemData;

        [Header("Animation Settings")]
        [SerializeField] private float _spinSpeed = 45f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobRange = 0.08f;

        private Vector3 _startPosition;
        private bool _isPickedUp = false;

        private void Start()
        {
            _startPosition = transform.position;

            // Ensure collider is configured
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                // Add default capsule or box trigger
                BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
                boxCol.size = Vector3.one * 1.5f;
            }
            else
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            // Stationary item, no animations needed
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isPickedUp) return;

            // Walk-over pickup (Minecraft-style)
            if (other.CompareTag("Player") || other.GetComponent<FirstPersonController>() != null)
            {
                TryPickup();
            }
        }

        public bool TryPickup()
        {
            if (_isPickedUp) return false;

            if (InventoryManager.Instance != null)
            {
                if (InventoryManager.Instance.AddItem(itemData))
                {
                    _isPickedUp = true;
                    PlayPickupSound();
                    
                    // Particle / Visual Feedback can be added here, otherwise just destroy
                    Destroy(gameObject);
                    return true;
                }
            }
            return false;
        }

        private void PlayPickupSound()
        {
            try
            {
                AudioClip beepClip = CreatePickupBeepClip();
                AudioSource.PlayClipAtPoint(beepClip, transform.position, 1.0f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to play procedural pickup sound: " + e.Message);
            }
        }

        private AudioClip CreatePickupBeepClip()
        {
            int samplerate = 44100;
            float duration = 0.15f;
            int sampleCount = (int)(samplerate * duration);
            float[] samples = new float[sampleCount];

            // Generate double chime beep procedurally
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / samplerate;
                
                // Double pitch effect: first half low, second half high
                float freq = t < 0.07f ? 523.25f : 659.25f; // C5 to E5 chord transition
                float fadeOut = Mathf.Clamp01((duration - t) / 0.04f);

                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.25f * fadeOut;
            }

            AudioClip clip = AudioClip.Create("PickupBeep", sampleCount, 1, samplerate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
