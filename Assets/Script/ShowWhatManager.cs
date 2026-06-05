using UnityEngine;

public class ShowWhatManager : MonoBehaviour
{
    [SerializeField] private GameObject InputPanel;
    [SerializeField] private GameObject InteractPanel;
    [SerializeField] private GameObject InteractModels;
    [SerializeField] private GameObject Default;
    [SerializeField] private GameObject DefaultModels;
    [SerializeField] private MainManager mainManager;
    private bool startInput = false;
    private bool startInteract = false; 

    void Start()
    {
        InputPanel.SetActive(startInput);

        InteractPanel.SetActive(startInteract);
        InteractModels.SetActive(startInteract);

        Default.SetActive(!(startInput || startInteract));
        DefaultModels.SetActive(!(startInput || startInteract));

        mainManager = GetComponent<MainManager>();
    }

    private void Refresh()
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
        Refresh();
    }

    public void FinishInput()
    {
        startInput = false;
        Refresh();
        mainManager.ContinueDialogue();
    }

    public void ActivateInteract()
    {
        startInteract = true;
        Refresh();
    }

    public void FinishInteract()
    {
        startInteract = false;
        Refresh();
        mainManager.ContinueDialogue();
    }
}
