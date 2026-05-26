using UnityEngine;

public class AndroidTTS : MonoBehaviour
{
    public MainManager mainManager;
    private const int TtsSuccess = 0;
    private const int QueueFlush = 0;
    private readonly System.Collections.Generic.Queue<string> pendingTexts = new System.Collections.Generic.Queue<string>();

    private volatile bool initialized;
    private bool configured;
    private AndroidJavaClass unityPlayer;
    private AndroidJavaObject activity;
    private AndroidJavaObject tts;
    private AndroidJavaObject speakParams;

    private class TTSInitListener : AndroidJavaProxy
    {
        private AndroidTTS androidTTS;

        public TTSInitListener(AndroidTTS androidTTS)
            : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.androidTTS = androidTTS;
        }

        public void onInit(int status)
        {
            if (status == TtsSuccess)
            {
                androidTTS.initialized = true;
                Debug.Log("Android TTS initialized.");
            }
            else
            {
                Debug.LogError($"Android TTS init failed with status {status}.");
            }
        }
    }


    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        mainManager = GetComponent<MainManager>();
        InitTTS();
#endif
    }

    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (initialized && !configured)
        {
            ConfigureTTS();
        }

        if (initialized && pendingTexts.Count > 0)
        {
            var text = pendingTexts.Dequeue();
            SpeakNow(text);
        }
#endif
    }

    private void InitTTS()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            speakParams = new AndroidJavaObject("android.os.Bundle");
            tts = new AndroidJavaObject("android.speech.tts.TextToSpeech",
                activity, new TTSInitListener(this));
        }
        catch (AndroidJavaException e)
        {
            Debug.LogError($"Android TTS init exception: {e.Message}");
            initialized = false;
        }
#endif
    }

    private void ConfigureTTS()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (tts == null) return;

        try
        {
            using var localeClass = new AndroidJavaClass("java.util.Locale");
            using var locale = localeClass.GetStatic<AndroidJavaObject>("US");
            var languageResult = tts.Call<int>("setLanguage", locale);
            tts.Call<int>("setSpeechRate", 1.0f);
            configured = true;
            Debug.Log($"Android TTS configured. setLanguage result={languageResult}");
        }
        catch (AndroidJavaException e)
        {
            Debug.LogError($"Android TTS configure exception: {e.Message}");
        }
#endif
    }

    public void Speak(string text)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (string.IsNullOrEmpty(text)) return;

        if (!initialized || !configured || tts == null)
        {
            pendingTexts.Enqueue(text);
            Debug.Log($"Android TTS queued before init: {text}");
            return;
        }

        SpeakNow(text);
#endif
    }

    private void SpeakNow(string text)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var result = tts.Call<int>("speak", text, QueueFlush, speakParams, $"unity_tts_{Time.frameCount}");
            Debug.Log($"Android TTS speak result={result}, text={text}");

            if (result != TtsSuccess)
            {
                Debug.LogError($"Android TTS speak failed with result {result}.");
            }
        }
        catch (AndroidJavaException e)
        {
            Debug.LogError($"Android TTS speak exception: {e.Message}");
        }
#endif
    }

    public void Shutdown()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (tts == null) return;

        tts.Call("stop");
        tts.Call("shutdown");
        tts.Dispose();
        speakParams?.Dispose();
        tts = null;
        speakParams = null;
        initialized = false;
        configured = false;
        pendingTexts.Clear();
#endif
    }

    public void OnDestroy()
    {
        Shutdown();
    }
}
