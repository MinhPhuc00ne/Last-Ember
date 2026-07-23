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

        [Header("Fly / Noclip Settings")]
        public bool isFlyMode = false;
        public float flySpeed = 25.0f;
        public float fastFlySpeed = 100.0f;

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
            if (_characterController != null)
            {
                _characterController.stepOffset = 0.6f;
                _characterController.slopeLimit = 60.0f;
            }
            
            // Try to find the camera in children
            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null)
            {
                // Fallback 1: Search for MainCamera in scene and parent it
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    _playerCamera = mainCam;
                    _playerCamera.transform.SetParent(transform);
                    _playerCamera.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                    _playerCamera.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    // Fallback 2: Create a new Camera under Player
                    GameObject camObj = new GameObject("Main Camera");
                    camObj.tag = "MainCamera";
                    camObj.transform.SetParent(transform);
                    camObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                    camObj.transform.localRotation = Quaternion.identity;
                    _playerCamera = camObj.AddComponent<Camera>();
                    camObj.AddComponent<AudioListener>();
                    Debug.Log("FirstPersonController: Auto-created Main Camera under Player.");
                }
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

            // Toggle Fly mode with V key
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                isFlyMode = !isFlyMode;
                _velocity = Vector3.zero;
                Debug.Log($"Fly Mode: {(isFlyMode ? "ENABLED" : "DISABLED")}");
            }

            // Quick Teleport Hotkeys
            if (Keyboard.current != null)
            {
                // Key 1: Teleport to Main Scene Spawn
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
                {
                    TeleportTo(new Vector3(0f, 10f, -140f));
                    Debug.Log("Teleported to Main Scene Spawn");
                }
            }

            if (!_isCursorLocked || isInspecting)
            {
                return;
            }

            HandleLook();

            if (isFlyMode)
            {
                HandleFlyMovement();
            }
            else
            {
                HandleMovement();
            }
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

        private void TeleportTo(Vector3 targetPos)
        {
            if (_characterController != null)
            {
                _characterController.enabled = false;
                transform.position = targetPos;
                _characterController.enabled = true;
            }
            else
            {
                transform.position = targetPos;
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

        private void HandleFlyMovement()
        {
            float speed = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ? fastFlySpeed : flySpeed;

            Vector3 flyDir = Vector3.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) flyDir += _playerCamera.transform.forward;
                if (Keyboard.current.sKey.isPressed) flyDir -= _playerCamera.transform.forward;
                if (Keyboard.current.dKey.isPressed) flyDir += _playerCamera.transform.right;
                if (Keyboard.current.aKey.isPressed) flyDir -= _playerCamera.transform.right;

                if (Keyboard.current.eKey.isPressed || Keyboard.current.spaceKey.isPressed) flyDir += Vector3.up;
                if (Keyboard.current.qKey.isPressed || Keyboard.current.leftCtrlKey.isPressed) flyDir -= Vector3.up;
            }

            if (flyDir.sqrMagnitude > 0.01f)
            {
                flyDir.Normalize();
            }

            if (_characterController != null) _characterController.enabled = false;
            transform.position += flyDir * speed * Time.deltaTime;
            if (_characterController != null) _characterController.enabled = true;
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 13;
            style.normal.textColor = Color.white;
            style.richText = true;

            string flyStatus = isFlyMode ? "<color=#55FF55>[FLY MODE ACTIVE]</color>" : "<color=#FFFF55>[WALK MODE]</color>";
            string text = $"<b>Antigravity Controls</b> {flyStatus}\n" +
                         "• <b>[V]</b> Toggle Fly/Noclip (Hold <b>Shift</b> to fly at 100m/s)\n" +
                         "• <b>[1]</b> Teleport to Spawn\n" +
                         "• <b>[ESC]</b> Toggle Mouse Lock";

            GUI.Box(new Rect(10, 10, 390, 80), "");
            GUI.Label(new Rect(20, 15, 370, 70), text, style);
        }
    }
}

