using UnityEngine;

[CreateAssetMenu(
    fileName = "_Minigame",
    menuName = "Scriptable Objects/Minigame Template"
)]
public class MinigameTemplateSO : ScriptableObject
{
    [Header("Identity")]
    public string displayName;

    [TextArea]
    public string description;

    [Header("Music")]
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 1f;

    [Header("Ambience")]
    public AudioClip ambience;
    [Range(0f, 1f)] public float ambienceVolume = 1f;

    [Header("Stingers")]
    public AudioClip gameStartSound;
    [Range(0f, 1f)] public float gameStartVolume = 1f;
    public AudioClip failureSound;
    [Range(0f, 1f)] public float failureVolume = 1f;
    public AudioClip resetSound;
    [Range(0f, 1f)] public float resetVolume = 1f;

    [Header("Future Gameplay Settings")]
    public float defaultRoundDuration = 60f;
}