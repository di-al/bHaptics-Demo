namespace BhapticsDemo
{
    public static class VestMotorIndex
    {
        public const int MotorsPerPanel = 16;
        public const int TotalMotors = 32;
        public const int GridSize = 4;

        public static bool TryGetMotorIndexFromCubeName(string cubeName, out int motorIndex)
        {
            motorIndex = -1;
            if (string.IsNullOrEmpty(cubeName) || !cubeName.StartsWith("Cube"))
            {
                return false;
            }

            bool isBack = cubeName.Contains("(1)");
            int numberEnd = cubeName.IndexOf(' ');
            if (numberEnd < 0)
            {
                numberEnd = cubeName.Length;
            }

            if (!int.TryParse(cubeName.Substring(4, numberEnd - 4), out int cubeNumber))
            {
                return false;
            }

            if (cubeNumber < 1 || cubeNumber > MotorsPerPanel)
            {
                return false;
            }

            motorIndex = (cubeNumber - 1) + (isBack ? MotorsPerPanel : 0);
            return true;
        }

        public static string GetCubeName(int motorIndex)
        {
            if (motorIndex < 0 || motorIndex >= TotalMotors)
            {
                return "invalid";
            }

            int cubeNumber = (motorIndex % MotorsPerPanel) + 1;
            return motorIndex < MotorsPerPanel ? $"Cube{cubeNumber}" : $"Cube{cubeNumber} (1)";
        }

        public static string GetMotorLabel(int motorIndex)
        {
            if (motorIndex < 0 || motorIndex >= TotalMotors)
            {
                return "invalid";
            }

            string panel = motorIndex < MotorsPerPanel ? "Front" : "Back";
            return $"{GetCubeName(motorIndex)} : {panel} motor {motorIndex}";
        }
    }
}
