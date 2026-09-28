#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BhapticsDemo.Editor
{
    public static class BhapticsDemoSceneSetup
    {
        private const string BhapticsPrefabPath = "Assets/Bhaptics/SDK2/Prefabs/[bhaptics].prefab";
        private const string AudioSetPath = "Assets/Resources/MultichannelAudioSet.asset";
        private const string ManagerName = "BhapticsDemo";

        [MenuItem("Bhaptics Demo/Setup Sample Scene")]
        public static void SetupSampleScene()
        {
            var manager = Object.FindAnyObjectByType<BhapticsDemoManager>();
            if (manager == null)
            {
                var go = new GameObject(ManagerName);
                manager = go.AddComponent<BhapticsDemoManager>();
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BhapticsPrefabPath);
            var audioSet = AssetDatabase.LoadAssetAtPath<MultichannelAudioSet>(AudioSetPath);
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("bhapticsPrefab").objectReferenceValue = prefab;
            serialized.FindProperty("audioClips").objectReferenceValue = audioSet;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (Object.FindAnyObjectByType<Bhaptics.SDK2.BhapticsSDK2>() == null && prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "[bhaptics]";
                var sdk = instance.GetComponent<Bhaptics.SDK2.BhapticsSDK2>();
                if (sdk != null)
                {
                    var sdkSerialized = new SerializedObject(sdk);
                    sdkSerialized.FindProperty("autoRunBhapticsPlayer").boolValue = true;
                    sdkSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            VestCubeSetup.AssignCubesInScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Bhaptics Demo/Validate Motor Mapping")]
        public static void ValidateMotorMapping()
        {
            VestCubeSetup.AssignCubesInScene();
            var cubes = Object.FindObjectsByType<VestCube>(FindObjectsSortMode.None);
            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (var cube in cubes)
            {
                seen.Add(cube.MotorIndex);
            }

            if (seen.Count != VestMotorIndex.TotalMotors)
            {
                Debug.LogWarning($"[BhapticsDemo] Expected {VestMotorIndex.TotalMotors} motors, found {seen.Count}.");
            }
        }
    }
}
#endif
