using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using Antigravity;

namespace Antigravity.Editor
{
    [InitializeOnLoad]
    public static class SetupFirstPersonScene
    {
        static SetupFirstPersonScene()
        {
            // Delay the call to make sure the editor and scene are fully loaded
            EditorApplication.delayCall += AutoSetup;
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
            // Check if player, terrain, grass field, forest, and house already exist
            GameObject player = GameObject.Find("Player");
            GameObject terrain = GameObject.Find("ProceduralTerrain");
            GameObject grass = GameObject.Find("GrassField");
            GameObject forest = GameObject.Find("Forest");
            GameObject house = GameObject.Find("House");
            
            // Check if the existing house is the old primitive cube house
            bool isPrimitiveHouse = house != null && house.transform.Find("Walls") != null;

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

            if (player != null && terrain != null && grass != null && forest != null && house != null && !isPrimitiveHouse && !needsTerrainTexture && !force)
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

            // 1d. Create Procedural Forest
            CreateForest();

            // 2. Create Obstacles aligned to terrain height
            CreateObstacle("Obstacle_Red", new Vector3(8, 1, 8), new Vector3(2, 2, 2), new Color(0.8f, 0.2f, 0.2f));
            CreateObstacle("Obstacle_Blue", new Vector3(-15, 2.5f, 20), new Vector3(3, 5, 3), new Color(0.2f, 0.2f, 0.8f));
            CreateObstacle("Obstacle_Yellow", new Vector3(18, 1.5f, -12), new Vector3(2, 3, 2), new Color(0.8f, 0.8f, 0.2f));
            CreateObstacle("Obstacle_Orange", new Vector3(-10, 0.5f, -15), new Vector3(4, 1, 4), new Color(0.8f, 0.5f, 0.1f));

            // 3. Create Player
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
            }
            player.transform.position = new Vector3(0, 1.01f, 0); // Spawns in the flat valley center
            player.transform.rotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;

            // Add or configure CharacterController
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = player.AddComponent<CharacterController>();
            }
            cc.height = 2.0f;
            cc.radius = 0.5f;
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

            // 4. Setup Camera
            GameObject mainCam = GameObject.FindWithTag("MainCamera");
            if (mainCam != null)
            {
                mainCam.transform.SetParent(player.transform);
                mainCam.transform.localPosition = new Vector3(0, 0.8f, 0); // eye height
                mainCam.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogWarning("Antigravity Setup: Could not find GameObject with tag 'MainCamera' to parent to Player.");
            }

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

            int width = 80;
            int depth = 80;
            float spacing = 2.5f; // Total size = 200x200
            Vector3[] vertices = new Vector3[(width + 1) * (depth + 1)];
            int[] triangles = new int[width * depth * 6];
            Vector2[] uvs = new Vector2[vertices.Length];

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
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            Material groundMat = GetOrCreateMaterial("GroundMaterial", new Color(1f, 1f, 1f));

            // Load realistic forest ground textures from the ADG package
            string diffusePath = "Assets/ThirdParty/ADG_Textures/ground_vol1/ground1/ground1_Diffuse.tga";
            string normalPath = "Assets/ThirdParty/ADG_Textures/ground_vol1/ground1/ground1_Normal.tga";
            
            Texture2D diffuseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            if (diffuseTex != null)
            {
                groundMat.SetTexture("_BaseMap", diffuseTex);
                groundMat.SetTextureScale("_BaseMap", new Vector2(40f, 40f)); // Tiling scale for detailed forest floor
            }
            if (normalTex != null)
            {
                groundMat.SetTexture("_BumpMap", normalTex);
                groundMat.SetTextureScale("_BumpMap", new Vector2(40f, 40f));
                groundMat.EnableKeyword("_NORMALMAP");
            }

