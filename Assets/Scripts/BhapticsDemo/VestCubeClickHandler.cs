using UnityEngine;
using UnityEngine.InputSystem;

namespace BhapticsDemo
{
    public class VestCubeClickHandler : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private int intensity = 100;
        [SerializeField] private int durationMs = 200;

        private HapticsDashboardUI dashboard;

        public int Intensity
        {
            get => intensity;
            set => intensity = Mathf.Clamp(value, 1, 100);
        }

        public int DurationMs
        {
            get => durationMs;
            set => durationMs = Mathf.Max(100, value);
        }

        public void SetDashboard(HapticsDashboardUI ui)
        {
            dashboard = ui;
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (targetCamera == null || Mouse.current == null)
            {
                return;
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                return;
            }

            var cube = hit.collider.GetComponent<VestCube>();
            if (cube == null || cube.MotorIndex < 0)
            {
                return;
            }

            FireMotor(cube);
        }

        private void FireMotor(VestCube cube)
        {
            if (cube == null || cube.MotorIndex < 0 || VestOutputMonitor.Instance == null)
            {
                return;
            }

            int requestId = VestOutputMonitor.Instance.PlayMotor(cube.MotorIndex, intensity, durationMs);
            cube.FlashHighlight();
            dashboard?.NotifyMotorFired(cube.MotorIndex, requestId, intensity, durationMs);
        }
    }
}
