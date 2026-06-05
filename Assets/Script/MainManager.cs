using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


public class MainManager : MonoBehaviour
{
    public enum RobotAnimation
    {
        Idle,
        Wave,
        Nod
    }

    [System.Serializable]
    public struct DialogueLine
    {
        public string text;
        public RobotAnimation animation;
    }

    [Header("animation")]
    public Animator animator;

    [Header("Dialogue")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("UI")]
    public TMP_Text subtitleText;

    [Header("Audio / TTS")]
    // public TTSClient ttsClient;
    public AudioSource audioSource;

    public int index = 0;
    public AndroidTTS androidTTS;
    public ShowWhatManager showWhatManager;
    public string curText;

    private Coroutine showTextCoroutine;


    void Start ()
    {
        if (!TryGetComponent(out androidTTS))
        {
            androidTTS = gameObject.AddComponent<AndroidTTS>();
        }

        if (!TryGetComponent(out showWhatManager))
        {
            showWhatManager = gameObject.AddComponent<ShowWhatManager>();
        }

        StartCoroutine(showText());
    }

    public void ContinueDialogue()
    {
        if (showTextCoroutine == null && isActiveAndEnabled)
        {
            showTextCoroutine = StartCoroutine(showText());
        }
    }

    IEnumerator showText()
    {
        while (index >= 0 && index < lines.Count)
        {
            curText = lines[index].text;
            var command = curText.Trim();

            if (command == "$input$")
            {
                index++;
                showWhatManager.ActivateInput();
                yield break;
            }

            else if (command == "$interact$")
            {
                index++;
                showWhatManager.ActivateInteract();
                yield break;
            }

            else
            {            
                subtitleText.text = curText;
                androidTTS?.Speak(curText);
                SetAnimation();
                index++;

            }


            float seconds = Mathf.Clamp(1f + curText.Length / 900f * 60f, 2.0f, 12.0f);
            yield return new WaitForSeconds(seconds);
        }

        subtitleText.text = "";
        showTextCoroutine = null;
    }

    private void SetAnimation()
    {
        RobotAnimation curAnimation = lines[index].animation;
        Debug.Log("Current animation: " + curAnimation);

        if (curAnimation == RobotAnimation.Idle)
        {
            return;
        }

        animator.SetTrigger(curAnimation.ToString());
    }
}