            meshRenderer.sharedMaterial = groundMat;
        }

        private static float GetTerrainHeight(float xPos, float zPos)
        {
            float scale = 0.025f;
            float heightMultiplier = 14f;

            // Flatten a large yard of radius 18 meters around the house at (0, 15)
            float dx = xPos - 0f;
            float dz = zPos - 15f;
            float distToHouse = Mathf.Sqrt(dx * dx + dz * dz);

            // Completely flat within 18m (covers both house and player spawn at 0,0)
            // Blends smoothly into the mountains over a 15m transition zone
            float houseFlatten = Mathf.SmoothStep(0f, 1f, (distToHouse - 18f) / 15f);
            if (distToHouse <= 18f)
            {
                houseFlatten = 0f;
            }

            // Multi-octave Perlin Noise for peaks and valleys
            float height = Mathf.PerlinNoise((xPos + 5000f) * scale, (zPos + 5000f) * scale) * heightMultiplier;
            height += Mathf.PerlinNoise((xPos + 3000f) * scale * 2.5f, (zPos + 3000f) * scale * 2.5f) * (heightMultiplier * 0.2f);
            
            return height * houseFlatten;
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

            string texturePath = "Assets/_Project/Textures/GrassTexture.png";
            if (!System.IO.File.Exists(texturePath))
            {
                Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }

                // Draw grass blades with smooth green color variation
                for (int y = 0; y < 64; y++)
                {
                    float factor = (float)y / 64f;
                    DrawBlade(tex, 32, y, factor, 6f, 0.1f);  // Middle blade
                    DrawBlade(tex, 20, y, factor, 4f, -0.25f); // Left blade
                    DrawBlade(tex, 44, y, factor, 5f, 0.2f);   // Right blade
                }

                tex.Apply();
                byte[] bytes = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes(texturePath, bytes);
                AssetDatabase.Refresh();
            }

            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GrassMaterial.mat");
            if (grassMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                grassMat = new Material(shader);
                
                Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                grassMat.SetTexture("_BaseMap", grassTex);
                grassMat.SetFloat("_AlphaClip", 1f);
                grassMat.SetFloat("_Cutoff", 0.35f);
                grassMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off); // Double sided
                grassMat.enableInstancing = true;

                AssetDatabase.CreateAsset(grassMat, "Assets/_Project/Materials/GrassMaterial.mat");
            }

            // Generate Grass Instances in a grid or random fashion
            int grassCount = 1200;
            float fieldSize = 120f;
            
            // Extract mesh from a temp primitive to avoid resource loading issues
            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Mesh quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempQuad);

            for (int i = 0; i < grassCount; i++)
            {
                float x = Random.Range(-fieldSize / 2f, fieldSize / 2f);
                float z = Random.Range(-fieldSize / 2f, fieldSize / 2f);

                // Do not spawn too close to center valley
                float distToCenter = Mathf.Sqrt(x * x + z * z);
                if (distToCenter < 6f) continue;

                float terrainHeight = GetTerrainHeight(x, z);

                // Create a single grass cluster (X-shaped billboards)
                GameObject grassCluster = new GameObject("Grass_" + i);
                grassCluster.transform.SetParent(grassFolder.transform);
                grassCluster.transform.position = new Vector3(x, terrainHeight, z);
                
                float scale = Random.Range(0.8f, 1.4f);
                grassCluster.transform.localScale = new Vector3(scale, scale, scale);

                // Quad 1
                GameObject q1 = new GameObject("Q1");
                q1.transform.SetParent(grassCluster.transform);
                q1.transform.localPosition = new Vector3(0, 0.5f, 0); // Position pivot at bottom
                q1.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 180), 0);
                q1.transform.localScale = Vector3.one;
                MeshFilter mf1 = q1.AddComponent<MeshFilter>();
                mf1.sharedMesh = quadMesh;
                MeshRenderer mr1 = q1.AddComponent<MeshRenderer>();
                mr1.sharedMaterial = grassMat;
                mr1.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // Quad 2 (rotated 90 degrees relative to Quad 1)
                GameObject q2 = new GameObject("Q2");
                q2.transform.SetParent(grassCluster.transform);
                q2.transform.localPosition = new Vector3(0, 0.5f, 0);
                q2.transform.localRotation = q1.transform.localRotation * Quaternion.Euler(0, 90, 0);
                q2.transform.localScale = Vector3.one;
                MeshFilter mf2 = q2.AddComponent<MeshFilter>();
                mf2.sharedMesh = quadMesh;
                MeshRenderer mr2 = q2.AddComponent<MeshRenderer>();
                mr2.sharedMaterial = grassMat;
                mr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static void DrawBlade(Texture2D tex, int baseX, int y, float factor, float startWidth, float lean)
        {
            if (y > 60) return; // Cap height
            
            float width = Mathf.Lerp(startWidth, 0.5f, factor);
            float currentX = baseX + (y * lean);
            
            int startX = Mathf.RoundToInt(currentX - width / 2f);
            int endX = Mathf.RoundToInt(currentX + width / 2f);

            // Green gradient: darker at base, lighter at tips
            Color grassColor = Color.Lerp(new Color(0.08f, 0.35f, 0.08f, 1f), new Color(0.35f, 0.75f, 0.15f, 1f), factor);

            for (int x = startX; x <= endX; x++)
            {
                if (x >= 0 && x < 64)
                {
                    tex.SetPixel(x, y, grassColor);
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
                
                // Align house with terrain height (place it slightly back at Z = 15)
                float terrainHeight = GetTerrainHeight(0f, 15f);
                house.transform.position = new Vector3(0f, terrainHeight, 15f);
                house.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Rotate to face player spawn (0,0,0)
                house.transform.localScale = Vector3.one;
            }
            else
            {
                // Fallback to primitive stylized house if prefab is missing
                Debug.LogWarning("Antigravity: Could not load House Prefab, falling back to primitives.");
                
                house = new GameObject("House");
                house.transform.position = new Vector3(0, 0, 12f);
                house.transform.rotation = Quaternion.identity;
                house.transform.localScale = Vector3.one;

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

            string treePrefabPath = "Assets/ThirdParty/ALP_Assets/Big Oak Tree FREE/Prefabs/OakBigTree01_pr.prefab";
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(treePrefabPath);

            if (treePrefab != null)
            {
                int treeCount = 90; // Detailed trees, 90 is dense and high-performance
                float fieldSize = 140f;

                for (int i = 0; i < treeCount; i++)
                {
                    float x = Random.Range(-fieldSize / 2f, fieldSize / 2f);
                    float z = Random.Range(-fieldSize / 2f, fieldSize / 2f);

                    // Do not spawn trees inside the player spawn zone and around the house
                    float dist = Mathf.Sqrt(x * x + z * z);
                    if (dist < 22f) continue; // Keep space clear around house

                    float terrainHeight = GetTerrainHeight(x, z);

                    GameObject tree = PrefabUtility.InstantiatePrefab(treePrefab) as GameObject;
                    tree.name = "Tree_" + i;
                    tree.transform.SetParent(forest.transform);
                    tree.transform.position = new Vector3(x, terrainHeight, z);

                    float scale = Random.Range(0.6f, 1.1f);
                    tree.transform.localScale = new Vector3(scale, scale, scale);
                    tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
            }
            else
            {
                Debug.LogWarning("Antigravity: Could not load Tree Prefab, falling back to primitives.");
                
                Material trunkMat = GetOrCreateMaterial("TrunkMaterial", new Color(0.42f, 0.26f, 0.1f));
                Material leavesMat = GetOrCreateMaterial("LeavesMaterial", new Color(0.12f, 0.38f, 0.16f));

                int treeCount = 150;
                float fieldSize = 140f;

                for (int i = 0; i < treeCount; i++)
                {
                    float x = Random.Range(-fieldSize / 2f, fieldSize / 2f);
                    float z = Random.Range(-fieldSize / 2f, fieldSize / 2f);

                    float dist = Mathf.Sqrt(x * x + z * z);
                    if (dist < 18f) continue;

                    float terrainHeight = GetTerrainHeight(x, z);

                    GameObject tree = new GameObject("Tree_" + i);
                    tree.transform.SetParent(forest.transform);
                    tree.transform.position = new Vector3(x, terrainHeight, z);

                    float height = Random.Range(3.5f, 5.5f);
                    float leavesScale = Random.Range(2.5f, 3.8f);

                    GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    trunk.name = "Trunk";
                    trunk.transform.SetParent(tree.transform);
                    trunk.transform.localPosition = new Vector3(0f, height / 2f, 0f);
                    trunk.transform.localScale = new Vector3(0.4f, height / 2f, 0.4f);
                    trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

                    GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leaves.name = "Leaves";
                    leaves.transform.SetParent(tree.transform);
                    leaves.transform.localPosition = new Vector3(0f, height + leavesScale / 3f, 0f);
                    leaves.transform.localScale = new Vector3(leavesScale, leavesScale, leavesScale);
                    leaves.GetComponent<Renderer>().sharedMaterial = leavesMat;

                    SphereCollider leavesCol = leaves.GetComponent<SphereCollider>();
                    if (leavesCol != null)
                    {
                        Object.DestroyImmediate(leavesCol);
                    }
                }
            }
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
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                mat = new Material(shader);
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
    }
}
