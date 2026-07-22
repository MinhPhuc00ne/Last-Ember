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
        private InspectableObject _currentHoveredInspectable;

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
                _currentHoveredInspectable = null;
                HandleInteractionInput();
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
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
                InspectableObject inspectable = hit.collider.GetComponent<InspectableObject>();
                if (inspectable != null)
                {
                    _currentHoveredInspectable = inspectable;
                    return;
                }
            }

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
        }
    }
}
