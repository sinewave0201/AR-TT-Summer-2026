using UnityEngine;

public class DIYActivate : MonoBehaviour
{
    [Header("UIPanels")]
    public GameObject PostSessionUI;
    public GameObject SessionUI;
    public GameObject DIYUI;

    void OnEnable()
    {
        PostSessionUI.SetActive(false);
        SessionUI.SetActive(false);
        DIYUI.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EndDIY()
    {
        SessionUI.SetActive(true);
        PostSessionUI.SetActive(true);
        DIYUI.SetActive(false);
        gameObject.SetActive(false);
    }
}
