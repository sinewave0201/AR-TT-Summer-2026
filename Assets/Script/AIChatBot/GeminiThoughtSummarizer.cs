using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GeminiThoughtSummarizer : MonoBehaviour
{
    [System.Serializable]
    private class GeminiRequest
    {
        public GeminiContent[] contents;
        public GeminiGenerationConfig generationConfig;
    }

    [System.Serializable]
    private class GeminiContent
    {
        public string role;
        public GeminiPart[] parts;
    }

    [System.Serializable]
    private class GeminiPart
    {
        public string text;
    }

    [System.Serializable]
    private class GeminiGenerationConfig
    {
        public float temperature;
        public int maxOutputTokens;
    }

    [System.Serializable]
    private class GeminiResponse
    {
        public GeminiCandidate[] candidates;
    }

    [System.Serializable]
    private class GeminiCandidate
    {
        public GeminiContent content;
    }

    [Header("Gemini")]
    [SerializeField] private string apiKeyEnvironmentVariable = "GEMINI_API_KEY";
    [SerializeField] private string model = "gemini-2.5-flash";
    [SerializeField] private int maxOutputTokens = 80;
    [SerializeField] private float temperature = 0.2f;

    public void SummarizeThoughtsToVault(string[] thoughts, VaultManager vaultManager)
    {
        if (thoughts == null || thoughts.Length == 0)
        {
            Debug.LogWarning("No thoughts to summarize.");
            return;
        }

        if (vaultManager == null)
        {
            Debug.LogError("GeminiThoughtSummarizer needs a VaultManager reference.");
            return;
        }

        string apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError($"Gemini API key is empty. Set the {apiKeyEnvironmentVariable} environment variable before starting Unity.");
            return;
        }

        StartCoroutine(SendSummarizeRequest(thoughts, vaultManager, apiKey));
    }

    private IEnumerator SendSummarizeRequest(string[] thoughts, VaultManager vaultManager, string apiKey)
    {
        string prompt = BuildPrompt(thoughts);
        GeminiRequest requestBody = new GeminiRequest
        {
            contents = new[]
            {
                new GeminiContent
                {
                    role = "user",
                    parts = new[]
                    {
                        new GeminiPart { text = prompt }
                    }
                }
            },
            generationConfig = new GeminiGenerationConfig
            {
                temperature = temperature,
                maxOutputTokens = maxOutputTokens
            }
        };

        string json = JsonUtility.ToJson(requestBody);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log($"Gemini thought summary request started. thoughts={thoughts.Length}");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Gemini thought summary failed. status={request.responseCode}, error={request.error}");
            Debug.LogError($"Gemini error body: {request.downloadHandler.text}");
            yield break;
        }

        string summary = ExtractSummary(request.downloadHandler.text);
        if (string.IsNullOrWhiteSpace(summary))
        {
            Debug.LogError($"Gemini summary response did not contain text. body={request.downloadHandler.text}");
            yield break;
        }

        vaultManager.AIAddToBubbleVault(summary.Trim());
        Debug.Log($"Gemini thought summary saved to vault: {summary}");
    }

    private string GetApiKey()
    {
        return System.Environment.GetEnvironmentVariable(apiKeyEnvironmentVariable);
    }

    private string BuildPrompt(string[] thoughts)
    {
        string joinedThoughts = string.Join("\n- ", thoughts);
        return "Summarize these user thoughts into one short, compassionate vault bubble. "
            + "Return only the summary text, no bullets, no quotes, max 20 words.\n\n"
            + "Thoughts:\n- "
            + joinedThoughts;
    }

    private string ExtractSummary(string responseJson)
    {
        GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(responseJson);
        if (response == null || response.candidates == null || response.candidates.Length == 0)
        {
            return null;
        }

        GeminiContent content = response.candidates[0].content;
        if (content == null || content.parts == null || content.parts.Length == 0)
        {
            return null;
        }

        return content.parts[0].text;
    }
}
