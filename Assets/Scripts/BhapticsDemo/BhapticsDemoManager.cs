using Bhaptics.SDK2;
using UnityEngine;

namespace BhapticsDemo
{
    public class BhapticsDemoManager : MonoBehaviour
    {
        [SerializeField] private GameObject bhapticsPrefab;
        [SerializeField] private MultichannelAudioSet audioClips;

        private void Awake()
        {
            EnsureBhapticsInitializer();
            EnsureAudioClips();

            VestCubeSetup.AssignCubesInScene();

            var clickHandler = gameObject.AddComponent<VestCubeClickHandler>();
            gameObject.AddComponent<VestOutputMonitor>();
            var audioDriver = gameObject.AddComponent<AudioToHapticsDriver>();
            audioDriver.enabled = false;
            var dashboard = gameObject.AddComponent<HapticsDashboardUI>();
            dashboard.Initialize(clickHandler, audioDriver, audioClips);
        }

        private void EnsureAudioClips()
        {
            if (audioClips != null)
            {
                return;
            }

#if UNITY_EDITOR
            audioClips = UnityEditor.AssetDatabase.LoadAssetAtPath<MultichannelAudioSet>(
                "Assets/Resources/MultichannelAudioSet.asset");
#endif

            if (audioClips == null)
            {
                audioClips = Resources.Load<MultichannelAudioSet>("MultichannelAudioSet");
            }

            if (audioClips == null)
            {
                Debug.LogError(
                    "[BhapticsDemo] MultichannelAudioSet is not assigned on BhapticsDemoManager. " +
                    "Assign Assets/Resources/MultichannelAudioSet.asset in the Inspector (required for builds).");
            }
        }
        private void EnsureBhapticsInitializer()
        {
            if (FindAnyObjectByType<BhapticsSDK2>() != null)
            {
                return;
            }

#if UNITY_EDITOR
            if (bhapticsPrefab == null)
            {
                bhapticsPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Bhaptics/SDK2/Prefabs/[bhaptics].prefab");
            }
#endif

            if (bhapticsPrefab == null)
            {
                Debug.LogError("[BhapticsDemo] Assign [bhaptics] prefab on BhapticsDemoManager.");
                return;
            }

            Instantiate(bhapticsPrefab);
        }
    }
}
