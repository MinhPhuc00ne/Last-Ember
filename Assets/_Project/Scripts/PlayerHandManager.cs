using UnityEngine;

namespace Antigravity
{
    public class PlayerHandManager : MonoBehaviour
    {
        public static PlayerHandManager Instance { get; private set; }

        [Header("Hand View Models")]
        public GameObject flashlightViewModel;
        public GameObject keyViewModel;
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

        private bool _isInitialized = false;

        private void Start()
        {
            TryInitialize();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                TryInitialize();
            }
        }

        private void TryInitialize()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnActiveSlotChanged += UpdateHandItems;
                InventoryManager.Instance.OnInventoryChanged += RefreshHandItems;
                _isInitialized = true;
                RefreshHandItems();
            }
        }

        private void OnDestroy()
        {
            if (_isInitialized && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnActiveSlotChanged -= UpdateHandItems;
                InventoryManager.Instance.OnInventoryChanged -= RefreshHandItems;
            }
        }

        public void RefreshHandItems()
        {
            if (InventoryManager.Instance != null)
            {
                UpdateHandItems(InventoryManager.Instance.ActiveSlotIndex);
            }
        }

        private void UpdateHandItems(int activeSlotIndex)
        {
            if (InventoryManager.Instance == null) return;

            ItemData activeItem = InventoryManager.Instance.GetActiveItem();
            string itemName = activeItem != null ? activeItem.itemName : "Null";
            Debug.Log($"[HandManager] Updating hand. Active item: {itemName}. LampViewModel: {(lampViewModel != null ? lampViewModel.name : "Null")}");

            // 1. Manage Flashlight ViewModel
            if (flashlightViewModel != null)
            {
                bool isFlashlightActive = activeItem != null && activeItem.itemName == "Đèn pin";
                
                // If we disable the flashlight, turn off the light source first
                if (!isFlashlightActive)
                {
                    Light lt = flashlightViewModel.GetComponent<Light>();
                    if (lt != null) lt.enabled = false;
                }
                
                flashlightViewModel.SetActive(isFlashlightActive);
            }

            // 2. Manage Key ViewModel
            if (keyViewModel != null)
            {
                bool isKeyActive = activeItem != null && activeItem.itemName == "Chìa khoá";
                keyViewModel.SetActive(isKeyActive);
            }

            // 3. Manage Lamp ViewModel
            if (lampViewModel != null)
            {
                bool isLampActive = activeItem != null && activeItem.itemName == "Đèn dầu";
                Debug.Log($"[HandManager] Lamp active state: {isLampActive}. Position: {lampViewModel.transform.localPosition}. Scale: {lampViewModel.transform.localScale}");
                
                if (isLampActive)
                {
                    Camera mainCam = GetComponentInChildren<Camera>();
                    if (mainCam == null) mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        foreach (var r in lampViewModel.GetComponentsInChildren<Renderer>())
                        {
                            Vector3 relativePos = mainCam.transform.InverseTransformPoint(r.transform.position);
                            Debug.Log($"[HandManager] Renderer '{r.name}' active: {r.gameObject.activeInHierarchy}, enabled: {r.enabled}, relative to camera: {relativePos}, bounds: {r.bounds}");
                        }
                    }
                }

                if (!isLampActive)
                {
                    LampController lc = lampViewModel.GetComponent<LampController>();
                    if (lc != null && lc.lampLight != null) lc.lampLight.enabled = false;
                }
                
                lampViewModel.SetActive(isLampActive);
            }
        }
    }
}
