using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

public sealed class SimulationEnvironmentGuard : MonoBehaviour
{
    private static SimulationEnvironmentGuard instance;
    private Coroutine verification;
    private Coroutine sceneTransition;
    private bool isRestoring;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateGuard()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android uses ARCore and regular SceneManager loading. Do not create
        // the persistent XR Simulation guard in an Android player build.
        return;
#else
        if (FindFirstObjectByType<SimulationEnvironmentGuard>() != null)
        {
            return;
        }

        GameObject guardObject = new GameObject(nameof(SimulationEnvironmentGuard));
        DontDestroyOnLoad(guardObject);
        guardObject.AddComponent<SimulationEnvironmentGuard>();
#endif
    }

    private void OnEnable()
    {
        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
        {
            instance = null;
        }
    }

    public static void LoadScenePreservingSimulation(int buildIndex)
    {
        if (instance == null)
        {
            Debug.LogWarning("Simulation environment guard is unavailable; loading the scene normally.");
            SceneManager.LoadScene(buildIndex);
            return;
        }

        if (instance.sceneTransition != null)
        {
            Debug.LogWarning("A scene transition is already in progress.");
            return;
        }

        instance.sceneTransition = instance.StartCoroutine(
            instance.ReplaceActiveScene(buildIndex));
    }

    private IEnumerator ReplaceActiveScene(int buildIndex)
    {
        Scene previousScene = SceneManager.GetActiveScene();

        // Each gameplay scene owns an AR Session, XR Origin, and input action
        // manager. Fully remove the old rig before creating the next one so the
        // two scenes cannot fight over XR subsystems or disable shared actions.
        // The additive Simulation Environment scene remains loaded throughout.
        if (previousScene.IsValid() && previousScene.isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(previousScene);
            if (unload != null)
            {
                yield return unload;
            }
        }

        AsyncOperation load = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);

        if (load == null)
        {
            Debug.LogError($"Could not start loading scene at build index {buildIndex}.");
            sceneTransition = null;
            yield break;
        }

        yield return load;

        Scene destinationScene = SceneManager.GetSceneByBuildIndex(buildIndex);
        if (!destinationScene.IsValid() || !destinationScene.isLoaded)
        {
            Debug.LogError($"Scene at build index {buildIndex} did not finish loading.");
            sceneTransition = null;
            yield break;
        }

        SceneManager.SetActiveScene(destinationScene);

        sceneTransition = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (verification != null)
        {
            StopCoroutine(verification);
        }

        verification = StartCoroutine(VerifyEnvironmentAfterUnload());
    }

    private IEnumerator VerifyEnvironmentAfterUnload()
    {
        // Single-scene loading finishes removing additive scenes after the
        // gameplay sceneLoaded event. Give Unity two frames to complete it.
        yield return null;
        yield return null;

        verification = null;

        if (isRestoring || IsEnvironmentLoaded() || !IsSimulationLoaderActive())
        {
            yield break;
        }

        ARSession arSession = FindFirstObjectByType<ARSession>();
        if (arSession == null)
        {
            Debug.LogWarning(
                "Simulated Environment Scene is missing and this scene has no ARSession.");
            yield break;
        }

        isRestoring = true;
        Debug.Log("Simulated Environment Scene is missing. Recreating it through ARSession.Reset().");
        arSession.Reset();

        yield return null;
        isRestoring = false;

        if (IsEnvironmentLoaded())
        {
            yield break;
        }

        Debug.LogWarning(
            "ARSession.Reset() did not restore XR Simulation. Restarting the Simulation loader.");

        yield return RestartSimulationLoader();

        if (IsEnvironmentLoaded())
        {
            Debug.Log("Simulated Environment Scene was restored by restarting the XR Simulation loader.");
        }
        else
        {
            Debug.LogError(
                "XR Simulation loader restarted, but the Simulated Environment Scene is still missing.");
        }
    }

    private static IEnumerator RestartSimulationLoader()
    {
        XRManagerSettings xrManager = XRGeneralSettings.Instance?.Manager;
        if (xrManager == null)
        {
            Debug.LogError("XR Manager Settings could not be found.");
            yield break;
        }

        ARSession[] sessions =
            FindObjectsByType<ARSession>(FindObjectsSortMode.None);
        XROrigin[] xrOrigins =
            FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool[] activeOrigins = new bool[xrOrigins.Length];

        // Disable every AR manager before destroying its subsystem. Otherwise a
        // trackable manager can retain stale plane data from the old provider.
        foreach (ARSession session in sessions)
        {
            session.enabled = false;
        }

        for (int i = 0; i < xrOrigins.Length; i++)
        {
            activeOrigins[i] = xrOrigins[i].gameObject.activeSelf;
            if (activeOrigins[i])
            {
                xrOrigins[i].gameObject.SetActive(false);
            }
        }

        yield return null;

        if (xrManager.isInitializationComplete)
        {
            xrManager.StopSubsystems();
            xrManager.DeinitializeLoader();
        }

        xrManager.InitializeLoaderSync();
        if (!xrManager.isInitializationComplete || xrManager.activeLoader == null)
        {
            Debug.LogError("XR Simulation loader failed to initialize.");
            yield break;
        }

        xrManager.StartSubsystems();

        // Give the new simulation provider time to load its environment before
        // managers subscribe and create trackables from it.
        yield return null;
        yield return null;

        for (int i = 0; i < xrOrigins.Length; i++)
        {
            if (xrOrigins[i] != null && activeOrigins[i])
            {
                xrOrigins[i].gameObject.SetActive(true);
            }
        }

        foreach (ARSession session in sessions)
        {
            if (session != null)
            {
                session.enabled = true;
            }
        }

        // Plane changes are delivered after the managers are enabled.
        yield return null;
    }

    private static bool IsSimulationLoaderActive()
    {
        XRManagerSettings xrManager = XRGeneralSettings.Instance?.Manager;
        string loaderType = xrManager?.activeLoader?.GetType().FullName;

        return loaderType != null && loaderType.Contains("Simulation");
    }

    private static bool IsEnvironmentLoaded()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded && scene.name.Contains("Simulated Environment Scene"))
            {
                return true;
            }
        }

        return false;
    }
}
