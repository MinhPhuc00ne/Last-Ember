using UnityEngine;
using UnityEngine.InputSystem;

namespace Antigravity
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5.0f;
        public float jumpHeight = 1.5f;
        public float gravity = -9.81f;

        [Header("Look Settings")]
        public float mouseSensitivity = 0.15f;
        public float upperLookLimit = 80.0f;
        public float lowerLookLimit = -80.0f;

        private CharacterController _characterController;
        private Camera _playerCamera;
        private Vector3 _velocity;
        private float _xRotation = 0f;
        private bool _isGrounded;
        private bool _isCursorLocked = true;

        public bool isInspecting { get; set; } = false;

        public bool IsCursorLocked
        {
            get => _isCursorLocked;
            set
            {
                _isCursorLocked = value;
                UpdateCursorState();
            }
        }

        void Start()
        {
            _characterController = GetComponent<CharacterController>();
            
            // Try to find the camera in children
            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null)
            {
                Debug.LogError("FirstPersonController: No camera found in children of Player GameObject!");
            }

            UpdateCursorState();
        }

        void Update()
        {
            // Toggle cursor lock with Escape key
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _isCursorLocked = !_isCursorLocked;
                UpdateCursorState();
            }

            if (!_isCursorLocked)
            {
                return;
            }

            if (isInspecting)
            {
                return;
            }

            HandleLook();
            HandleMovement();
        }

        private void UpdateCursorState()
        {
            if (_isCursorLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void HandleLook()
        {
            if (Mouse.current == null || _playerCamera == null) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

            // Yaw rotation (horizontal, rotate player body)
            transform.Rotate(Vector3.up * mouseDelta.x);

            // Pitch rotation (vertical, rotate camera only)
            _xRotation -= mouseDelta.y;
            _xRotation = Mathf.Clamp(_xRotation, lowerLookLimit, upperLookLimit);
            _playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }

        private void HandleMovement()
        {
            // 1. Ground check
            _isGrounded = _characterController.isGrounded;
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Slight downward force to keep grounded
            }

            // 2. Read Keyboard movement
            Vector2 inputDir = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) inputDir.y += 1f;
                if (Keyboard.current.sKey.isPressed) inputDir.y -= 1f;
                if (Keyboard.current.aKey.isPressed) inputDir.x -= 1f;
                if (Keyboard.current.dKey.isPressed) inputDir.x += 1f;
            }

            // Normalize to prevent faster diagonal movement
            if (inputDir.magnitude > 1f)
            {
                inputDir.Normalize();
            }

            Vector3 move = transform.right * inputDir.x + transform.forward * inputDir.y;
            _characterController.Move(move * moveSpeed * Time.deltaTime);

            // 3. Jump
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // 4. Apply Gravity
            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }
    }
}
