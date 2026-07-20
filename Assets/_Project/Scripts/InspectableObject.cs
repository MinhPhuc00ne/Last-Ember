using UnityEngine;
using UnityEngine.InputSystem;

namespace Antigravity
{
    public class InspectableObject : MonoBehaviour
    {
        public string objectName = "Cái bình";

        [Header("Inspection Settings")]
        [SerializeField] private float _inspectDistance = 0.5f; // Distance from camera
        [SerializeField] private float _rotationSpeed = 0.3f;

        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Transform _originalParent;
        private bool _isBeingInspected = false;
        private Camera _mainCamera;
        private FirstPersonController _fpc;

        public bool IsBeingInspected => _isBeingInspected;

        private void Start()
        {
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            _originalParent = transform.parent;
            _mainCamera = Camera.main;
            _fpc = FindFirstObjectByType<FirstPersonController>();
        }

        private void Update()
        {
            if (!_isBeingInspected) return;

            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            // Follow camera position and orientation smoothly in front of it
            Vector3 targetPosition = _mainCamera.transform.position + _mainCamera.transform.forward * _inspectDistance;
            transform.position = targetPosition;

            // Rotate object using mouse delta
            if (Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue() * _rotationSpeed;
                // Rotate around vertical and horizontal screen axes relative to camera
                transform.Rotate(_mainCamera.transform.up, -mouseDelta.x, Space.World);
                transform.Rotate(_mainCamera.transform.right, mouseDelta.y, Space.World);
            }
        }

        public void StartInspection()
        {
            if (_isBeingInspected) return;

            // Keep track of latest position/rotation in case it was changed by physics/editor
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            _isBeingInspected = true;

            if (_fpc == null) _fpc = FindFirstObjectByType<FirstPersonController>();
            if (_fpc != null)
            {
                _fpc.isInspecting = true;
            }

            // Disable physics while inspecting so it doesn't fall/collide
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // Temporarily disable standard collider trigger so it doesn't trigger inventory pickup/interaction
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }

        public void StopInspection()
        {
            if (!_isBeingInspected) return;

            _isBeingInspected = false;

            if (_fpc == null) _fpc = FindFirstObjectByType<FirstPersonController>();
            if (_fpc != null)
            {
                _fpc.isInspecting = false;
            }

            // Return to original local position and rotation relative to original parent
            transform.position = _originalPosition;
            transform.rotation = _originalRotation;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }
}
