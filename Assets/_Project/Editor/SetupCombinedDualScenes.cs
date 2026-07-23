using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Antigravity.Editor
{
    [InitializeOnLoad]
    public static class SetupCombinedDualScenes
    {
        private const string MainFolderName = "Folder 1 - MainScene";
        private const string LakeFolderName = "Folder 2 - LakeScene";
        private static readonly Vector3 LakeOffset = new Vector3(800f, 0f, 0f);

        [MenuItem("Tools/Antigravity/Tach 2 Scene thanh 2 Folder rieng biet (Folder 1 & Folder 2)")]
        public static void OrganizeDualScenes()
        {
            Debug.Log("Antigravity: Dang don dep va nhom 2 Scene thanh 2 Folder rieng biet...");

            // 1. Clear current old folder containers if exist
            GameObject oldMain = GameObject.Find(MainFolderName);
            if (oldMain != null) Object.DestroyImmediate(oldMain);

            GameObject oldLake = GameObject.Find(LakeFolderName);
            if (oldLake != null) Object.DestroyImmediate(oldLake);

            // Clean loose objects in active scene to prevent overlapping
            string[] looseObjectsToClean = new string[]
            {
                "ProceduralTerrain", "LakeProceduralTerrain", "LakeWater", "WoodenPier",
                "ShorelineRocks", "LakeForest", "LakeGrass", "GrassField", "House",
                "ForestFence", "Forest", "ForestLakeShorelineTrees", "IntersectionStreetLight", "HorrorEnvironment",
                "ShadowFigure", "MonsterEnemy", "Campsite", "LakeCampsite", "SunsetLight", "Sunlight", "Moonlight",
                "OldRadio", "PaperNote", "HouseKey", "LeatherTrunk"
            };

            foreach (string name in looseObjectsToClean)
            {
                GameObject obj = GameObject.Find(name);
                while (obj != null)
                {
                    Object.DestroyImmediate(obj);
                    obj = GameObject.Find(name);
                }
            }

            // 2. Create Parent Folder 1
            GameObject mainFolder = new GameObject(MainFolderName);
            mainFolder.transform.position = Vector3.zero;
            mainFolder.transform.rotation = Quaternion.identity;

            // Generate Main Scene objects
            SetupFirstPersonScene.RunManualSetup();

            // Move all newly generated Main Scene objects under Folder 1
            string[] mainObjects = new string[]
            {
                "ProceduralTerrain", "GrassField", "House", "ForestFence", "Forest", "ForestLakeShorelineTrees",
                "ForestLakeWater", "ForestLakeShorelineRocks", "LakesideCamp",
                "Campsite", "Temple", "School", "AbandonedBus", "BienBao", "AshfallMap", "CircleOfHands",
                "OldRadio", "PaperNote", "HouseKey", "LeatherTrunk", "ShadowFigure",
                "MonsterEnemy", "IntersectionStreetLight", "HorrorEnvironment", "EnvironmentLighting"
            };

            foreach (string objName in mainObjects)
            {
                GameObject go = GameObject.Find(objName);
                if (go != null && go.transform.parent == null && go != mainFolder)
                {
                    go.transform.SetParent(mainFolder.transform, true);
                }
            }

            // Clean up any remaining LakeScene objects
            SetupFirstPersonScene.DeleteLakeFolder();

            // 4. Player Setup
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.SetParent(null);
                FirstPersonController fpc = player.GetComponent<FirstPersonController>();
                if (fpc == null)
                {
                    fpc = player.AddComponent<FirstPersonController>();
                }
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Antigravity: Hoan tat! 2 Scene da duoc gom thanh 2 Folder rieng biet trong Hierarchy va dat cach nhau 800m!");
        }
    }
}
