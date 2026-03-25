using UnityEngine;

// 전역 사운드 매니저. BGM + 효과음 재생 관리.
// _Managers 오브젝트에 부착.
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.4f;

    [Header("SFX")]
    [SerializeField] private AudioClip miningClip;
    [SerializeField] private AudioClip stackClip;
    [SerializeField] private AudioClip upgradeClip;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.8f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private float lastMiningTime = -99f;
    private const float miningCooldown = 0.1f;  // 100ms 내 중복 채굴음 무시

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
    }

    void Start()
    {
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    public void PlayMining()
    {
        if (Time.time - lastMiningTime < miningCooldown) return;
        lastMiningTime = Time.time;
        PlaySFX(miningClip);
    }
    public void PlayStack()   => PlaySFX(stackClip);
    public void PlayUpgrade() => PlaySFX(upgradeClip);

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
