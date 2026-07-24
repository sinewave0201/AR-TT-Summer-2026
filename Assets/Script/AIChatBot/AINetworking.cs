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
        public string prompt;//text to show, could be 0 during braindump
        public string emotions;//sad, neutral, happy
        public string dominant;//a dominent emotion from the three
        public string action;//what to do next
        // action values:
        // "greeting" → play greeting
        // null → just show prompt + react
        // "choose_strategy" → show choices; extra: strategies, due
        // to submit a choice send value as message: "vault" / "letter" / "flower", "1"-"5", "again" / "finish"
        // "rating" → ask 1-5; extra: strategy, stored
        // "again_or_end" → offer another round; extra: options
        // "end" → session over, disable input
        // "archive" → show archive; extra: items
        public string step;//current step
    }

    private string sessionId;
    private string chatUrl = "https://tt-chatbot.onrender.com/chat";
    private string resetUrl = "https://tt-chatbot.onrender.com/reset";

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

    [Header("WOO Interaction")]
    [SerializeField] private bool useWooInteractionFlow;

    private bool isWaitingForAI;
    private bool isNewSession = true;
    private bool isWaitingForWooInteraction;
    private int initialDialogueLineCount;

    void Start()
    {
        initialDialogueLineCount = sessionManager != null
            ? sessionManager.lines.Count
            : 0;

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

        string message = inputField.text;
        Debug.Log($"MSG sent to AI: {message}");
        StartCoroutine(SendChatRequest(message));
    }

    IEnumerator SendChatRequest(string msg)
    {
        isWaitingForAI = true;
        SetLoading(true);

        if (isNewSession)
        {
            bool resetSucceeded = false;
            yield return ResetAIConversation(succeeded => resetSucceeded = succeeded);

            if (!resetSucceeded)
            {
                isWaitingForAI = false;
                SetLoading(false);
                Debug.Log("Failed MSG 1");
                ShowRequestFailedMessage();
                yield break;
            }

            isNewSession = false;
        }

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
            Debug.Log("Failed MSG 2");
            ShowRequestFailedMessage();
            yield break;
        }

        Debug.Log($"AI reply received after {elapsed:F1}s. status={request.responseCode}, body={request.downloadHandler.text}");
        ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
        request.Dispose();

        ResponseHandeler(response);
    }

    private IEnumerator ResetAIConversation(System.Action<bool> onCompleted)
    {
        ChatRequest resetRequest = new ChatRequest
        {
            message = string.Empty,
            session_id = sessionId
        };

        string json = JsonUtility.ToJson(resetRequest);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = CreatePostRequest(resetUrl, bodyRaw))
        {
            Debug.Log($"Resetting AI conversation. url={resetUrl}, session_id={sessionId}");
            yield return request.SendWebRequest();

            bool succeeded = request.result == UnityWebRequest.Result.Success;
            if (succeeded)
            {
                Debug.Log($"AI conversation reset successfully. status={request.responseCode}");
            }
            else
            {
                Debug.LogError($"AI conversation reset failed. result={request.result}, status={request.responseCode}, error={request.error}");
                Debug.LogError($"AI reset error body: {request.downloadHandler.text}");
            }

            onCompleted?.Invoke(succeeded);
        }
    }

    private UnityWebRequest CreateChatRequest(byte[] bodyRaw)
    {
        return CreatePostRequest(chatUrl, bodyRaw);
    }

    private UnityWebRequest CreatePostRequest(string url, byte[] bodyRaw)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
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

        sessionManager.AddLinesToSession(requestFailedMessage, SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
        sessionManager.AddLinesToSession("$input$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
        sessionManager.ContinueDialogue();
    }

    #region handles AI responses
    void ResponseHandeler(ChatResponse response)
    {
        if (response == null)
        {
            Debug.LogError("AI response could not be parsed.");
            Debug.Log("Failed MSG 3");
            ShowRequestFailedMessage();
            return;
        }

        string action = response.action;
        string responseText = response.prompt;
        string responseEmo = response.dominant;

        Debug.Log($"Handling AI response. action={action}, message={responseText}");

        if (action == "" ||action == null || action == "rating")//only during null and rating can use this temp
        {
            AddResponseTextToSession(responseText, responseEmo);
            sessionManager.AddLinesToSession("$input$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
            sessionManager.ContinueDialogue();
        }

        else if (action == "greeting")//play greeting
        {
            AddResponseTextToSession(responseText, "greeting"); 
            sessionManager.AddLinesToSession("$input$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
            sessionManager.ContinueDialogue();
        }

        
        else if (action == "again_or_end")
        {
            AddResponseTextToSession(responseText, responseEmo); 
            sessionManager.AddLinesToSession("input ONLY again and finish to choose!", SessionManager.RobotAnimation.Agreeing, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
            sessionManager.AddLinesToSession("$input$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
            sessionManager.ContinueDialogue();
        }

        else if (action == "archive")//what is this? do nothing for now
        {
            sessionManager.ContinueDialogue();
        }


        else if (action == "end")//end the session
        {
            isWaitingForWooInteraction = false;

            //add the last sentence
            AddResponseTextToSession(responseText, responseEmo);

            sessionManager.dialogueDisplayEnd = false;
            sessionManager.ContinueDialogue();
            
            StartCoroutine(WaitForFinalDialogue());
        }

        else if (action == "choose_strategy")//replace the default choose strategy with unity one
        {
            if (useWooInteractionFlow)
            {
                AddWooInteractionToSession();
                isWaitingForWooInteraction = true;
            }
            else
            {
                sessionManager.AddLinesToSession("Now, tell me what emotion does this thought bring you.",
                    SessionManager.RobotAnimation.Wave, SessionManager.BubbleAnimation.Appear, SessionManager.RobotSound.Neutral);
                sessionManager.AddLinesToSession("$choose$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
                sessionManager.AddLinesToSession("That's how you feel. I see.",
                    SessionManager.RobotAnimation.Nod, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
                sessionManager.AddLinesToSession("Watch what happens to the bubble.",
                    SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
                sessionManager.AddLinesToSession("$bubbleBehavior$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default, SessionManager.RobotSound.Neutral);
            }

            //you have to reply it else it will be bitchy
            StartCoroutine(SendChatRequest("flower"));

            sessionManager.ContinueDialogue();

            if (!useWooInteractionFlow)
            {
                SaveRiverThoughtToVault(response);

                // AIChatBot2 has no DIY stage, so it continues automatically.
                StartCoroutine(SendChatRequest("reply"));
            }
        }

        else
        {
            Debug.Log("Failed MSG 4");
            ShowRequestFailedMessage();
        }
    }

    private void AddWooInteractionToSession()
    {
        sessionManager.AddLinesToSession(
            "I hear you. Your thought has turned into a thought bubble.",
            SessionManager.RobotAnimation.GetBubble,
            SessionManager.BubbleAnimation.Appear,
            SessionManager.RobotSound.Neutral);
        sessionManager.AddLinesToSession(
            "It doesn't have any colors yet. Why not give it your own color?",
            SessionManager.RobotAnimation.Idle,
            SessionManager.BubbleAnimation.Default,
            SessionManager.RobotSound.Neutral);
        sessionManager.AddLinesToSession(
            "$DIY$",
            SessionManager.RobotAnimation.Idle,
            SessionManager.BubbleAnimation.Default,
            SessionManager.RobotSound.Neutral);
        sessionManager.AddLinesToSession(
            "Looks great. One last step: how does this thought make you feel?",
            SessionManager.RobotAnimation.Agreeing,
            SessionManager.BubbleAnimation.Default,
            SessionManager.RobotSound.Neutral);
        sessionManager.AddLinesToSession(
            "$choose$",
            SessionManager.RobotAnimation.Idle,
            SessionManager.BubbleAnimation.Default,
            SessionManager.RobotSound.Neutral);
        sessionManager.AddLinesToSession(
            "Watch how your bubble evolves.",
            SessionManager.RobotAnimation.Agreeing,
            SessionManager.BubbleAnimation.Default,
            SessionManager.RobotSound.Neutral);
        sessionManager.AddLinesToSession(
            "$bubbleBehavior$",
            SessionManager.RobotAnimation.Idle,
            SessionManager.BubbleAnimation.Default,
            SessionManager.RobotSound.Neutral);
    }

    public void CompleteWooInteraction()
    {
        if (!useWooInteractionFlow || !isWaitingForWooInteraction)
        {
            return;
        }

        isWaitingForWooInteraction = false;
        StartCoroutine(SendChatRequest("reply"));
    }


    private IEnumerator WaitForFinalDialogue()
    {
        //wait till the last sentence was over
        yield return new WaitUntil(() => sessionManager.dialogueDisplayEnd);

        sessionManager.sessionShowManager.Finish();
        RemoveDynamicDialogueLines();

        // Reset before EndSession deactivates the object running this coroutine.
        isNewSession = true;
        isWaitingForWooInteraction = false;

        sessionManager.EndSession();
        mainSelectManager?.CloseSession();

        if (completedSessionCanvas != null)
        {
            completedSessionCanvas.SetActive(true);
        }

        sessionTracker?.SetStatus();
    }

    private void RemoveDynamicDialogueLines()
    {
        if (sessionManager == null ||
            sessionManager.lines.Count <= initialDialogueLineCount)
        {
            return;
        }

        sessionManager.lines.RemoveRange(
            initialDialogueLineCount,
            sessionManager.lines.Count - initialDialogueLineCount);
    }
    #endregion
    
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

    private void AddResponseTextToSession(string responseText, string responseEmo)
    {
        SessionManager.RobotAnimation robotAnimation = GetAnim(responseEmo);
        SessionManager.RobotSound robotSound = GetSound(responseEmo);

        //have only emoptions
        if (responseText == "")
        {
            //if it is empty, pass in the animations only
            Debug.Log("empty prompt passed in");
            sessionManager.AddLinesToSession("", robotAnimation, SessionManager.BubbleAnimation.Default, robotSound);
        }
        
        else
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

                sessionManager.AddLinesToSession(trimmedLine, robotAnimation, SessionManager.BubbleAnimation.Default, robotSound);
            }    
        }

    }

    private void SaveRiverThoughtToVault(ChatResponse response)
    {
        if (vaultManager == null)
        {
            Debug.LogError("AINetworking needs a VaultManager reference to save river thoughts.");
            return;
        }

        string bubbleContent = response.prompt;

        if (string.IsNullOrWhiteSpace(bubbleContent))
        {
            Debug.LogWarning("River response did not include a prompt. Nothing was saved to vault.");
            return;
        }

        vaultManager.AIAddToBubbleVault(bubbleContent.Trim());
        Debug.Log($"River thought saved to vault: {bubbleContent}");
    }

    //function used to get robot animation
    private SessionManager.RobotAnimation GetAnim(string emotion)
    {
        if (emotion == "sad")
        {
            return SessionManager.RobotAnimation.Nod;
        }

        else if (emotion == "neutral")
        {
            return SessionManager.RobotAnimation.Nod;
        }

        else if (emotion == "happy")
        {
            return SessionManager.RobotAnimation.Agreeing;
        }

        else if (emotion == "greeting")
        {
            return SessionManager.RobotAnimation.Wave;
        }

        return SessionManager.RobotAnimation.Idle;
    }

    private SessionManager.RobotSound GetSound(string emotion)
    {
        if (emotion == "sad")
        {
            return SessionManager.RobotSound.Sad;
        }

        else if (emotion == "neutral")
        {
            return SessionManager.RobotSound.Neutral;
        }

        else if (emotion == "happy")
        {
            return SessionManager.RobotSound.Happy;
        }

        else if (emotion == "greeting")
        {
            return SessionManager.RobotSound.Greeting;
        }

        return SessionManager.RobotSound.Talking;
    }
}
