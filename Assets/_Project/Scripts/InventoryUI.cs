using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Antigravity
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("UI Roots")]
        public GameObject hotbarRoot;
        public GameObject inventoryScreenRoot;
        public GameObject promptRoot;
        
        [Header("Prompt Details")]
        public TextMeshProUGUI promptText;

        [Header("Lamp Oil HUD")]
        public GameObject oilBarRoot;
        public Image oilBarFill;

        [Header("Inventory Details View")]
        public TextMeshProUGUI detailsTitleText;
        public TextMeshProUGUI detailsDescriptionText;
        public Button dropButton;

        [Header("Colors & Sprites")]
        public Sprite defaultSlotSprite;
        public Sprite activeSlotBorderSprite;
        public Color normalSlotColor = new Color(0.12f, 0.12f, 0.12f, 0.75f);
        public Color selectedSlotColor = new Color(0.24f, 0.24f, 0.24f, 0.9f);

        // UI representation arrays
        private Image[] _hotbarSlotIcons;
        private Image[] _hotbarSlotBorders;
        
        private Image[] _inventoryScreenSlotIcons;
        private int _selectedInventorySlotIndex = -1;
        private FirstPersonController _player;

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
            _player = FindFirstObjectByType<FirstPersonController>();

            // Setup cache lists
            CacheUIReferences();

            // Register events
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += RefreshUI;
                InventoryManager.Instance.OnActiveSlotChanged += UpdateActiveHotbarSlotHighlight;
            }

            // Bind drop button
            if (dropButton != null)
            {
                dropButton.onClick.AddListener(DropSelectedInventoryItem);
                dropButton.gameObject.SetActive(false);
            }

            // Initialize UI States
            if (inventoryScreenRoot != null) inventoryScreenRoot.SetActive(false);
            if (hotbarRoot != null) hotbarRoot.SetActive(true);
            if (promptRoot != null) promptRoot.SetActive(false);

            RefreshUI();
            UpdateActiveHotbarSlotHighlight(0);
        }

        private void Update()
        {
            HandleInputs();
            UpdateInteractionPrompt();
            UpdateOilBarUI();
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
                InventoryManager.Instance.OnActiveSlotChanged -= UpdateActiveHotbarSlotHighlight;
            }
        }

        private void CacheUIReferences()
        {
            int slotCount = 9;
            _hotbarSlotIcons = new Image[slotCount];
            _hotbarSlotBorders = new Image[slotCount];
            _inventoryScreenSlotIcons = new Image[slotCount];

            // Cache Hotbar slots
            if (hotbarRoot != null)
            {
                for (int i = 0; i < slotCount; i++)
                {
                    Transform slotTrans = hotbarRoot.transform.Find($"Slot_{i}");
                    if (slotTrans != null)
                    {
                        Transform iconTrans = slotTrans.Find("Icon");
                        if (iconTrans != null) _hotbarSlotIcons[i] = iconTrans.GetComponent<Image>();

                        Transform borderTrans = slotTrans.Find("Border");
                        if (borderTrans != null) _hotbarSlotBorders[i] = borderTrans.GetComponent<Image>();
                    }
                }
            }

            // Cache Inventory Screen slots
            if (inventoryScreenRoot != null)
            {
                Transform gridTrans = inventoryScreenRoot.transform.Find("Panel/SlotsGrid");
                if (gridTrans != null)
                {
                    for (int i = 0; i < slotCount; i++)
                    {
                        Transform slotTrans = gridTrans.Find($"Slot_{i}");
                        if (slotTrans != null)
                        {
                            Transform iconTrans = slotTrans.Find("Icon");
                            if (iconTrans != null) _inventoryScreenSlotIcons[i] = iconTrans.GetComponent<Image>();

                            // Bind Button click event dynamically to select the slot in UI
                            Button btn = slotTrans.GetComponent<Button>();
                            if (btn != null)
                            {
                                int index = i; // local copy for closure
                                btn.onClick.AddListener(() => SelectInventorySlot(index));
                            }
                        }
                    }
                }
            }
        }

        private void HandleInputs()
        {
            if (Keyboard.current == null) return;

            // 1. Tab - Toggle Hotbar visibility
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (hotbarRoot != null)
                {
                    hotbarRoot.SetActive(!hotbarRoot.activeSelf);
                }
            }

            // 2. Escape / E - Close inventory screen if it's already open
            if (inventoryScreenRoot != null && inventoryScreenRoot.activeSelf)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame || 
                    (Keyboard.current.eKey.wasPressedThisFrame && !PlayerInteraction.Instance.CurrentHoveredPickup))
                {
                    ToggleInventory();
                }
            }
        }

        private void UpdateInteractionPrompt()
        {
            if (promptRoot == null || promptText == null) return;

            // Show prompt only if cursor is locked (game is playing)
            if (Cursor.lockState == CursorLockMode.Locked && PlayerInteraction.Instance != null)
            {
                if (PlayerInteraction.Instance.CurrentHoveredPickup != null)
                {
                    promptRoot.SetActive(true);
                    promptText.text = $"Bấm [E] để nhặt {PlayerInteraction.Instance.CurrentHoveredPickup.itemData.itemName}";
                }
                else if (PlayerInteraction.Instance.CurrentHoveredInspectable != null)
                {
                    promptRoot.SetActive(true);
                    promptText.text = $"Bấm [R] để xem {PlayerInteraction.Instance.CurrentHoveredInspectable.objectName}";
                }
                else
                {
                    promptRoot.SetActive(false);
                }
            }
            else
            {
                promptRoot.SetActive(false);
            }
        }

        private void UpdateOilBarUI()
        {
            if (oilBarRoot == null || oilBarFill == null) return;

            ItemData activeItem = InventoryManager.Instance != null ? InventoryManager.Instance.GetActiveItem() : null;
            bool holdingLamp = activeItem != null && activeItem.itemName == "Đèn dầu";

            oilBarRoot.SetActive(holdingLamp);

            if (holdingLamp)
            {
                LampController lamp = FindFirstObjectByType<LampController>();
                if (lamp != null)
                {
                    float ratio = lamp.currentOil / lamp.maxOil;
                    oilBarFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
                }
            }
        }

        public void ToggleInventory()
        {
            if (inventoryScreenRoot == null) return;

            bool targetActive = !inventoryScreenRoot.activeSelf;
            inventoryScreenRoot.SetActive(targetActive);

            if (_player != null)
            {
                _player.IsCursorLocked = !targetActive;
            }
            else
            {
                Cursor.lockState = targetActive ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = targetActive;
            }

            if (targetActive)
            {
                // Reset detail view and select first filled slot or active slot
                SelectInventorySlot(InventoryManager.Instance != null ? InventoryManager.Instance.ActiveSlotIndex : 0);
            }
            else
            {
                _selectedInventorySlotIndex = -1;
            }

            RefreshUI();
        }

        public void RefreshUI()
        {
            if (InventoryManager.Instance == null) return;

            ItemData[] items = InventoryManager.Instance.Slots;
            int count = InventoryManager.Instance.SlotCount;

            for (int i = 0; i < count; i++)
            {
                ItemData item = items[i];

                // Update Hotbar Slot UI
                if (_hotbarSlotIcons[i] != null)
                {
                    if (item != null && item.icon != null)
                    {
                        _hotbarSlotIcons[i].sprite = item.icon;
                        _hotbarSlotIcons[i].enabled = true;
                    }
                    else
                    {
                        _hotbarSlotIcons[i].sprite = null;
                        _hotbarSlotIcons[i].enabled = false;
                    }
                }

                // Update Inventory Screen Slot UI
                if (_inventoryScreenSlotIcons[i] != null)
                {
                    if (item != null && item.icon != null)
                    {
                        _inventoryScreenSlotIcons[i].sprite = item.icon;
                        _inventoryScreenSlotIcons[i].enabled = true;
                    }
                    else
                    {
                        _inventoryScreenSlotIcons[i].sprite = null;
                        _inventoryScreenSlotIcons[i].enabled = false;
                    }

                    // Style grid background slots based on selection
                    Image slotBackground = _inventoryScreenSlotIcons[i].transform.parent.GetComponent<Image>();
                    if (slotBackground != null)
                    {
                        slotBackground.color = (i == _selectedInventorySlotIndex) ? selectedSlotColor : normalSlotColor;
                    }
                }
            }

            UpdateDetailsView();
        }

        private void UpdateActiveHotbarSlotHighlight(int activeIndex)
        {
            if (_hotbarSlotBorders == null) return;

            for (int i = 0; i < _hotbarSlotBorders.Length; i++)
            {
                if (_hotbarSlotBorders[i] != null)
                {
                    _hotbarSlotBorders[i].enabled = (i == activeIndex);
                }
            }
        }

        public void SelectInventorySlot(int index)
        {
            if (index < 0 || index >= 9) return;
            
            _selectedInventorySlotIndex = index;
            RefreshUI();
        }

        private void UpdateDetailsView()
        {
            if (detailsTitleText == null || detailsDescriptionText == null || dropButton == null) return;

            if (InventoryManager.Instance == null || _selectedInventorySlotIndex < 0)
            {
                detailsTitleText.text = "Không có vật phẩm";
                detailsDescriptionText.text = "Chọn một ô trong túi đồ để xem chi tiết.";
                dropButton.gameObject.SetActive(false);
                return;
            }

            ItemData selectedItem = InventoryManager.Instance.Slots[_selectedInventorySlotIndex];
            if (selectedItem != null)
            {
                detailsTitleText.text = selectedItem.itemName;
                if (selectedItem.itemName == "Chìa khoá")
                {
                    detailsDescriptionText.text = "Chìa khoá bằng đồng cổ dùng để mở khoá cửa nhà.";
                }
                else if (selectedItem.itemName == "Đèn dầu")
                {
                    detailsDescriptionText.text = "Đèn dầu cổ giúp thắp sáng góc cực rộng. Cần dầu để duy trì ánh sáng.";
                }
                else
                {
                    detailsDescriptionText.text = $"Một vật phẩm tên là {selectedItem.itemName}.";
                }
                dropButton.gameObject.SetActive(true);
            }
            else
            {
                detailsTitleText.text = "Ô Trống";
                detailsDescriptionText.text = "Ô này hiện chưa chứa đồ.";
                dropButton.gameObject.SetActive(false);
            }
        }

        private void DropSelectedInventoryItem()
        {
            if (InventoryManager.Instance == null || _selectedInventorySlotIndex < 0) return;

            ItemData item = InventoryManager.Instance.Slots[_selectedInventorySlotIndex];
            if (item != null)
            {
                Vector3 spawnPos = Vector3.zero;
                if (_player != null)
                {
                    spawnPos = _player.transform.position + _player.transform.forward * 1.5f + Vector3.up * 0.5f;
                }
                else
                {
                    spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
                }

                // Drop item into world
                GameObject dropObj;
                if (item.prefab != null)
                {
                    dropObj = Instantiate(item.prefab, spawnPos, Quaternion.identity);
                }
                else
                {
                    // Call the static creator in InventorySetup to spawn a beautiful key
                    dropObj = InventorySetup.CreateKeyGameObject(spawnPos);
                }

                // Attach pickup component if missing
                ItemPickup pickup = dropObj.GetComponent<ItemPickup>();
                if (pickup == null) pickup = dropObj.AddComponent<ItemPickup>();
                pickup.itemData = item;

                // Remove from inventory
                InventoryManager.Instance.RemoveItem(_selectedInventorySlotIndex);
                
                // Refresh
                RefreshUI();
            }
        }
    }
}
