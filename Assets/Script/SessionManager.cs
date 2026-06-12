using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


public class SessionManager : MonoBehaviour
{
    public enum BubbleAnimation
    {
        Default,
        Appear
    }

    public enum RobotAnimation
    {
        Idle,
        Wave,
        Nod,
        GetBubble
    }

    [System.Serializable]
    public struct DialogueLine
    {
        public string text;
        public RobotAnimation robotAnimation;
        public BubbleAnimation bubbleAnimation;
    }

    [Header("animation")]
    public Animator robotAnimator;
    public Animator bubbleAnimator;

    [Header("Dialogue")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("UI")]
    public TMP_Text subtitleText;

    [Header("Audio / TTS")]
    // public TTSClient ttsClient;
    public AudioSource audioSource;

    public int index = 0;
    public AndroidTTS androidTTS;
    public SessionShowManager sessionShowManager;
    public string curText;

    private Coroutine showTextCoroutine;

    [Header("End Of Session Logic")]
    public GameObject endSessionPanel;

    void Start ()
    {
        if (!TryGetComponent(out androidTTS))
        {
            androidTTS = gameObject.AddComponent<AndroidTTS>();
        }

        if (!TryGetComponent(out sessionShowManager))
        {
            sessionShowManager = gameObject.AddComponent<SessionShowManager>();
        }
    }


    public void BeginSession()
    {
        index = 0;
        endSessionPanel.SetActive(false);
        sessionShowManager.ResetToDefault();
        showTextCoroutine = StartCoroutine(showText());
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

            if (command.Length >= 2 && command[0] == '$' && command[command.Length - 1] == '$')
            {
                string commandName = command.Substring(1, command.Length - 2).Trim();
                index++;
                sessionShowManager.Activate(commandName);
                showTextCoroutine = null;
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

        Debug.Log("end of session!");
        subtitleText.text = "";
        showTextCoroutine = null;
    }

    private void SetAnimation()
    {
        RobotAnimation curRobotAnimation = lines[index].robotAnimation;
        BubbleAnimation curBubbleAnimation = lines[index].bubbleAnimation;

        if (curRobotAnimation == RobotAnimation.Idle)
        {
            if (curBubbleAnimation == BubbleAnimation.Default)
            {
                return;
            }
            else
            {
                bubbleAnimator.SetTrigger(curBubbleAnimation.ToString());
            }
        }

        else
        {
            robotAnimator.SetTrigger(curRobotAnimation.ToString());

            if (curBubbleAnimation == BubbleAnimation.Default)
            {
                return;
            }
            else
            {
                bubbleAnimator.SetTrigger(curBubbleAnimation.ToString());
            }
        }
    }

    public void EndSession()
    {
        // 2. 清掉 session 显示内容
        subtitleText.text = "";

        // 3. 关掉 EndSession panel
        endSessionPanel.SetActive(false);

        // 4. 把 SessionShowManager 回到 defaultObjects 状态
        sessionShowManager.ResetToDefault();

        // 5. 重置 dialogue index 下一次重新开始
        index = 0;
        showTextCoroutine = null;

        // 6. 最后关闭整个 session UI / session manager
        gameObject.SetActive(false);
    }
}
