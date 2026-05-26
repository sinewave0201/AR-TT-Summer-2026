using UnityEngine;

public class ShowWhatManager : MonoBehaviour
{
    [SerializeField] private GameObject InputPanel;
    [SerializeField] private GameObject InteractPanel;
    [SerializeField] private GameObject InteractModels;
    [SerializeField] private GameObject Default;
    [SerializeField] private GameObject DefaultModels;
    private bool startInput = false;
    private bool startInteract = false; 

    void Start()
    {
        InputPanel.SetActive(startInput);

        InteractPanel.SetActive(startInteract);
        InteractModels.SetActive(startInteract);

        Default.SetActive(!(startInput || startInteract));
        DefaultModels.SetActive(!(startInput || startInteract));
    }

    void Update()
    {
        InputPanel.SetActive(startInput);

        InteractPanel.SetActive(startInteract);
        InteractModels.SetActive(startInteract);

        Default.SetActive(!(startInput || startInteract));
        DefaultModels.SetActive(!(startInput || startInteract));
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

    public void ActivateInteract()
    {
        startInteract = true;
        Time.timeScale = 0f;
    }

    public void FinishInteract()
    {
        startInteract = false;
        Time.timeScale = 1f;
    }
}
