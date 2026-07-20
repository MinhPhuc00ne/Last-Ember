using UnityEditor;
using UnityEngine;
using System.IO;

namespace Antigravity.Editor
{
    [InitializeOnLoad]
    public static class ImportHelper
    {
        static ImportHelper()
        {
            // Subscribe to import completion to auto-upgrade materials to URP
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
        }

        private static void OnImportPackageCompleted(string packageName)
        {
            Debug.Log($"Antigravity: Package '{packageName}' imported. Reorganizing and upgrading materials to URP...");
            
            // Auto-move imported folders to ThirdParty
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty"))
            {
                AssetDatabase.CreateFolder("Assets", "ThirdParty");
            }

            if (AssetDatabase.IsValidFolder("Assets/ALP_Assets"))
            {
                AssetDatabase.MoveAsset("Assets/ALP_Assets", "Assets/ThirdParty/ALP_Assets");
            }
            if (AssetDatabase.IsValidFolder("Assets/BobGrandmaster"))
            {
                AssetDatabase.MoveAsset("Assets/BobGrandmaster", "Assets/ThirdParty/BobGrandmaster");
            }
            if (AssetDatabase.IsValidFolder("Assets/ADG_Textures"))
            {
                AssetDatabase.MoveAsset("Assets/ADG_Textures", "Assets/ThirdParty/ADG_Textures");
            }
            if (AssetDatabase.IsValidFolder("Assets/RawWoodenFurnitureFree"))
            {
                AssetDatabase.MoveAsset("Assets/RawWoodenFurnitureFree", "Assets/ThirdParty/RawWoodenFurnitureFree");
            }
            if (AssetDatabase.IsValidFolder("Assets/B3DArt"))
            {
                AssetDatabase.MoveAsset("Assets/B3DArt", "Assets/ThirdParty/B3DArt");
            }
            if (AssetDatabase.IsValidFolder("Assets/Assets/Radio"))
            {
                AssetDatabase.MoveAsset("Assets/Assets/Radio", "Assets/ThirdParty/Radio");
            }
            
            // Cleanup extra Assets folder if it is empty after move
            string extraAssetsPath = Path.Combine(Application.dataPath, "Assets");
            if (AssetDatabase.IsValidFolder("Assets/Assets") && Directory.Exists(extraAssetsPath) && Directory.GetFileSystemEntries(extraAssetsPath).Length == 0)
            {
                AssetDatabase.DeleteAsset("Assets/Assets");
            }

            if (AssetDatabase.IsValidFolder("Assets/SpaceZeta_RusticSmallCabinet"))
            {
                AssetDatabase.MoveAsset("Assets/SpaceZeta_RusticSmallCabinet", "Assets/ThirdParty/SpaceZeta_RusticSmallCabinet");
            }



            UpgradeMaterialsToURPLit();
            
            // Force Setup scene to run again to place the new models correctly
            SetupFirstPersonScene.RunManualSetup();
        }

        [MenuItem("Tools/Antigravity/Import Assets Directly")]
        public static void ImportCachedAssets()
        {
            string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            
            // 1. Import Big Oak Tree FREE
            string treePath = Path.Combine(appData, @"Unity\Asset Store-5.x\ALP\3D ModelsVegetation\Big Oak Tree FREE.unitypackage");
            if (File.Exists(treePath))
            {
                Debug.Log("Antigravity: Importing Big Oak Tree...");
                AssetDatabase.ImportPackage(treePath, false);
            }
            else
            {
                Debug.LogError("Antigravity: Could not find Big Oak Tree at: " + treePath);
            }

            // 2. Import Country House
            string housePath = Path.Combine(appData, @"Unity\Asset Store-5.x\ALP\3D ModelsEnvironmentsUrban\ountry house.unitypackage");
            if (File.Exists(housePath))
            {
                Debug.Log("Antigravity: Importing Country House...");
                AssetDatabase.ImportPackage(housePath, false);
            }
            else
            {
                Debug.LogError("Antigravity: Could not find Country House at: " + housePath);
            }

            // 3. Import Outdoor Ground Textures
            string groundPath = Path.Combine(appData, @"Unity\Asset Store-5.x\A dogs life software\Textures MaterialsGround\Outdoor Ground Textures.unitypackage");
            if (File.Exists(groundPath))
            {
                Debug.Log("Antigravity: Importing Outdoor Ground Textures...");
                AssetDatabase.ImportPackage(groundPath, false);
            }
            else
            {
                Debug.LogError("Antigravity: Could not find Outdoor Ground Textures at: " + groundPath);
            }

            // 4. Import Raw Wooden Furniture Free
            string furniturePath = Path.Combine(appData, @"Unity\Asset Store-5.x\AmbiMesh\3D ModelsPropsFurniture\Raw Wooden Furniture Free.unitypackage");
            if (File.Exists(furniturePath))
            {
                Debug.Log("Antigravity: Importing Raw Wooden Furniture...");
                AssetDatabase.ImportPackage(furniturePath, false);
            }
            else
            {
                Debug.LogError("Antigravity: Could not find Raw Wooden Furniture at: " + furniturePath);
            }

            // 5. Import Black Cherry Firewood
            string firewoodPath = Path.Combine(appData, @"Unity\Asset Store-5.x\Baldinoboy\3D ModelsProps\Black Cherry Firewood 01.unitypackage");
            if (File.Exists(firewoodPath))
            {
                Debug.Log("Antigravity: Importing Black Cherry Firewood...");
                AssetDatabase.ImportPackage(firewoodPath, false);
            }
            else
            {
                Debug.LogError("Antigravity: Could not find Black Cherry Firewood at: " + firewoodPath);
            }

            // 6. Import Old Radio P
            string radioPath = Path.Combine(appData, @"Unity\Asset Store-5.x\Jell3D\3D ModelsProps\Old Radio P.unitypackage");
            if (File.Exists(radioPath))
            {
                Debug.Log("Antigravity: Importing Old Radio P...");
                AssetDatabase.ImportPackage(radioPath, false);
            }
            else
            {
                Debug.LogError("Antigravity: Could not find Old Radio P at: " + radioPath);
            }

            // 7. Import Rustic Small Cabinet
            string cabinetPath = Path.Combine(appData, @"Unity\Asset Store-5.x\SpaceZeta\3D ModelsPropsFurniture\Rustic Small Cabinet.unitypackage");
            if (File.Exists(cabinetPath))
            {
                Debug.Log("Antigravity: Importing Rustic Small Cabinet...");
                AssetDatabase.ImportPackage(cabinetPath, false);
            }
            else
            {
                Debug.LogError("Antigravity: Could not find Rustic Small Cabinet at: " + cabinetPath);
            }

            // 8. Import MN D (Monster)
            string monsterPath = Path.Combine(appData, @"Unity\Asset Store-5.x\BobGrandmaster\3D ModelsCharactersCreatures\MN D.unitypackage");
            if (File.Exists(monsterPath))
            {
                Debug.Log("Antigravity: Importing MN D (Monster)...");
                AssetDatabase.ImportPackage(monsterPath, false);
            }
            else
            {
                Debug.LogWarning("Antigravity: Could not find MN D at: " + monsterPath);
            }
        }

