using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.AI;
using Antigravity;

namespace Antigravity.Editor
{
    [InitializeOnLoad]
    public static class SetupFirstPersonScene
    {
        private const float HouseScaleVal = 1.1f;

        static SetupFirstPersonScene()
        {
            // Delay the call to make sure the editor and scene are fully loaded
            EditorApplication.delayCall += AutoSetup;
            EditorApplication.delayCall += AutoSetupTemple;
            EditorApplication.delayCall += AutoSetupSchool;
            EditorApplication.delayCall += AutoSetupBus;
            EditorApplication.delayCall += AutoSetupPicture;
        }

        [MenuItem("Tools/Antigravity/Setup First Person Scene")]
        public static void RunManualSetup()
        {
            Setup(force: true);
        }

        private static void AutoSetup()
        {
            EditorApplication.delayCall -= AutoSetup;
            Setup(force: false);
        }

        private static void Setup(bool force)
        {
            // Check if player, terrain, grass field, forest, house, and shadow figure already exist
            GameObject player = GameObject.Find("Player");
            GameObject terrain = GameObject.Find("ProceduralTerrain");
            GameObject grass = GameObject.Find("GrassField");
            GameObject forest = GameObject.Find("Forest");
            GameObject house = GameObject.Find("House");
            GameObject shadowFigure = GameObject.Find("ShadowFigure");
            GameObject fence = GameObject.Find("ForestFence");
            GameObject campsite = GameObject.Find("Campsite");
            GameObject monster = GameObject.Find("MonsterEnemy");
            
            // Check if the existing house is the old primitive cube house
            bool isPrimitiveHouse = house != null && house.transform.Find("Walls") != null;

            // Check if the existing house has correct scale
            bool houseScaledCorrectly = house != null && Mathf.Approximately(house.transform.localScale.x, HouseScaleVal);

            // Check if the terrain material is missing the realistic ground texture
            bool needsTerrainTexture = false;
            if (terrain != null)
            {
                Renderer terrainRenderer = terrain.GetComponent<Renderer>();
                if (terrainRenderer != null && terrainRenderer.sharedMaterial != null)
                {
                    needsTerrainTexture = terrainRenderer.sharedMaterial.GetTexture("_BaseMap") == null;
                }
            }

            if (player != null && terrain != null && grass != null && forest != null && house != null && shadowFigure != null && fence != null && campsite != null && monster == null && !isPrimitiveHouse && houseScaledCorrectly && !needsTerrainTexture && !force)
            {
                // Already setup with prefabs and textures, skip auto setup
                return;
            }

            Debug.Log("Antigravity: Starting scene setup with mountains, terrain, grass, house, and forest...");

            // Ensure Materials folder exists
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }

            // 1. Create Procedural Mountain Terrain
            CreateProceduralTerrain();

            // 1b. Create Grass Field
            CreateGrassField();

            // 1c. Create Stylized 3D House
            CreateHouse();
            house = GameObject.Find("House");

            // 1d. Create Forest Fence Perimeter with 1 Entrance Opening
            CreateForestFence();

            // 1e. Create Procedural Forest
            CreateForest();

            // 2. Cleanup old primitive obstacles if they exist (replacing with realistic horror props)
            string[] oldObstacles = new[] { "Obstacle_Red", "Obstacle_Blue", "Obstacle_Yellow", "Obstacle_Orange" };
            foreach (var obsName in oldObstacles)
            {
                GameObject obs = GameObject.Find(obsName);
                if (obs != null) Object.DestroyImmediate(obs);
            }

            // 3. Create Player (Spawns right in front of the forest fence entrance looking down the main trail)
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
            }
            float spawnY = GetTerrainHeight(0f, -108f) + 1.2f;
            player.transform.position = new Vector3(0f, spawnY, -108f);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Face North into the forest entrance opening
            player.transform.localScale = Vector3.one;

