using UnityEngine;
using UnityEngine.InputSystem;

namespace Antigravity
{
    public class PlayerInteraction : MonoBehaviour
    {
        public static PlayerInteraction Instance { get; private set; }

        [Header("Interaction Settings")]
        [SerializeField] private float _interactRange = 4.0f;

        private Camera _playerCamera;
        private InteractableTrunk _currentHoveredTrunk;

        public InteractableTrunk CurrentHoveredTrunk => _currentHoveredTrunk;

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
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                _currentHoveredTrunk = null;
                return;
            }

            PerformRaycast();
            HandleInteractionInput();
        }

        private void PerformRaycast()
        {
            if (_playerCamera == null) _playerCamera = Camera.main;
            if (_playerCamera == null) return;

            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, _interactRange))
            {
                InteractableTrunk trunk = hit.collider.GetComponentInParent<InteractableTrunk>();
                if (trunk != null)
                {
                    _currentHoveredTrunk = trunk;
                    return;
                }
            }

            _currentHoveredTrunk = null;
        }

        private void HandleInteractionInput()
        {
            if (Keyboard.current == null) return;

            // E key for trunk open / close
            if (Keyboard.current.eKey.wasPressedThisFrame && _currentHoveredTrunk != null)
            {
                _currentHoveredTrunk.ToggleOpen();
            }
        }

        private void OnGUI()
        {
            if (_currentHoveredTrunk != null)
            {
                string prompt = _currentHoveredTrunk.GetPromptText();
                DrawInteractionPrompt(prompt);
            }
        }

        private void DrawInteractionPrompt(string text)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 18;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(1.0f, 0.95f, 0.8f);

            float width = 440f;
            float height = 48f;
            float posX = (Screen.width - width) / 2f;
            float posY = Screen.height - 130f;

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            GUI.Box(new Rect(posX, posY, width, height), text, style);
            GUI.backgroundColor = oldBg;
        }
    }
}
