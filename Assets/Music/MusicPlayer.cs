using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicPlayer : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private List<AudioClip> musicFiles = new List<AudioClip>();
    [SerializeField] private AudioSource audioSource;

    [Header("Controls")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text currentSongText;

    private int currentSongIndex;
    private bool songHasStarted;

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(PlayPreviousSong);

        if (nextButton != null)
            nextButton.onClick.AddListener(PlayNextSong);
    }

    private void Start()
    {
        if (musicFiles.Count > 0 && audioSource != null)
            PlaySong(currentSongIndex);
        else
            UpdateSongName();
    }

    private void Update()
    {
        // isPlaying becomes false when the current clip reaches its end.
        if (songHasStarted && audioSource != null && !audioSource.isPlaying)
            PlayNextSong();
    }

    public void PlayPreviousSong()
    {
        if (!CanPlayMusic())
            return;

        currentSongIndex =
            (currentSongIndex - 1 + musicFiles.Count) % musicFiles.Count;
        PlaySong(currentSongIndex);
    }

    public void PlayNextSong()
    {
        if (!CanPlayMusic())
            return;

        currentSongIndex = (currentSongIndex + 1) % musicFiles.Count;
        PlaySong(currentSongIndex);
    }

    private void PlaySong(int index)
    {
        AudioClip clip = musicFiles[index];

        if (clip == null)
        {
            songHasStarted = false;
            UpdateSongName();
            return;
        }

        audioSource.clip = clip;
        audioSource.Play();
        songHasStarted = true;
        UpdateSongName();
    }

    private bool CanPlayMusic()
    {
        return audioSource != null && musicFiles.Count > 0;
    }

    private void UpdateSongName()
    {
        if (currentSongText == null)
            return;

        AudioClip clip = audioSource != null ? audioSource.clip : null;
        currentSongText.text = clip != null ? clip.name : string.Empty;
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(PlayPreviousSong);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(PlayNextSong);
    }
}
