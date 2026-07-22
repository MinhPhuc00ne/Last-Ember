using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Antigravity.Editor
{
    [InitializeOnLoad]
    public static class SetupLakeScene
    {
        static SetupLakeScene()
        {
            EditorApplication.delayCall += AutoRun;
        }

        private static void AutoRun()
        {
            EditorApplication.delayCall -= AutoRun;

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "LakeScene") return;

            if (GameObject.Find("LakeProceduralTerrain") == null || GameObject.Find("LakeWater") == null)
            {
                Debug.Log("Antigravity: Auto-generating Lake Map...");
                BuildLakeInActiveScene(cleanNonLakeObjects: true);
            }
        }

        [MenuItem("Tools/Antigravity/Setup Lake Scene")]
        public static void Setup()
        {
            string scenePath = "Assets/_Project/Scenes/LakeScene.unity";
            Scene lakeScene;

            // Check if scene exists, if not create it
            if (!System.IO.File.Exists(scenePath))
            {
                string dir = System.IO.Path.GetDirectoryName(scenePath);
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                lakeScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(lakeScene, scenePath);
            }
            else
            {
                lakeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            BuildLakeInActiveScene(cleanNonLakeObjects: true);
        }

        public static void BuildLakeInActiveScene(bool cleanNonLakeObjects = false)
        {
            Debug.Log("Antigravity: Setting up procedural lake scene...");

            if (cleanNonLakeObjects)
            {
                // Cleanup any non-lake scene objects to prevent terrain overlap when running stand-alone
                string[] objectsToClean = new string[]
                {
                    "ProceduralTerrain", "GrassField", "House", "ForestFence", "Forest",
                    "IntersectionStreetLight", "HorrorEnvironment", "ShadowFigure",
                    "AncientTemple", "SchoolBuilding", "AbandonedSchool", "AbandonedBus",
                    "CreepyPicture", "MonsterEnemy", "EnvironmentLighting",
                    "Folder 1 - MainScene", "Folder 2 - LakeScene", "[Folder_1_MainScene]", "[Folder_2_LakeScene]",
                    "[Island_1_MainForest]", "[Island_2_LakeRegion]"
                };

                foreach (string name in objectsToClean)
                {
                    GameObject obj = GameObject.Find(name);
                    while (obj != null)
                    {
                        Object.DestroyImmediate(obj);
                        obj = GameObject.Find(name);
                    }
                }
            }

            // 1. Generate Procedural Terrain Mesh
            CreateProceduralTerrain();

            // 2. Create Lake Water
            CreateLakeWater();

            // 3. Create Wooden Pier/Dock
            CreateWoodenPier();

            // 4. Create Campsite
            CreateCampsite();

            // 5. Scatter Shoreline Rocks
            ScatterRocks();

            // 6. Scatter Forest Trees
            ScatterTrees();

            // 7. Cleanup Grass and Flowers if exist
            ScatterGrassAndFlowers();

            // 8. Setup Lighting & Atmosphere (Sunset Golden Hour)
            SetupLightingAndAtmosphere();

            // 9. Setup Player
            SetupPlayer();

            // Save scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Antigravity: Procedural Lake Scene Setup Completed Successfully!");
        }

        private static float GetTerrainHeight(float xPos, float zPos)
        {
            float dist = Mathf.Sqrt(xPos * xPos + zPos * zPos);
            float height = 0f;

            // Lake basin and Shoreline
            if (dist < 180f)
            {
                // Central deep lake basin (Y = -15m to -3m)
                float t = dist / 180f;
                height = Mathf.Lerp(-16f, -3f, t * t);
            }
            else if (dist < 260f)
            {
                // Shoreline / Beach sloping up to Y = 2.0f
                float t = (dist - 180f) / 80f;
                height = Mathf.Lerp(-3f, 2.0f, Mathf.SmoothStep(0f, 1f, t));
            }
            else if (dist < 340f)
            {
                // Grassy valley / shoreline zone (Y = 2.0f to 4.0f)
                float t = (dist - 260f) / 80f;
                height = Mathf.Lerp(2.0f, 4.0f, Mathf.SmoothStep(0f, 1f, t));

                // Add small rolling hills
                float bump1 = Mathf.PerlinNoise(xPos * 0.015f + 50f, zPos * 0.015f + 50f) * 2.5f;
                float bump2 = Mathf.PerlinNoise(xPos * 0.06f + 120f, zPos * 0.06f + 120f) * 0.8f;
                height += bump1 + bump2;
            }
            else
            {
                // Mountain region
                float t = (dist - 340f) / 220f;
                t = Mathf.Clamp01(t);

                float mtNoise1 = Mathf.PerlinNoise(xPos * 0.004f + 25f, zPos * 0.004f + 25f) * 95f;
                float mtNoise2 = Mathf.PerlinNoise(xPos * 0.012f + 85f, zPos * 0.012f + 85f) * 35f;
                float mtNoise3 = Mathf.PerlinNoise(xPos * 0.035f + 195f, zPos * 0.035f + 195f) * 8f;

                height = 4.0f + Mathf.SmoothStep(0f, 1f, t) * (20f + mtNoise1 + mtNoise2 + mtNoise3);
            }

            // Flatten a small zone for Player Spawn, Campsite, and Pier root:
            // Pier root is at (0, 245) - just on the northern shore of the lake.
            Vector2 flatCenter = new Vector2(0f, 245f);
            float distToFlat = Vector2.Distance(new Vector2(xPos, zPos), flatCenter);
            if (distToFlat < 25f)
            {
                float flatT = Mathf.Clamp01((distToFlat - 8f) / 17f);
                height = Mathf.Lerp(2.0f, height, Mathf.SmoothStep(0f, 1f, flatT));
            }

            return height;
        }

        private static void CreateProceduralTerrain()
        {
            // Remove old terrain if exists
            GameObject oldTerrain = GameObject.Find("LakeProceduralTerrain");
            if (oldTerrain != null) Object.DestroyImmediate(oldTerrain);

            GameObject terrain = new GameObject("LakeProceduralTerrain");
            terrain.transform.position = Vector3.zero;
            terrain.transform.rotation = Quaternion.identity;
            terrain.transform.localScale = Vector3.one;

            MeshFilter meshFilter = terrain.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = terrain.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = terrain.AddComponent<MeshCollider>();

            Mesh mesh = new Mesh();
            mesh.name = "Lake Scene Terrain Mesh";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            int width = 500;
            int depth = 500;
            float spacing = 3.0f; // Total size = 1500m x 1500m
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

                    // Compute normal for texture blending
                    float eps = 0.5f;
                    float hL = GetTerrainHeight(xPos - eps, zPos);
                    float hR = GetTerrainHeight(xPos + eps, zPos);
                    float hD = GetTerrainHeight(xPos, zPos - eps);
                    float hU = GetTerrainHeight(xPos, zPos + eps);
                    Vector3 normal = new Vector3(hL - hR, 2f * eps, hD - hU).normalized;

                    float steepness = 1.0f - normal.y; // 0 = flat, 1 = vertical
                    
                    float rockWeight = 0f;
                    if (steepness > 0.28f)
                    {
                        rockWeight = Mathf.Clamp01((steepness - 0.28f) / 0.15f);
                    }

                    float sandWeight = 0f;
                    if (height < 2.2f)
                    {
                        sandWeight = Mathf.Clamp01((2.2f - height) / 4.5f);
                    }

                    // Vertex Color channels: R = Sand/Dirt, G = Rock, B = Grass (default)
                    colors[idx] = new Color(sandWeight * (1f - rockWeight), rockWeight, 0f, 1f);
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

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            // Reuse project's GroundMaterial
            Material groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GroundMaterial.mat");
            if (groundMat != null)
            {
                meshRenderer.sharedMaterial = groundMat;
            }
            else
            {
                Debug.LogWarning("Antigravity: GroundMaterial.mat not found. Creating a fallback material.");
                Material fallbackMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                fallbackMat.color = new Color(0.2f, 0.4f, 0.2f);
                meshRenderer.sharedMaterial = fallbackMat;
            }
        }

        private static void CreateLakeWater()
        {
            GameObject water = GameObject.Find("LakeWater");
            if (water != null) Object.DestroyImmediate(water);

            water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "LakeWater";
            
            // Placed at Y = 1.5f (water level)
            water.transform.position = new Vector3(0f, 1.5f, 0f);
            water.transform.rotation = Quaternion.identity;
            
            // Unity Plane default size is 10x10, so 65x65 is 650m x 650m (covers the 500m diameter lake)
            water.transform.localScale = new Vector3(65f, 1f, 65f);

            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/WaterMaterial.mat");
            if (waterMat != null)
            {
                water.GetComponent<Renderer>().sharedMaterial = waterMat;
            }

            MeshCollider waterCol = water.GetComponent<MeshCollider>();
            if (waterCol != null)
            {
                waterCol.isTrigger = true;
            }
        }

        private static void CreateWoodenPier()
        {
            GameObject pierFolder = GameObject.Find("WoodenPier");
            if (pierFolder != null) Object.DestroyImmediate(pierFolder);
            pierFolder = new GameObject("WoodenPier");

            string bridgePrefabPath = "Assets/Flooded_Grounds/Prefabs/Buildings/Bridge/BLD_Bridge_B.prefab";
            GameObject bridgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bridgePrefabPath);
            if (bridgePrefab != null)
            {
                // Aligned along Z-axis extending Southwards
                // Bridge length is approx 12.0 meters. Let's place 3 segments end-to-end.
                float segmentLength = 12.0f;
                float startZ = 246.0f;
                for (int i = 0; i < 3; i++)
                {
                    GameObject segment = PrefabUtility.InstantiatePrefab(bridgePrefab, pierFolder.transform) as GameObject;
                    segment.name = "Pier_Segment_" + i;
                    
                    // Position at Y = 1.6f (slightly above water Y = 1.5f)
                    segment.transform.position = new Vector3(0f, 1.6f, startZ - i * segmentLength);
                    segment.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Rotate to align along Z axis
                    segment.transform.localScale = new Vector3(1f, 1f, 1f);
                    
                    // Ensure it has colliders
                    foreach (var mr in segment.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (mr.gameObject.GetComponent<Collider>() == null)
                        {
                            mr.gameObject.AddComponent<MeshCollider>();
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Antigravity: BLD_Bridge_B.prefab not found, building primitive pier.");
                // Primitive pier fallback
                for (int i = 0; i < 3; i++)
                {
                    GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plank.name = "Pier_Plank_" + i;
                    plank.transform.SetParent(pierFolder.transform);
                    plank.transform.position = new Vector3(0f, 1.6f, 246.0f - i * 12.0f);
                    plank.transform.localScale = new Vector3(3f, 0.2f, 12f);
                    
                    Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/TrunkMaterial.mat");
                    if (woodMat != null) plank.GetComponent<Renderer>().sharedMaterial = woodMat;
                }
            }
        }

        private static void CreateCampsite()
        {
            GameObject camp = GameObject.Find("Campsite");
            if (camp != null) Object.DestroyImmediate(camp);
            camp = new GameObject("Campsite");
            camp.transform.position = new Vector3(15f, 2.0f, 245f);

            // Campfire
            string campfirePath = "Assets/PolygonPilots/Campfire/Prefabs/CampFire.prefab";
            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(campfirePath);
            if (firePrefab != null)
            {
                GameObject fire = PrefabUtility.InstantiatePrefab(firePrefab, camp.transform) as GameObject;
                fire.name = "Campfire";
                fire.transform.localPosition = Vector3.zero;
                fire.transform.localScale = Vector3.one * 1.5f;

                GameObject flameLight = new GameObject("EmberLight");
                flameLight.transform.SetParent(fire.transform, false);
                flameLight.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                Light lt = flameLight.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 12f;
                lt.intensity = 1.6f;
                lt.color = new Color(0.95f, 0.45f, 0.1f);
                lt.shadows = LightShadows.Soft;
                
                // Add LightFlicker script if it exists
                System.Type flickerType = System.Type.GetType("Antigravity.LightFlicker") ?? System.Type.GetType("LightFlicker");
                if (flickerType != null)
                {
                    flameLight.AddComponent(flickerType);
                }
            }
            else
            {
                GameObject fireFallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fireFallback.name = "Campfire";
                fireFallback.transform.SetParent(camp.transform);
                fireFallback.transform.localPosition = Vector3.zero;
                fireFallback.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
            }

            // Tents
            string tentPrefabPath = "Assets/PolygonPilots/Campfire/Prefabs/Tent.prefab";
            GameObject tentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tentPrefabPath);

            Vector3[] tentPositions = new Vector3[]
            {
                new Vector3(-4f, 0f, 3.5f),   // Tent 1
                new Vector3(4.5f, 0f, 2.5f)    // Tent 2
            };
            float[] tentRotations = new float[] { 135f, 225f };

            for (int i = 0; i < 2; i++)
            {
                float tX = camp.transform.position.x + tentPositions[i].x;
                float tZ = camp.transform.position.z + tentPositions[i].z;
                float tY = GetTerrainHeight(tX, tZ);

                if (tentPrefab != null)
                {
                    GameObject tent = PrefabUtility.InstantiatePrefab(tentPrefab, camp.transform) as GameObject;
                    tent.name = "Tent_" + (i + 1);
                    tent.transform.position = new Vector3(tX, tY, tZ);
                    tent.transform.rotation = Quaternion.Euler(0f, tentRotations[i], 0f);
                    tent.transform.localScale = Vector3.one * 1.3f;
                }
                else
                {
                    GameObject tentFallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tentFallback.name = "Tent_" + (i + 1);
                    tentFallback.transform.SetParent(camp.transform);
                    tentFallback.transform.position = new Vector3(tX, tY + 0.9f, tZ);
                    tentFallback.transform.rotation = Quaternion.Euler(0f, tentRotations[i], 0f);
                    tentFallback.transform.localScale = new Vector3(2.2f, 1.8f, 2.8f);
                }
            }

            // Tarp shelter
            string tarpPrefabPath = "Assets/ArtisticMechanics/Bush-Craft Extension Pack/Prefabs/Stealth/Tarp-forest_tent_s.prefab";
            GameObject tarpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tarpPrefabPath);
            if (tarpPrefab != null)
            {
                float tarpX = camp.transform.position.x;
                float tarpZ = camp.transform.position.z - 5f;
                float tarpY = GetTerrainHeight(tarpX, tarpZ);

                GameObject tarp = PrefabUtility.InstantiatePrefab(tarpPrefab, camp.transform) as GameObject;
                tarp.name = "BushcraftTarp";
                tarp.transform.position = new Vector3(tarpX, tarpY, tarpZ);
                tarp.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                tarp.transform.localScale = Vector3.one * 1.2f;
            }
        }

        private static void ScatterRocks()
        {
            GameObject rocksFolder = GameObject.Find("ShorelineRocks");
            if (rocksFolder != null) Object.DestroyImmediate(rocksFolder);
            rocksFolder = new GameObject("ShorelineRocks");

            string[] rockPaths = new string[]
            {
                "Assets/Flooded_Grounds/Prefabs/Nature/Rocks/Rock_A.prefab",
                "Assets/Flooded_Grounds/Prefabs/Nature/Rocks/Rock_B.prefab",
                "Assets/Flooded_Grounds/Prefabs/Nature/Rocks/CobbleRock_A.prefab",
                "Assets/Flooded_Grounds/Prefabs/Nature/Rocks/CobbleRock_B.prefab",
                "Assets/Flooded_Grounds/Prefabs/Nature/Rocks/CobbleRock_C.prefab"
            };

            System.Collections.Generic.List<GameObject> rockPrefabs = new System.Collections.Generic.List<GameObject>();
            foreach (var path in rockPaths)
            {
                GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (rock != null) rockPrefabs.Add(rock);
            }

            if (rockPrefabs.Count > 0)
            {
                int rockCount = 70;
                for (int i = 0; i < rockCount; i++)
                {
                    float angle = Random.Range(0f, 2f * Mathf.PI);
                    float radius = Random.Range(249f, 256f);
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;

                    // Keep player spawn/pier clear
                    if (Mathf.Abs(x) < 25f && z > 215f) continue;

                    float y = GetTerrainHeight(x, z);

                    GameObject chosenRock = rockPrefabs[Random.Range(0, rockPrefabs.Count)];
                    GameObject rockObj = PrefabUtility.InstantiatePrefab(chosenRock, rocksFolder.transform) as GameObject;
                    rockObj.name = "ShoreRock_" + i;
                    rockObj.transform.position = new Vector3(x, y - 0.15f, z);
                    rockObj.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                    float scale = Random.Range(0.6f, 1.9f);
                    rockObj.transform.localScale = new Vector3(scale, scale, scale);

                    // Add colliders if missing
                    foreach (var filter in rockObj.GetComponentsInChildren<MeshFilter>())
                    {
                        if (filter.gameObject.GetComponent<MeshCollider>() == null)
                        {
                            filter.gameObject.AddComponent<MeshCollider>();
                        }
                    }
                }
            }
        }

        private static void ScatterTrees()
        {
            GameObject forestFolder = GameObject.Find("LakeForest");
            if (forestFolder != null) Object.DestroyImmediate(forestFolder);
            forestFolder = new GameObject("LakeForest");

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

            if (pinePrefabs.Count > 0)
            {
                int treeCount = 2800;
                float fieldSize = 1300f;
                for (int i = 0; i < treeCount; i++)
                {
                    float x = Random.Range(-fieldSize / 2f, fieldSize / 2f);
                    float z = Random.Range(-fieldSize / 2f, fieldSize / 2f);

                    float dist = Mathf.Sqrt(x * x + z * z);
                    // Leave lake and shoreline clear of trees
                    if (dist < 265f) continue;
                    
                    // Exclude player spawn area
                    if (Mathf.Abs(x) < 30f && z > 210f && z < 270f) continue;

                    float y = GetTerrainHeight(x, z);

                    GameObject chosenPrefab = pinePrefabs[Random.Range(0, pinePrefabs.Count)];
                    GameObject tree = PrefabUtility.InstantiatePrefab(chosenPrefab, forestFolder.transform) as GameObject;
                    tree.name = "ForestTree_" + i;
                    tree.transform.position = new Vector3(x, y, z);
                    float scale = Random.Range(0.8f, 1.6f);
                    tree.transform.localScale = new Vector3(scale, scale, scale);
                    tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    // Add colliders if missing
                    foreach (var filter in tree.GetComponentsInChildren<MeshFilter>())
                    {
                        if (filter.gameObject.GetComponent<MeshCollider>() == null)
                        {
                            filter.gameObject.AddComponent<MeshCollider>();
                        }
                    }
                }
            }
        }

        private static void ScatterGrassAndFlowers()
        {
            string[] grassFolders = new string[] { "LakeGrass", "GrassField", "ForestLakeGrass" };
            foreach (string name in grassFolders)
            {
                GameObject folder = GameObject.Find(name);
                while (folder != null)
                {
                    Object.DestroyImmediate(folder);
                    folder = GameObject.Find(name);
                }
            }
        }

        private static void SetupLightingAndAtmosphere()
        {
            // Sunset Directional Light
            Light sunLight = null;
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional && l.gameObject.name == "SunsetLight")
                {
                    sunLight = l;
                    break;
                }
            }

            if (sunLight == null)
            {
                // Disable existing directional lights in the scene setup to avoid conflicts
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        l.gameObject.SetActive(false);
                    }
                }

                GameObject go = new GameObject("SunsetLight");
                sunLight = go.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }

            sunLight.gameObject.SetActive(true);
            sunLight.transform.rotation = Quaternion.Euler(11f, 240f, 0f); // Low angle sunset
            sunLight.intensity = 1.9f;
            sunLight.color = new Color(1.0f, 0.52f, 0.28f); // Sunset gold/orange
            sunLight.shadows = LightShadows.Soft;

            // Render settings for sunset
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.38f, 0.42f); // purple/pink ambient sky
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.25f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.14f, 0.14f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.58f, 0.35f, 0.42f); // sunset foggy atmosphere
            RenderSettings.fogDensity = 0.005f;
        }

        private static void SetupPlayer()
        {
            GameObject player = GameObject.Find("Player");
            if (player != null) Object.DestroyImmediate(player);

            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";

            // Spawn at the beginning of the wooden pier at Y = 2.0f, facing Southwards over the lake
            player.transform.position = new Vector3(0f, 2.8f, 247f); // Capsule height offset
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Face South
            player.transform.localScale = Vector3.one;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = Vector3.zero;

            // Add FirstPersonController
            System.Type fpcType = System.Type.GetType("Antigravity.FirstPersonController") ?? System.Type.GetType("FirstPersonController");
            if (fpcType != null)
            {
                player.AddComponent(fpcType);
            }

            CapsuleCollider capCol = player.GetComponent<CapsuleCollider>();
            if (capCol != null) Object.DestroyImmediate(capCol);

            MeshRenderer playerRenderer = player.GetComponent<MeshRenderer>();
            if (playerRenderer != null) playerRenderer.enabled = false;

            // Setup camera
            GameObject mainCam = GameObject.FindWithTag("MainCamera");
            if (mainCam != null)
            {
                mainCam.transform.SetParent(player.transform);
                mainCam.transform.localPosition = new Vector3(0f, 0.7f, 0f); // Eye height
                mainCam.transform.localRotation = Quaternion.identity;

                // Flashlight setup
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

                flashlight.transform.localPosition = new Vector3(0.2f, -0.22f, 0.35f);
                flashlight.transform.localRotation = Quaternion.identity;

                Light flashLightComp = flashlight.GetComponent<Light>();
                if (flashLightComp == null) flashLightComp = flashlight.AddComponent<Light>();
                flashLightComp.type = LightType.Spot;
                flashLightComp.range = 32f;
                flashLightComp.spotAngle = 42f;
                flashLightComp.innerSpotAngle = 26f;
                flashLightComp.intensity = 2.4f;
                flashLightComp.color = new Color(0.98f, 0.95f, 0.88f);
                flashLightComp.shadows = LightShadows.Soft;

                System.Type fcType = System.Type.GetType("Antigravity.FlashlightController") ?? System.Type.GetType("FlashlightController");
                if (fcType != null && flashlight.GetComponent(fcType) == null)
                {
                    flashlight.AddComponent(fcType);
                }
            }
        }
    }
}