        [MenuItem("Tools/Antigravity/Upgrade Imported Materials to URP")]
        public static void UpgradeMaterialsToURPLit()
        {
            // Find all materials in the imported Assets folder
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            int upgradedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Only upgrade materials inside ThirdParty folder
                if (!path.Contains("Assets/ThirdParty")) continue;

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                Shader shader = mat.shader;
                
                // If it has a shader that is not URP-compatible, we upgrade it
                if (shader != null && !shader.name.StartsWith("Universal Render Pipeline/") && !shader.name.StartsWith("Shader Graphs/"))
                {
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                    if (mat.HasProperty("_BaseColor"))
                    {
                        mainColor = mat.GetColor("_BaseColor");
                    }

                    // Check if it's a cutout texture (like leaves or billboards)
                    bool isCutout = shader.name.Contains("Cutout") || 
                                    shader.name.Contains("Billboard") || 
                                    shader.name.Contains("Leaves") ||
                                    (mat.HasProperty("_Cutoff") && mat.GetFloat("_Cutoff") > 0.01f);

                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader != null)
                    {
                        mat.shader = urpShader;
                        if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                        mat.SetColor("_BaseColor", mainColor);

                        if (isCutout)
                        {
                            mat.SetFloat("_AlphaClip", 1f);
                            mat.SetFloat("_Cutoff", mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.35f);
                            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off); // Render double-sided
                            mat.EnableKeyword("_ALPHATEST_ON");
                            mat.SetOverrideTag("RenderType", "TransparentCutout");
                            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                        }

                        // Map Normal Map
                        if (mat.HasProperty("_BumpMap"))
                        {
                            Texture bumpMap = mat.GetTexture("_BumpMap");
                            if (bumpMap != null)
                            {
                                mat.SetTexture("_BumpMap", bumpMap);
                                mat.EnableKeyword("_NORMALMAP");
                            }
                        }

                        EditorUtility.SetDirty(mat);
                        upgradedCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Antigravity: Upgraded {upgradedCount} materials to URP Lit successfully!");
            
            // Refresh EditorScene
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/Antigravity/Fix Monster Material")]
        public static void FixMonster()
        {


            // Upgrade all materials inside Assets/ThirdParty/BobGrandmaster/MN D
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            int upgradedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("BobGrandmaster") && !path.Contains("MN D")) continue;

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader != null)
                {
                    // Get textures
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    if (mat.HasProperty("_BaseColor")) mainColor = mat.GetColor("_BaseColor");

                    mat.shader = urpShader;
                    if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                    mat.SetColor("_BaseColor", mainColor);

                    // Set standard smoothness and metallic
                    mat.SetFloat("_Smoothness", 0.1f);
                    mat.SetFloat("_Metallic", 0.0f);

                    // Also try normal map if exists
                    if (mat.HasProperty("_BumpMap"))
                    {
                        Texture bumpMap = mat.GetTexture("_BumpMap");
                        if (bumpMap != null)
                        {
                            mat.SetTexture("_BumpMap", bumpMap);
                            mat.EnableKeyword("_NORMALMAP");
                        }
                    }

                    EditorUtility.SetDirty(mat);
                    upgradedCount++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Antigravity: Upgraded {upgradedCount} monster materials to URP Lit!");

            // Force Setup scene to run again to update references
            SetupFirstPersonScene.RunManualSetup();
        }
    }
}
