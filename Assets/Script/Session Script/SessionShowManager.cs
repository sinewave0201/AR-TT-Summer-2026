using UnityEngine;
using System.Collections.Generic;

public class SessionShowManager : MonoBehaviour
{
    [System.Serializable]
    public class StagesGroup
    {
        public string name;
        public GameObject[] objects;
    }


    [SerializeField] private StagesGroup[] groups;
    [SerializeField] private GameObject[] defaultObjects;
    private string activeGroup = "";
    [SerializeField] private SessionManager sessionManager;


    void Start()
    {
        Refresh();
        sessionManager = GetComponent<SessionManager>();
    }

    private void Refresh()
    {
        bool hasActiveGroup = !string.IsNullOrEmpty(activeGroup);

        foreach (var group in groups)
        {
            bool shouldShow = group.name == activeGroup;

            foreach (var obj in group.objects)
            {
                if (obj != null)
                {
                    obj.SetActive(shouldShow);
                }
            }
        }

        foreach (var obj in defaultObjects)
        {
            if (obj != null)
            {
                obj.SetActive(!hasActiveGroup);
            }
        }
    }


    public void Activate(string curGroupName)
    {
        activeGroup = curGroupName;
        Refresh();
    }

    public void Finish()
    {
        activeGroup = "";
        Refresh();
    }
}