            // Add or configure CharacterController
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = player.AddComponent<CharacterController>();
            }
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = Vector3.zero;

            // Add or configure FirstPersonController
            FirstPersonController fpc = player.GetComponent<FirstPersonController>();
            if (fpc == null)
            {
                fpc = player.AddComponent<FirstPersonController>();
            }

            // Remove Capsule Collider because CharacterController already handles collision
            CapsuleCollider capCol = player.GetComponent<CapsuleCollider>();
            if (capCol != null)
            {
                Object.DestroyImmediate(capCol);
            }

            MeshRenderer playerRenderer = player.GetComponent<MeshRenderer>();
            if (playerRenderer != null)
            {
                playerRenderer.enabled = false; // Hide debug capsule body in game
            }

            // 4. Setup Camera
            GameObject mainCam = GameObject.FindWithTag("MainCamera");
            if (mainCam != null)
            {
                mainCam.transform.SetParent(player.transform);
                mainCam.transform.localPosition = new Vector3(0, 0.7f, 0); // eye height (1.6m from floor)
                mainCam.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogWarning("Antigravity Setup: Could not find GameObject with tag 'MainCamera' to parent to Player.");
            }

            // 4b. Setup Player Flashlight
            if (mainCam != null)
            {
                Transform flashlightTrans = mainCam.transform.Find("Flashlight");
                GameObject flashlight = null;
                if (flashlightTrans != null)
                {
                    flashlight = flashlightTrans.gameObject;
                }
                else
                {
                    flashlight = new GameObject("Flashlight");
                    flashlight.transform.SetParent(mainCam.transform);
                }
                
                flashlight.transform.localPosition = new Vector3(0.2f, -0.22f, 0.35f); // Position offset to bottom-right viewmodel
                flashlight.transform.localRotation = Quaternion.identity;

                // Create physical model representing the flashlight on hand
                Transform bodyTrans = flashlight.transform.Find("Body");
                if (bodyTrans != null)
                {
                    Object.DestroyImmediate(bodyTrans.gameObject);
                }
                
                GameObject body = new GameObject("Body");
                body.transform.SetParent(flashlight.transform);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Point forward
                body.transform.localScale = Vector3.one;

                // Handle (Cylinder)
                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                handle.name = "Handle";
                handle.transform.SetParent(body.transform);
                handle.transform.localPosition = new Vector3(0f, 0f, 0f);
                handle.transform.localScale = new Vector3(0.025f, 0.1f, 0.025f);
                Object.DestroyImmediate(handle.GetComponent<Collider>());
                
                // Head (Cylinder)
                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                head.name = "Head";
                head.transform.SetParent(body.transform);
                head.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                head.transform.localScale = new Vector3(0.045f, 0.03f, 0.045f);
                Object.DestroyImmediate(head.GetComponent<Collider>());

                // Apply a metallic dark material to the flashlight body
                Material flashMat = GetOrCreateMaterial("FlashlightMaterial", new Color(0.12f, 0.12f, 0.12f));
                flashMat.SetFloat("_Smoothness", 0.75f);
                flashMat.SetFloat("_Metallic", 0.85f);
                
                handle.GetComponent<Renderer>().sharedMaterial = flashMat;
                head.GetComponent<Renderer>().sharedMaterial = flashMat;

                Light flashLightComp = flashlight.GetComponent<Light>();
                if (flashLightComp == null) flashLightComp = flashlight.AddComponent<Light>();
                flashLightComp.type = LightType.Spot;
                flashLightComp.range = 28f;
                flashLightComp.spotAngle = 40f;
                flashLightComp.innerSpotAngle = 25f;
                flashLightComp.intensity = 2.2f;
                flashLightComp.color = new Color(0.98f, 0.96f, 0.85f); // Warm incandescent flashlight bulb
                flashLightComp.shadows = LightShadows.Soft;

                // Attach flashlight controller
                FlashlightController fc = flashlight.GetComponent<FlashlightController>();
                if (fc == null) flashlight.AddComponent<FlashlightController>();
            }

            // 4bb. Setup Player Lamp ViewModel
            if (mainCam != null)
            {
                SetupPlayerLampViewModel(mainCam);
            }

            // 4c. Setup Horror Scene environment, fog, moonlight and scattered props
            CreateHorrorScene(house, player);

            // 4d. Remove Monster MN D as requested
            RemoveMonster();

            // 4e. Build Campsite with 3 Tents and Campfire near Lake
            CreateCampsite();

            // Setup Vase and Oil Lamp configuration
            SetupHouseVase(house);
            ConfigureOilLampMaterial();

            // Save active scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Antigravity: First-Person Scene Setup Completed Successfully!");
        }

        private static void CreateProceduralTerrain()
        {
            // Delete old flat ground if it exists
            GameObject oldGround = GameObject.Find("Ground");
            if (oldGround != null)
            {
                Object.DestroyImmediate(oldGround);
            }

            GameObject terrain = GameObject.Find("ProceduralTerrain");
            if (terrain == null)
            {
                terrain = new GameObject("ProceduralTerrain");
            }
            terrain.transform.position = Vector3.zero;
            terrain.transform.rotation = Quaternion.identity;
            terrain.transform.localScale = Vector3.one;

            MeshFilter meshFilter = terrain.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = terrain.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer = terrain.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = terrain.AddComponent<MeshRenderer>();

            MeshCollider meshCollider = terrain.GetComponent<MeshCollider>();
            if (meshCollider == null) meshCollider = terrain.AddComponent<MeshCollider>();

            // Generate procedural terrain mesh
            Mesh mesh = new Mesh();
            mesh.name = "Procedural Mountain Terrain";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Allow more than 65k vertices (500x500 grid)

            int width = 500;
            int depth = 500;
            float spacing = 2.5f; // Total size = 1250x1250
            Vector3[] vertices = new Vector3[(width + 1) * (depth + 1)];
            int[] triangles = new int[width * depth * 6];
            Vector2[] uvs = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];

            for (int z = 0; z <= depth; z++)
            {
                for (int x = 0; x <= width; x++)
                {
                    int idx = z * (width + 1) + x;
                    float xPos = (x - width / 2f) * spacing;
                    float zPos = (z - depth / 2f) * spacing;

                    float height = GetTerrainHeight(xPos, zPos);

                    vertices[idx] = new Vector3(xPos, height, zPos);
                    uvs[idx] = new Vector2((float)x / width, (float)z / depth);

                    // Calculate vertex weights for texture blending (R = Dirt Path, G = Rock Cliff)
                    float pathDist = GetDistanceToPathNetwork(xPos, zPos, out float pathWidth);
                    float dirtWeight = 0f;
                    float halfWidth = pathWidth * 0.5f;
                    if (pathDist <= halfWidth)
                    {
                        dirtWeight = 1.0f;
                    }
                    else if (pathDist <= (halfWidth + 1.6f))
                    {
                        float norm = (pathDist - halfWidth) / 1.6f;
                        float edgeNoise = (Mathf.PerlinNoise(xPos * 0.15f + 17f, zPos * 0.15f + 17f) - 0.5f) * 0.35f;
                        norm = Mathf.Clamp01(norm + edgeNoise);
                        dirtWeight = Mathf.SmoothStep(1.0f, 0.0f, norm);
                    }

                    // Rock weight on steep hillsides/mountains
                    float rockWeight = 0f;
                    if (height > 18f)
                    {
                        rockWeight = Mathf.Clamp01((height - 18f) / 25f);
                    }

                    colors[idx] = new Color(dirtWeight, rockWeight, 0f, 1f);
                }
            }

            int triIdx = 0;
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int row1 = z * (width + 1) + x;
                    int row2 = (z + 1) * (width + 1) + x;

                    triangles[triIdx++] = row1;
                    triangles[triIdx++] = row2;
                    triangles[triIdx++] = row1 + 1;

                    triangles[triIdx++] = row1 + 1;
                    triangles[triIdx++] = row2;
                    triangles[triIdx++] = row2 + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = null;
            meshFilter.sharedMesh = mesh;

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;

            Material groundMat = GetOrCreateMaterial("GroundMaterial", new Color(1f, 1f, 1f));

            Shader blendShader = Shader.Find("Antigravity/ForestTerrainBlend");
            if (blendShader == null) blendShader = Shader.Find("Universal Render Pipeline/Lit");
            if (blendShader == null) blendShader = Shader.Find("Standard");
            groundMat.shader = blendShader;

            // Load PBR ground textures from ADG_Textures package
            Texture2D grassDiff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/ADG_Textures/ground_vol1/ground6/ground6_Diffuse.tga");
            Texture2D grassNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/ADG_Textures/ground_vol1/ground6/ground6_Normal.tga");

            Texture2D dirtDiff  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/ADG_Textures/ground_vol1/ground1/ground1_Diffuse.tga");
            Texture2D dirtNorm  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/ADG_Textures/ground_vol1/ground1/ground1_Normal.tga");

            Texture2D rockDiff  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/ADG_Textures/ground_vol1/ground5/ground5_Diffuse.tga");
            Texture2D rockNorm  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/ADG_Textures/ground_vol1/ground5/ground5_Normal.tga");

            if (groundMat.HasProperty("_GrassTex") && grassDiff != null) groundMat.SetTexture("_GrassTex", grassDiff);
            if (groundMat.HasProperty("_GrassNormal") && grassNorm != null) groundMat.SetTexture("_GrassNormal", grassNorm);

            if (groundMat.HasProperty("_DirtTex") && dirtDiff != null) groundMat.SetTexture("_DirtTex", dirtDiff);
            if (groundMat.HasProperty("_DirtNormal") && dirtNorm != null) groundMat.SetTexture("_DirtNormal", dirtNorm);

            if (groundMat.HasProperty("_RockTex") && rockDiff != null) groundMat.SetTexture("_RockTex", rockDiff);
            if (groundMat.HasProperty("_RockNormal") && rockNorm != null) groundMat.SetTexture("_RockNormal", rockNorm);

            if (groundMat.HasProperty("_Tiling")) groundMat.SetFloat("_Tiling", 140.0f);
            if (groundMat.HasProperty("_Smoothness")) groundMat.SetFloat("_Smoothness", 0.15f);

            // Fallback bindings if URP Lit is used
            if (groundMat.HasProperty("_BaseMap") && grassDiff != null) groundMat.SetTexture("_BaseMap", grassDiff);
            if (groundMat.HasProperty("_BumpMap") && grassNorm != null) groundMat.SetTexture("_BumpMap", grassNorm);

            EditorUtility.SetDirty(groundMat);
            AssetDatabase.SaveAssets();

            meshRenderer.sharedMaterial = groundMat;
        }

        private static float GetTerrainHeight(float xPos, float zPos)
        {
            float height = 0f;

            // Lake centered at (70, -70) with radius 80m
            float ldx = xPos - 70f;
            float ldz = zPos - (-70f);
            float distToLake = Mathf.Sqrt(ldx * ldx + ldz * ldz);
            if (distToLake < 80f)
            {
                float lakeDepressionFactor = Mathf.SmoothStep(0f, 1f, distToLake / 80f);
                height = Mathf.Lerp(-6.0f, 0f, lakeDepressionFactor);
            }

            // Forest floor rolling hills and bumpy terrain
            float hillNoise1 = Mathf.PerlinNoise(xPos * 0.007f + 42f, zPos * 0.007f + 42f) * 6.5f;
            float hillNoise2 = Mathf.PerlinNoise(xPos * 0.022f + 142f, zPos * 0.022f + 142f) * 3.0f;
            float bumpNoise  = Mathf.PerlinNoise(xPos * 0.07f + 242f, zPos * 0.07f + 242f) * 0.8f;
            
            // Flattening mask around key structures so buildings sit flat
            float flatMask = 1.0f;
            float distToHouse = Vector2.Distance(new Vector2(xPos, zPos), new Vector2(65f, 30f));
            if (distToHouse < 20f) flatMask *= Mathf.SmoothStep(0f, 1f, (distToHouse - 8f) / 12f);

            float distToEnt = Vector2.Distance(new Vector2(xPos, zPos), new Vector2(0f, -108f));
            if (distToEnt < 18f) flatMask *= Mathf.SmoothStep(0f, 1f, (distToEnt - 6f) / 12f);

            float distToCamp = Vector2.Distance(new Vector2(xPos, zPos), new Vector2(-20f, 22f));
            if (distToCamp < 18f) flatMask *= Mathf.SmoothStep(0f, 1f, (distToCamp - 6f) / 12f);

            float distToSchool = Vector2.Distance(new Vector2(xPos, zPos), new Vector2(-200f, 40f));
            if (distToSchool < 35f) flatMask *= Mathf.SmoothStep(0f, 1f, (distToSchool - 15f) / 20f);

            height += (hillNoise1 + hillNoise2 + bumpNoise) * flatMask;

            // Path sunken rut depression (Winding dirt path profile)
            float pathDist = GetDistanceToPathNetwork(xPos, zPos, out float pathWidth);
            if (pathDist < (pathWidth * 1.3f))
            {
                float normDist = pathDist / (pathWidth * 0.5f);
                if (normDist <= 1.0f)
                {
                    // Sunken center rut (-0.32m deep) with path surface roughness
                    float rutDip = (1.0f - Mathf.SmoothStep(0f, 1.0f, normDist)) * -0.32f;
                    float pathRoughness = (Mathf.PerlinNoise(xPos * 0.18f + 9f, zPos * 0.18f + 9f) - 0.5f) * 0.14f;
                    height += (rutDip + pathRoughness);
                }
                else if (normDist <= 1.3f)
                {
                    // Raised soil rim (+0.08m) at dirt trail edges
                    float rim = Mathf.Sin((normDist - 1.0f) / 0.3f * Mathf.PI) * 0.08f;
                    height += rim;
                }
            }

            // High Mountains surrounding the valley
            float xMin = -300f;
            float xMax = 300f;
            float zMin = -180f;
            float zMax = 350f;

            float distX = 0f;
            if (xPos > xMax) distX = xPos - xMax;
            else if (xPos < xMin) distX = xMin - xPos;

            float distZ = 0f;
            if (zPos > zMax) distZ = zPos - zMax;
            else if (zPos < zMin) distZ = zMin - zPos;

            float distToValley = Mathf.Sqrt(distX * distX + distZ * distZ);
            if (distToValley > 0f)
            {
                float t = distToValley / 180f;
                t = Mathf.Clamp01(t);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                float noise1 = Mathf.PerlinNoise(xPos * 0.008f, zPos * 0.008f);
                float noise2 = Mathf.PerlinNoise(xPos * 0.02f + 100f, zPos * 0.02f + 100f);
                float noise3 = Mathf.PerlinNoise(xPos * 0.05f + 200f, zPos * 0.05f + 200f);

                float mountainHeight = smoothT * (90f + noise1 * 70f + noise2 * 25f + noise3 * 5f);
                height = Mathf.Max(height, mountainHeight);
            }

            return height;
        }

        private static void CreateGrassField()
        {
            GameObject grassFolder = GameObject.Find("GrassField");
            if (grassFolder != null)
            {
                Object.DestroyImmediate(grassFolder);
            }
            grassFolder = new GameObject("GrassField");

            // Ensure Textures folder exists
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Textures"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Textures");
            }

            // 1. Generate Tall Grass Blade Texture
            string grassTexPath = "Assets/_Project/Textures/GrassBladeTexture.png";
            if (!System.IO.File.Exists(grassTexPath))
            {
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 128; x++)
                        tex.SetPixel(x, y, Color.clear);

                for (int y = 0; y < 128; y++)
                {
                    float factor = (float)y / 128f;
                    DrawRichBlade(tex, 64, y, factor, 14f, 0.15f, false);  // Center main blade
                    DrawRichBlade(tex, 36, y, factor, 10f, -0.35f, false); // Left leaning blade
                    DrawRichBlade(tex, 92, y, factor, 11f, 0.32f, false);  // Right leaning blade
                    DrawRichBlade(tex, 20, y, factor, 8f, -0.5f, false);   // Far left blade
                    DrawRichBlade(tex, 108, y, factor, 8f, 0.45f, false);  // Far right blade
                }
                tex.Apply();
                System.IO.File.WriteAllBytes(grassTexPath, tex.EncodeToPNG());
                AssetDatabase.Refresh();
            }

            // 2. Generate White Flower Meadow Texture
            string flowerTexPath = "Assets/_Project/Textures/FlowerTexture.png";
            if (!System.IO.File.Exists(flowerTexPath))
            {
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 128; x++)
                        tex.SetPixel(x, y, Color.clear);

                for (int y = 0; y < 128; y++)
                {
                    float factor = (float)y / 128f;
                    DrawRichBlade(tex, 64, y, factor, 12f, 0.1f, true);   // Center blade with white flower head
                    DrawRichBlade(tex, 32, y, factor, 9f, -0.3f, true);  // Left blade with white flower head
                    DrawRichBlade(tex, 96, y, factor, 10f, 0.25f, true); // Right blade with white flower head
                }
                tex.Apply();
                System.IO.File.WriteAllBytes(flowerTexPath, tex.EncodeToPNG());
                AssetDatabase.Refresh();
            }

            Shader windShader = Shader.Find("Antigravity/GrassWindShader");
            if (windShader == null) windShader = Shader.Find("Universal Render Pipeline/Lit");
            if (windShader == null) windShader = Shader.Find("Standard");

            // Grass Material
            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GrassMaterial.mat");
            if (grassMat == null || grassMat.shader != windShader)
            {
                grassMat = new Material(windShader);
                Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(grassTexPath);
                if (grassMat.HasProperty("_BaseMap")) grassMat.SetTexture("_BaseMap", grassTex);
                if (grassMat.HasProperty("_Cutoff")) grassMat.SetFloat("_Cutoff", 0.3f);
                AssetDatabase.CreateAsset(grassMat, "Assets/_Project/Materials/GrassMaterial.mat");
            }

            // Flower Material
            Material flowerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/FlowerMaterial.mat");
            if (flowerMat == null || flowerMat.shader != windShader)
            {
                flowerMat = new Material(windShader);
                Texture2D flowerTex = AssetDatabase.LoadAssetAtPath<Texture2D>(flowerTexPath);
                if (flowerMat.HasProperty("_BaseMap")) flowerMat.SetTexture("_BaseMap", flowerTex);
                if (flowerMat.HasProperty("_Cutoff")) flowerMat.SetFloat("_Cutoff", 0.3f);
                AssetDatabase.CreateAsset(flowerMat, "Assets/_Project/Materials/FlowerMaterial.mat");
            }

            // Mesh Quad template
            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Mesh quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempQuad);

            int grassCount = 14000;
            float fieldSize = 900f;
            float houseScale = HouseScaleVal;

            for (int i = 0; i < grassCount; i++)
            {
                float x = Random.Range(-fieldSize / 2f, fieldSize / 2f);
                float z = Random.Range(-fieldSize / 2f, fieldSize / 2f);

                float distToCenter = Mathf.Sqrt(x * x + z * z);
                if (distToCenter < 5f) continue;

                // House clearance
                if (Mathf.Abs(x - 65f) < 16f && Mathf.Abs(z - 30f) < 16f) continue;

                // Campsite clearance
                float distToCamp = Mathf.Sqrt((x - (-20f)) * (x - (-20f)) + (z - 22f) * (z - 22f));
                if (distToCamp < 5f) continue;

                // Lake clearance
                float distToLake = Mathf.Sqrt((x - 70f) * (x - 70f) + (z - (-70f)) * (z - (-70f)));
                if (distToLake < 78f) continue;

                // Temple clearance
                float distToTemple = Mathf.Sqrt(x * x + (z - 250f) * (z - 250f));
                if (distToTemple < 38f) continue;

                // School clearance
                float distToSchool = Mathf.Sqrt((x - (-200f)) * (x - (-200f)) + (z - 40f) * (z - 40f));
                if (distToSchool < 42f) continue;

                // Path clearance
                if (IsNearAnyPath(x, z, 0.4f)) continue;

                float terrainHeight = GetTerrainHeight(x, z);

                GameObject grassCluster = new GameObject("Grass_" + i);
                grassCluster.transform.SetParent(grassFolder.transform);
                grassCluster.transform.position = new Vector3(x, terrainHeight, z);
                
                float scale = Random.Range(1.3f, 2.3f);
                grassCluster.transform.localScale = new Vector3(scale, scale, scale);

                Material activeMat = (Random.value < 0.35f) ? flowerMat : grassMat;

                // 3-star quads for dense 3D volume from any angle (0, 60, 120 degrees)
                float baseRot = Random.Range(0, 180);
                for (int q = 0; q < 3; q++)
                {
                    GameObject quadObj = new GameObject("Q" + (q + 1));
                    quadObj.transform.SetParent(grassCluster.transform);
                    quadObj.transform.localPosition = new Vector3(0, 0.5f, 0);
                    quadObj.transform.localRotation = Quaternion.Euler(0, baseRot + q * 60, 0);
                    quadObj.transform.localScale = Vector3.one;

                    MeshFilter mf = quadObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = quadMesh;
                    MeshRenderer mr = quadObj.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = activeMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private static void DrawRichBlade(Texture2D tex, int baseX, int y, float factor, float startWidth, float lean, bool addFlowers)
        {
            if (y > 115) return;
            
            float width = Mathf.Lerp(startWidth, 0.8f, factor);
            float currentX = baseX + (y * lean);
            
            int startX = Mathf.RoundToInt(currentX - width / 2f);
            int endX = Mathf.RoundToInt(currentX + width / 2f);

            // Rich green gradient: dark root -> vibrant stalk -> bright tip
            Color darkRoot = new Color(0.08f, 0.28f, 0.08f, 1f);
            Color midStalk = new Color(0.22f, 0.62f, 0.15f, 1f);
            Color brightTip = new Color(0.42f, 0.82f, 0.20f, 1f);

            Color grassColor = factor < 0.5f 
                ? Color.Lerp(darkRoot, midStalk, factor * 2f) 
                : Color.Lerp(midStalk, brightTip, (factor - 0.5f) * 2f);

            for (int x = startX; x <= endX; x++)
            {
                if (x >= 0 && x < 128)
                {
                    tex.SetPixel(x, y, grassColor);
                }
            }

            // Draw delicate white flower heads near the tip of blades
            if (addFlowers && y > 85 && y < 105)
            {
                int flowerRadius = 4;
                int fx = Mathf.RoundToInt(currentX);
                int fy = y;

                for (int dy = -flowerRadius; dy <= flowerRadius; dy++)
                {
                    for (int dx = -flowerRadius; dx <= flowerRadius; dx++)
                    {
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d <= flowerRadius)
                        {
                            int px = fx + dx;
                            int py = fy + dy;
                            if (px >= 0 && px < 128 && py >= 0 && py < 128)
                            {
                                Color col = (d < 1.5f) ? new Color(1.0f, 0.88f, 0.1f, 1.0f) : new Color(0.98f, 0.98f, 0.98f, 1.0f);
                                tex.SetPixel(px, py, col);
                            }
                        }
                    }
                }
            }
        }

        private static void CreateHouse()
        {
            GameObject house = GameObject.Find("House");
            if (house != null)
            {
                Object.DestroyImmediate(house);
            }

            string housePrefabPath = "Assets/ThirdParty/ALP_Assets/country house01/Prefabs/House_Prefab.prefab";
            GameObject housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(housePrefabPath);
            if (housePrefab != null)
            {
                house = PrefabUtility.InstantiatePrefab(housePrefab) as GameObject;
                house.name = "House";
                
                // Align house with terrain height (place it along the east trail branch at X = 65, Z = 30)
                float terrainHeight = GetTerrainHeight(65f, 30f);
                house.transform.position = new Vector3(65f, terrainHeight, 30f);
                house.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Rotate to face West towards trail intersection
                house.transform.localScale = new Vector3(HouseScaleVal, HouseScaleVal, HouseScaleVal);
                
                DecorateHouse(house);
            }
            else
            {
                // Fallback to primitive stylized house if prefab is missing
                Debug.LogWarning("Antigravity: Could not load House Prefab, falling back to primitives.");
                
                house = new GameObject("House");
                house.transform.position = new Vector3(65f, GetTerrainHeight(65f, 30f), 30f);
                house.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                house.transform.localScale = new Vector3(HouseScaleVal, HouseScaleVal, HouseScaleVal);

                Material wallMat = GetOrCreateMaterial("HouseWallMaterial", new Color(0.92f, 0.88f, 0.8f));
                Material roofMat = GetOrCreateMaterial("HouseRoofMaterial", new Color(0.8f, 0.25f, 0.2f));
                Material doorMat = GetOrCreateMaterial("HouseDoorMaterial", new Color(0.35f, 0.2f, 0.08f));
                Material windowMat = GetOrCreateMaterial("HouseWindowMaterial", new Color(0.5f, 0.8f, 0.9f));
                Material chimneyMat = GetOrCreateMaterial("HouseChimneyMaterial", new Color(0.3f, 0.3f, 0.3f));

                GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
                walls.name = "Walls";
                walls.transform.SetParent(house.transform);
                walls.transform.localPosition = new Vector3(0f, 2f, 0f);
                walls.transform.localScale = new Vector3(6f, 4f, 6f);
                walls.GetComponent<Renderer>().sharedMaterial = wallMat;

                GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.name = "Roof";
                roof.transform.SetParent(house.transform);
                roof.transform.localPosition = new Vector3(0f, 4.5f, 0f);
                roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                roof.transform.localScale = new Vector3(4.5f, 4.5f, 6.2f);
                roof.GetComponent<Renderer>().sharedMaterial = roofMat;

                GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = "Door";
                door.transform.SetParent(house.transform);
                door.transform.localPosition = new Vector3(0f, 1f, -3.05f);
                door.transform.localScale = new Vector3(1.2f, 2f, 0.1f);
                door.GetComponent<Renderer>().sharedMaterial = doorMat;

                GameObject winFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
                winFront.name = "WindowFront";
                winFront.transform.SetParent(house.transform);
                winFront.transform.localPosition = new Vector3(1.8f, 2.2f, -3.05f);
                winFront.transform.localScale = new Vector3(1.2f, 1.2f, 0.1f);
                winFront.GetComponent<Renderer>().sharedMaterial = windowMat;

                GameObject winSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
                winSide.name = "WindowSide";
                winSide.transform.SetParent(house.transform);
                winSide.transform.localPosition = new Vector3(-3.05f, 2.2f, 0f);
                winSide.transform.localScale = new Vector3(0.1f, 1.2f, 1.8f);
                winSide.GetComponent<Renderer>().sharedMaterial = windowMat;

                GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chimney.name = "Chimney";
                chimney.transform.SetParent(house.transform);
                chimney.transform.localPosition = new Vector3(1.5f, 4.2f, 1.5f);
                chimney.transform.localScale = new Vector3(0.6f, 2.2f, 0.6f);
                chimney.GetComponent<Renderer>().sharedMaterial = chimneyMat;
            }
        }

        private static void CreateForest()
        {
            GameObject forest = GameObject.Find("Forest");
            if (forest != null)
            {
                Object.DestroyImmediate(forest);
            }
            forest = new GameObject("Forest");
            forest.transform.position = Vector3.zero;
            forest.transform.rotation = Quaternion.identity;
            forest.transform.localScale = Vector3.one;

            // Load high quality Conifer Pine Tree prefabs from BOTD package
            string[] pinePaths = new string[]
            {
                "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Tall BOTD URP.prefab",
                "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Medium BOTD URP.prefab",
                "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Small BOTD URP.prefab",
                "Assets/ThirdParty/ALP_Assets/Big Oak Tree FREE/Prefabs/OakBigTree01_pr.prefab"
            };

            System.Collections.Generic.List<GameObject> pinePrefabs = new System.Collections.Generic.List<GameObject>();
            foreach (var p in pinePaths)
            {
                GameObject pObj = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pObj != null) pinePrefabs.Add(pObj);
            }

            // Fallback Oak Tree if pine prefabs missing
            GameObject fallbackOak = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/ALP_Assets/Big Oak Tree FREE/Prefabs/OakBigTree01_pr.prefab");

            if (pinePrefabs.Count > 0)
            {
                int treeCount = 1300; // Dense realistic pine forest
                float fieldSize = 900f;

                for (int i = 0; i < treeCount; i++)
                {
                    float x = Random.Range(-fieldSize / 2f, fieldSize / 2f);
                    float z = Random.Range(-fieldSize / 2f, fieldSize / 2f);

                    // 1. Player Spawn & Fence Entrance Clearance
                    float distToPlayer = Vector2.Distance(new Vector2(x, z), new Vector2(0f, -108f));
                    if (distToPlayer < 20f) continue;
                    if (Mathf.Abs(x) < 8f && z < -90f) continue;

                    // 2. Outer Fence Boundary Clearance
                    if (x < -180f || x > 180f || z < -100f || z > 258f) continue;

                    // 3. House Clearance (House at 65, 30)
                    float distToHouse = Vector2.Distance(new Vector2(x, z), new Vector2(65f, 30f));
                    if (distToHouse < 25f) continue;

                    // 4. Campsite Clearance (Campsite at -20, 22)
                    float distToCamp = Vector2.Distance(new Vector2(x, z), new Vector2(-20f, 22f));
                    if (distToCamp < 25f) continue;

                    // 5. Lake Basin Clearance (Lake at 70, -70)
                    float distToLake = Vector2.Distance(new Vector2(x, z), new Vector2(70f, -70f));
                    if (distToLake < 82f) continue;

                    // 6. Ancient Temple Clearance (Temple at 0, 250)
                    float distToTemple = Vector2.Distance(new Vector2(x, z), new Vector2(0f, 250f));
                    if (distToTemple < 50f) continue;

                    // 7. Abandoned School Clearance (School at -200, 40)
                    float distToSchool = Vector2.Distance(new Vector2(x, z), new Vector2(-200f, 40f));
                    if (distToSchool < 55f) continue;

                    // 8. Crossroads / Streetlight Clearance (0, 30)
                    float distToIntersection = Vector2.Distance(new Vector2(x, z), new Vector2(0f, 30f));
                    if (distToIntersection < 18f) continue;

                    // 9. Dirt Trails / Paths Clearance
                    if (IsNearAnyPath(x, z, 3.2f)) continue;

                    float terrainHeight = GetTerrainHeight(x, z);

                    GameObject chosenPrefab = pinePrefabs[Random.Range(0, pinePrefabs.Count)];
                    GameObject tree = PrefabUtility.InstantiatePrefab(chosenPrefab) as GameObject;
                    tree.name = "Tree_" + i;
                    tree.transform.SetParent(forest.transform);
                    tree.transform.position = new Vector3(x, terrainHeight, z);

                    float scale = Random.Range(0.85f, 1.45f);
                    tree.transform.localScale = new Vector3(scale, scale, scale);
                    tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }

                // Place Special Tree behind School with key hidden under roots (per Kịch bản.docx)
                float schoolTreeX = -205f;
                float schoolTreeZ = 40f;
                float schoolTreeY = GetTerrainHeight(schoolTreeX, schoolTreeZ);
                GameObject chosenSpecialPrefab = pinePrefabs[0]; // Tall Conifer Pine Tree
                GameObject specialTree = PrefabUtility.InstantiatePrefab(chosenSpecialPrefab) as GameObject;
                specialTree.name = "SpecialLeafTree_BehindSchool";
                specialTree.transform.SetParent(forest.transform);
                specialTree.transform.position = new Vector3(schoolTreeX, schoolTreeY, schoolTreeZ);
                specialTree.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

                // Add Key item under roots of special tree
                GameObject keyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                keyObj.name = "SchoolKey_Item";
                keyObj.transform.SetParent(specialTree.transform);
                keyObj.transform.localPosition = new Vector3(0.3f, 0.15f, 0.4f);
                keyObj.transform.localScale = new Vector3(0.2f, 0.08f, 0.4f);
                Material keyMat = GetOrCreateMaterial("KeyMaterial", new Color(0.85f, 0.75f, 0.2f));
                keyObj.GetComponent<Renderer>().sharedMaterial = keyMat;
            }
            else
            {
                Debug.LogWarning("Antigravity: Could not load Tree Prefab, falling back to primitives.");
                
                Material trunkMat = GetOrCreateMaterial("TrunkMaterial", new Color(0.42f, 0.26f, 0.1f));
                Material leavesMat = GetOrCreateMaterial("LeavesMaterial", new Color(0.12f, 0.38f, 0.16f));

                int treeCount = 1000;
                float fieldSize = 900f;

                for (int i = 0; i < treeCount; i++)
                {
                    float x = Random.Range(-fieldSize / 2f, fieldSize / 2f);
                    float z = Random.Range(-fieldSize / 2f, fieldSize / 2f);

                    // Do not spawn trees near the player spawn (0, 0)
                    float distToPlayer = Mathf.Sqrt(x * x + z * z);
                    if (distToPlayer < 10f) continue;

                    // Do not spawn trees inside the house bounds or yard (House is at 0, 15, scaled 1.6f)
                    if (Mathf.Abs(x) < 18f && z > 0f && z < 32f) continue;

                    // Do not spawn trees inside the lake clearing centered at (70, -70)
                    float distToLake = Mathf.Sqrt((x - 70f) * (x - 70f) + (z - (-70f)) * (z - (-70f)));
                    if (distToLake < 80f) continue;

                    // Do not spawn trees inside the giant temple clearing centered at (0, 250)
                    float distToTemple = Mathf.Sqrt(x * x + (z - 250f) * (z - 250f));
                    if (distToTemple < 40f) continue;

                    // Do not spawn trees inside the school clearing centered at (-200, 40)
                    float distToSchool = Mathf.Sqrt((x - (-200f)) * (x - (-200f)) + (z - 40f) * (z - 40f));
                    if (distToSchool < 45f) continue;

                    // Do not spawn trees near paths/trails to keep roads clear
                    if (IsNearAnyPath(x, z, 2.0f)) continue;

                    float terrainHeight = GetTerrainHeight(x, z);

                    GameObject tree = new GameObject("Tree_" + i);
                    tree.transform.SetParent(forest.transform);
                    tree.transform.position = new Vector3(x, terrainHeight, z);

                    float height = Random.Range(3.5f, 5.5f);

                    GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    trunk.name = "Trunk";
                    trunk.transform.SetParent(tree.transform);
                    trunk.transform.localPosition = new Vector3(0f, height / 2f, 0f);
                    trunk.transform.localScale = new Vector3(0.4f, height / 2f, 0.4f);
                    trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
                }
            }
        }

        private static void CreateForestFence()
        {
            GameObject fenceContainer = GameObject.Find("ForestFence");
            if (fenceContainer != null)
            {
                Object.DestroyImmediate(fenceContainer);
            }
            fenceContainer = new GameObject("ForestFence");
            fenceContainer.transform.position = Vector3.zero;

            string fencePrefabPath = "Assets/GVOZDY/Garden Wooden Fence modular outdoor prop/Prefabs/Fence_4_Dark.prefab";
            GameObject fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fencePrefabPath);
            if (fencePrefab == null)
            {
                fencePrefabPath = "Assets/GVOZDY/Garden Wooden Fence modular outdoor prop/Prefabs/Fence_4_Light.prefab";
                fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fencePrefabPath);
            }

            float minX = -180f;
            float maxX = 180f;
            float minZ = -100f;
            float maxZ = 260f;
            float step = 3.0f; // Distance per segment

            Material fallbackMat = null;

            // 1. South Wall (z = minZ) with EXACTLY 1 entrance opening between x = -3.5f and +3.5f
            for (float x = minX; x <= maxX; x += step)
            {
                if (x >= -3.5f && x <= 3.5f) continue; // Single entrance gap
                if (fencePrefab != null)
                {
                    SpawnFencePiece(fencePrefab, fenceContainer.transform, new Vector3(x, 0, minZ), Quaternion.Euler(0, 0, 0));
                }
                else
                {
                    if (fallbackMat == null) fallbackMat = GetOrCreateMaterial("FenceMaterial", new Color(0.35f, 0.22f, 0.12f));
                    SpawnPrimitiveFencePiece(fenceContainer.transform, fallbackMat, new Vector3(x, 0, minZ), Quaternion.Euler(0, 0, 0));
                }
            }

            // Frame the single entrance opening with side post segments
            if (fencePrefab != null)
            {
                SpawnFencePiece(fencePrefab, fenceContainer.transform, new Vector3(-3.5f, 0, minZ), Quaternion.Euler(0, 90, 0));
                SpawnFencePiece(fencePrefab, fenceContainer.transform, new Vector3(3.5f, 0, minZ), Quaternion.Euler(0, -90, 0));
            }

            // 2. North Wall (z = maxZ)
            for (float x = minX; x <= maxX; x += step)
            {
                if (fencePrefab != null)
                    SpawnFencePiece(fencePrefab, fenceContainer.transform, new Vector3(x, 0, maxZ), Quaternion.Euler(0, 180, 0));
                else
                    SpawnPrimitiveFencePiece(fenceContainer.transform, fallbackMat, new Vector3(x, 0, maxZ), Quaternion.Euler(0, 180, 0));
            }

            // 3. West Wall (x = minX)
            for (float z = minZ; z <= maxZ; z += step)
            {
                if (fencePrefab != null)
                    SpawnFencePiece(fencePrefab, fenceContainer.transform, new Vector3(minX, 0, z), Quaternion.Euler(0, 90, 0));
                else
                    SpawnPrimitiveFencePiece(fenceContainer.transform, fallbackMat, new Vector3(minX, 0, z), Quaternion.Euler(0, 90, 0));
            }

            // 4. East Wall (x = maxX)
            for (float z = minZ; z <= maxZ; z += step)
            {
                if (fencePrefab != null)
                    SpawnFencePiece(fencePrefab, fenceContainer.transform, new Vector3(maxX, 0, z), Quaternion.Euler(0, -90, 0));
                else
                    SpawnPrimitiveFencePiece(fenceContainer.transform, fallbackMat, new Vector3(maxX, 0, z), Quaternion.Euler(0, -90, 0));
            }
        }

        private static void SpawnFencePiece(GameObject prefab, Transform parent, Vector3 basePos, Quaternion rot)
        {
            float y = GetTerrainHeight(basePos.x, basePos.z);
            GameObject piece = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            piece.transform.position = new Vector3(basePos.x, y, basePos.z);
            piece.transform.rotation = rot;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            MeshRenderer[] renderers = piece.GetComponentsInChildren<MeshRenderer>();
            foreach (var mr in renderers)
            {
                if (mr.sharedMaterials != null)
                {
                    foreach (var m in mr.sharedMaterials)
                    {
                        if (m != null && m.shader != urpLit && m.shader.name != "Universal Render Pipeline/Lit")
                        {
                            m.shader = urpLit;
                            EditorUtility.SetDirty(m);
                        }
                    }
                }

                if (mr.GetComponent<Collider>() == null)
                {
                    mr.gameObject.AddComponent<MeshCollider>();
                }
            }
        }

        private static void SpawnPrimitiveFencePiece(Transform parent, Material mat, Vector3 basePos, Quaternion rot)
        {
            float y = GetTerrainHeight(basePos.x, basePos.z);
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "FenceSegment";
            p.transform.SetParent(parent);
            p.transform.position = new Vector3(basePos.x, y + 1.0f, basePos.z);
            p.transform.rotation = rot;
            p.transform.localScale = new Vector3(3.0f, 2.0f, 0.2f);
            p.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateIntersectionStreetLight()
        {
            GameObject lamp = GameObject.Find("IntersectionStreetLight");
            if (lamp != null) Object.DestroyImmediate(lamp);

            lamp = new GameObject("IntersectionStreetLight");
            float y = GetTerrainHeight(0f, 30f);
            lamp.transform.position = new Vector3(0f, y, 30f);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(lamp.transform);
            pole.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            pole.transform.localScale = new Vector3(0.15f, 2.5f, 0.15f);
            Material poleMat = GetOrCreateMaterial("StreetLightPoleMat", new Color(0.15f, 0.15f, 0.15f));
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "LampHead";
            head.transform.SetParent(lamp.transform);
            head.transform.localPosition = new Vector3(0f, 5.0f, 0.4f);
            head.transform.localScale = new Vector3(0.5f, 0.3f, 0.8f);
            head.GetComponent<Renderer>().sharedMaterial = poleMat;

            GameObject lightObj = new GameObject("LightSource");
            lightObj.transform.SetParent(lamp.transform);
            lightObj.transform.localPosition = new Vector3(0f, 4.8f, 0.4f);

            Light spot = lightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 25f;
            spot.spotAngle = 75f;
            spot.innerSpotAngle = 35f;
            spot.intensity = 4.5f;
            spot.color = new Color(1.0f, 0.92f, 0.75f);
            spot.shadows = LightShadows.Soft;
            spot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static void CreateObstacle(string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject obs = GameObject.Find(name);
            if (obs == null)
            {
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.name = name;
            }
            
            // Align obstacle with terrain surface
            float terrainHeight = GetTerrainHeight(pos.x, pos.z);
            obs.transform.position = new Vector3(pos.x, terrainHeight + scale.y / 2f, pos.z);
            obs.transform.localScale = scale;
            
            Material mat = GetOrCreateMaterial(name + "_Mat", color);
            obs.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = "Assets/_Project/Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateInvisibleMaterial()
        {
            string path = "Assets/_Project/Materials/InvisibleMaterial.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                mat = new Material(shader);
                mat.name = "InvisibleMaterial";
                
                if (shader.name.Contains("Universal Render Pipeline") || shader.name.Contains("URP"))
                {
                    mat.SetFloat("_Surface", 1f); // Transparent
                    mat.SetFloat("_Blend", 0f); // Alpha blend
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.SetOverrideTag("RenderType", "Transparent");
                }
                else
                {
                    mat.SetFloat("_Mode", 3f); // Transparent in Standard Shader
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.SetColor("_Color", new Color(0f, 0f, 0f, 0f));
                    mat.SetOverrideTag("RenderType", "Transparent");
                }
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void DecorateHouse(GameObject house)
        {
            if (house == null) return;
            
            // Check if it's the primitive house
            bool isPrimitiveHouse = house.transform.Find("Walls") != null;
            if (isPrimitiveHouse)
            {
                Debug.Log("Antigravity: Primitive house detected, skipping detailed decorations.");
                return;
            }

            Debug.Log("Antigravity: Decorating house with imported furniture...");

            // 1. Bed in Room 08 (Bedroom, left side of the house) - neatly aligned in the corner flat on floor
            string bedPath = "Assets/ThirdParty/RawWoodenFurnitureFree/Prefabs/BedSingle01emptyV1.prefab";
            GameObject bedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bedPath);
            if (bedPrefab != null)
            {
                GameObject bed = PrefabUtility.InstantiatePrefab(bedPrefab, house.transform) as GameObject;
                bed.name = "BedSingle";
                bed.transform.localPosition = new Vector3(-5.4f, 0.25f, -3.4f);
                bed.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // 2. Small cabinet in Room 08 (Bedroom) next to bed
            string cab3Path = "Assets/ThirdParty/SpaceZeta_RusticSmallCabinet/Prefabs/Cabinet3.prefab";
            GameObject cab3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(cab3Path);
            if (cab3Prefab != null)
            {
                GameObject cab = PrefabUtility.InstantiatePrefab(cab3Prefab, house.transform) as GameObject;
                cab.name = "BedroomCabinet";
                cab.transform.localPosition = new Vector3(-3.4f, 0.25f, -3.4f);
                cab.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // 3. Living Room Cabinet in Room 01 (Living Room, center/front)
            string cab1Path = "Assets/ThirdParty/SpaceZeta_RusticSmallCabinet/Prefabs/Cabinet1.prefab";
            GameObject cab1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(cab1Path);
            GameObject livingCabinet = null;
            if (cab1Prefab != null)
            {
                livingCabinet = PrefabUtility.InstantiatePrefab(cab1Prefab, house.transform) as GameObject;
                livingCabinet.name = "LivingCabinet";
                livingCabinet.transform.localPosition = new Vector3(-2.2f, 0.25f, -3.5f);
                livingCabinet.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // 4. Radio on top of the Living Room Cabinet (parented directly to the cabinet)
            string radioPath = "Assets/ThirdParty/Radio/Prefabs/Radio.prefab";
            GameObject radioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(radioPath);
            if (radioPrefab != null && livingCabinet != null)
            {
                GameObject radio = PrefabUtility.InstantiatePrefab(radioPrefab, livingCabinet.transform) as GameObject;
                radio.name = "OldRadio";
                radio.transform.localPosition = new Vector3(0f, 0.58f, 0f); // Placed perfectly on top of Cabinet1
                radio.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                radio.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f); // Scale radio to fit cabinet nicely
            }

            // 5. Table & Chairs in Room 01 (Living Room)
            string tablePath = "Assets/ThirdParty/RawWoodenFurnitureFree/Prefabs/Table01v2.prefab";
            GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tablePath);
            if (tablePrefab != null)
            {
                GameObject table = PrefabUtility.InstantiatePrefab(tablePrefab, house.transform) as GameObject;
                table.name = "DiningTable";
                table.transform.localPosition = new Vector3(1.2f, 0.25f, -2.0f);
                table.transform.localRotation = Quaternion.identity;

                string chairPath = "Assets/ThirdParty/RawWoodenFurnitureFree/Prefabs/Chair02v2.prefab";
                GameObject chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(chairPath);
                if (chairPrefab != null)
                {
                    // Chair 1
                    GameObject chair1 = PrefabUtility.InstantiatePrefab(chairPrefab, house.transform) as GameObject;
                    chair1.name = "Chair_1";
                    chair1.transform.localPosition = new Vector3(1.2f, 0.25f, -1.2f);
                    chair1.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                    // Chair 2
                    GameObject chair2 = PrefabUtility.InstantiatePrefab(chairPrefab, house.transform) as GameObject;
                    chair2.name = "Chair_2";
                    chair2.transform.localPosition = new Vector3(1.2f, 0.25f, -2.8f);
                    chair2.transform.localRotation = Quaternion.identity;
                }
            }

            // 6. Firewood next to the fireplace/stove in Room 05 (Kitchen, right side)
            string firewoodStackPath = "Assets/ThirdParty/B3DArt/FirewoodBC01/FirewoodStackBlackCherry01_LOD.prefab";
            GameObject firewoodStackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(firewoodStackPath);
            if (firewoodStackPrefab != null)
            {
                GameObject stack = PrefabUtility.InstantiatePrefab(firewoodStackPrefab, house.transform) as GameObject;
                stack.name = "FirewoodStack";
                stack.transform.localPosition = new Vector3(2.2f, 0.25f, -0.5f);
                stack.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                stack.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f); // Slightly smaller scale for indoor firewood stack
            }

            // Place some individual firewood pieces on the floor for realism
            string firewoodPiece1Path = "Assets/ThirdParty/B3DArt/FirewoodBC01/FirewoodBlackCherryPiece01_LOD.prefab";
            GameObject piecePrefab1 = AssetDatabase.LoadAssetAtPath<GameObject>(firewoodPiece1Path);
            if (piecePrefab1 != null)
            {
                GameObject piece1 = PrefabUtility.InstantiatePrefab(piecePrefab1, house.transform) as GameObject;
                piece1.name = "FirewoodPiece_1";
                piece1.transform.localPosition = new Vector3(2.3f, 0.25f, 0.2f);
                piece1.transform.localRotation = Quaternion.Euler(15f, 45f, 0f);
            }

            string firewoodPiece2Path = "Assets/ThirdParty/B3DArt/FirewoodBC01/FirewoodBlackCherryPiece02_LOD.prefab";
            GameObject piecePrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>(firewoodPiece2Path);
            if (piecePrefab2 != null)
            {
                GameObject piece2 = PrefabUtility.InstantiatePrefab(piecePrefab2, house.transform) as GameObject;
                piece2.name = "FirewoodPiece_2";
                piece2.transform.localPosition = new Vector3(2.0f, 0.25f, 0.3f);
                piece2.transform.localRotation = Quaternion.Euler(-10f, 110f, 15f);
            }

            // 7. Kitchen Cabinet in Room 05
            string cab4Path = "Assets/ThirdParty/SpaceZeta_RusticSmallCabinet/Prefabs/Cabinet4.prefab";
            GameObject cab4Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(cab4Path);
            if (cab4Prefab != null)
            {
                GameObject cab = PrefabUtility.InstantiatePrefab(cab4Prefab, house.transform) as GameObject;
                cab.name = "KitchenCabinet";
                cab.transform.localPosition = new Vector3(5.5f, 0.25f, 2.0f);
                cab.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        private static void SetupHouseVase(GameObject house)
        {
            if (house == null) return;

            string vaseFbx = "Assets/Models/Vase/Meshy_AI_Ribbed_Brown_Ceramic__0715162255_texture.fbx";
            string vaseTex = "Assets/Models/Vase/Meshy_AI_Ribbed_Brown_Ceramic__0715162255_texture.png";
            string vaseNormal = "Assets/Models/Vase/Meshy_AI_Ribbed_Brown_Ceramic__0715162255_texture_normal.png";
            string vaseMatPath = "Assets/Models/Vase/M_Vase.mat";

            // Configure Normal Map import settings in editor
            TextureImporter normalImporter = AssetImporter.GetAtPath(vaseNormal) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Material vaseMat = AssetDatabase.LoadAssetAtPath<Material>(vaseMatPath);
            bool isNew = false;
            if (vaseMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                vaseMat = new Material(shader);
                isNew = true;
            }

            Texture2D diffuseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(vaseTex);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(vaseNormal);

            if (vaseMat.shader.name.Contains("Universal Render Pipeline") || vaseMat.shader.name.Contains("URP"))
            {
                if (diffuseTex != null) vaseMat.SetTexture("_BaseMap", diffuseTex);
                if (normalTex != null) vaseMat.SetTexture("_BumpMap", normalTex);
                vaseMat.SetFloat("_Smoothness", 0.3f);
            }
            else
            {
                if (diffuseTex != null) vaseMat.SetTexture("_MainTex", diffuseTex);
                if (normalTex != null) vaseMat.SetTexture("_BumpMap", normalTex);
                vaseMat.SetFloat("_Glossiness", 0.3f);
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(vaseMat, vaseMatPath);
            }
            else
            {
                EditorUtility.SetDirty(vaseMat);
            }
            AssetDatabase.SaveAssets();

            GameObject vasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vaseFbx);
            if (vasePrefab != null)
            {
                // Clear old Vase
                Transform oldVase = house.transform.Find("HouseVase");
                if (oldVase != null) Object.DestroyImmediate(oldVase.gameObject);

                GameObject vaseObj = PrefabUtility.InstantiatePrefab(vasePrefab, house.transform) as GameObject;
                vaseObj.name = "HouseVase";
                vaseObj.transform.localPosition = new Vector3(1.0f, 0.95f, -2.0f); // Placed on dining table
                vaseObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                vaseObj.transform.localScale = Vector3.one * 0.1f;

                foreach (var r in vaseObj.GetComponentsInChildren<MeshRenderer>())
                {
                    r.sharedMaterial = vaseMat;
                }

                if (vaseObj.GetComponent<Collider>() == null)
                {
                    BoxCollider box = vaseObj.AddComponent<BoxCollider>();
                    box.size = Vector3.one * 3f;
                }

                // Attach InspectableObject script
                InspectableObject inspect = vaseObj.GetComponent<InspectableObject>();
                if (inspect == null) inspect = vaseObj.AddComponent<InspectableObject>();
            }

            // Setup Oil Lamp inside house (placed in editor)
            SetupHouseLamp(house);
        }

        private static void ConfigureOilLampMaterial()
        {
            string lampTex = "Assets/Models/Oil Lamp/Meshy_AI_Vintage_kerosene_lant_0715162024_texture.png";
            string lampNormal = "Assets/Models/Oil Lamp/Meshy_AI_Vintage_kerosene_lant_0715162024_texture_normal.png";
            string lampMatPath = "Assets/Models/Oil Lamp/M_OilLamp.mat";

            TextureImporter normalImporter = AssetImporter.GetAtPath(lampNormal) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Material lampMat = AssetDatabase.LoadAssetAtPath<Material>(lampMatPath);
            bool isNew = false;
            if (lampMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                lampMat = new Material(shader);
                isNew = true;
            }

            Texture2D diffuseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(lampTex);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(lampNormal);

            if (lampMat.shader.name.Contains("Universal Render Pipeline") || lampMat.shader.name.Contains("URP"))
            {
                if (diffuseTex != null) lampMat.SetTexture("_BaseMap", diffuseTex);
                if (normalTex != null) lampMat.SetTexture("_BumpMap", normalTex);
                lampMat.SetFloat("_Metallic", 0.7f);
                lampMat.SetFloat("_Smoothness", 0.6f);
            }
            else
            {
                if (diffuseTex != null) lampMat.SetTexture("_MainTex", diffuseTex);
                if (normalTex != null) lampMat.SetTexture("_BumpMap", normalTex);
                lampMat.SetFloat("_Metallic", 0.7f);
                lampMat.SetFloat("_Glossiness", 0.6f);
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(lampMat, lampMatPath);
            }
            else
            {
                EditorUtility.SetDirty(lampMat);
            }
            AssetDatabase.SaveAssets();
        }

        private static void SetupHouseLamp(GameObject house)
        {
            if (house == null) return;

            string lampFbx = "Assets/Models/Oil Lamp/Meshy_AI_Vintage_kerosene_lant_0715162024_texture.fbx";
            string lampMatPath = "Assets/Models/Oil Lamp/M_OilLamp.mat";
            GameObject lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lampFbx);
            Material lampMat = AssetDatabase.LoadAssetAtPath<Material>(lampMatPath);

            if (lampPrefab != null)
            {
                // Clear old Lamp
                Transform oldLamp = house.transform.Find("HouseLamp");
                if (oldLamp != null) Object.DestroyImmediate(oldLamp.gameObject);

                GameObject lampObj = PrefabUtility.InstantiatePrefab(lampPrefab, house.transform) as GameObject;
                lampObj.name = "HouseLamp";
                lampObj.transform.localPosition = new Vector3(-3.4f, 0.83f, -3.4f); // Placed on bedroom cabinet next to bed
                lampObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                lampObj.transform.localScale = Vector3.one * 0.15f;

                foreach (var r in lampObj.GetComponentsInChildren<MeshRenderer>())
                {
                    r.sharedMaterial = lampMat;
                }

                if (lampObj.GetComponent<Collider>() == null)
                {
                    BoxCollider box = lampObj.AddComponent<BoxCollider>();
                    box.size = Vector3.one * 2f;
                }
            }
        }

        private static void SetupPlayerLampViewModel(GameObject mainCam)
        {
            Transform lampViewModelTrans = mainCam.transform.Find("LampViewModel");
            GameObject lampViewModelGo = null;
            if (lampViewModelTrans != null)
            {
                lampViewModelGo = lampViewModelTrans.gameObject;
                Object.DestroyImmediate(lampViewModelGo);
            }

            string lampFbx = "Assets/Models/Oil Lamp/Meshy_AI_Vintage_kerosene_lant_0715162024_texture.fbx";
            string lampMatPath = "Assets/Models/Oil Lamp/M_OilLamp.mat";
            GameObject lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lampFbx);
            Material lampMat = AssetDatabase.LoadAssetAtPath<Material>(lampMatPath);

            if (lampPrefab != null)
            {
                lampViewModelGo = PrefabUtility.InstantiatePrefab(lampPrefab, mainCam.transform) as GameObject;
                lampViewModelGo.name = "LampViewModel";
                
                // Position the lamp viewmodel beautifully in the hand area
                lampViewModelGo.transform.localPosition = new Vector3(0.24f, -0.28f, 0.45f);
                lampViewModelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                lampViewModelGo.transform.localScale = Vector3.one * 0.08f;

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

        private static void CreateHorrorScene(GameObject house, GameObject player)
        {
            Debug.Log("Antigravity: Setting up URP Horror environment, moonlight, fog, and creepy assets...");

            // 1. Lighting Setup (Sunlight & Moonlight & Day-Night Cycle)
            Light sunLight = null;
            Light moonLight = null;
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    if (l.gameObject.name == "Sunlight" || l.gameObject.name == "Sun")
                    {
                        sunLight = l;
                    }
                    else if (l.gameObject.name == "Moonlight" || l.gameObject.name == "Moon")
                    {
                        moonLight = l;
                    }
                    else if (sunLight == null)
                    {
                        sunLight = l;
                        l.gameObject.name = "Sunlight";
                    }
                }
            }

            if (sunLight == null)
            {
                GameObject go = new GameObject("Sunlight");
                sunLight = go.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }
            sunLight.transform.rotation = Quaternion.Euler(38f, 135f, 0f);
            sunLight.intensity = 1.25f;
            sunLight.color = new Color(1.0f, 0.94f, 0.82f);
            sunLight.shadows = LightShadows.Soft;

            if (moonLight == null)
            {
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional && l != sunLight)
                    {
                        moonLight = l;
                        l.gameObject.name = "Moonlight";
                        break;
                    }
                }
                if (moonLight == null)
                {
                    GameObject go = new GameObject("Moonlight");
                    moonLight = go.AddComponent<Light>();
                    moonLight.type = LightType.Directional;
                }
            }
            moonLight.intensity = 0.08f;
            moonLight.color = new Color(0.15f, 0.2f, 0.35f);
            moonLight.shadows = LightShadows.Soft;

            // Setup DayNightCycleManager
            GameObject cycleGo = GameObject.Find("DayNightCycleManager");
            if (cycleGo == null)
            {
                cycleGo = new GameObject("DayNightCycleManager");
            }
            DayNightCycle cycle = cycleGo.GetComponent<DayNightCycle>();
            if (cycle == null)
            {
                cycle = cycleGo.AddComponent<DayNightCycle>();
            }
            cycle.sunLight = sunLight;
            cycle.moonLight = moonLight;
            cycle.dayDurationInSeconds = 120f; // 2 minutes cycle
            cycle.isPermanentlyNight = false; // Lock at night by default -> Disabled for daytime/sunny setup
            cycle.timeOfDay = 0.35f; // Morning/Daytime

            // Apply atmospheric forest lighting and fog settings in Editor & Game View
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.54f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.42f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.22f, 0.15f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.48f, 0.53f, 0.51f);
            RenderSettings.fogDensity = 0.007f;

            // 1b. Create the Lake
            CreateLake();

            // 1c. Create Dirt Trails (Main trail from spawn/fence entrance leading into forest to central 4-way intersection and branching out)
            CreatePath("Path_Spawn_To_Intersection", new Vector3(0f, 0f, -108f), new Vector3(0f, 0f, 30f), 3.0f);
            CreatePath("Path_Intersection_To_House", new Vector3(0f, 0f, 30f), new Vector3(60f, 0f, 30f), 2.5f);
            CreatePath("Path_Intersection_To_Lake", new Vector3(0f, 0f, 30f), new Vector3(-65f, 0f, -30f), 2.5f);
            CreatePath("Path_Intersection_To_School", new Vector3(0f, 0f, 30f), new Vector3(-120f, 0f, 150f), 2.5f);

            // 1d. Create Central Intersection Street Light
            CreateIntersectionStreetLight();

            // 3. Re-create Horror Environment parent container
            GameObject horrorEnv = GameObject.Find("HorrorEnvironment");
            if (horrorEnv != null)
            {
                Object.DestroyImmediate(horrorEnv);
            }
            horrorEnv = new GameObject("HorrorEnvironment");

            // 4. Candle in House Living Room (Clean up old duplicated ones first)
            GameObject[] oldCandles = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in oldCandles)
            {
                if (c == null) continue;
                if (c.name == "HouseCandle")
                {
                    Object.DestroyImmediate(c);
                }
            }

            GameObject candle = new GameObject("HouseCandle");
            candle.transform.SetParent(house.transform);
            candle.transform.localPosition = new Vector3(0.5f, 0.95f, -2.0f); // Placed on the living room table
            Light candleLight = candle.AddComponent<Light>();
            candleLight.type = LightType.Point;
            candleLight.range = 9f;
            candleLight.intensity = 0.9f;
            candleLight.color = new Color(0.95f, 0.45f, 0.12f); // Warm flame orange
            candleLight.shadows = LightShadows.Soft;
            candle.AddComponent<LightFlicker>(); // Makes the candle flicker naturally!



            // Setup the Shadow Figure
            CreateShadowFigure();
        }

        private static void CreateLake()
        {
            GameObject water = GameObject.Find("LakeWater");
            if (water != null)
            {
                Object.DestroyImmediate(water);
            }

            // Create flat water plane using Unity's built-in Plane primitive
            water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "LakeWater";
            
            // Set collider as trigger so player doesn't walk on water
            MeshCollider col = water.GetComponent<MeshCollider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Position centered at (70f, -1.2f, -70f)
            water.transform.position = new Vector3(70f, -1.2f, -70f);
            
            // Standard Plane is 10x10, so scale 17.0 makes it 170m wide, covering the basin perfectly
            water.transform.localScale = new Vector3(17.0f, 1f, 17.0f);
            water.transform.rotation = Quaternion.identity;

            // Generate Transparent Water Material
            Material waterMat = GetOrCreateMaterial("WaterMaterial", new Color(0.08f, 0.22f, 0.28f, 0.6f));
            
            Shader waterShader = Shader.Find("Universal Render Pipeline/Lit");
            if (waterShader == null) waterShader = Shader.Find("Standard");
            if (waterMat.shader != waterShader)
            {
                waterMat.shader = waterShader;
            }

            if (waterMat.shader.name.Contains("Universal Render Pipeline") || waterMat.shader.name.Contains("URP"))
            {
                waterMat.SetFloat("_Surface", 1f); // Transparent
                waterMat.SetFloat("_Blend", 0f); // Alpha blend
                waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                waterMat.SetInt("_ZWrite", 0);
                waterMat.SetColor("_BaseColor", new Color(0.08f, 0.22f, 0.28f, 0.6f));
                waterMat.SetFloat("_Smoothness", 0.95f);
                waterMat.SetFloat("_Metallic", 0.1f);
                waterMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                waterMat.SetOverrideTag("RenderType", "Transparent");
            }
            else
            {
                waterMat.SetFloat("_Mode", 3f); // Transparent in Standard Shader
                waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                waterMat.SetInt("_ZWrite", 0);
                waterMat.DisableKeyword("_ALPHATEST_ON");
                waterMat.EnableKeyword("_ALPHABLEND_ON");
                waterMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                waterMat.SetColor("_Color", new Color(0.08f, 0.22f, 0.28f, 0.6f));
                waterMat.SetFloat("_Glossiness", 0.95f);
                waterMat.SetFloat("_Metallic", 0.1f);
                waterMat.SetOverrideTag("RenderType", "Transparent");
            }
            
            waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            EditorUtility.SetDirty(waterMat);
            AssetDatabase.SaveAssets();

            water.GetComponent<Renderer>().sharedMaterial = waterMat;
        }

        private static void CreatePath(string name, Vector3 start, Vector3 end, float width)
        {
            GameObject pathGo = GameObject.Find(name);
            if (pathGo != null)
            {
                Object.DestroyImmediate(pathGo);
            }
            
            pathGo = new GameObject(name);
            pathGo.transform.position = Vector3.zero;
            pathGo.transform.rotation = Quaternion.identity;
            
            Vector3 dir = (end - start).normalized;
            float dist = Vector3.Distance(start, end);
            
            int segments = Mathf.CeilToInt(dist * 1.5f); // Subdivide for smooth terrain contour alignment
            float segLength = dist / segments;

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";
            
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            int[] triangles = new int[segments * 6];
            Vector2[] uvs = new Vector2[vertices.Length];

            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * (width / 2f);

            for (int i = 0; i <= segments; i++)
            {
                Vector3 center = start + dir * (i * segLength);
                
                Vector3 leftPoint = center - right;
                Vector3 rightPoint = center + right;
                
                leftPoint.y = GetTerrainHeight(leftPoint.x, leftPoint.z) + 0.08f; // Elevate above ground so path mesh is clearly visible over terrain and grass
                rightPoint.y = GetTerrainHeight(rightPoint.x, rightPoint.z) + 0.08f;

                vertices[i * 2] = leftPoint;
                vertices[i * 2 + 1] = rightPoint;

                uvs[i * 2] = new Vector2(0f, (float)i / segments * (dist / width));
                uvs[i * 2 + 1] = new Vector2(1f, (float)i / segments * (dist / width));
            }

            int triIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int row1 = i * 2;
                int row2 = (i + 1) * 2;

                triangles[triIdx++] = row1;
                triangles[triIdx++] = row2;
                triangles[triIdx++] = row1 + 1;

                triangles[triIdx++] = row1 + 1;
                triangles[triIdx++] = row2;
                triangles[triIdx++] = row2 + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            MeshFilter mf = pathGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = pathGo.AddComponent<MeshRenderer>();
            
            Material pathMat = GetOrCreateMaterial("DirtPathMaterial", new Color(0.42f, 0.28f, 0.16f, 1.0f));
            pathMat.SetFloat("_Smoothness", 0.1f);

            Shader pathShader = Shader.Find("Universal Render Pipeline/Lit");
            if (pathShader == null) pathShader = Shader.Find("Standard");
            if (pathMat.shader != pathShader)
            {
                pathMat.shader = pathShader;
            }
            if (pathMat.HasProperty("_BaseColor")) pathMat.SetColor("_BaseColor", new Color(0.42f, 0.28f, 0.16f, 1.0f));
            if (pathMat.HasProperty("_Color")) pathMat.SetColor("_Color", new Color(0.42f, 0.28f, 0.16f, 1.0f));
            
            // Try to load ADG dirt texture
            string dirtDiffuse = "Assets/ThirdParty/ADG_Textures/ground_vol1/ground2/ground2_Diffuse.tga";
            Texture2D dirtTex = AssetDatabase.LoadAssetAtPath<Texture2D>(dirtDiffuse);
            if (dirtTex != null)
            {
                if (pathMat.HasProperty("_BaseMap"))
                {
                    pathMat.SetTexture("_BaseMap", dirtTex);
                    pathMat.SetTextureScale("_BaseMap", new Vector2(1f, 4f));
                }
                if (pathMat.HasProperty("_MainTex"))
                {
                    pathMat.SetTexture("_MainTex", dirtTex);
                    pathMat.SetTextureScale("_MainTex", new Vector2(1f, 4f));
                }
            }

            EditorUtility.SetDirty(pathMat);
            AssetDatabase.SaveAssets();
            
            mr.sharedMaterial = pathMat;
            
            GameObject terrain = GameObject.Find("ProceduralTerrain");
            if (terrain != null)
            {
                pathGo.transform.SetParent(terrain.transform);
            }
        }

        private static bool IsNearAnyPath(float x, float z, float minDistance)
        {
            float dist = GetDistanceToPathNetwork(x, z, out float pathWidth);
            return dist < (pathWidth * 0.5f + minDistance);
        }

        private static float GetDistanceToPathNetwork(float x, float z, out float pathWidth)
        {
            pathWidth = 3.2f;

            // List of winding trail segments in the forest network:
            // (startX, startZ, endX, endZ, width, curveAmplitude, curveFrequency)
            float[,] paths = new float[,]
            {
                // 1. Entrance -> Crossroads
                { 0f, -108f, 0f, 30f, 3.4f, 4.5f, 0.03f },
                // 2. Crossroads -> House
                { 0f, 30f, 65f, 30f, 2.8f, 3.0f, 0.04f },
                // 3. Crossroads -> Campsite
                { 0f, 30f, -20f, 22f, 2.6f, 2.5f, 0.05f },
                // 4. Crossroads -> Temple (North Winding Trail)
                { 0f, 30f, 0f, 250f, 3.2f, 8.5f, 0.025f },
                // 5. Crossroads -> School (West Winding Trail)
                { 0f, 30f, -200f, 40f, 3.0f, 6.5f, 0.02f },
                // 6. Entrance -> Lake Shore (South-East Trail)
                { 0f, -108f, 70f, -70f, 2.6f, 3.8f, 0.035f },
                // 7. House -> Temple Forest Loop Shortcut
                { 65f, 30f, 20f, 150f, 2.5f, 5.0f, 0.03f }
            };

            Vector2 point = new Vector2(x, z);
            float minDistance = float.MaxValue;

            for (int i = 0; i < paths.GetLength(0); i++)
            {
                Vector2 start = new Vector2(paths[i, 0], paths[i, 1]);
                Vector2 end = new Vector2(paths[i, 2], paths[i, 3]);
                float pWidth = paths[i, 4];
                float amp = paths[i, 5];
                float freq = paths[i, 6];

                float dist = DistanceToWindingSegment(point, start, end, amp, freq);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    pathWidth = pWidth;
                }
            }

            return minDistance;
        }

        private static float DistanceToWindingSegment(Vector2 p, Vector2 a, Vector2 b, float amplitude, float frequency)
        {
            Vector2 ab = b - a;
            float len = ab.magnitude;
            if (len < 0.001f) return Vector2.Distance(p, a);

            Vector2 dir = ab / len;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            Vector2 ap = p - a;
            float proj = Vector2.Dot(ap, dir);
            proj = Mathf.Clamp(proj, 0f, len);

            // Sine & Perlin noise winding curve displacement
            float curveOffset = Mathf.Sin(proj * frequency + (a.x * 0.1f)) * amplitude 
                              + (Mathf.PerlinNoise(proj * 0.04f + a.x, proj * 0.04f + a.y) - 0.5f) * (amplitude * 1.2f);

            Vector2 pathPoint = a + dir * proj + perp * curveOffset;
            return Vector2.Distance(p, pathPoint);
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = p - a;
            float ab2 = Vector2.Dot(ab, ab);
            if (ab2 < 0.0001f) return Vector2.Distance(p, a);
            
            float t = Vector2.Dot(ap, ab) / ab2;
            t = Mathf.Clamp01(t);
            Vector2 closest = a + t * ab;
            return Vector2.Distance(p, closest);
        }

        private static void CreateShadowFigure()
        {
            GameObject shadow = GameObject.Find("ShadowFigure");
            if (shadow != null)
            {
                Object.DestroyImmediate(shadow);
            }

            // Create humanoid capsule representation
            shadow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            shadow.name = "ShadowFigure";
            
            // Adjust dimensions to look like a tall thin stalker
            shadow.transform.localScale = new Vector3(0.4f, 1.1f, 0.4f); // height will be 2.2m (capsule default is 2m, so 2 * 1.1 = 2.2m)
            
            // Disable shadow receiving to look like a black silhouette void
            MeshRenderer mr = shadow.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = false;

                // Create unlit black material so it remains a pitch-black void under the flashlight
                Material shadowMat = GetOrCreateMaterial("ShadowFigureMaterial", Color.black);
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader != null)
                {
                    shadowMat.shader = unlitShader;
                }
                shadowMat.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.95f)); // pure black
                mr.sharedMaterial = shadowMat;
            }

            // Set collider as trigger so player doesn't bump into it
            CapsuleCollider col = shadow.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Attach ShadowFigure runtime controller script
            ShadowFigure sf = shadow.AddComponent<ShadowFigure>();
            sf.disappearDistance = 12f;
            
            // Predefine 5 spawn locations behind trees bordering the front yard
            // Tree positions bordering the yard flat area (Z > 22m or X/Z combinations outside the 22m radius yard)
            sf.spawnPoints = new[]
            {
                new Vector3(-20f, 0f, 10f),   // Left border
                new Vector3(20f, 0f, 10f),    // Right border
                new Vector3(-15f, 0f, -18f),  // Left-front border
                new Vector3(15f, 0f, -18f),   // Right-front border
                new Vector3(0f, 0f, -25f)     // Rear border (behind player)
            };

            // Set window stalking coordinates (relative to House at 0, 15)
            sf.localWindows = new[]
            {
                new Vector3(-2.0f, 0f, -4.5f), // Front Left Window
                new Vector3(2.0f, 0f, -4.5f),  // Front Right Window
                new Vector3(-5.5f, 0f, -1.0f), // Left Side Window
                new Vector3(5.5f, 0f, -1.0f)   // Right Side Window
            };
            
            sf.localWindowSpawns = new[]
            {
                new Vector3(-2.0f, 0f, -8.0f), // Outside Front Left
                new Vector3(2.0f, 0f, -8.0f),  // Outside Front Right
                new Vector3(-9.0f, 0f, -1.0f), // Outside Left Side
                new Vector3(9.0f, 0f, -1.0f)   // Outside Right Side
            };
        }

        private static void RemoveMonster()
        {
            GameObject[] oldMonsters = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in oldMonsters)
            {
                if (m == null) continue;
                if (m.name == "MonsterEnemy" || m.name.Contains("MN D") || m.name.Contains("MinD") || m.name.Contains("BobGrandmaster"))
                {
                    Object.DestroyImmediate(m);
                }
            }
        }

        private static void CreateCampsite()
        {
            GameObject camp = GameObject.Find("Campsite");
            if (camp != null)
            {
                Object.DestroyImmediate(camp);
            }
            camp = new GameObject("Campsite");

            // Campsite location near lake (-65, -35)
            float campX = -65f;
            float campZ = -35f;
            float groundY = GetTerrainHeight(campX, campZ);
            camp.transform.position = new Vector3(campX, groundY, campZ);

            // 1. Campfire / Bếp lửa (PolygonPilots CampFire prefab)
            string firePrefabPath = "Assets/PolygonPilots/Campfire/Prefabs/CampFire.prefab";
            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(firePrefabPath);
            if (firePrefab != null)
            {
                GameObject fireObj = PrefabUtility.InstantiatePrefab(firePrefab, camp.transform) as GameObject;
                fireObj.name = "Campfire";
                fireObj.transform.localPosition = Vector3.zero;
                fireObj.transform.localScale = Vector3.one * 1.4f;

                // Dying embers light source
                GameObject flameLight = new GameObject("EmberLight");
                flameLight.transform.SetParent(fireObj.transform, false);
                flameLight.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                
                Light lt = flameLight.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 8f;
                lt.intensity = 0.85f;
                lt.color = new Color(0.95f, 0.42f, 0.1f);
                lt.shadows = LightShadows.Soft;
                flameLight.AddComponent<LightFlicker>();
            }
            else
            {
                // Primitive Campfire fallback
                GameObject fireObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fireObj.name = "Campfire";
                fireObj.transform.SetParent(camp.transform);
                fireObj.transform.localPosition = Vector3.zero;
                fireObj.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
                Material ashMat = GetOrCreateMaterial("CampfireAshMaterial", new Color(0.15f, 0.12f, 0.1f));
                fireObj.GetComponent<Renderer>().sharedMaterial = ashMat;
            }

            // 2. 3 Tents (PolygonPilots Tent prefab as described in script)
            string tentPrefabPath = "Assets/PolygonPilots/Campfire/Prefabs/Tent.prefab";
            GameObject tentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tentPrefabPath);

            Vector3[] tentPositions = new Vector3[]
            {
                new Vector3(4.5f, 0f, 4.0f),   // Tent 1 (North-East)
                new Vector3(-5.0f, 0f, 2.5f),  // Tent 2 (North-West)
                new Vector3(1.0f, 0f, -5.5f)   // Tent 3 (South)
            };
            float[] tentRotations = new float[] { 135f, 225f, 15f };

            for (int i = 0; i < 3; i++)
            {
                float tX = campX + tentPositions[i].x;
                float tZ = campZ + tentPositions[i].z;
                float tY = GetTerrainHeight(tX, tZ);

                if (tentPrefab != null)
                {
                    GameObject tent = PrefabUtility.InstantiatePrefab(tentPrefab, camp.transform) as GameObject;
                    tent.name = "Tent_" + (i + 1);
                    tent.transform.position = new Vector3(tX, tY, tZ);
                    tent.transform.rotation = Quaternion.Euler(0f, tentRotations[i], 0f);
                    tent.transform.localScale = Vector3.one * 1.35f;
                }
                else
                {
                    // Primitive tent fallback
                    GameObject tent = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tent.name = "Tent_" + (i + 1);
                    tent.transform.SetParent(camp.transform);
                    tent.transform.position = new Vector3(tX, tY + 1.0f, tZ);
                    tent.transform.rotation = Quaternion.Euler(0f, tentRotations[i], 0f);
                    tent.transform.localScale = new Vector3(2.5f, 1.8f, 3.2f);
                    Material tentMat = GetOrCreateMaterial("TentFallbackMaterial", new Color(0.25f, 0.35f, 0.22f));
                    tent.GetComponent<Renderer>().sharedMaterial = tentMat;
                }
            }

            // 3. BushCraft Tarp shelter on the side
            string tarpPrefabPath = "Assets/ArtisticMechanics/Bush-Craft Extension Pack/Prefabs/Stealth/Tarp-forest_tent_s.prefab";
            GameObject tarpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tarpPrefabPath);
            if (tarpPrefab != null)
            {
                float tarpX = campX - 7f;
                float tarpZ = campZ - 4f;
                float tarpY = GetTerrainHeight(tarpX, tarpZ);
                GameObject tarp = PrefabUtility.InstantiatePrefab(tarpPrefab, camp.transform) as GameObject;
                tarp.name = "BushcraftTarp";
                tarp.transform.position = new Vector3(tarpX, tarpY, tarpZ);
                tarp.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                tarp.transform.localScale = Vector3.one * 1.2f;
            }

            // 4. Firewood pieces near campfire
            string firewoodPath = "Assets/ThirdParty/B3DArt/FirewoodBC01/FirewoodStackBlackCherry01_LOD.prefab";
            GameObject firewoodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(firewoodPath);
            if (firewoodPrefab != null)
            {
                GameObject stack = PrefabUtility.InstantiatePrefab(firewoodPrefab, camp.transform) as GameObject;
                stack.name = "CampFirewood";
                stack.transform.position = new Vector3(campX + 2.2f, groundY, campZ - 1.2f);
                stack.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                stack.transform.localScale = Vector3.one * 0.7f;
            }

            // Auto-fix any material shaders on campsite renderers to URP Lit
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            foreach (var mr in camp.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.sharedMaterials != null)
                {
                    foreach (var m in mr.sharedMaterials)
                    {
                        if (m != null && m.shader != urpLit && !m.shader.name.Contains("Universal Render Pipeline") && !m.shader.name.Contains("URP"))
                        {
                            m.shader = urpLit;
                            EditorUtility.SetDirty(m);
                        }
                    }
                }
            }
        }

        private static void CreateMonsterAnimatorController()
        {
            // Configure loop time for loopable animations
            ConfigureLoopTime("Assets/MN D/FBX/IDOL.FBX");
            ConfigureLoopTime("Assets/MN D/FBX/WALKING.FBX");
            ConfigureLoopTime("Assets/MN D/FBX/RUN.FBX");

            string controllerPath = "Assets/_Project/Materials/MonsterAnimatorController.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // Add parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Load clips
            AnimationClip idleClip = GetAnimationClipFromFBX("Assets/MN D/FBX/IDOL.FBX");
            AnimationClip walkClip = GetAnimationClipFromFBX("Assets/MN D/FBX/WALKING.FBX");
            AnimationClip runClip = GetAnimationClipFromFBX("Assets/MN D/FBX/RUN.FBX");
            AnimationClip attackClip = GetAnimationClipFromFBX("Assets/MN D/FBX/ATTACK.FBX");

            // Create states
            AnimatorState idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            AnimatorState walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;

            AnimatorState runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;

            AnimatorState attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;

            // Transitions
            // Idle -> Walk
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.duration = 0.25f;

            // Walk -> Idle
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.duration = 0.25f;

            // Walk -> Run
            AnimatorStateTransition walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 3.0f, "Speed");
            walkToRun.duration = 0.25f;

            // Run -> Walk
            AnimatorStateTransition runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 3.0f, "Speed");
            runToWalk.duration = 0.25f;

            // Any -> Attack
            AnimatorStateTransition anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToAttack.duration = 0.1f;

            // Attack -> Idle
            AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.9f;
            attackToIdle.duration = 0.25f;
        }

        private static AnimationClip GetAnimationClipFromFBX(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip && !asset.name.StartsWith("__preview__"))
                {
                    return asset as AnimationClip;
                }
            }
            return null;
        }

        private static void ConfigureLoopTime(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        clips[i].loopTime = true;
                        clips[i].loopPose = true;
                    }
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void AutoSetupTemple()
        {
            EditorApplication.delayCall -= AutoSetupTemple;
            GameObject temple = GameObject.Find("AncientTemple");
            if (temple != null)
            {
                // If it is the old temple (not at X=0, Z=120), destroy it so we can regenerate the new colossal one!
                if (Mathf.Abs(temple.transform.position.x) > 5f || Mathf.Abs(temple.transform.position.z - 250f) > 5f)
                {
                    Object.DestroyImmediate(temple);
                }
                else
                {
                    return;
                }
            }
            SetupTemple();
        }

        [MenuItem("Tools/Antigravity/Import and Setup Temple")]
        public static void SetupTemple()
        {
            string fbxPath = "Assets/Models/Temple/Meshy_AI_Halo_in_the_Green_Cor_0713113844_texture.fbx";
            string baseTexPath = "Assets/Models/Temple/Meshy_AI_Halo_in_the_Green_Cor_0713113844_texture.png";
            string normalPath = "Assets/Models/Temple/Meshy_AI_Halo_in_the_Green_Cor_0713113844_texture_normal.png";
            string emissionPath = "Assets/Models/Temple/Meshy_AI_Halo_in_the_Green_Cor_0713113844_texture_emission.png";
            string metallicPath = "Assets/Models/Temple/Meshy_AI_Halo_in_the_Green_Cor_0713113844_texture_metallic.png";
            string roughnessPath = "Assets/Models/Temple/Meshy_AI_Halo_in_the_Green_Cor_0713113844_texture_roughness.png";
            string matPath = "Assets/Models/Temple/M_Temple.mat";

            // 1. Configure Normal Map Import Settings
            TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            // 2. Create and setup Material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            bool isNewMaterial = false;
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                isNewMaterial = true;
            }

            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            Texture2D emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);

            if (mat.shader.name.Contains("Universal Render Pipeline") || mat.shader.name.Contains("URP"))
            {
                if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (emissionTex != null)
                {
                    mat.SetTexture("_EmissionMap", emissionTex);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                }
                if (metallicTex != null) mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Smoothness", 0.5f);
                mat.SetFloat("_Metallic", 0.2f);
            }
            else
            {
                if (baseTex != null) mat.SetTexture("_MainTex", baseTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (emissionTex != null)
                {
                    mat.SetTexture("_EmissionMap", emissionTex);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                }
                if (metallicTex != null) mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Glossiness", 0.5f);
                mat.SetFloat("_Metallic", 0.2f);
            }

            if (isNewMaterial)
            {
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();

            // 3. Load FBX and Place in Scene
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxPrefab == null)
            {
                Debug.LogError("Temple FBX prefab not found at: " + fbxPath);
                return;
            }

            GameObject temple = GameObject.Find("AncientTemple");
            if (temple != null)
            {
                Object.DestroyImmediate(temple);
            }

            temple = PrefabUtility.InstantiatePrefab(fbxPrefab) as GameObject;
            if (temple == null) return;

            temple.name = "AncientTemple";

            // Position behind the house (House is at X=0, Z=15; place temple at X=0, Z=250)
            float targetZ = 250f;
            float height = GetTerrainHeight(0f, targetZ);
            temple.transform.position = new Vector3(0f, height, targetZ);
            temple.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            // Apply material to all Renderers
            MeshRenderer[] renderers = temple.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer r in renderers)
            {
                r.sharedMaterial = mat;
            }

            // Calculate real bounds by multiplying the raw mesh bounds by the local scale of the child transforms.
            // This bypasses the un-updated world matrices in the editor frame.
            Bounds combinedBounds = new Bounds();
            bool boundsInitialized = false;
            foreach (var filter in temple.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                
                Bounds localBounds = filter.sharedMesh.bounds;
                Vector3 childScale = filter.transform.localScale;
                Transform current = filter.transform;
                while (current != null && current != temple.transform)
                {
                    current = current.parent;
                    if (current != null && current != temple.transform)
                    {
                        childScale = Vector3.Scale(childScale, current.localScale);
                    }
                }

                Vector3 scaledSize = Vector3.Scale(localBounds.size, childScale);
                Vector3 scaledCenter = Vector3.Scale(localBounds.center, childScale);
                Bounds scaledBounds = new Bounds(scaledCenter, scaledSize);

                if (!boundsInitialized)
                {
                    combinedBounds = scaledBounds;
                    boundsInitialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(scaledBounds);
                }
            }

            // Calculate auto-scale to target 500m height for a truly colossal and majestic look!
            float targetHeight = 35.0f;
            float currentHeight = boundsInitialized ? Mathf.Max(combinedBounds.size.x, Mathf.Max(combinedBounds.size.y, combinedBounds.size.z)) : 0.015f;
            if (currentHeight > 0.001f)
            {
                float scaleVal = targetHeight / currentHeight;
                temple.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
            }
            else
            {
                temple.transform.localScale = new Vector3(2300f, 2300f, 2300f);
            }

            // Clear any trees near temple position (X=0, Z=250) within 40m radius
            GameObject forest = GameObject.Find("Forest");
            if (forest != null)
            {
                System.Collections.Generic.List<GameObject> treesToDelete = new System.Collections.Generic.List<GameObject>();
                foreach (Transform tree in forest.transform)
                {
                    float dist = Mathf.Sqrt(tree.position.x * tree.position.x + (tree.position.z - targetZ) * (tree.position.z - targetZ));
                    if (dist < 40f)
                    {
                        treesToDelete.Add(tree.gameObject);
                    }
                }
                foreach (var tree in treesToDelete)
                {
                    Object.DestroyImmediate(tree);
                }
                if (treesToDelete.Count > 0)
                {
                    Debug.Log("Antigravity: Cleared " + treesToDelete.Count + " trees around the temple clearing.");
                }
            }

            // Add MeshColliders for collision
            foreach (var filter in temple.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.gameObject.GetComponent<MeshCollider>() == null)
                {
                    filter.gameObject.AddComponent<MeshCollider>();
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Antigravity: AncientTemple successfully imported, materialized, scaled to 120m height, colliders added, and placed at (0, " + height + ", " + targetZ + ")!");
        }

        private static void AutoSetupSchool()
        {
            EditorApplication.delayCall -= AutoSetupSchool;
            GameObject school = GameObject.Find("AbandonedSchool");
            if (school != null)
            {
                // Check if the school is positioned approximately at target coordinates (-200, 40)
                if (Mathf.Abs(school.transform.position.x - (-200f)) > 5f || Mathf.Abs(school.transform.position.z - 40f) > 5f)
                {
                    Object.DestroyImmediate(school);
                }
                else
                {
                    return;
                }
            }
            SetupSchool();
        }

        [MenuItem("Tools/Antigravity/Import and Setup School")]
        public static void SetupSchool()
        {
            string fbxPath = "Assets/Models/School/Meshy_AI_Ivyclad_Courtyard_Rui_0714013114_texture.fbx";
            string baseTexPath = "Assets/Models/School/Meshy_AI_Ivyclad_Courtyard_Rui_0714013114_texture.png";
            string normalPath = "Assets/Models/School/Meshy_AI_Ivyclad_Courtyard_Rui_0714013114_texture_normal.png";
            string emissionPath = "Assets/Models/School/Meshy_AI_Ivyclad_Courtyard_Rui_0714013114_texture_emission.png";
            string metallicPath = "Assets/Models/School/Meshy_AI_Ivyclad_Courtyard_Rui_0714013114_texture_metallic.png";
            string roughnessPath = "Assets/Models/School/Meshy_AI_Ivyclad_Courtyard_Rui_0714013114_texture_roughness.png";
            string matPath = "Assets/Models/School/M_School.mat";

            // 1. Configure Normal Map Import Settings
            TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            // 2. Create and setup Material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            bool isNewMaterial = false;
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                isNewMaterial = true;
            }

            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            Texture2D emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);

            if (mat.shader.name.Contains("Universal Render Pipeline") || mat.shader.name.Contains("URP"))
            {
                if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (emissionTex != null)
                {
                    mat.SetTexture("_EmissionMap", emissionTex);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                }
                if (metallicTex != null) mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Smoothness", 0.5f);
                mat.SetFloat("_Metallic", 0.2f);
            }
            else
            {
                if (baseTex != null) mat.SetTexture("_MainTex", baseTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (emissionTex != null)
                {
                    mat.SetTexture("_EmissionMap", emissionTex);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                }
                if (metallicTex != null) mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Glossiness", 0.5f);
                mat.SetFloat("_Metallic", 0.2f);
            }

            if (isNewMaterial)
            {
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();

            // 3. Load FBX and Place in Scene
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxPrefab == null)
            {
                Debug.LogError("School FBX prefab not found at: " + fbxPath);
                return;
            }

            GameObject school = GameObject.Find("AbandonedSchool");
            if (school != null)
            {
                Object.DestroyImmediate(school);
            }

            school = PrefabUtility.InstantiatePrefab(fbxPrefab) as GameObject;
            if (school == null) return;

            school.name = "AbandonedSchool";

            // Position to the left of the house (House is at X=0, Z=15; place school at X=-200, Z=40)
            float targetX = -200f;
            float targetZ = 40f;
            float height = GetTerrainHeight(targetX, targetZ);
            school.transform.position = new Vector3(targetX, height, targetZ);
            school.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            // Apply material to all Renderers
            MeshRenderer[] renderers = school.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer r in renderers)
            {
                r.sharedMaterial = mat;
            }

            // Calculate bounds
            Bounds combinedBounds = new Bounds();
            bool boundsInitialized = false;
            foreach (var filter in school.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                
                Bounds localBounds = filter.sharedMesh.bounds;
                Vector3 childScale = filter.transform.localScale;
                Transform current = filter.transform;
                while (current != null && current != school.transform)
                {
                    current = current.parent;
                    if (current != null && current != school.transform)
                    {
                        childScale = Vector3.Scale(childScale, current.localScale);
                    }
                }

                Vector3 scaledSize = Vector3.Scale(localBounds.size, childScale);
                Vector3 scaledCenter = Vector3.Scale(localBounds.center, childScale);
                Bounds scaledBounds = new Bounds(scaledCenter, scaledSize);

                if (!boundsInitialized)
                {
                    combinedBounds = scaledBounds;
                    boundsInitialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(scaledBounds);
                }
            }

            // Calculate auto-scale to target 4800m size for a realistic human-scaled school (scaleVal approx 103x, visual size approx 47m)
            float targetSize = 4800.0f;
            float currentSize = boundsInitialized ? Mathf.Max(combinedBounds.size.x, Mathf.Max(combinedBounds.size.y, combinedBounds.size.z)) : 0.015f;
            if (currentSize > 0.001f)
            {
                float scaleVal = targetSize / currentSize;
                school.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
            }
            else
            {
                school.transform.localScale = new Vector3(103f, 103f, 103f);
            }

            // Add MeshColliders for collision
            foreach (var filter in school.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.gameObject.GetComponent<MeshCollider>() == null)
                {
                    filter.gameObject.AddComponent<MeshCollider>();
                }
            }

            // Clear any trees near school position (X=-200, Z=40) within 45m radius
            GameObject forest = GameObject.Find("Forest");
            if (forest != null)
            {
                System.Collections.Generic.List<GameObject> treesToDelete = new System.Collections.Generic.List<GameObject>();
                foreach (Transform tree in forest.transform)
                {
                    float dist = Mathf.Sqrt((tree.position.x - targetX) * (tree.position.x - targetX) + (tree.position.z - targetZ) * (tree.position.z - targetZ));
                    if (dist < 45f)
                    {
                        treesToDelete.Add(tree.gameObject);
                    }
                }
                foreach (var tree in treesToDelete)
                {
                    Object.DestroyImmediate(tree);
                }
                if (treesToDelete.Count > 0)
                {
                    Debug.Log("Antigravity: Cleared " + treesToDelete.Count + " trees around the school clearing.");
                }
            }

            // Disable fog to let the user inspect the school easily
            RenderSettings.fog = false;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.sceneViewState.showFog = false;
                SceneView.lastActiveSceneView.Repaint();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Antigravity: School successfully imported, materialized, scaled to human size, colliders added, and placed at (" + targetX + ", " + height + ", " + targetZ + ")!");
        }

        private static void AutoSetupBus()
        {
            EditorApplication.delayCall -= AutoSetupBus;
            GameObject bus = GameObject.Find("AbandonedBus");
            if (bus != null)
            {
                if (Mathf.Abs(bus.transform.position.x - (-9f)) > 2f || Mathf.Abs(bus.transform.position.z - 7f) > 2f)
                {
                    Object.DestroyImmediate(bus);
                }
                else
                {
                    return;
                }
            }
            SetupBus();
        }

        [MenuItem("Tools/Antigravity/Import and Setup Bus")]
        public static void SetupBus()
        {
            string fbxPath = "Assets/Models/Bus/Meshy_AI_Abandoned_Bus_in_an_O_0714102856_texture.fbx";
            string baseTexPath = "Assets/Models/Bus/Meshy_AI_Abandoned_Bus_in_an_O_0714102856_texture.png";
            string normalPath = "Assets/Models/Bus/Meshy_AI_Abandoned_Bus_in_an_O_0714102856_texture_normal.png";
            string emissionPath = "Assets/Models/Bus/Meshy_AI_Abandoned_Bus_in_an_O_0714102856_texture_emission.png";
            string metallicPath = "Assets/Models/Bus/Meshy_AI_Abandoned_Bus_in_an_O_0714102856_texture_metallic.png";
            string roughnessPath = "Assets/Models/Bus/Meshy_AI_Abandoned_Bus_in_an_O_0714102856_texture_roughness.png";
            string matPath = "Assets/Models/Bus/M_Bus.mat";

            // 1. Configure Normal Map Import Settings
            TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            // 2. Create and setup Material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            bool isNewMaterial = false;
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                isNewMaterial = true;
            }

            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            Texture2D emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);

            if (mat.shader.name.Contains("Universal Render Pipeline") || mat.shader.name.Contains("URP"))
            {
                if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (emissionTex != null)
                {
                    mat.SetTexture("_EmissionMap", emissionTex);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                }
                if (metallicTex != null) mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Smoothness", 0.5f);
                mat.SetFloat("_Metallic", 0.2f);
            }
            else
            {
                if (baseTex != null) mat.SetTexture("_MainTex", baseTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (emissionTex != null)
                {
                    mat.SetTexture("_EmissionMap", emissionTex);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                }
                if (metallicTex != null) mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Glossiness", 0.5f);
                mat.SetFloat("_Metallic", 0.2f);
            }

            if (isNewMaterial)
            {
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();

            // 3. Load FBX and Place in Scene
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxPrefab == null)
            {
                Debug.LogError("Bus FBX prefab not found at: " + fbxPath);
                return;
            }

            GameObject bus = GameObject.Find("AbandonedBus");
            if (bus != null)
            {
                Object.DestroyImmediate(bus);
            }

            bus = PrefabUtility.InstantiatePrefab(fbxPrefab) as GameObject;
            if (bus == null) return;

            bus.name = "AbandonedBus";

            // Position in front yard to the left of the house path
            float targetX = -9.0f;
            float targetZ = 7.0f;
            float height = GetTerrainHeight(targetX, targetZ);
            bus.transform.position = new Vector3(targetX, height, targetZ);
            bus.transform.rotation = Quaternion.Euler(-90f, 0f, 110f);

            // Apply material to all Renderers
            MeshRenderer[] renderers = bus.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer r in renderers)
            {
                r.sharedMaterial = mat;
            }

            // Calculate bounds
            Bounds combinedBounds = new Bounds();
            bool boundsInitialized = false;
            foreach (var filter in bus.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                
                Bounds localBounds = filter.sharedMesh.bounds;
                Vector3 childScale = filter.transform.localScale;
                Transform current = filter.transform;
                while (current != null && current != bus.transform)
                {
                    current = current.parent;
                    if (current != null && current != bus.transform)
                    {
                        childScale = Vector3.Scale(childScale, current.localScale);
                    }
                }

                Vector3 scaledSize = Vector3.Scale(localBounds.size, childScale);
                Vector3 scaledCenter = Vector3.Scale(localBounds.center, childScale);
                Bounds scaledBounds = new Bounds(scaledCenter, scaledSize);

                if (!boundsInitialized)
                {
                    combinedBounds = scaledBounds;
                    boundsInitialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(scaledBounds);
                }
            }

            // Calculate auto-scale to target 11.0m size
            float targetSize = 11.0f;
            float currentSize = boundsInitialized ? Mathf.Max(combinedBounds.size.x, Mathf.Max(combinedBounds.size.y, combinedBounds.size.z)) : 0.015f;
            if (currentSize > 0.001f)
            {
                float scaleVal = targetSize / currentSize;
                bus.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
            }
            else
            {
                bus.transform.localScale = new Vector3(1f, 1f, 1f);
            }

            // Add MeshColliders for collision
            foreach (var filter in bus.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.gameObject.GetComponent<MeshCollider>() == null)
                {
                    filter.gameObject.AddComponent<MeshCollider>();
                }
            }

            // Clear any trees near bus position within 6m radius
            GameObject forest = GameObject.Find("Forest");
            if (forest != null)
            {
                System.Collections.Generic.List<GameObject> treesToDelete = new System.Collections.Generic.List<GameObject>();
                foreach (Transform tree in forest.transform)
                {
                    float dist = Mathf.Sqrt((tree.position.x - targetX) * (tree.position.x - targetX) + (tree.position.z - targetZ) * (tree.position.z - targetZ));
                    if (dist < 6f)
                    {
                        treesToDelete.Add(tree.gameObject);
                    }
                }
                foreach (var tree in treesToDelete)
                {
                    Object.DestroyImmediate(tree);
                }
                if (treesToDelete.Count > 0)
                {
                    Debug.Log("Antigravity: Cleared " + treesToDelete.Count + " trees around the bus.");
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Antigravity: Bus successfully imported, materialized, scaled to " + targetSize + "m length, colliders added, and placed at (" + targetX + ", " + height + ", " + targetZ + ")!");
        }

        private static void AutoSetupPicture()
        {
            EditorApplication.delayCall -= AutoSetupPicture;
            GameObject picture = GameObject.Find("CreepyPicture");
            if (picture != null)
            {
                if (Mathf.Abs(picture.transform.position.x - (-2.8f)) > 1f || Mathf.Abs(picture.transform.position.z - 11.2f) > 1f)
                {
                    Object.DestroyImmediate(picture);
                }
                else
                {
                    return;
                }
            }
            SetupPicture();
        }

        [MenuItem("Tools/Antigravity/Import and Setup Picture")]
        public static void SetupPicture()
        {
            string fbxPath = "Assets/Models/Picture/Meshy_AI_Heat_Studio_Framed_Po_0714102826_texture.fbx";
            string baseTexPath = "Assets/Models/Picture/Meshy_AI_Heat_Studio_Framed_Po_0714102826_texture.png";
            string normalPath = "Assets/Models/Picture/Meshy_AI_Heat_Studio_Framed_Po_0714102826_texture_normal.png";
            string emissionPath = "Assets/Models/Picture/Meshy_AI_Heat_Studio_Framed_Po_0714102826_texture_emission.png";
            string metallicPath = "Assets/Models/Picture/Meshy_AI_Heat_Studio_Framed_Po_0714102826_texture_metallic.png";
            string roughnessPath = "Assets/Models/Picture/Meshy_AI_Heat_Studio_Framed_Po_0714102826_texture_roughness.png";
            string matPath = "Assets/Models/Picture/M_Picture.mat";

            // 1. Configure Normal Map Import Settings
            TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            // 2. Create and setup Material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            bool isNewMaterial = false;
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                isNewMaterial = true;
            }

            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            Texture2D emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);

            if (mat.shader.name.Contains("Universal Render Pipeline") || mat.shader.name.Contains("URP"))
            {
                if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (emissionTex != null)
                {
                    mat.SetTexture("_EmissionMap", emissionTex);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                }
                if (metallicTex != null) mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Smoothness", 0.5f);
                mat.SetFloat("_Metallic", 0.2f);
            }
            else
            {
                if (baseTex != null) mat.SetTexture("_MainTex", baseTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                if (emissionTex != null)
                {
                    mat.SetTexture("_EmissionMap", emissionTex);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                }
                if (metallicTex != null) mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Glossiness", 0.5f);
                mat.SetFloat("_Metallic", 0.2f);
            }

            if (isNewMaterial)
            {
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();

            // 3. Load FBX and Place in Scene
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxPrefab == null)
            {
                Debug.LogError("Picture FBX prefab not found at: " + fbxPath);
                return;
            }

            GameObject picture = GameObject.Find("CreepyPicture");
            if (picture != null)
            {
                Object.DestroyImmediate(picture);
            }

            picture = PrefabUtility.InstantiatePrefab(fbxPrefab) as GameObject;
            if (picture == null) return;

            picture.name = "CreepyPicture";

            // Position in front of the house, hanging/leaning on the front wall
            float targetX = -2.8f;
            float targetZ = 11.2f;
            float height = GetTerrainHeight(targetX, targetZ);
            picture.transform.position = new Vector3(targetX, height + 1.2f, targetZ);
            picture.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            // Apply material to all Renderers
            MeshRenderer[] renderers = picture.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer r in renderers)
            {
                r.sharedMaterial = mat;
            }

            // Calculate bounds
            Bounds combinedBounds = new Bounds();
            bool boundsInitialized = false;
            foreach (var filter in picture.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                
                Bounds localBounds = filter.sharedMesh.bounds;
                Vector3 childScale = filter.transform.localScale;
                Transform current = filter.transform;
                while (current != null && current != picture.transform)
                {
                    current = current.parent;
                    if (current != null && current != picture.transform)
                    {
                        childScale = Vector3.Scale(childScale, current.localScale);
                    }
                }

                Vector3 scaledSize = Vector3.Scale(localBounds.size, childScale);
                Vector3 scaledCenter = Vector3.Scale(localBounds.center, childScale);
                Bounds scaledBounds = new Bounds(scaledCenter, scaledSize);

                if (!boundsInitialized)
                {
                    combinedBounds = scaledBounds;
                    boundsInitialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(scaledBounds);
                }
            }

            // Calculate auto-scale to target 2.0m height/width
            float targetSize = 2.0f;
            float currentSize = boundsInitialized ? Mathf.Max(combinedBounds.size.x, Mathf.Max(combinedBounds.size.y, combinedBounds.size.z)) : 0.015f;
            if (currentSize > 0.001f)
            {
                float scaleVal = targetSize / currentSize;
                picture.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
            }
            else
            {
                picture.transform.localScale = new Vector3(1f, 1f, 1f);
            }

            // Add MeshColliders for collision
            foreach (var filter in picture.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.gameObject.GetComponent<MeshCollider>() == null)
                {
                    filter.gameObject.AddComponent<MeshCollider>();
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Antigravity: Picture successfully imported, materialized, scaled to " + targetSize + "m, colliders added, and placed at (" + targetX + ", " + (height + 1.2f) + ", " + targetZ + ")!");
        }

        [MenuItem("Tools/Antigravity/Toggle Fog")]
        public static void ToggleFog()
        {
            RenderSettings.fog = !RenderSettings.fog;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.sceneViewState.showFog = RenderSettings.fog;
                SceneView.lastActiveSceneView.Repaint();
            }
            Debug.Log("Antigravity: Fog is now " + (RenderSettings.fog ? "ENABLED" : "DISABLED"));
        }

        [MenuItem("Tools/Antigravity/Inspect Ground Textures")]
        public static void InspectGroundTextures()
        {
            for (int i = 1; i <= 14; i++)
            {
                string path = "Assets/ThirdParty/ADG_Textures/ground_vol1/ground" + i + "/ground" + i + "_Diffuse.tga";
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    bool wasReadable = importer != null && importer.isReadable;
                    if (importer != null && !wasReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }
                    
                    Color[] pixels = tex.GetPixels();
                    float r = 0, g = 0, b = 0;
                    foreach (Color c in pixels)
                    {
                        r += c.r;
                        g += c.g;
                        b += c.b;
                    }
                    r /= pixels.Length;
                    g /= pixels.Length;
                    b /= pixels.Length;
                    Debug.Log("Ground " + i + ": R=" + r + " G=" + g + " B=" + b + " - Path: " + path);
                }
            }
        }
    }
}
