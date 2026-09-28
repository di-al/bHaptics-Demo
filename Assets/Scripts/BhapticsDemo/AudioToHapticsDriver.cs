using Bhaptics.SDK2;
using System.Text;
using UnityEngine;

namespace BhapticsDemo
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioToHapticsDriver : MonoBehaviour
    {
        [SerializeField] private float masterGain = 8f;
        [SerializeField] private int updateIntervalMs = 50;
        [SerializeField] private int motorDurationMs = 100;

        private AudioSource audioSource;
        private AudioClip playingClip;
        private float[] sampleBuffer;
        private float[] channelLevels;
        private readonly int[] motors = new int[VestMotorIndex.TotalMotors];
        private float nextUpdateTime;
        private bool playing;
        private string lastError;
        private string playingClipName;

        public bool IsClipPlaying => playing && audioSource != null && audioSource.isPlaying;
        public string LastError => lastError;
        public string PlayingClipName => playingClipName;
        public int LastActiveMotors { get; private set; }
        public int LastChannelCount { get; private set; }
        public float[] LastChannelLevels { get; private set; }
        public float LastTotalRms { get; private set; }

        public float MasterGain
        {
            get => masterGain;
            set => masterGain = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.mute = true;
            audioSource.volume = 0f;
        }

        private void OnDisable()
        {
            if (!playing)
            {
                return;
            }

            playing = false;
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            BhapticsLibrary.StopAll();
        }

        public void PlayClip(AudioClip clip)
        {
            if (clip == null)
            {
                lastError = "No audio clip";
                return;
            }

            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            playingClip = clip;
            playingClipName = clip.name;
            lastError = null;
            sampleBuffer = new float[2048 * clip.channels];
            channelLevels = new float[clip.channels];

            audioSource.clip = clip;
            audioSource.time = 0f;
            playing = true;
            enabled = true;
            audioSource.Play();
        }

        public void StopClipPlayback()
        {
            bool wasPlaying = playing || (audioSource != null && audioSource.isPlaying);
            playing = false;
            playingClip = null;
            playingClipName = null;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (wasPlaying)
            {
                BhapticsLibrary.StopAll();
            }

            enabled = false;
        }

        private void Update()
        {
            if (!playing || Time.unscaledTime < nextUpdateTime)
            {
                return;
            }

            nextUpdateTime = Time.unscaledTime + updateIntervalMs / 1000f;

            if (playingClip == null || !audioSource.isPlaying)
            {
                string finished = playingClipName;
                StopClipPlayback();
                lastError = string.IsNullOrEmpty(finished) ? null : $"Finished: {finished}";
                return;
            }

            if (!BhapticsSDK2.IsInitialized || !BhapticsLibrary.IsBhapticsAvailable(false))
            {
                lastError = "bHaptics not ready";
                return;
            }

            if (!TryReadClipLevels(playingClip))
            {
                return;
            }

            SendMotors();
        }

        private bool TryReadClipLevels(AudioClip clip)
        {
            int channelCount = clip.channels;
            int frames = Mathf.Min(2048, clip.samples);
            if (frames <= 0)
            {
                lastError = "Audio clip has no samples";
                return false;
            }

            if (sampleBuffer == null || sampleBuffer.Length != frames * channelCount)
            {
                sampleBuffer = new float[frames * channelCount];
            }

            if (channelLevels == null || channelLevels.Length != channelCount)
            {
                channelLevels = new float[channelCount];
            }

            int start = audioSource.timeSamples - frames;
            if (start < 0)
            {
                start = 0;
            }

            if (start + frames > clip.samples)
            {
                start = clip.samples - frames;
            }

            if (!clip.GetData(sampleBuffer, start))
            {
                lastError = "Could not read audio samples";
                return false;
            }

            float totalRms = 0f;
            for (int c = 0; c < channelCount; c++)
            {
                float sumSq = 0f;
                for (int i = 0; i < frames; i++)
                {
                    float sample = sampleBuffer[i * channelCount + c];
                    sumSq += sample * sample;
                }

                channelLevels[c] = Mathf.Sqrt(sumSq / Mathf.Max(1, frames));
                totalRms += channelLevels[c];
            }

            LastChannelCount = channelCount;
            LastChannelLevels = channelLevels;
            LastTotalRms = totalRms / Mathf.Max(1, channelCount);
            lastError = null;
            return true;
        }

        private void SendMotors()
        {
            ChannelMotorMapper.MapChannelsToMotors(LastChannelCount, channelLevels, masterGain, motors);
            LastActiveMotors = 0;
            for (int i = 0; i < motors.Length; i++)
            {
                if (motors[i] > 0)
                {
                    LastActiveMotors++;
                }
            }

            if (LastActiveMotors == 0)
            {
                return;
            }

            VestOutputMonitor.Instance?.PlayMotors(motors, motorDurationMs);
        }

        public string BuildDebugStatus()
        {
            var sb = new StringBuilder();
            if (IsClipPlaying)
            {
                sb.AppendLine($"Clip: {playingClipName}");
                sb.AppendLine($"motors firing: {LastActiveMotors}/{VestMotorIndex.TotalMotors}");
            }
            else
            {
                sb.AppendLine("Use the 6ch or 8ch buttons");
            }

            if (!string.IsNullOrEmpty(lastError))
            {
                sb.AppendLine(lastError);
            }

            return sb.ToString();
        }
    }
}
