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
        public string message;
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

    void Start()
    {
        string newId = System.Guid.NewGuid().ToString();
        sessionId = newId;
    }

    public void SendMSGToAI()
    {
        Debug.Log($"MSG sent to AI: {inputField.text}");
        StartCoroutine(SendChatRequest(inputField.text));
    }

    IEnumerator SendChatRequest(string msg)
    {
        float startTime = Time.realtimeSinceStartup;
        ChatRequest newCR = new ChatRequest{message = msg, session_id = sessionId};
        string json = JsonUtility.ToJson(newCR);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        
        UnityWebRequest request = new UnityWebRequest(chatUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log($"AI request started. Waiting for reply... url={chatUrl}, session_id={sessionId}");
        yield return request.SendWebRequest();

        float elapsed = Time.realtimeSinceStartup - startTime;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"AI request failed after {elapsed:F1}s. result={request.result}, status={request.responseCode}, error={request.error}");
            Debug.LogError($"AI error body: {request.downloadHandler.text}");
        }
        else
        {
            Debug.Log($"AI reply received after {elapsed:F1}s. status={request.responseCode}, body={request.downloadHandler.text}");
            ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
            ResponseHandeler(response);
        }
    }

    void ResponseHandeler(ChatResponse response)
    {
        if (response == null)
        {
            Debug.LogError("AI response could not be parsed.");
            return;
        }

        string action = response.action;
        Debug.Log($"Handling AI response. action={action}, message={response.message}");

        if (string.IsNullOrEmpty(action) || action == "null")
        {
            sessionManager.AddLinesToSession(response.message, SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default);
            sessionManager.AddLinesToSession("$input$", SessionManager.RobotAnimation.Idle, SessionManager.BubbleAnimation.Default);
            sessionManager.ContinueDialogue();
        }

        else if (action == "end")
        {
            sessionManager.EndSession();
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


            //store the bubble
            //a function to turn response.thoughts into strings, or maybe call llm to summarize it
            //vaultManager.AIAddToVault(string);
        }

        else
        {
            Debug.LogWarning($"Unhandled AI action: {action}");
        }
    }
}
