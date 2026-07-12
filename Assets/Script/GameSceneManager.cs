using UnityEngine;
using UnityEngine.SceneManagement;
public class GameSceneManager : MonoBehaviour
{
    public int SceneIndex;
    public int diySceneIndex;
    private static int returnSceneIndex = -1;
    // Update is called once per frame
    public void SwitchScene()
    {
        SceneManager.LoadScene(SceneIndex);
    }

    public void EnterDIYScene()
    {
        // Remember where the player came from.
        returnSceneIndex = SceneManager.GetActiveScene().buildIndex;

        SceneManager.LoadScene(diySceneIndex);
    }

    public void ReturnFromDIY()
    {
        if (returnSceneIndex >= 0)
        {
            SceneManager.LoadScene(returnSceneIndex);
        }
        else
        {
            Debug.LogWarning("No return scene has been recorded.");
        }
    }
}
