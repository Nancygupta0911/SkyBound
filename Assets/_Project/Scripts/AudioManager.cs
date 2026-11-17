using UnityEngine;
using UnityEngine.InputSystem;

namespace Skybound
{
    /// <summary>
    /// Central audio hub: looping music, pooled 2D sound effects and a persisted mute toggle.
    /// Survives the whole session and is driven by the gameplay systems through static helpers.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string MuteKey = "skybound.muted";

        [Header("Music")]
        [SerializeField] private AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.30f;
        [SerializeField, Range(0f, 1f)] private float duckedMusicVolume = 0.10f;
        [SerializeField] private float fadeSpeed = 0.6f;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip[] coinClips;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip landClip;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip fallClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.75f;
        [SerializeField] private int voiceCount = 8;

        private AudioSource _music;
        private AudioSource[] _voices;
        private int _nextVoice;
        private float _musicTarget;
        private float _musicCurrent;

        public bool Muted { get; private set; }

        /// <summary>Raised whenever the mute state changes so the UI can refresh.</summary>
        public event System.Action<bool> MuteChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;

            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = musicClip;
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.volume = 0f;

            _voices = new AudioSource[Mathf.Max(1, voiceCount)];
            for (int i = 0; i < _voices.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = sfxVolume;
                _voices[i] = source;
            }

            ApplyMute();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) ToggleMute();

            if (_music == null) return;

            _musicCurrent = Mathf.MoveTowards(_musicCurrent, _musicTarget, fadeSpeed * Time.unscaledDeltaTime);
            _music.volume = _musicCurrent;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ music

        /// <summary>Starts the looping theme (called when a run begins so browser audio is unlocked by the click).</summary>
        public void StartMusic()
        {
            if (_music == null || _music.clip == null) return;

            if (!_music.isPlaying) _music.Play();
            _musicTarget = musicVolume;
            if (_musicCurrent <= 0f) _musicCurrent = 0f;
        }

        /// <summary>Lowers the music while a result screen is shown.</summary>
        public void DuckMusic(bool ducked)
        {
            _musicTarget = ducked ? duckedMusicVolume : musicVolume;
        }

        // ------------------------------------------------------------------ effects

        public void PlayCoin()
        {
            if (coinClips == null || coinClips.Length == 0) return;
            Play(coinClips[Random.Range(0, coinClips.Length)], 1f, Random.Range(0.97f, 1.06f));
        }

        public void PlayJump() => Play(jumpClip, 0.55f, Random.Range(1.02f, 1.12f));

        public void PlayLand(float impact)
        {
            float strength = Mathf.Clamp01(impact);
            Play(landClip, Mathf.Lerp(0.25f, 0.65f, strength), Mathf.Lerp(1.08f, 0.92f, strength));
        }

        public void PlayFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;
            Play(footstepClips[Random.Range(0, footstepClips.Length)], 0.22f, Random.Range(0.95f, 1.08f));
        }

        public void PlayFall() => Play(fallClip, 0.8f, Random.Range(0.98f, 1.02f));

        public void PlayWin() => Play(winClip, 0.9f, 1f);

        public void PlayClick() => Play(clickClip, 0.55f, Random.Range(0.97f, 1.03f));

        // ------------------------------------------------------------------ mute

        public void ToggleMute()
        {
            SetMuted(!Muted);
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMute();
            MuteChanged?.Invoke(muted);
        }

        private void ApplyMute()
        {
            AudioListener.volume = Muted ? 0f : 1f;
        }

        // ------------------------------------------------------------------ internals

        private void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || _voices == null) return;

            AudioSource source = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;

            source.pitch = pitch;
            source.volume = volume * sfxVolume;
            source.clip = clip;
            source.Play();
        }

        // ------------------------------------------------------------------ static helpers

        public static void Coin()
        {
            if (Instance != null) Instance.PlayCoin();
        }

        public static void Jump()
        {
            if (Instance != null) Instance.PlayJump();
        }

        public static void Land(float impact)
        {
            if (Instance != null) Instance.PlayLand(impact);
        }

        public static void Footstep()
        {
            if (Instance != null) Instance.PlayFootstep();
        }

        public static void Fall()
        {
            if (Instance != null) Instance.PlayFall();
        }

        public static void Win()
        {
            if (Instance != null) Instance.PlayWin();
        }

        public static void Click()
        {
            if (Instance != null) Instance.PlayClick();
        }
    }
}
