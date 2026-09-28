using Bhaptics.SDK2;
using UnityEngine;

namespace BhapticsDemo
{
    public class VestOutputMonitor : MonoBehaviour
    {
        public static VestOutputMonitor Instance { get; private set; }

        private readonly int[] motorOutputs = new int[VestMotorIndex.TotalMotors];
        private readonly float[] motorExpiry = new float[VestMotorIndex.TotalMotors];
        private readonly int[] singleMotor = new int[VestMotorIndex.TotalMotors];

        public int[] MotorOutputs => motorOutputs;

        public int GetRemainingMs(int motorIndex)
        {
            if (motorIndex < 0 || motorIndex >= motorExpiry.Length)
            {
                return 0;
            }

            float remaining = motorExpiry[motorIndex] - Time.unscaledTime;
            return remaining > 0f ? Mathf.CeilToInt(remaining * 1000f) : 0;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < motorOutputs.Length; i++)
            {
                if (motorExpiry[i] > 0f && now >= motorExpiry[i])
                {
                    motorOutputs[i] = 0;
                    motorExpiry[i] = 0f;
                }
            }
        }

        public int PlayMotor(int motorIndex, int intensity, int durationMs)
        {
            if (motorIndex < 0 || motorIndex >= singleMotor.Length)
            {
                return -1;
            }

            intensity = Mathf.Clamp(intensity, 0, 100);
            durationMs = Mathf.Max(100, durationMs);
            System.Array.Clear(singleMotor, 0, singleMotor.Length);
            singleMotor[motorIndex] = intensity;
            RecordMotor(motorIndex, intensity, durationMs);
            return BhapticsLibrary.PlayMotors((int)PositionType.Vest, singleMotor, durationMs);
        }

        public int PlayMotors(int[] motors, int durationMs)
        {
            durationMs = Mathf.Max(100, durationMs);
            RecordMotors(motors, durationMs);
            return BhapticsLibrary.PlayMotors((int)PositionType.Vest, motors, durationMs);
        }

        private void RecordMotor(int motorIndex, int intensity, int durationMs)
        {
            if (motorIndex < 0 || motorIndex >= motorOutputs.Length)
            {
                return;
            }

            motorOutputs[motorIndex] = intensity;
            motorExpiry[motorIndex] = Time.unscaledTime + durationMs / 1000f;
        }

        private void RecordMotors(int[] motors, int durationMs)
        {
            if (motors == null)
            {
                return;
            }

            float expiry = Time.unscaledTime + durationMs / 1000f;
            int count = Mathf.Min(motors.Length, motorOutputs.Length);
            for (int i = 0; i < count; i++)
            {
                if (motors[i] > 0)
                {
                    motorOutputs[i] = motors[i];
                    motorExpiry[i] = expiry;
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < motorOutputs.Length; i++)
            {
                motorOutputs[i] = 0;
                motorExpiry[i] = 0f;
            }
        }
    }
}
