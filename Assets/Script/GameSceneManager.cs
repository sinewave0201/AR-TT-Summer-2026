using UnityEngine;
using UnityEngine.SceneManagement;
public class GameSceneManager : MonoBehaviour
{
    public int SceneIndex;
    // Update is called once per frame
    public void SwitchScene()
    {
        SceneManager.LoadScene(SceneIndex);
    }
}
