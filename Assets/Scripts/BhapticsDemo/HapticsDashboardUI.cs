using System.Text;
using Bhaptics.SDK2;
using UnityEngine;
using UnityEngine.UI;

namespace BhapticsDemo
{
    public class HapticsDashboardUI : MonoBehaviour
    {
        private VestCubeClickHandler clickHandler;
        private AudioToHapticsDriver audioDriver;
        private MultichannelAudioSet audioSet;

        private Text statusText;
        private Text lastMotorText;
        private Text outputsText;
        private Image[] motorOutputCells;
        private Slider intensitySlider;
        private Slider durationSlider;
        private Text intensityValueText;
        private Text durationValueText;
        private int lastRequestId = -1;

        public void Initialize(VestCubeClickHandler handler, AudioToHapticsDriver audio, MultichannelAudioSet clips)
        {
            clickHandler = handler;
            audioDriver = audio;
            audioSet = clips;
            BuildUi();
            clickHandler?.SetDashboard(this);
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("HapticsDashboardCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = CreatePanel(canvasGo.transform, new Vector2(10, -10), new Vector2(420, 380));

            statusText = CreateText(panel.transform, "Status", 18, TextAnchor.UpperLeft, new Vector2(10, -10), new Vector2(400, 130));
            lastMotorText = CreateText(panel.transform, "Last motor", 17, TextAnchor.UpperLeft, new Vector2(10, -140), new Vector2(400, 44));

            intensitySlider = CreateSlider(panel.transform, "Intensity", 1, 100, 100, new Vector2(10, -180), out intensityValueText, "{0}%");
            durationSlider = CreateSlider(panel.transform, "Duration", 100, 1000, 200, new Vector2(10, -230), out durationValueText, "{0} ms");

            CreateButton(panel.transform, "Stop All", new Vector2(10, -280), () =>
            {
                BhapticsLibrary.StopAll();
                VestOutputMonitor.Instance?.Clear();
            });
            CreateButton(panel.transform, "Ping Vest", new Vector2(140, -280), () => BhapticsLibrary.Ping(PositionType.Vest));
            CreateButton(panel.transform, "Play All Motors", new Vector2(270, -280), PlayAllMotors);

            CreateButton(panel.transform, "Play 6ch Audio", new Vector2(10, -324), PlaySixChannelAudio);
            CreateButton(panel.transform, "Play 8ch Audio", new Vector2(140, -324), PlayEightChannelAudio);
            CreateButton(panel.transform, "Stop Audio", new Vector2(270, -324), StopAudio);

            BuildOutputsPanel(canvasGo.transform);

            intensitySlider.onValueChanged.AddListener(v =>
            {
                int intensity = Mathf.RoundToInt(v);
                if (intensityValueText != null)
                {
                    intensityValueText.text = $"{intensity}%";
                }

                if (clickHandler != null)
                {
                    clickHandler.Intensity = intensity;
                }

                if (audioDriver != null)
                {
                    audioDriver.MasterGain = intensity / 10f;
                }
            });

            durationSlider.onValueChanged.AddListener(v =>
            {
                int durationMs = Mathf.RoundToInt(v);
                if (durationValueText != null)
                {
                    durationValueText.text = $"{durationMs} ms";
                }

                if (clickHandler != null)
                {
                    clickHandler.DurationMs = durationMs;
                }
            });
        }

        private void Update()
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = BuildStatusText();

            RefreshOutputsPanel();
        }

        public void NotifyMotorFired(int motorIndex, int requestId, int intensity, int durationMs)
        {
            lastRequestId = requestId;
            string label = VestMotorIndex.GetMotorLabel(motorIndex);
            lastMotorText.text = $"Last: {label} | req {requestId} | {intensity}% / {durationMs}ms";
        }

        private string BuildStatusText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("bHaptics Vest Demo");
            sb.AppendLine($"SDK initialized: {BhapticsSDK2.IsInitialized}");
            sb.AppendLine($"Player available: {BhapticsLibrary.IsBhapticsAvailable(false)}");
            sb.AppendLine($"Playing: {BhapticsLibrary.IsPlaying()}");

