using UnityEditor;
using UnityEngine;
using System.IO;

namespace Antigravity.Editor
{
    [InitializeOnLoad]
    public static class ImportPackagesAutomation
    {
        private static readonly string[] PackagePaths = new string[]
        {
            @"C:\Users\MSI VN\AppData\Roaming\Unity\Asset Store-5.x\forst\3D ModelsVegetationTrees\Conifers BOTD.unitypackage",
            @"C:\Users\MSI VN\AppData\Roaming\Unity\Asset Store-5.x\Vladislav Pochezhertsev\Textures MaterialsNature\Grass And Flowers Pack 1.unitypackage",
            @"C:\Users\MSI VN\AppData\Roaming\Unity\Asset Store-5.x\A dogs life software\Textures MaterialsGround\Outdoor Ground Textures.unitypackage",
            @"C:\Users\MSI VN\AppData\Roaming\Unity\Asset Store-5.x\Sandro T\3D ModelsEnvironments\Flooded Grounds.unitypackage"
        };

        static ImportPackagesAutomation()
        {
            EditorApplication.delayCall += AutoImportDownloadedPackages;
        }

        [MenuItem("Tools/Antigravity/Import Downloaded Asset Packages")]
        public static void ImportAllDownloadedPackages()
        {
            int importedCount = 0;
            foreach (var pkgPath in PackagePaths)
            {
                if (File.Exists(pkgPath))
                {
                    Debug.Log("Antigravity: Auto-importing downloaded Unity Package: " + Path.GetFileName(pkgPath));
                    AssetDatabase.ImportPackage(pkgPath, false); // false = silent auto import without popup
                    importedCount++;
                }
                else
                {
                    Debug.LogWarning("Antigravity: Package file not found at " + pkgPath);
                }
            }

            if (importedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log("Antigravity: All " + importedCount + " Asset Packages imported successfully!");
            }
        }

        private static void AutoImportDownloadedPackages()
        {
            EditorApplication.delayCall -= AutoImportDownloadedPackages;
            // Packages are already imported into the project. Auto-import disabled to prevent re-extracting legacy PostProcessing.
        }
    }
}
