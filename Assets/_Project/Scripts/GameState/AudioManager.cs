using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    private const string MasterVolume = "MasterVolume";
    private const string MusicVolume = "MusicVolume";
    private const string AmbienceVolume = "AmbienceVolume";
    private const string SfxVolume = "SFXVolume";
    private const string UiVolume = "UIVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic() { if (musicSource != null) musicSource.Stop(); }
    public void PauseMusic() { if (musicSource != null) musicSource.Pause(); }
    public void ResumeMusic() { if (musicSource != null && musicSource.clip != null) musicSource.UnPause(); }

    public void PlayAmbience(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (ambienceSource == null || clip == null) return;
        ambienceSource.clip = clip;
        ambienceSource.volume = volume;
        ambienceSource.loop = loop;
        ambienceSource.Play();
    }

    public void StopAmbience() { if (ambienceSource != null) ambienceSource.Stop(); }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlayUI(AudioClip clip, float volumeScale = 1f)
    {
        if (uiSource == null || clip == null) return;
        uiSource.PlayOneShot(clip, volumeScale);
    }

    public void SetMasterVolume(float value) => SetMixerVolume(MasterVolume, value);
    public void SetMusicVolume(float value) => SetMixerVolume(MusicVolume, value);
    public void SetAmbienceVolume(float value) => SetMixerVolume(AmbienceVolume, value);
    public void SetSFXVolume(float value) => SetMixerVolume(SfxVolume, value);
    public void SetUIVolume(float value) => SetMixerVolume(UiVolume, value);

    private void SetMixerVolume(string parameter, float value)
    {
        if (audioMixer == null) return;
        value = Mathf.Clamp(value, 0.0001f, 1f);
        audioMixer.SetFloat(parameter, Mathf.Log10(value) * 20f);
    }
}