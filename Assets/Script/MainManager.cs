using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


public class MainManager : MonoBehaviour
{
    [Header("Dialogue Texts")]
    public List<string> lines = new List<string>();

    [Header("UI")]
    public TMP_Text subtitleText;

    [Header("Audio / TTS")]
    // public TTSClient ttsClient;
    public AudioSource audioSource;

    public int index = 0;
    public AndroidTTS androidTTS;
    public ShowWhatManager showWhatManager;
    public string curText;


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

    IEnumerator showText()
    {
        while (index >= 0 && index < lines.Count)
        {
            curText = lines[index];
            var command = curText.Trim();

            if (command == "$input$")
            {
                index++;
                showWhatManager.ActivateInput();
            }

            else if (command == "$interact$")
            {
                index++;
                showWhatManager.ActivateInteract();
            }

            else
            {            
                subtitleText.text = curText;
                androidTTS?.Speak(curText);
                index++;
            }


            yield return new WaitForSeconds(4f);
        }

        subtitleText.text = "";
    }
}
