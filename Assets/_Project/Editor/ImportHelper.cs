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
            if (AssetDatabase.IsValidFolder("Assets/ADG_Textures"))
            {
                AssetDatabase.MoveAsset("Assets/ADG_Textures", "Assets/ThirdParty/ADG_Textures");
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
        }

        [MenuItem("Tools/Antigravity/Upgrade Imported Materials to URP")]
        public static void UpgradeMaterialsToURPLit()
        {
            // Find all materials in the imported Assets/ALP_Assets folder
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            int upgradedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Only upgrade materials inside ALP_Assets or a dogs life software
                if (!path.Contains("Assets/ALP_Assets") && !path.Contains("Assets/A dogs life software") &&
                    !path.Contains("Assets/ThirdParty/ALP_Assets") && !path.Contains("Assets/ThirdParty/A dogs life software")) continue;

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
    }
}
