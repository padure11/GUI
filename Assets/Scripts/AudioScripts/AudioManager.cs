using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("UI Clips")]
    public AudioClip uiClick;
    public AudioClip uiHover;
    public AudioClip levelComplete;
    public AudioClip codeRun;
    public AudioClip codeError;

    [Header("Robot Clips")]
    public AudioClip walkStep;
    public AudioClip jump;
    public AudioClip land;
    public AudioClip push;
    public AudioClip press;
    public AudioClip pull;
    public AudioClip die;
    public AudioClip turn;

    [Header("Mechanic Clips")]
    public AudioClip leverPull;
    public AudioClip buttonPress;
    public AudioClip portalEnter;
    public AudioClip electricBuzz;
    public AudioClip cratePush;
    public AudioClip plateActivate;

    [Header("Defaults")]
    [Range(0f, 1f)] public float defaultMusicVolume = 0.5f;
    [Range(0f, 1f)] public float defaultSfxVolume = 0.8f;

    private const string PrefMusicVolume = "audio.musicVolume";
    private const string PrefSfxVolume = "audio.sfxVolume";

    private float musicVolume;
    private float sfxVolume;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        musicVolume = PlayerPrefs.GetFloat(PrefMusicVolume, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(PrefSfxVolume, defaultSfxVolume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PrefMusicVolume, musicVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PrefSfxVolume, sfxVolume);
        ApplyVolumes();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // Shortcuts so callers do not have to reach into the clips bag themselves.
    public void PlayUIClick() => PlaySFX(uiClick);
    public void PlayJump() => PlaySFX(jump);
    public void PlayLand() => PlaySFX(land);
    public void PlayWalkStep() => PlaySFX(walkStep);
    public void PlayPush() => PlaySFX(push);
    public void PlayPress() => PlaySFX(press);
    public void PlayPull() => PlaySFX(pull);
    public void PlayDie() => PlaySFX(die);
    public void PlayTurn() => PlaySFX(turn);
    public void PlayLevelComplete() => PlaySFX(levelComplete);
    public void PlayCodeRun() => PlaySFX(codeRun);
    public void PlayLeverPull() => PlaySFX(leverPull);
    public void PlayButtonPress() => PlaySFX(buttonPress);
    public void PlayPortalEnter() => PlaySFX(portalEnter);
    public void PlayCratePush() => PlaySFX(cratePush);
    public void PlayPlateActivate() => PlaySFX(plateActivate);
}