            var devices = BhapticsLibrary.GetDevices();
            if (devices.Count == 0)
            {
                sb.AppendLine("Devices: none");
            }
            else
            {
                foreach (var device in devices)
                {
                    sb.AppendLine($"{device.DeviceName} | {device.Position}");
                    sb.AppendLine($"  paired={device.IsPaired} connected={device.IsConnected} battery={device.Battery}%");
                }
            }

            return sb.ToString();
        }

        private void PlayAllMotors()
        {
            var motors = new int[VestMotorIndex.TotalMotors];
            int level = clickHandler != null ? clickHandler.Intensity : 50;
            for (int i = 0; i < motors.Length; i++)
            {
                motors[i] = level;
            }

            int duration = clickHandler != null ? clickHandler.DurationMs : 200;
            VestOutputMonitor.Instance?.PlayMotors(motors, duration);
        }

        private void PlaySixChannelAudio()
        {
            audioDriver?.PlayClip(audioSet != null ? audioSet.sixChannelClip : null);
        }

        private void PlayEightChannelAudio()
        {
            audioDriver?.PlayClip(audioSet != null ? audioSet.eightChannelClip : null);
        }

        private void StopAudio()
        {
            audioDriver?.StopClipPlayback();
            BhapticsLibrary.StopAll();
            VestOutputMonitor.Instance?.Clear();
        }

        private void BuildOutputsPanel(Transform canvasTransform)
        {
            var panel = CreateAnchoredPanel(canvasTransform, new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(400f, 920f));

            CreateText(panel.transform, "Actuator Outputs", 20, TextAnchor.UpperLeft, new Vector2(12f, -10f), new Vector2(370f, 26f));

            motorOutputCells = new Image[VestMotorIndex.TotalMotors];
            const float cellSize = 26f;
            const float gap = 3f;
            const float frontOriginY = -42f;
            const float backOriginY = -158f;
            const float gridWidth = VestMotorIndex.GridSize * cellSize + (VestMotorIndex.GridSize - 1) * gap;

            CreateText(panel.transform, "Front", 15, TextAnchor.UpperLeft, new Vector2(12f, frontOriginY - 6f), new Vector2(80f, 20f));
            CreateText(panel.transform, "Back", 15, TextAnchor.UpperLeft, new Vector2(12f, backOriginY - 6f), new Vector2(80f, 20f));

            float gridStartX = 12f + (376f - gridWidth) * 0.5f;
            for (int motor = 0; motor < VestMotorIndex.TotalMotors; motor++)
            {
                int local = motor % VestMotorIndex.MotorsPerPanel;
                int row = local / VestMotorIndex.GridSize;
                int col = local % VestMotorIndex.GridSize;
                bool isBack = motor >= VestMotorIndex.MotorsPerPanel;
                float originY = isBack ? backOriginY : frontOriginY;

                var cell = new GameObject($"MotorCell_{motor}");
                cell.transform.SetParent(panel.transform, false);
                var image = cell.AddComponent<Image>();
                image.color = new Color(0.15f, 0.15f, 0.18f, 1f);
                motorOutputCells[motor] = image;

                var rect = cell.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(
                    gridStartX + col * (cellSize + gap),
                    originY - row * (cellSize + gap));
                rect.sizeDelta = new Vector2(cellSize, cellSize);
            }

            outputsText = CreateText(panel.transform, "", 14, TextAnchor.UpperLeft, new Vector2(12f, -278f), new Vector2(376f, 630f));
        }

        private void RefreshOutputsPanel()
        {
            if (motorOutputCells == null || VestOutputMonitor.Instance == null)
            {
                return;
            }

            var monitor = VestOutputMonitor.Instance;
            int[] outputs = monitor.MotorOutputs;

            if (outputsText != null)
            {
                outputsText.text = BuildFullOutputsText(monitor, outputs);
            }

            for (int i = 0; i < motorOutputCells.Length; i++)
            {
                int value = outputs[i];
                float t = value / 100f;
                motorOutputCells[i].color = value > 0
                    ? Color.Lerp(new Color(0.1f, 0.35f, 0.15f), new Color(0.2f, 0.95f, 0.35f), t)
                    : new Color(0.15f, 0.15f, 0.18f, 1f);
            }
        }

