using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Antigravity
{
    public class InventorySetup : MonoBehaviour
    {
        private static InventorySetup _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnRuntimeMethodLoad()
        {
            GameObject initializer = new GameObject("InventorySystemInitializer");
            initializer.AddComponent<InventorySetup>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Make sure this persists across scenes
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetupInventorySystem();
        }

        [ContextMenu("Setup Inventory System")]
        public void SetupInventorySystem()
        {
            Debug.Log("Antigravity: Running Inventory System setup...");

            // 1. Create the Key ItemData (always do this first)
            ItemData keyItem = CreateKeyItemData();

            // 2. Spawn the Physical Key in the house (or in front of player if no house)
            SpawnHouseKey(keyItem);

            // 2b. Spawn the Physical Lamp in the house
            ItemData lampItem = CreateLampItemData();
            SpawnHouseLamp(lampItem);

            // 3. Find or Setup Player GameObjects
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
                if (fpc != null) player = fpc.gameObject;
            }
            if (player == null)
            {
                Camera cam = Camera.main;
                if (cam != null) player = cam.gameObject;
            }

            if (player == null)
            {
                Debug.LogWarning("InventorySetup: Player GameObject or Camera not found in scene! Key and Lamp spawned, but UI and controller scripts could not be attached.");
                return;
            }

            // Ensure InventoryManager is attached to Player
            InventoryManager inventoryManager = player.GetComponent<InventoryManager>();
            if (inventoryManager == null)
            {
                inventoryManager = player.AddComponent<InventoryManager>();
            }

            // Ensure PlayerInteraction is attached to Player
            PlayerInteraction playerInteraction = player.GetComponent<PlayerInteraction>();
            if (playerInteraction == null)
            {
                playerInteraction = player.AddComponent<PlayerInteraction>();
            }

            // Setup Flashlight item in slot 0 on startup
            ItemData flashlightItem = CreateFlashlightItemData();
            if (inventoryManager != null)
            {
                if (inventoryManager.Slots[0] == null)
                {
                    inventoryManager.Slots[0] = flashlightItem;
                }
            }

            // Setup Hand Manager and Hand ViewModels
            SetupHandManager(player, flashlightItem);

            // 4. Create the Canvas and UI hierarchy
            CreateUI(player);

            Debug.Log("Antigravity: Inventory System setup completed successfully!");
        }

        private void CreateUI(GameObject player)
        {
            // Delete existing canvas if already setup to prevent duplicates
            GameObject oldCanvas = GameObject.Find("InventoryCanvas");
            if (oldCanvas != null)
            {
                Destroy(oldCanvas);
            }

            GameObject canvasGo = new GameObject("InventoryCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGo.AddComponent<GraphicRaycaster>();

            // A thin font reference if TMP is present, otherwise TMPro handles default internally
            TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;

            // 2.1. Center Crosshair Dot
            GameObject crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(canvasGo.transform, false);
            Image chImg = crosshair.AddComponent<Image>();
            chImg.color = new Color(1f, 1f, 1f, 0.4f);
            RectTransform chRect = crosshair.GetComponent<RectTransform>();
            chRect.sizeDelta = new Vector2(6, 6);
            chRect.anchoredPosition = Vector2.zero;

            // 2.2. Interaction Prompt
            GameObject prompt = new GameObject("InteractionPrompt");
            prompt.transform.SetParent(canvasGo.transform, false);
            prompt.SetActive(false);
            RectTransform promptRect = prompt.AddComponent<RectTransform>();
            promptRect.anchoredPosition = new Vector2(0f, -80f);
            promptRect.sizeDelta = new Vector2(600f, 60f);

            // Backplate for prompt to make it readable
            Image promptBg = prompt.AddComponent<Image>();
            promptBg.color = new Color(0f, 0f, 0f, 0.5f);

            GameObject promptTextGo = new GameObject("Text");
            promptTextGo.transform.SetParent(prompt.transform, false);
            TextMeshProUGUI pText = promptTextGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) pText.font = fontAsset;
            pText.fontSize = 24;
            pText.alignment = TextAlignmentOptions.Center;
            pText.color = Color.white;
            pText.text = "Bấm [E] để nhặt vật phẩm";
            RectTransform pTextRect = promptTextGo.GetComponent<RectTransform>();
            pTextRect.anchorMin = Vector2.zero;
            pTextRect.anchorMax = Vector2.one;
            pTextRect.sizeDelta = Vector2.zero;

            // 2.3. Hotbar HUD (9 Slots at Bottom)
            GameObject hotbar = new GameObject("HotbarHUD");
            hotbar.transform.SetParent(canvasGo.transform, false);
            RectTransform hotbarRect = hotbar.AddComponent<RectTransform>();
            hotbarRect.anchorMin = new Vector2(0.5f, 0f);
            hotbarRect.anchorMax = new Vector2(0.5f, 0f);
            hotbarRect.pivot = new Vector2(0.5f, 0f);
            hotbarRect.anchoredPosition = new Vector2(0f, 30f);
            hotbarRect.sizeDelta = new Vector2(630f, 74f); // 9 * 64 + gaps

            Image hotbarBg = hotbar.AddComponent<Image>();
            hotbarBg.color = new Color(0.08f, 0.08f, 0.08f, 0.65f); // Modern glassmorphic background

            HorizontalLayoutGroup hotbarLayout = hotbar.AddComponent<HorizontalLayoutGroup>();
            hotbarLayout.padding = new RectOffset(5, 5, 5, 5);
            hotbarLayout.spacing = 6;
            hotbarLayout.childAlignment = TextAnchor.MiddleCenter;
            hotbarLayout.childControlHeight = false;
            hotbarLayout.childControlWidth = false;
            hotbarLayout.childForceExpandHeight = false;
            hotbarLayout.childForceExpandWidth = false;

            // Build 9 Slots for Hotbar
            for (int i = 0; i < 9; i++)
            {
                GameObject slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(hotbar.transform, false);
                RectTransform slotRect = slot.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(64f, 64f);

                Image slotBg = slot.AddComponent<Image>();
                slotBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

                // Selection Border
                GameObject border = new GameObject("Border");
                border.transform.SetParent(slot.transform, false);
                Image borderImg = border.AddComponent<Image>();
                borderImg.color = new Color(1f, 0.82f, 0.18f, 0.95f); // Golden active outline
                borderImg.enabled = false;
                RectTransform borderRect = border.GetComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.sizeDelta = new Vector2(4, 4); // Outline margin

                // Item Icon
                GameObject icon = new GameObject("Icon");
                icon.transform.SetParent(slot.transform, false);
                Image iconImg = icon.AddComponent<Image>();
                iconImg.enabled = false;
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(44f, 44f);
            }

            // 2.4. Full Inventory Screen Overlay (Disabled by default)
            GameObject invScreen = new GameObject("InventoryScreen");
            invScreen.transform.SetParent(canvasGo.transform, false);
            invScreen.SetActive(false);
            RectTransform invScreenRect = invScreen.AddComponent<RectTransform>();
            invScreenRect.anchorMin = Vector2.zero;
            invScreenRect.anchorMax = Vector2.one;
            invScreenRect.sizeDelta = Vector2.zero;

            Image overlayBg = invScreen.AddComponent<Image>();
            overlayBg.color = new Color(0f, 0f, 0f, 0.6f); // Fade out ambient background

            // Inventory Box Center Panel
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(invScreen.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(700f, 480f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.05f, 0.05f, 0.92f); // Glassmorphism dark frame

            // Panel Title
            GameObject title = new GameObject("Title");
            title.transform.SetParent(panel.transform, false);
            TextMeshProUGUI tText = title.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) tText.font = fontAsset;
            tText.fontSize = 28;
            tText.alignment = TextAlignmentOptions.Center;
            tText.color = new Color(1f, 0.82f, 0.18f, 1f);
            tText.text = "TÚI ĐỒ (INVENTORY)";
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -25f);
            titleRect.sizeDelta = new Vector2(500f, 40f);

            // Panel Slots Container
            GameObject grid = new GameObject("SlotsGrid");
            grid.transform.SetParent(panel.transform, false);
            RectTransform gridRect = grid.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, 40f);
            gridRect.sizeDelta = new Vector2(600f, 76f);

            HorizontalLayoutGroup gridLayout = grid.AddComponent<HorizontalLayoutGroup>();
            gridLayout.padding = new RectOffset(5, 5, 5, 5);
            gridLayout.spacing = 8;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.childControlHeight = false;
            gridLayout.childControlWidth = false;
            gridLayout.childForceExpandHeight = false;
            gridLayout.childForceExpandWidth = false;

            // Build 9 Buttons inside grid
            for (int i = 0; i < 9; i++)
            {
                GameObject slotBtn = new GameObject($"Slot_{i}");
                slotBtn.transform.SetParent(grid.transform, false);
                RectTransform slotRect = slotBtn.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(58f, 58f);

                Image slotImg = slotBtn.AddComponent<Image>();
                slotImg.color = new Color(0.12f, 0.12f, 0.12f, 0.75f);

                slotBtn.AddComponent<Button>();

                // Item Icon
                GameObject icon = new GameObject("Icon");
                icon.transform.SetParent(slotBtn.transform, false);
                Image iconImg = icon.AddComponent<Image>();
                iconImg.enabled = false;
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(40f, 40f);
            }

            // Selected Item Detail Card
            GameObject detailCard = new GameObject("DetailCard");
            detailCard.transform.SetParent(panel.transform, false);
            RectTransform cardRect = detailCard.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0f);
            cardRect.anchorMax = new Vector2(0.5f, 0f);
            cardRect.pivot = new Vector2(0.5f, 0f);
            cardRect.anchoredPosition = new Vector2(0f, 35f);
            cardRect.sizeDelta = new Vector2(620f, 150f);

            Image cardBg = detailCard.AddComponent<Image>();
            cardBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Detail Title
            GameObject detailTitle = new GameObject("DetailTitle");
            detailTitle.transform.SetParent(detailCard.transform, false);
            TextMeshProUGUI dtText = detailTitle.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) dtText.font = fontAsset;
            dtText.fontSize = 22;
            dtText.alignment = TextAlignmentOptions.Left;
            dtText.color = Color.white;
            dtText.text = "Không có vật phẩm";
            RectTransform dtRect = detailTitle.GetComponent<RectTransform>();
            dtRect.anchoredPosition = new Vector2(25f, 40f);
            dtRect.sizeDelta = new Vector2(350f, 30f);

            // Detail Description
            GameObject detailDesc = new GameObject("DetailDesc");
            detailDesc.transform.SetParent(detailCard.transform, false);
            TextMeshProUGUI ddText = detailDesc.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) ddText.font = fontAsset;
            ddText.fontSize = 16;
            ddText.alignment = TextAlignmentOptions.TopLeft;
            ddText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            ddText.text = "Chọn một ô trong túi đồ để xem chi tiết.";
            RectTransform ddRect = detailDesc.GetComponent<RectTransform>();
            ddRect.anchoredPosition = new Vector2(25f, -25f);
            ddRect.sizeDelta = new Vector2(350f, 80f);

            // Drop Button
            GameObject dropBtnGo = new GameObject("DropButton");
            dropBtnGo.transform.SetParent(detailCard.transform, false);
            RectTransform dbRect = dropBtnGo.AddComponent<RectTransform>();
            dbRect.anchoredPosition = new Vector2(210f, 0f);
            dbRect.sizeDelta = new Vector2(140f, 50f);

            Image dbImg = dropBtnGo.AddComponent<Image>();
            dbImg.color = new Color(0.6f, 0.15f, 0.15f, 0.9f); // Solid reddish color
            
            Button dbBtn = dropBtnGo.AddComponent<Button>();

            GameObject dbTextGo = new GameObject("Text");
            dbTextGo.transform.SetParent(dropBtnGo.transform, false);
            TextMeshProUGUI dbText = dbTextGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) dbText.font = fontAsset;
            dbText.fontSize = 18;
            dbText.alignment = TextAlignmentOptions.Center;
            dbText.color = Color.white;
            dbText.text = "BỎ ĐỒ (DROP)";
            RectTransform dbTextRect = dbTextGo.GetComponent<RectTransform>();
            dbTextRect.anchorMin = Vector2.zero;
            dbTextRect.anchorMax = Vector2.one;
            dbTextRect.sizeDelta = Vector2.zero;

            // Close button (Top Right X)
            GameObject closeBtnGo = new GameObject("CloseButton");
            closeBtnGo.transform.SetParent(panel.transform, false);
            RectTransform cbRect = closeBtnGo.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(1f, 1f);
            cbRect.anchorMax = new Vector2(1f, 1f);
            cbRect.pivot = new Vector2(1f, 1f);
            cbRect.anchoredPosition = new Vector2(-15f, -15f);
            cbRect.sizeDelta = new Vector2(35f, 35f);

            Image cbImg = closeBtnGo.AddComponent<Image>();
            cbImg.color = new Color(0.25f, 0.25f, 0.25f, 0.8f);

            Button cbBtn = closeBtnGo.AddComponent<Button>();

            GameObject cbTextGo = new GameObject("Text");
            cbTextGo.transform.SetParent(closeBtnGo.transform, false);
            TextMeshProUGUI cbText = cbTextGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) cbText.font = fontAsset;
            cbText.fontSize = 18;
            cbText.alignment = TextAlignmentOptions.Center;
            cbText.color = Color.white;
            cbText.text = "X";
            RectTransform cbTextRect = cbTextGo.GetComponent<RectTransform>();
            cbTextRect.anchorMin = Vector2.zero;
            cbTextRect.anchorMax = Vector2.one;
            cbTextRect.sizeDelta = Vector2.zero;

            // 2.6. Lamp Oil HUD Slider
            GameObject oilBarGo = new GameObject("OilBarHUD");
            oilBarGo.transform.SetParent(canvasGo.transform, false);
            oilBarGo.SetActive(false); // Only active when holding the lamp
            RectTransform oilRect = oilBarGo.AddComponent<RectTransform>();
            oilRect.anchorMin = new Vector2(0.5f, 0f);
            oilRect.anchorMax = new Vector2(0.5f, 0f);
            oilRect.pivot = new Vector2(0.5f, 0f);
            oilRect.anchoredPosition = new Vector2(0f, 115f); // Above hotbar
            oilRect.sizeDelta = new Vector2(200f, 15f);

            // Oil bar background
            Image oilBg = oilBarGo.AddComponent<Image>();
            oilBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Fill image
            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(oilBarGo.transform, false);
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(1.0f, 0.65f, 0.1f, 0.9f); // Warm flame orange color
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f); // Start with full fill
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;

            // Label text "DẦU ĐÈN"
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(oilBarGo.transform, false);
            TextMeshProUGUI labelText = labelGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) labelText.font = fontAsset;
            labelText.fontSize = 10;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            labelText.text = "DẦU ĐÈN (OIL)";
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            // 2.5. Attach and Bind UI Manager Script (InventoryUI)
            InventoryUI uiManager = canvasGo.AddComponent<InventoryUI>();
            uiManager.hotbarRoot = hotbar;
            uiManager.inventoryScreenRoot = invScreen;
            uiManager.promptRoot = prompt;
            uiManager.promptText = pText;
            uiManager.detailsTitleText = dtText;
            uiManager.detailsDescriptionText = ddText;
            uiManager.dropButton = dbBtn;
            uiManager.oilBarRoot = oilBarGo;
            uiManager.oilBarFill = fillImg;

            // Bind UI Close Button to toggle method
            cbBtn.onClick.AddListener(uiManager.ToggleInventory);
        }

        private ItemData CreateKeyItemData()
        {
            // Dynamically construct Key ItemData
            ItemData keyData = ScriptableObject.CreateInstance<ItemData>();
            keyData.itemName = "Chìa khoá";
            
            // Procedurally draw Key Icon Sprite to match original texture style
            Sprite iconSprite = CreateKeyIconSprite();
            keyData.icon = iconSprite;
            keyData.prefab = null; // Let the UI drop code use our custom procedural 3D model

            return keyData;
        }

        private Sprite CreateKeyIconSprite()
        {
            // Build a 64x64 Texture2D for the key icon
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            
            // Clear texture with full transparent alpha
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            Color goldColor = new Color(1.0f, 0.83f, 0.15f, 1f);
            Color shadowColor = new Color(0.75f, 0.58f, 0.05f, 1f);

            // Draw circular loop handle (center cx=20, cy=32)
            int cx = 20;
            int cy = 32;
            int outerRad = 9;
            int innerRad = 5;
            for (int dy = -outerRad; dy <= outerRad; dy++)
            {
                for (int dx = -outerRad; dx <= outerRad; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= outerRad && dist >= innerRad)
                    {
                        // Add shadow depth color to bottom-right curve
                        bool isShadow = dx > 2 || dy < -2;
                        tex.SetPixel(cx + dx, cy + dy, isShadow ? shadowColor : goldColor);
                    }
                }
            }

            // Draw shaft extending from x=29 to x=52 at y=32 (thickness of 3 pixels)
            for (int x = 29; x <= 52; x++)
            {
                tex.SetPixel(x, 31, shadowColor);
                tex.SetPixel(x, 32, goldColor);
                tex.SetPixel(x, 33, shadowColor);
            }

            // Draw teeth at x=44..46 and x=49..51 going downward (from y=24 to 30)
            for (int y = 24; y <= 30; y++)
            {
                tex.SetPixel(44, y, shadowColor);
                tex.SetPixel(45, y, goldColor);
                tex.SetPixel(46, y, shadowColor);

                tex.SetPixel(49, y, shadowColor);
                tex.SetPixel(50, y, goldColor);
                tex.SetPixel(51, y, shadowColor);
            }

            tex.Apply();

            // Create Sprite from Texture
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
            return sprite;
        }

        private void SpawnHouseKey(ItemData keyData)
        {
            // Calculate placement position
            GameObject house = GameObject.Find("House");
            Vector3 spawnPosition;

            if (house != null)
            {
                // House found! Place on dining table in the living room
                // DiningTable is placed at local offset (1.2f, 0.25f, -2.0f) in SetupFirstPersonScene
                // Table height surface is at local Y ~ 0.95 relative to house transform
                spawnPosition = house.transform.TransformPoint(new Vector3(1.8f, 0.95f, -2.0f));
                Debug.Log($"InventorySetup: Placed key on the dining table inside House at: {spawnPosition}");
            }
            else
            {
                // Fallback: spawn 3m in front of spawn point (0, 0, 0)
                spawnPosition = new Vector3(0f, 1.1f, 3.5f);
                Debug.LogWarning($"InventorySetup: House object not found in scene! Spawning key in front of player spawn at: {spawnPosition}");
            }

            // Check if key already exists in scene
            GameObject existingKey = GameObject.Find("HouseKey");
            if (existingKey != null)
            {
                Destroy(existingKey);
            }

            // Spawn key model
            GameObject keyObj = CreateKeyGameObject(spawnPosition);
            
            // Attach ItemPickup
            ItemPickup pickup = keyObj.AddComponent<ItemPickup>();
            pickup.itemData = keyData;
        }

        public static GameObject CreateKeyGameObject(Vector3 position)
        {
            GameObject keyRoot = new GameObject("HouseKey");
            keyRoot.transform.position = position;

            // 1. Shaft (Cylinder)
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(keyRoot.transform);
            shaft.transform.localPosition = new Vector3(0f, 0f, 0f);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Lie flat forward
            shaft.transform.localScale = new Vector3(0.02f, 0.15f, 0.02f); // Long thin cylinder
            Destroy(shaft.GetComponent<Collider>()); // Colliders are only on root

            // 2. Ring/Head (Cylinder)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Head";
            head.transform.SetParent(keyRoot.transform);
            head.transform.localPosition = new Vector3(0f, 0f, -0.15f); // Back end of shaft
            head.transform.localRotation = Quaternion.identity;
            head.transform.localScale = new Vector3(0.08f, 0.015f, 0.08f);
            Destroy(head.GetComponent<Collider>());

            // Cutout effect in key ring
            GameObject cutout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cutout.name = "Cutout";
            cutout.transform.SetParent(head.transform);
            cutout.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            cutout.transform.localScale = new Vector3(0.5f, 1.1f, 0.5f);
            Destroy(cutout.GetComponent<Collider>());
            
            Material cutoutMat = GetOrCreateMaterial("KeyCutoutMaterial", new Color(0.05f, 0.05f, 0.05f));
            cutout.GetComponent<Renderer>().sharedMaterial = cutoutMat;

            // 3. Teeth (Box)
            GameObject teeth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            teeth.name = "Teeth";
            teeth.transform.SetParent(keyRoot.transform);
            teeth.transform.localPosition = new Vector3(0.04f, 0f, 0.1f); // Front end of shaft
            teeth.transform.localRotation = Quaternion.identity;
            teeth.transform.localScale = new Vector3(0.06f, 0.015f, 0.04f);
            Destroy(teeth.GetComponent<Collider>());

            // 4. Gold Material
            Material goldMat = GetOrCreateMaterial("GoldMaterial", new Color(1.0f, 0.82f, 0.18f));
            goldMat.SetFloat("_Metallic", 0.9f);
            goldMat.SetFloat("_Smoothness", 0.8f);
            shaft.GetComponent<Renderer>().sharedMaterial = goldMat;
            head.GetComponent<Renderer>().sharedMaterial = goldMat;
            teeth.GetComponent<Renderer>().sharedMaterial = goldMat;

            // 5. Light Glow (Incandescent orange glow)
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(keyRoot.transform);
            glow.transform.localPosition = Vector3.zero;
            Light lt = glow.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.range = 3f;
            lt.intensity = 1.8f;
            lt.color = new Color(1.0f, 0.85f, 0.35f);
            lt.shadows = LightShadows.None;

            // 6. Box Collider for Player Trigger & Raycast detection
            BoxCollider col = keyRoot.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0f, 0f);
            col.size = new Vector3(0.3f, 0.3f, 0.5f);

            return keyRoot;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            
            Material mat = new Material(litShader);
            mat.name = name;
            mat.color = color;
            
            // Set standard metallic properties if standard shader
            if (litShader.name.Contains("URP") || litShader.name.Contains("Lit"))
            {
                mat.SetFloat("_Metallic", 0.9f);
                mat.SetFloat("_Smoothness", 0.8f);
            }
            return mat;
        }

        private ItemData CreateFlashlightItemData()
        {
            ItemData flashlightData = ScriptableObject.CreateInstance<ItemData>();
            flashlightData.itemName = "Đèn pin";
            flashlightData.icon = CreateFlashlightIconSprite();
            flashlightData.prefab = null; // Viewmodel handled procedurally
            return flashlightData;
        }

        private Sprite CreateFlashlightIconSprite()
        {
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    tex.SetPixel(x, y, Color.clear);

            Color bodyColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            Color headColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            Color lightColor = new Color(1.0f, 0.92f, 0.4f, 0.65f);

            // Draw a clean diagonal flashlight
            for (int i = 0; i < 18; i++)
            {
                int x = 20 + i;
                int y = 20 + i;
                tex.SetPixel(x, y, bodyColor);
                tex.SetPixel(x - 1, y + 1, bodyColor);
                tex.SetPixel(x + 1, y - 1, bodyColor);
            }

            // Flashlight Head
            int hx = 38;
            int hy = 38;
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    if (Mathf.Abs(dx - dy) < 3 && (dx + dy) >= 0 && (dx + dy) <= 6)
                    {
                        tex.SetPixel(hx + dx, hy + dy, headColor);
                    }
                }
            }

            // Light Ray Beam
            for (int i = 0; i < 15; i++)
            {
                int bx = 42 + i;
                int by = 42 + i;
                for (int w = -i / 2; w <= i / 2; w++)
                {
                    if (bx - w >= 0 && bx - w < 64 && by + w >= 0 && by + w < 64)
                    {
                        tex.SetPixel(bx - w, by + w, lightColor);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
        }

        private void SetupHandManager(GameObject player, ItemData flashlightItem)
        {
            Camera mainCam = player.GetComponentInChildren<Camera>();
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            // Find Flashlight GameObject under Camera
            Transform flashlightTrans = mainCam.transform.Find("Flashlight");
            GameObject flashlightGo = flashlightTrans != null ? flashlightTrans.gameObject : null;

            // Create Key ViewModel under Camera if it doesn't exist
            Transform keyViewModelTrans = mainCam.transform.Find("KeyViewModel");
            GameObject keyViewModelGo = null;
            if (keyViewModelTrans != null)
            {
                keyViewModelGo = keyViewModelTrans.gameObject;
            }
            else
            {
                keyViewModelGo = new GameObject("KeyViewModel");
                keyViewModelGo.transform.SetParent(mainCam.transform, false);

                // Build miniature key geometry
                GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "Shaft";
                shaft.transform.SetParent(keyViewModelGo.transform, false);
                shaft.transform.localPosition = new Vector3(0f, 0f, 0f);
                shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shaft.transform.localScale = new Vector3(0.01f, 0.08f, 0.01f);
                DestroyImmediate(shaft.GetComponent<Collider>());

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                head.name = "Head";
                head.transform.SetParent(keyViewModelGo.transform, false);
                head.transform.localPosition = new Vector3(0f, 0f, -0.08f);
                head.transform.localScale = new Vector3(0.04f, 0.008f, 0.04f);
                DestroyImmediate(head.GetComponent<Collider>());

                GameObject teeth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                teeth.name = "Teeth";
                teeth.transform.SetParent(keyViewModelGo.transform, false);
                teeth.transform.localPosition = new Vector3(0.02f, 0f, 0.05f);
                teeth.transform.localScale = new Vector3(0.03f, 0.008f, 0.02f);
                DestroyImmediate(teeth.GetComponent<Collider>());

                Material goldMat = GetOrCreateMaterial("GoldMaterial", new Color(1f, 0.82f, 0.18f));
                shaft.GetComponent<Renderer>().sharedMaterial = goldMat;
                head.GetComponent<Renderer>().sharedMaterial = goldMat;
                teeth.GetComponent<Renderer>().sharedMaterial = goldMat;

                // Position the key viewmodel beautifully on screen
                keyViewModelGo.transform.localPosition = new Vector3(0.2f, -0.2f, 0.35f);
                keyViewModelGo.transform.localRotation = Quaternion.Euler(20f, 60f, -15f);
                keyViewModelGo.SetActive(false); // Inactive by default
            }

            // Ensure PlayerHandManager is attached to Player
            PlayerHandManager handManager = player.GetComponent<PlayerHandManager>();
            if (handManager == null)
            {
                handManager = player.AddComponent<PlayerHandManager>();
            }

            // Find Lamp ViewModel under Camera (it was created at editor-time!)
            Transform lampViewModelTrans = mainCam.transform.Find("LampViewModel");
            GameObject lampViewModelGo = lampViewModelTrans != null ? lampViewModelTrans.gameObject : null;

#if UNITY_EDITOR
            if (lampViewModelGo == null)
            {
                string lampFbx = "Assets/Models/Oil Lamp/Meshy_AI_Vintage_kerosene_lant_0715162024_texture.fbx";
                string lampMatPath = "Assets/Models/Oil Lamp/M_OilLamp.mat";
                GameObject lampPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(lampFbx);
                Material lampMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(lampMatPath);

                if (lampPrefab != null)
                {
                    lampViewModelGo = Instantiate(lampPrefab, mainCam.transform);
                    lampViewModelGo.name = "LampViewModel";
                    
                    // Position the lamp viewmodel beautifully in the hand area
                    lampViewModelGo.transform.localPosition = new Vector3(0.18f, -0.2f, 0.4f);
                    lampViewModelGo.transform.localRotation = Quaternion.Euler(-90f, 180f, 0f);
                    lampViewModelGo.transform.localScale = Vector3.one * 200f;

                    // Apply material
                    foreach (var r in lampViewModelGo.GetComponentsInChildren<MeshRenderer>())
                    {
                        r.sharedMaterial = lampMat;
                    }

                    // Add point/spot light for lighting
                    GameObject lightObj = new GameObject("LightSource");
                    lightObj.transform.SetParent(lampViewModelGo.transform, false);
                    lightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    Light lt = lightObj.AddComponent<Light>();
                    lt.type = LightType.Spot;
                    lt.spotAngle = 110f;
                    lt.innerSpotAngle = 40f;
                    lt.range = 22f;
                    lt.intensity = 2.5f;
                    lt.color = new Color(1.0f, 0.65f, 0.2f);
                    lt.shadows = LightShadows.Soft;

                    // Attach the LampController script
                    LampController lc = lampViewModelGo.GetComponent<LampController>();
                    if (lc == null) lc = lampViewModelGo.AddComponent<LampController>();
                    lc.lampLight = lt;

                    lampViewModelGo.SetActive(false);
                }
            }
#endif

            // Enforce correct scale and position for existing or newly created LampViewModel
            if (lampViewModelGo != null)
            {
                lampViewModelGo.transform.localPosition = new Vector3(0.18f, -0.2f, 0.4f);
                lampViewModelGo.transform.localRotation = Quaternion.Euler(-90f, 180f, 0f);
                lampViewModelGo.transform.localScale = Vector3.one * 200f;
            }

            handManager.flashlightViewModel = flashlightGo;
            handManager.keyViewModel = keyViewModelGo;
            handManager.lampViewModel = lampViewModelGo;

            // Force update on start
            handManager.RefreshHandItems();
        }

        private ItemData CreateLampItemData()
        {
            ItemData lampData = ScriptableObject.CreateInstance<ItemData>();
            lampData.itemName = "Đèn dầu";
            lampData.icon = CreateLampIconSprite();
            lampData.prefab = null; // Handled procedurally in HandManager
            return lampData;
        }

        private Sprite CreateLampIconSprite()
        {
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    tex.SetPixel(x, y, Color.clear);

            Color metalColor = new Color(0.6f, 0.4f, 0.2f, 1f);
            Color glassColor = new Color(0.8f, 0.9f, 1.0f, 0.5f);
            Color flameColor = new Color(1.0f, 0.6f, 0.1f, 1f);

            // Base (metal)
            for (int y = 14; y <= 18; y++)
                for (int x = 24; x <= 40; x++)
                    tex.SetPixel(x, y, metalColor);

            // Glass chimney
            for (int y = 19; y <= 35; y++)
            {
                int width = 8 - (int)(Mathf.Abs(y - 27) * 0.3f);
                for (int x = 32 - width; x <= 32 + width; x++)
                {
                    tex.SetPixel(x, y, glassColor);
                }
            }

            // Flame
            for (int y = 22; y <= 28; y++)
            {
                int width = 2 - (int)((y - 22) * 0.3f);
                for (int x = 32 - width; x <= 32 + width; x++)
                {
                    tex.SetPixel(x, y, flameColor);
                }
            }

            // Cap and handle (metal)
            for (int y = 36; y <= 40; y++)
                for (int x = 26; x <= 38; x++)
                    tex.SetPixel(x, y, metalColor);

            // Ring handle at top
            int cx = 32;
            int cy = 46;
            int r = 6;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= r - 1.5f && dist <= r + 0.5f)
                    {
                        tex.SetPixel(cx + dx, cy + dy, metalColor);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
        }

        private void SpawnHouseLamp(ItemData lampData)
        {
            GameObject lampObj = GameObject.Find("HouseLamp");
            if (lampObj != null)
            {
                // Attach ItemPickup
                ItemPickup pickup = lampObj.GetComponent<ItemPickup>();
                if (pickup == null) pickup = lampObj.AddComponent<ItemPickup>();
                pickup.itemData = lampData;

                // Make sure collider is trigger so we can walk-over or press E
                Collider col = lampObj.GetComponent<Collider>();
                if (col != null)
                {
                    col.isTrigger = true;
                }
            }
            else
            {
                Debug.LogWarning("InventorySetup: HouseLamp GameObject not found in scene at runtime! Make sure Setup First Person Scene menu has been run.");
            }
        }
    }
}
