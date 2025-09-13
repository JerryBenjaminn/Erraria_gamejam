using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I; // kyll‰, yksi kirjan merkki riitt‰‰ jamissa

    [Header("Mixer")]
    public AudioMixer masterMixer;     // Master.mixer
    [Range(-80, 0)] public float defaultMusicDb = -8f;
    [Range(-80, 0)] public float defaultSfxDb = -6f;

    [Header("Music")]
    public AudioMixerGroup musicGroup;
    public AudioClip menuMusic;
    public AudioClip levelMusic;
    public AudioClip bossMusic;
    public float musicFadeTime = 0.8f;

    [Header("SFX")]
    public AudioMixerGroup sfxGroup;
    [Tooltip("Helppo avain->clip kirja. Lis‰‰ t‰h‰n kaikki SFX:t.")]
    public AudioClip[] sfxClips;                // drag n drop
    public string[] sfxKeys;                    // samassa j‰rjestyksess‰
    Dictionary<string, AudioClip> sfxMap;

    [Header("Pooling")]
    public int sfxPoolSize = 10;

    // Music: kaksi kanavaa ristiinh‰ivytykseen
    AudioSource musicA, musicB;
    AudioSource[] sfxPool;
    int sfxIndex;
    AudioSource currentMusic, nextMusic;
    float musicFade;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Map SFX
        sfxMap = new Dictionary<string, AudioClip>();
        for (int i = 0; i < Mathf.Min(sfxClips.Length, sfxKeys.Length); i++)
            if (!sfxMap.ContainsKey(sfxKeys[i]) && sfxClips[i]) sfxMap.Add(sfxKeys[i], sfxClips[i]);

        // L‰hteet
        musicA = CreateSource("MusicA", musicGroup, true);
        musicB = CreateSource("MusicB", musicGroup, true);
        currentMusic = musicA;
        nextMusic = musicB;

        sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
            sfxPool[i] = CreateSource("SFX_" + i, sfxGroup, false);

        // Volumes
        masterMixer.SetFloat("MusicVol", defaultMusicDb);
        masterMixer.SetFloat("SFXVol", defaultSfxDb);
    }

    AudioSource CreateSource(string name, AudioMixerGroup group, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.outputAudioMixerGroup = group;
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f; // 2D oletus
        src.dopplerLevel = 0f;
        src.reverbZoneMix = 0f;
        return src;
    }

    void Update()
    {
        // Crossfade
        if (musicFade > 0f)
        {
            musicFade -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(musicFade / musicFadeTime);
            currentMusic.volume = 1f - t;
            nextMusic.volume = t;
            if (musicFade <= 0f)
            {
                // vaihda roolit
                var tmp = currentMusic;
                currentMusic = nextMusic;
                nextMusic = tmp;
                nextMusic.Stop();
                nextMusic.volume = 0f;
                currentMusic.volume = 1f;
            }
        }
    }

    // ---------- Public API ----------

    public static void PlayMusic(AudioClip clip, float? fadeTime = null)
    {
        if (!I || !clip) return;
        if (I.currentMusic.clip == clip && I.currentMusic.isPlaying) return;

        I.nextMusic.clip = clip;
        I.nextMusic.volume = 0f;
        I.nextMusic.Play();
        I.musicFadeTime = fadeTime ?? I.musicFadeTime;
        I.musicFade = I.musicFadeTime;
    }

    public static void PlayMenuMusic() => PlayMusic(I.menuMusic);
    public static void PlayLevelMusic() => PlayMusic(I.levelMusic);
    public static void PlayBossMusic() => PlayMusic(I.bossMusic);

    public static void PlaySFX(string key, float pitchVar = 0.0f)
    {
        if (!I || !I.sfxMap.TryGetValue(key, out var clip) || clip == null) return;
        var src = I.sfxPool[I.sfxIndex = (I.sfxIndex + 1) % I.sfxPool.Length];
        src.spatialBlend = 0f; // 2D
        src.transform.position = I.transform.position;
        src.pitch = 1f + Random.Range(-pitchVar, pitchVar);
        src.PlayOneShot(clip);
    }

    public static void PlaySFXAt(string key, Vector3 worldPos, float pitchVar = 0.0f)
    {
        if (!I || !I.sfxMap.TryGetValue(key, out var clip) || clip == null) return;
        var src = I.sfxPool[I.sfxIndex = (I.sfxIndex + 1) % I.sfxPool.Length];
        src.spatialBlend = 1f; // 3D
        src.transform.position = worldPos;
        src.minDistance = 3f;
        src.maxDistance = 25f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.pitch = 1f + Random.Range(-pitchVar, pitchVar);
        src.PlayOneShot(clip);
    }

    public static void SetMusicDb(float db) { if (I) I.masterMixer.SetFloat("MusicVol", db); }
    public static void SetSfxDb(float db) { if (I) I.masterMixer.SetFloat("SFXVol", db); }
}
