using FishNet.Object;
using UnityEngine;

public class HoleInTheWallAudio : NetworkBehaviour
{
    [SerializeField] private MinigameTemplateSO audioDefinition;
    [SerializeField] private bool cutMusicOnFailure = true;
    [SerializeField] private bool keepAmbienceOnFailure = true;
    [SerializeField] private bool restartMusicFromBeginning = true;

    private void OnEnable()
    {
        MinigameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        MinigameStateManager.OnStateChanged -= HandleStateChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartAudioObserversRpc();
    }

    [ObserversRpc(BufferLast = true)]
    private void StartAudioObserversRpc()
    {
        StartGameAudio();
    }

    private void HandleStateChanged(MinigameState state)
    {
        if (AudioManager.Instance == null || audioDefinition == null) return;

        switch (state)
        {
            case MinigameState.Playing:
                StartGameAudio();
                break;

            case MinigameState.Failed:
                HandleFailure();
                break;

            case MinigameState.Resetting:
                HandleReset();
                break;
        }
    }

    private void StartGameAudio()
    {
        if (AudioManager.Instance == null || audioDefinition == null) return;

        if (restartMusicFromBeginning)
            AudioManager.Instance.StopMusic();

        AudioManager.Instance.PlayMusic(audioDefinition.music, audioDefinition.musicVolume);
        AudioManager.Instance.PlayAmbience(audioDefinition.ambience, audioDefinition.ambienceVolume);
        AudioManager.Instance.PlaySFX(audioDefinition.gameStartSound, audioDefinition.gameStartVolume);
    }

    private void HandleFailure()
    {
        if (cutMusicOnFailure)
            AudioManager.Instance.StopMusic();

        if (!keepAmbienceOnFailure)
            AudioManager.Instance.StopAmbience();

        AudioManager.Instance.PlaySFX(audioDefinition.failureSound, audioDefinition.failureVolume);
    }

    private void HandleReset()
    {
        AudioManager.Instance.PlaySFX(audioDefinition.resetSound, audioDefinition.resetVolume);
    }
}