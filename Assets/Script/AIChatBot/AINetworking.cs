using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class AINetworking : MonoBehaviour
{
    [System.Serializable]
    public class ChatRequest
    {
        public string message;
        public string session_id;
    }

    [System.Serializable]
    public class ChatResponse
    {
        public string reply;
        public string action;
        public string[] thoughts;
    }

    private string sessionId;
    private string chatUrl = "https://tt-chatbot.onrender.com/chat";

    [Header("Session")]
    public SessionManager sessionManager;
    public VaultManager vaultManager;

    [Header("Input")]
    public TMP_InputField inputField;

    [Header("UI")]
    public GameObject loading;

    [Header("End Session")]
    [SerializeField] private MainSelectManager mainSelectManager;
    [SerializeField] private GameObject completedSessionCanvas;
    [SerializeField] private SessionTracker sessionTracker;

    [Header("Networking")]
    [SerializeField, Min(1)] private int requestTimeoutSeconds = 90;
    [SerializeField] private int maxRetryCount = 1;
    [SerializeField] private float retryDelaySeconds = 1.5f;
    [SerializeField] private string requestFailedMessage = "Sorry, I could not reach the AI service. Please check your internet connection and try again.";

    private bool isWaitingForAI;

    void Start()
    {
        if (PlayerPrefs.HasKey("device_session_id"))
        {
            sessionId = PlayerPrefs.GetString("device_session_id");
        }
        else
        {
            sessionId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("device_session_id", sessionId);
            PlayerPrefs.Save();
        }
    }

    public void SendMSGToAI()
    {
        if (isWaitingForAI)
        {
            Debug.LogWarning("AI request ignored because another request is still waiting for a reply.");
            return;
        }

        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text))
        {
            Debug.LogWarning("AI request ignored because the input field is empty.");
            return;
        }

        Debug.Log($"MSG sent to AI: {inputField.text}");
        StartCoroutine(SendChatRequest(inputField.text));
    }

    IEnumerator SendChatRequest(string msg)
    {
        isWaitingForAI = true;
        SetLoading(true);

        float startTime = Time.realtimeSinceStartup;
        ChatRequest newCR = new ChatRequest{message = msg, session_id = sessionId};
        string json = JsonUtility.ToJson(newCR);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = null;
        int attempt = 0;
        while (attempt <= maxRetryCount)
        {
            attempt++;
            request = CreateChatRequest(bodyRaw);

            Debug.Log($"AI request started. attempt={attempt}/{maxRetryCount + 1}, url={chatUrl}, session_id={sessionId}");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                break;
            }

            float failedElapsed = Time.realtimeSinceStartup - startTime;
            Debug.LogError($"AI request attempt {attempt} failed after {failedElapsed:F1}s. result={request.result}, status={request.responseCode}, error={request.error}");
            Debug.LogError($"AI error body: {request.downloadHandler.text}");

            if (attempt > maxRetryCount)
            {
                break;
            }

            request.Dispose();
            request = null;
            Debug.LogWarning($"Retrying AI request in {retryDelaySeconds:F1}s...");
            yield return new WaitForSeconds(retryDelaySeconds);
        }

        float elapsed = Time.realtimeSinceStartup - startTime;
        isWaitingForAI = false;
        SetLoading(false);

        if (request == null || request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"AI request failed after all retries. totalElapsed={elapsed:F1}s");
            request?.Dispose();
            ShowRequestFailedMessage();
            yield break;
        }

        Debug.Log($"AI reply received after {elapsed:F1}s. status={request.responseCode}, body={request.downloadHandler.text}");
        ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
        request.Dispose();

        ResponseHandeler(response);
    }

    private UnityWebRequest CreateChatRequest(byte[] bodyRaw)
    {
        UnityWebRequest request = new UnityWebRequest(chatUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = requestTimeoutSeconds;
        return request;
    }

    private void SetLoading(bool isActive)
    {
        if (loading != null)
        {
            loading.SetActive(isActive);
        }
    }

    private void ShowRequestFailedMessage()
    {
        if (sessionManager == null || string.IsNullOrWhiteSpace(requestFailedMessage))
        {
            return;
        }

        sessionManager.AddLinesToSession(requestFailedMessage, SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default);
        sessionManager.AddLinesToSession("$input$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default);
        sessionManager.ContinueDialogue();
    }

    void ResponseHandeler(ChatResponse response)
    {
        if (response == null)
        {
            Debug.LogError("AI response could not be parsed.");
            ShowRequestFailedMessage();
            return;
        }

        string action = response.action;
        string responseText = response.reply;

        Debug.Log($"Handling AI response. action={action}, message={responseText}");

        if (string.IsNullOrEmpty(action) || action == "null")
        {
            if (string.IsNullOrEmpty(responseText))
            {
                Debug.LogError("AI response did not contain message or reply text.");
                ShowRequestFailedMessage();
                return;
            }

            AddResponseTextToSession(responseText);
            sessionManager.AddLinesToSession("$input$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default);
            sessionManager.ContinueDialogue();
        }

        else if (action == "end")
        {
            EndSessionFromAI();
        }

        else if (action == "river")
        {
            sessionManager.AddLinesToSession("Now, Choose a way to deal with your thought!", 
                SessionManager.RobotAnimation.Wave, SessionManager.BubbleAnimation.Appear);
            sessionManager.AddLinesToSession("$choose$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default);
            sessionManager.AddLinesToSession("That's a great choice!", 
                SessionManager.RobotAnimation.Nod, SessionManager.BubbleAnimation.Default);
            sessionManager.AddLinesToSession("Watch what happend to the bubble..", 
                SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default);
            sessionManager.AddLinesToSession("$bubbleBehavior$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default);
            sessionManager.ContinueDialogue();


            SaveRiverThoughtToVault(response);
        }

        else
        {
            Debug.LogWarning($"Unhandled AI action: {action}");
            ShowRequestFailedMessage();
        }
    }

    private void EndSessionFromAI()
    {
        sessionManager.sessionShowManager.Finish();
        sessionManager.EndSession();
        mainSelectManager?.CloseSession();

        if (completedSessionCanvas != null)
        {
            completedSessionCanvas.SetActive(true);
        }

        sessionTracker?.SetStatus();
    }

    private string NormalizeLineEndings(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n");
    }

    private void AddResponseTextToSession(string responseText)
    {
        string normalizedText = NormalizeLineEndings(responseText);
        string[] lines = normalizedText.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            sessionManager.AddLinesToSession(trimmedLine, SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default);
        }
    }

    private void SaveRiverThoughtToVault(ChatResponse response)
    {
        if (vaultManager == null)
        {
            Debug.LogError("AINetworking needs a VaultManager reference to save river thoughts.");
            return;
        }

        string bubbleContent = response.reply;
        if (string.IsNullOrWhiteSpace(bubbleContent))
        {
            bubbleContent = BuildThoughtsText(response.thoughts);
        }

        if (string.IsNullOrWhiteSpace(bubbleContent))
        {
            Debug.LogWarning("River response did not include reply or thoughts. Nothing was saved to vault.");
            return;
        }

        vaultManager.AIAddToBubbleVault(bubbleContent.Trim());
        Debug.Log($"River thought saved to vault: {bubbleContent}");
    }

    private string BuildThoughtsText(string[] thoughts)
    {
        if (thoughts == null || thoughts.Length == 0)
        {
            return null;
        }

        return string.Join("\n", thoughts);
    }
}
