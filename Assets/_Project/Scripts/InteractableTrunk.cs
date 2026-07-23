using UnityEngine;

namespace Antigravity
{
    public class InteractableTrunk : MonoBehaviour
    {
        [Header("Trunk References")]
        [SerializeField] private Transform _lidTransform;
        [SerializeField] private float _openAngle = -95.0f; // Target local X rotation when opened
        [SerializeField] private float _openSpeed = 4.5f;

        [Header("Interaction Settings")]
        public string trunkName = "Rương Da Cổ (Leather Trunk)";

        private bool _isOpen = false;
        private Quaternion _closedRotation;
        private Quaternion _targetRotation;

        public bool IsOpen => _isOpen;

        private void Start()
        {
            if (_lidTransform == null)
            {
                _lidTransform = transform.Find("LidPivot");
                if (_lidTransform == null)
                {
                    _lidTransform = transform.Find("LidMesh");
                }
                if (_lidTransform == null)
                {
                    _lidTransform = transform.Find("Lid");
                }
            }

            if (_lidTransform != null)
            {
                _closedRotation = _lidTransform.localRotation;
                _targetRotation = _closedRotation;
            }
        }

        private void Update()
        {
            // Smooth Lid Animation
            if (_lidTransform != null)
            {
                _lidTransform.localRotation = Quaternion.Slerp(_lidTransform.localRotation, _targetRotation, Time.deltaTime * _openSpeed);
            }
        }

        public string GetPromptText()
        {
            return _isOpen ? "[E] Ấn E để Đóng " + trunkName : "[E] Ấn E để Mở " + trunkName;
        }

        public void ToggleOpen()
        {
            _isOpen = !_isOpen;

            if (_lidTransform != null)
            {
                if (_isOpen)
                {
                    _targetRotation = _closedRotation * Quaternion.Euler(_openAngle, 0f, 0f);
                }
                else
                {
                    _targetRotation = _closedRotation;
                }
            }

            Debug.Log($"Antigravity: Trunk '{trunkName}' is now {(_isOpen ? "OPENED" : "CLOSED")}");
        }
    }
}
