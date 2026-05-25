using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


public class DialogueManager : MonoBehaviour
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
    public InputManager inputManager;
    public string curText;


    void Start ()
    {
        if (!TryGetComponent(out androidTTS))
        {
            androidTTS = gameObject.AddComponent<AndroidTTS>();
        }

        if (!TryGetComponent(out inputManager))
        {
            inputManager = gameObject.AddComponent<InputManager>();
        }

        StartCoroutine(showText());
    }

    IEnumerator showText()
    {
        while (index >= 0 && index < lines.Count)
        {
            curText = lines[index];
            if (curText == "$input$")
            {
                inputManager.ActivateInput();
                index++;
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
