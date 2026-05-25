using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private GameObject InputPanel;
    [SerializeField] private GameObject Subtitle;
    private bool startInput = false;

    void Start()
    {
        InputPanel.SetActive(startInput);
        Subtitle.SetActive(!startInput);
    }

    void Update()
    {
        InputPanel.SetActive(startInput);
        Subtitle.SetActive(!startInput);
    }
    public void ActivateInput()
    {
        startInput = true;
        Time.timeScale = 0f;
    }

    public void FinishInput()
    {
        startInput = false;
        Time.timeScale = 1f;
    }
}
