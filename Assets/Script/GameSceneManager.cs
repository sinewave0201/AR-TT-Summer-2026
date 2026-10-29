using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public int SceneIndex;

    public void SwitchScene()
    {
        LoadSceneForCurrentPlatform(SceneIndex);
    }

    private static void LoadSceneForCurrentPlatform(int buildIndex)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // XR Simulation is an Editor/computer concern. On an Android device,
        // use Unity's normal single-scene loading path with the ARCore loader.
        SceneManager.LoadScene(buildIndex);
#else
        SimulationEnvironmentGuard.LoadScenePreservingSimulation(buildIndex);
#endif
    }
}
