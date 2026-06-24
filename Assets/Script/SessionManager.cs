using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    [SerializeField] private int subtitleMaxCharactersPerLine = 42;
    [SerializeField] private bool forceSubtitleLineBreaks = true;

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

        ConfigureSubtitleText();
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
            Debug.Log($"Continuing dialogue from index {index}. Total lines: {lines.Count}");
            showTextCoroutine = StartCoroutine(showText());
        }
        else
        {
            Debug.Log($"ContinueDialogue ignored. Coroutine active: {showTextCoroutine != null}, active and enabled: {isActiveAndEnabled}");
        }
    }

    IEnumerator showText()
    {
        while (index >= 0 && index < lines.Count)
        {
            curText = lines[index].text;
            if (string.IsNullOrEmpty(curText))
            {
                Debug.LogWarning($"Skipping empty dialogue line at index {index}.");
                index++;
                continue;
            }

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
                subtitleText.text = FormatSubtitleText(curText);
                androidTTS?.Speak(curText);
                SetAnimation();
                index++;

            }


            float seconds = Mathf.Clamp(1f + curText.Length / 900f * 60f, 2.0f, 12.0f);
            yield return new WaitForSeconds(seconds);
        }

        Debug.Log("Dialogue reached the end. Waiting for AI reply.");
        subtitleText.text = "";
        showTextCoroutine = null;
        yield break;
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

    private void ConfigureSubtitleText()
    {
        if (subtitleText == null)
        {
            Debug.LogWarning("SessionManager subtitleText is not assigned.");
            return;
        }

        subtitleText.enableWordWrapping = true;
        subtitleText.overflowMode = TextOverflowModes.Overflow;
    }

    private string FormatSubtitleText(string text)
    {
        if (!forceSubtitleLineBreaks || string.IsNullOrEmpty(text) || subtitleMaxCharactersPerLine <= 0)
        {
            return text;
        }

        string normalizedText = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n");

        string[] sourceLines = normalizedText.Split('\n');
        StringBuilder wrappedText = new StringBuilder(normalizedText.Length + sourceLines.Length);

        for (int i = 0; i < sourceLines.Length; i++)
        {
            if (i > 0)
            {
                wrappedText.Append('\n');
            }

            wrappedText.Append(WrapSubtitleLine(sourceLines[i], subtitleMaxCharactersPerLine));
        }

        return wrappedText.ToString();
    }

    private string WrapSubtitleLine(string line, int maxCharactersPerLine)
    {
        if (string.IsNullOrEmpty(line) || line.Length <= maxCharactersPerLine)
        {
            return line;
        }

        StringBuilder result = new StringBuilder(line.Length + line.Length / maxCharactersPerLine);
        StringBuilder currentLine = new StringBuilder(maxCharactersPerLine + 16);

        foreach (char character in line)
        {
            currentLine.Append(character);

            if (currentLine.Length < maxCharactersPerLine)
            {
                continue;
            }

            int breakIndex = FindLastSubtitleBreak(currentLine, Mathf.RoundToInt(maxCharactersPerLine * 0.55f));
            if (breakIndex < 0)
            {
                result.Append(currentLine.ToString().TrimEnd());
                result.Append('\n');
                currentLine.Length = 0;
                continue;
            }

            result.Append(currentLine.ToString(0, breakIndex + 1).TrimEnd());
            result.Append('\n');

            string remainder = currentLine.ToString(breakIndex + 1, currentLine.Length - breakIndex - 1).TrimStart();
            currentLine.Length = 0;
            currentLine.Append(remainder);
        }

        if (currentLine.Length > 0)
        {
            result.Append(currentLine.ToString().Trim());
        }

        return result.ToString();
    }

    private int FindLastSubtitleBreak(StringBuilder line, int minBreakIndex)
    {
        for (int i = line.Length - 1; i >= minBreakIndex; i--)
        {
            char character = line[i];
            if (char.IsWhiteSpace(character) || char.IsPunctuation(character))
            {
                return i;
            }
        }

        return -1;
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

    public void AddLinesToSession(string line, RobotAnimation rA, BubbleAnimation ba)
    {
        DialogueLine newLine = new DialogueLine{text = line, robotAnimation = rA, bubbleAnimation = ba};
        lines.Add(newLine);
    }
}
