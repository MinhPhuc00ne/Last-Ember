using UnityEngine;
using UnityEngine.InputSystem;

namespace Antigravity
{
    public class PlayerInteraction : MonoBehaviour
    {
        public static PlayerInteraction Instance { get; private set; }

        [Header("Interaction Settings")]
        [SerializeField] private float _interactRange = 3.5f;

        private Camera _playerCamera;
        private ItemPickup _currentHoveredPickup;
        private InspectableObject _currentHoveredInspectable;

        public ItemPickup CurrentHoveredPickup => _currentHoveredPickup;
        public InspectableObject CurrentHoveredInspectable => _currentHoveredInspectable;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }
        }

        private void Update()
        {
            // If currently inspecting an object, don't perform raycasts or standard interactions
            InspectableObject activeInspectable = null;
            var inspectables = FindObjectsByType<InspectableObject>(FindObjectsSortMode.None);
            foreach (var ins in inspectables)
            {
                if (ins.IsBeingInspected)
                {
                    activeInspectable = ins;
                    break;
                }
            }

            if (activeInspectable != null)
            {
                _currentHoveredPickup = null;
                _currentHoveredInspectable = null;
                HandleInteractionInput();
                return;
            }

            // If the inventory screen is open (cursor is free), don't perform raycasts or pick up items
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                _currentHoveredPickup = null;
                _currentHoveredInspectable = null;
                return;
            }

            PerformRaycast();
            HandleInteractionInput();
        }

        private void PerformRaycast()
        {
            if (_playerCamera == null) return;

            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, _interactRange))
            {
                ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    _currentHoveredPickup = pickup;
                    _currentHoveredInspectable = null;
                    return;
                }

                InspectableObject inspectable = hit.collider.GetComponent<InspectableObject>();
                if (inspectable != null)
                {
                    _currentHoveredPickup = null;
                    _currentHoveredInspectable = inspectable;
                    return;
                }
            }

            _currentHoveredPickup = null;
            _currentHoveredInspectable = null;
        }

        private void HandleInteractionInput()
        {
            if (Keyboard.current == null) return;

            // R key for inspection toggle
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                InspectableObject activeInspectable = null;
                var inspectables = FindObjectsByType<InspectableObject>(FindObjectsSortMode.None);
                foreach (var ins in inspectables)
                {
                    if (ins.IsBeingInspected)
                    {
                        activeInspectable = ins;
                        break;
                    }
                }

                if (activeInspectable != null)
                {
                    activeInspectable.StopInspection();
                }
                else if (_currentHoveredInspectable != null)
                {
                    _currentHoveredInspectable.StartInspection();
                }
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (_currentHoveredPickup != null)
                {
                    // Interact to pick up
                    _currentHoveredPickup.TryPickup();
                }
                else
                {
                    // Open inventory if not looking at an interactable
                    if (InventoryUI.Instance != null)
                    {
                        InventoryUI.Instance.ToggleInventory();
                    }
                }
            }
        }
    }
}
