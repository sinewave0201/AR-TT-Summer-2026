using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public enum VoiceRole : byte
    {
        Bubbly,
        Sleepy,
        Soft
    }

    public enum RobotSound : byte
    {
        Greeting,
        Happy,
        Neutral,
        Sad,
        Talking,
        Comforting,
        Concerned,
        Curious,
        Relieved
    }

    [Serializable]
    private struct VoiceCollection
    {
        public VoiceRole role;
        public List<AudioClip> clips;
    }

    [SerializeField] private AudioSource robotAudioSource;
    [SerializeField] private VoiceRole currentVoiceRole = VoiceRole.Bubbly;
    [SerializeField] private List<VoiceCollection> voiceCollections = new List<VoiceCollection>();

    private void Awake()
    {
        if (robotAudioSource == null)
        {
            robotAudioSource = GetComponent<AudioSource>();
        }
    }

    public void SelectVoiceRole(VoiceRole role)
    {
        currentVoiceRole = role;
    }

    public void Play(RobotSound sound)
    {
        if (robotAudioSource == null)
        {
            Debug.LogWarning("SoundManager cannot find the Robot AudioSource.", this);
            return;
        }

        VoiceCollection collection = default;
        bool foundCollection = false;
        for (int i = 0; i < voiceCollections.Count; i++)
        {
            if (voiceCollections[i].role != currentVoiceRole)
            {
                continue;
            }

            collection = voiceCollections[i];
            foundCollection = true;
            break;
        }

        int clipIndex = (int)sound;
        if (!foundCollection || collection.clips == null ||
            clipIndex < 0 || clipIndex >= collection.clips.Count ||
            collection.clips[clipIndex] == null)
        {
            Debug.LogWarning(
                $"No {currentVoiceRole}/{sound} clip is assigned in SoundManager.",
                this);
            return;
        }

        robotAudioSource.Stop();
        robotAudioSource.clip = collection.clips[clipIndex];
        robotAudioSource.Play();
    }
}
