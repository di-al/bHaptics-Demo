using UnityEngine;

namespace BhapticsDemo
{
    public static class VestCubeSetup
    {
        public static void AssignCubesInScene()
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (!VestMotorIndex.TryGetMotorIndexFromCubeName(renderer.gameObject.name, out int motorIndex))
                {
                    continue;
                }

                var cube = renderer.GetComponent<VestCube>();
                if (cube == null)
                {
                    cube = renderer.gameObject.AddComponent<VestCube>();
                }

                cube.Configure(motorIndex);
            }
        }
    }
}
