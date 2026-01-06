using UnityEngine;


public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;   // looping music
    public AudioSource sfxSource;   // one-shot sounds

    [Header("Clips")]
    public AudioClip backgroundMusic;
    public AudioClip beamAttackSound;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayBGM();
    }

    // ---------------- BGM ----------------
    public void PlayBGM()
    {
        if (bgmSource == null || backgroundMusic == null) return;

        bgmSource.clip = backgroundMusic;
        bgmSource.loop = true;
        bgmSource.volume = 0.5f;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    // ---------------- SFX ----------------
    public void PlayBeamAttack()
    {
        if (sfxSource == null || beamAttackSound == null) return;

        sfxSource.PlayOneShot(beamAttackSound, 1f);
    }
}
