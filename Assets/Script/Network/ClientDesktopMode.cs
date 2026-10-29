using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

public sealed class ClientDesktopMode : MonoBehaviour
{
    private readonly HashSet<int> unloadingSceneHandles = new HashSet<int>();

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShutdownXR();
        UnloadSimulationEnvironments();

        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(VerifyDesktopMode());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static void ShutdownXR()
    {
        XRManagerSettings manager = XRGeneralSettings.Instance?.Manager;
        if (manager == null || !manager.isInitializationComplete)
        {
            return;
        }

        manager.StopSubsystems();
        manager.DeinitializeLoader();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsSimulationEnvironment(scene))
        {
            UnloadScene(scene);
        }
    }

    private IEnumerator VerifyDesktopMode()
    {
        yield return null;
        yield return null;

        ShutdownXR();
        UnloadSimulationEnvironments();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UnloadSimulationEnvironments()
    {
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (IsSimulationEnvironment(scene))
            {
                UnloadScene(scene);
            }
        }
    }

    private void UnloadScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded ||
            !unloadingSceneHandles.Add(scene.handle))
        {
            return;
        }

        AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
        if (unload == null)
        {
            unloadingSceneHandles.Remove(scene.handle);
            return;
        }

        unload.completed += _ => unloadingSceneHandles.Remove(scene.handle);
    }

    private static bool IsSimulationEnvironment(Scene scene)
    {
        return scene.IsValid() &&
               scene.name.Contains("Simulated Environment Scene");
    }
}