        private string BuildFullOutputsText(VestOutputMonitor monitor, int[] outputs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Intensity: 0% to 100% per motor");
            sb.AppendLine($"Active: {CountActive(outputs)}/32 | Peak: {GetPeak(outputs)}% | SDK playing: {BhapticsLibrary.IsPlaying()}");
            if (lastRequestId >= 0)
            {
                sb.AppendLine($"Last request ID: {lastRequestId}");
            }

            sb.AppendLine();
            AppendMotorSection(sb, monitor, outputs, "Front (Cube1-Cube16, motors 0-15)", 0, VestMotorIndex.MotorsPerPanel);
            sb.AppendLine();
            AppendMotorSection(sb, monitor, outputs, "Back (Cube1 (1)-Cube16 (1), motors 16-31)", VestMotorIndex.MotorsPerPanel, VestMotorIndex.MotorsPerPanel);

            if (audioDriver != null && audioDriver.IsClipPlaying)
            {
                sb.AppendLine();
                sb.AppendLine("Audio to haptics");
                sb.AppendLine($"Clip: {audioDriver.PlayingClipName}");
                sb.AppendLine($"Gain: {audioDriver.MasterGain:F1} | Motors firing: {audioDriver.LastActiveMotors}");
                sb.AppendLine($"Input RMS: {audioDriver.LastTotalRms:F4}");
                if (!string.IsNullOrEmpty(audioDriver.LastError))
                {
                    sb.AppendLine(audioDriver.LastError);
                }

                float[] levels = audioDriver.LastChannelLevels;
                int channelCount = audioDriver.LastChannelCount;
                if (levels != null && channelCount > 0)
                {
                    for (int c = 0; c < channelCount; c++)
                    {
                        int motor = c < VestMotorIndex.TotalMotors ? c : -1;
                        int mapped = motor >= 0 ? outputs[motor] : 0;
                        sb.AppendLine($"  Ch{c}: RMS {levels[c]:F4} : M{motor} {mapped}%");
                    }
                }
            }

            return sb.ToString();
        }

        private static void AppendMotorSection(StringBuilder sb, VestOutputMonitor monitor, int[] outputs, string title, int start, int count)
        {
            sb.AppendLine(title);
            for (int i = start; i < start + count; i++)
            {
                int pct = outputs[i];
                int remainingMs = monitor.GetRemainingMs(i);
                sb.Append("  ").Append(VestMotorIndex.GetCubeName(i).PadRight(12));
                sb.Append(" M").Append(i.ToString().PadLeft(2)).Append("  ");
                sb.Append(pct.ToString().PadLeft(3)).Append('%');
                sb.AppendLine();
            }
        }

        private static int GetPeak(int[] outputs)
        {
            int peak = 0;
            for (int i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] > peak)
                {
                    peak = outputs[i];
                }
            }

            return peak;
        }

        private static int CountActive(int[] outputs)
        {
            int count = 0;
            for (int i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static RectTransform CreatePanel(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            return CreateAnchoredPanel(parent, new Vector2(0f, 1f), anchoredPos, size);
        }

        private static RectTransform CreateAnchoredPanel(Transform parent, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return rect;
        }

        private static void ConfigureUiText(Text text, int fontSize, TextAnchor anchor)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            text.supportRichText = false;

            var outline = text.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private static Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            ConfigureUiText(text, fontSize, anchor);
            text.text = content;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return text;
        }

        private static Slider CreateSlider(
            Transform parent,
            string label,
            float min,
            float max,
            float value,
            Vector2 pos,
            out Text valueText,
            string valueFormat)
        {
            CreateText(parent, label, 16, TextAnchor.UpperLeft, pos, new Vector2(200, 24));
            valueText = CreateText(parent, string.Format(valueFormat, Mathf.RoundToInt(value)), 16, TextAnchor.MiddleRight, pos + new Vector2(390f, 0f), new Vector2(80f, 24f));
            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.pivot = new Vector2(1f, 1f);
            var go = new GameObject(label + " Slider");
            go.transform.SetParent(parent, false);
            var slider = go.AddComponent<Slider>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos + new Vector2(0, -18);
            rect.sizeDelta = new Vector2(380, 20);

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5, 5);
            fillAreaRect.offsetMax = new Vector2(-5, -5);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.65f, 1f);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.fillRect = fillRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            return slider;
        }

        private static void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.25f, 0.35f, 0.55f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(128, 34);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            ConfigureUiText(text, 16, TextAnchor.MiddleCenter);
            text.text = label;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

    }
}
