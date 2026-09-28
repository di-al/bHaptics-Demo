using UnityEngine;

namespace BhapticsDemo
{
    public static class ChannelMotorMapper
    {
        public static void MapChannelsToMotors(int channelCount, float[] channelLevels, float gain, int[] motors)
        {
            if (motors == null || motors.Length == 0)
            {
                return;
            }

            System.Array.Clear(motors, 0, motors.Length);
            if (channelLevels == null)
            {
                return;
            }

            int count = Mathf.Min(channelCount, channelLevels.Length, motors.Length, VestMotorIndex.TotalMotors);
            for (int c = 0; c < count; c++)
            {
                int intensity = Mathf.RoundToInt(Mathf.Clamp01(channelLevels[c] * gain) * 100f);
                motors[c] = Mathf.Clamp(intensity, 0, 100);
            }
        }
    }
}
