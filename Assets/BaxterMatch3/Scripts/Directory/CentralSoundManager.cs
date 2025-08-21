using System.Collections;
using System.Collections.Generic;
using HelperScripts;
using UnityEngine;
using UnityEngine.Audio;

namespace Internal.Scripts
{
    /// <summary>
    /// Sound manager
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CentralSoundManager : MonoBehaviour
    {
        public static CentralSoundManager Instance;
        public AudioClip click;
        public AudioClip[] swish;
        public AudioClip[] drop;
        public AudioClip alert;
        public AudioClip timeOut;
        public AudioClip[] star;
        public AudioClip[] gameOver;
        public AudioClip cash;

        public AudioClip[] destroy;
        public AudioClip boostBomb;
        public AudioClip boostColorReplace;
        public AudioClip explosion;
        public AudioClip explosion2;
        public AudioClip getStarIngr;
        public AudioClip strippedExplosion;
        public AudioClip[] complete;
        public AudioClip block_destroy;
        public AudioClip wrongMatch;
        public AudioClip noMatch;
        public AudioClip appearStipedColorBomb;
        public AudioClip appearPackage;
        public AudioClip hammerHitEffect;
        public AudioClip cannonHitEffect;
        public AudioClip shuffleEffect;
        public AudioClip arrowHitEffect;
        public AudioClip windowBreakEffect;
        public AudioClip woodSmashEffect;
        public AudioClip bombExplodeEffect;
        public AudioClip chopperExplodeEffect;
        public AudioClip discoBallExplodeEffect;
        public AudioClip match3Effect;
        private AudioSource _audioSource;
        public AudioMixer audioMixer;
        private List<AudioClip> _clipsPlaying = new List<AudioClip>();

        ///SoundBase.Instance.audio.PlayOneShot( SoundBase.Instance.kreakWheel );

        // Use this for initialization
        void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            audioMixer = _audioSource.outputAudioMixerGroup?.audioMixer;
            if (transform.parent == null)
            {
                transform.parent = Camera.main?.transform;
                transform.localPosition = Vector3.zero;
            }

            // DontDestroyOnLoad(gameObject);
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);
        }

        private void Start()
        {
            audioMixer?.SetFloat("SoundVolume", PlayerPrefs.GetInt("Sound"));
        }

        public void PlayOneShot(AudioClip audioClip)
        {
            if (audioClip == null) return;
            MainManager.HapticAndShake();
            if (!GlobalValue.IsSoundOn) return;
            _audioSource.PlayOneShot(audioClip);
        }

        public void PlaySoundsRandom(AudioClip[] clip)
        {
            if (clip == null) return;
            if (clip.Length > 0)
                PlayOneShot(clip[Random.Range(0, clip.Length)]);
        }

        public void PlayLimitSound(AudioClip clip)
        {
            if (clip == null) return;
            if (_clipsPlaying.IndexOf(clip) < 0)
            {
                _clipsPlaying.Add(clip);
                PlayOneShot(clip);
                StartCoroutine(WaitForCompleteSound(clip));
            }
        }

        private IEnumerator WaitForCompleteSound(AudioClip clip)
        {
            if (clip == null) yield break;
            yield return new WaitForSeconds(0.2f);
            _clipsPlaying.Remove(_clipsPlaying.Find(x => clip));
        }
    }
}