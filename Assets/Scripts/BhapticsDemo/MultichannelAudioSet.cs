using UnityEngine;

namespace BhapticsDemo
{
    [CreateAssetMenu(fileName = "MultichannelAudioSet", menuName = "Bhaptics Demo/Multichannel Audio Set")]
    public class MultichannelAudioSet : ScriptableObject
    {
        public AudioClip sixChannelClip;
        public AudioClip eightChannelClip;
    }
}
