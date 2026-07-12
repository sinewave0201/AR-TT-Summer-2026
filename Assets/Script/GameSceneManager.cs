using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    public int SceneIndex;
    public int diySceneIndex;

    public void SwitchScene()
    {
        SimulationEnvironmentGuard.LoadScenePreservingSimulation(SceneIndex);
    }

    public void EnterDIYScene()
    {
        SimulationEnvironmentGuard.LoadScenePreservingSimulation(diySceneIndex);
    }

    public void ReturnFromDIY()
    {
        SimulationEnvironmentGuard.LoadScenePreservingSimulation(2);
    }

}
