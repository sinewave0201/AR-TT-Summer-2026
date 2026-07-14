using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicPlayer : MonoBehaviour
{
    private static AudioSource persistentAudioSource;

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
        BindPersistentAudioSource();

        if (previousButton != null)
        {
            previousButton.onClick.AddListener(PlayPreviousSong);
        }
            

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(PlayNextSong);
        }

    }

    private void Start()
    {
        if (audioSource == null)
        {
            Debug.LogError(
                "No GameObject tagged musicPlayer with an AudioSource was found.",
                this
            );
            UpdateSongName();
            return;
        }

        int clipIndex = musicFiles.IndexOf(audioSource.clip);
        if (clipIndex >= 0)
        {
            currentSongIndex = clipIndex;
        }

        UpdateSongName();
        songHasStarted = audioSource.isPlaying;
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

    private void BindPersistentAudioSource()
    {
        if (persistentAudioSource == null)
        {
            GameObject musicObject = GameObject.FindGameObjectWithTag("musicPlayer");
            if (musicObject == null)
            {
                audioSource = null;
                return;
            }

            persistentAudioSource = musicObject.GetComponent<AudioSource>();
            if (persistentAudioSource == null)
            {
                audioSource = null;
                Debug.LogError(
                    "The GameObject tagged musicPlayer has no AudioSource.",
                    musicObject
                );
                return;
            }

            // This is the standalone BGM object shown at the scene root.
            DontDestroyOnLoad(persistentAudioSource.gameObject);
        }

        audioSource = persistentAudioSource;
        StopDuplicateSceneBgmSources();
    }

    private static void StopDuplicateSceneBgmSources()
    {
        GameObject[] musicObjects = GameObject.FindGameObjectsWithTag("musicPlayer");

        foreach (GameObject musicObject in musicObjects)
        {
            AudioSource source = musicObject.GetComponent<AudioSource>();
            if (source == null || source == persistentAudioSource)
            {
                continue;
            }

            source.playOnAwake = false;
            source.Stop();
            source.enabled = false;
        }
    }

    private void UpdateSongName()
    {
        if (currentSongText == null)
            return;

        AudioClip clip = audioSource != null ? audioSource.clip : null;
        currentSongText.text = clip != null ? clip.name : string.Empty;

        Debug.Log("Song Name Updated");
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(PlayPreviousSong);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(PlayNextSong);
    }
}
