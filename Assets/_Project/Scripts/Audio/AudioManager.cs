using UnityEngine;

namespace SniperGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("UI & Combat Audio Clips (2D)")]
        [SerializeField] private AudioClip hitmarkerClip;
        [SerializeField] private AudioClip headshotHitmarkerClip;
        [Tooltip("De volledige countdown audio clip van 4.344 seconden (3 -> 2 -> 1 -> GO)")]
        [SerializeField] private AudioClip countdownSequenceClip;
        [SerializeField] private AudioClip roundWinClip;
        [SerializeField] private AudioClip roundLoseClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        private AudioSource _2dAudioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _2dAudioSource = gameObject.AddComponent<AudioSource>();
            _2dAudioSource.spatialBlend = 0f; // 2D Stereo
            _2dAudioSource.playOnAwake = false;
        }

        public void PlayHitmarker(bool isHeadshot = false)
        {
            AudioClip clip = isHeadshot ? (headshotHitmarkerClip != null ? headshotHitmarkerClip : hitmarkerClip) : hitmarkerClip;
            PlayClip2D(clip, 1.0f);
        }

        /// <summary>
        /// Speelt het complete countdown fragment (4.344s) eenmalig af.
        /// </summary>
        public void PlayCountdownSequence()
        {
            PlayClip2D(countdownSequenceClip, 1.0f);
        }

        public void PlayRoundOutcome(bool isWinner)
        {
            PlayClip2D(isWinner ? roundWinClip : roundLoseClip, 0.9f);
        }

        public void PlayMatchOutcome(bool isWinner)
        {
            PlayClip2D(isWinner ? victoryClip : defeatClip, 1.0f);
        }

        private void PlayClip2D(AudioClip clip, float volume)
        {
            if (clip != null && _2dAudioSource != null)
            {
                _2dAudioSource.PlayOneShot(clip, volume);
            }
        }
    }
}