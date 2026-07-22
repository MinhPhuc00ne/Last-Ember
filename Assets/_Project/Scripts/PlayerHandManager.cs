using UnityEngine;

namespace Antigravity
{
    public class PlayerHandManager : MonoBehaviour
    {
        public static PlayerHandManager Instance { get; private set; }

        [Header("Hand View Models")]
        public GameObject flashlightViewModel;
        public GameObject lampViewModel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    }
}
