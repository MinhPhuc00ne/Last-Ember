using UnityEngine;
using UnityEngine.InputSystem;

namespace Antigravity
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Inventory Settings")]
        [SerializeField] private int _slotCount = 9;
        
        private ItemData[] _slots;
        private int _activeSlotIndex = 0;

        public event System.Action OnInventoryChanged;
        public event System.Action<int> OnActiveSlotChanged;

        public ItemData[] Slots => _slots;
        public int ActiveSlotIndex => _activeSlotIndex;
        public int SlotCount => _slotCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            _slots = new ItemData[_slotCount];
        }

        private void Update()
        {
            // Only handle hotbar selection inputs if the game is active (cursor locked)
            if (Cursor.lockState != CursorLockMode.Locked) return;

            HandleNumberKeysInput();
            HandleScrollInput();
        }

        private void HandleNumberKeysInput()
        {
            if (Keyboard.current == null) return;

            int newIndex = -1;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) newIndex = 0;
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) newIndex = 1;
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) newIndex = 2;
            else if (Keyboard.current.digit4Key.wasPressedThisFrame) newIndex = 3;
            else if (Keyboard.current.digit5Key.wasPressedThisFrame) newIndex = 4;
            else if (Keyboard.current.digit6Key.wasPressedThisFrame) newIndex = 5;
            else if (Keyboard.current.digit7Key.wasPressedThisFrame) newIndex = 6;
            else if (Keyboard.current.digit8Key.wasPressedThisFrame) newIndex = 7;
            else if (Keyboard.current.digit9Key.wasPressedThisFrame) newIndex = 8;

            if (newIndex >= 0 && newIndex < _slotCount && newIndex != _activeSlotIndex)
            {
                SetActiveSlot(newIndex);
            }
        }

        private void HandleScrollInput()
        {
            if (Mouse.current == null) return;

            float scrollY = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.1f)
            {
                int newIndex = _activeSlotIndex;
                if (scrollY < 0f) // Scroll down -> select next slot
                {
                    newIndex = (_activeSlotIndex + 1) % _slotCount;
                }
                else if (scrollY > 0f) // Scroll up -> select previous slot
                {
                    newIndex = (_activeSlotIndex - 1 + _slotCount) % _slotCount;
                }

                if (newIndex != _activeSlotIndex)
                {
                    SetActiveSlot(newIndex);
                }
            }
        }

        public void SetActiveSlot(int index)
        {
            if (index >= 0 && index < _slotCount)
            {
                _activeSlotIndex = index;
                OnActiveSlotChanged?.Invoke(_activeSlotIndex);
            }
        }

        public bool AddItem(ItemData item)
        {
            if (item == null) return false;

            // Find first empty slot
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = item;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            
            Debug.LogWarning("Inventory is full!");
            return false;
        }

        public void RemoveItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slots.Length && _slots[slotIndex] != null)
            {
                _slots[slotIndex] = null;
                OnInventoryChanged?.Invoke();
            }
        }

        public ItemData GetActiveItem()
        {
            if (_activeSlotIndex >= 0 && _activeSlotIndex < _slots.Length)
            {
                return _slots[_activeSlotIndex];
            }
            return null;
        }

        public bool IsFull()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) return false;
            }
            return true;
        }
    }
}
