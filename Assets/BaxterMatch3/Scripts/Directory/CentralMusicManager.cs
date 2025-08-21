

using HelperScripts;
using UnityEngine;
using UnityEngine.Audio;

namespace Internal.Scripts
{
    /// <summary>
    /// Music manager
    /// </summary>
    public class CentralMusicManager : MonoBehaviour
    {
        public static CentralMusicManager Instance;
        public AudioClip[] music;
        private AudioSource audioSource;
        public AudioMixer audioMixer;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioMixer = audioSource.outputAudioMixerGroup.audioMixer;
            audioSource.loop = true;
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);
            DontDestroyOnLoad(this);
        }

        public void ChangeMusicPlayStatus()
        {
            if (audioSource.isPlaying)
            {
                audioMixer.SetFloat("MusicVolume", -80);
                audioSource.Pause();
            }
            else
            {
                if (GlobalValue.IsMusicOn)
                {
                    audioMixer.SetFloat("MusicVolume", 1);
                    audioSource.Play();
                }
            }
        }

        private void Start()
        {
            audioMixer.SetFloat("MusicVolume", PlayerPrefs.GetInt("Music"));
        }

        private void OnEnable()
        {
            MainManager.OnMapState += OnMapState;
            MainManager.OnEnterGame += OnGameState;
        }

        private void OnDisable()
        {
            MainManager.OnMapState -= OnMapState;
            MainManager.OnEnterGame -= OnGameState;
        }

        private void OnGameState()
        {
            if (audioSource.clip == music[0])
            {
                audioSource.clip = music[Random.Range(1, 3)];
            }

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        private void OnMapState()
        {
            if (audioSource.clip != music[0])
            {
                audioSource.clip = music[0];
            }

            if (!audioSource.isPlaying)
                audioSource.Play();
        }
    }
}