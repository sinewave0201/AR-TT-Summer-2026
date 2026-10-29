using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class LanClientConnector : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private TMP_InputField hostIpInput;
    [SerializeField] private ushort port = 7777;

    [Header("UI")]
    [SerializeField] private Button connectButton;
    [SerializeField] private TMP_Text connectionStatusText;

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = NetworkManager.Singleton;

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(StartClient);
        }

        SetStatus("Disconnected");
    }

    private void OnEnable()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= HandleClientConnected;
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (connectButton != null)
        {
            connectButton.onClick.RemoveListener(StartClient);
        }
    }

    public void StartClient()
    {
        if (networkManager == null)
        {
            SetStatus("Missing NetworkManager");
            return;
        }

        if (networkManager.IsListening)
        {
            SetStatus(networkManager.IsConnectedClient ? "Connected" : "Already started");
            return;
        }

        string hostAddress = hostIpInput != null
            ? hostIpInput.text.Trim()
            : string.Empty;

        if (string.IsNullOrWhiteSpace(hostAddress))
        {
            SetStatus("Enter the Android Host IP");
            return;
        }

        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            SetStatus("Missing UnityTransport");
            return;
        }

        transport.SetConnectionData(hostAddress, port);
        SetStatus($"Connecting to {hostAddress}:{port}...");
        SetConnectButtonInteractable(false);

        if (!networkManager.StartClient())
        {
            SetStatus("Failed to start Client");
            SetConnectButtonInteractable(true);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (networkManager != null && clientId == networkManager.LocalClientId)
        {
            SetStatus("Connected");
            SetConnectButtonInteractable(false);
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (networkManager != null && clientId == networkManager.LocalClientId)
        {
            SetStatus("Disconnected");
            SetConnectButtonInteractable(true);
        }
    }

    private void SetStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = status;
        }

        Debug.Log($"LAN Client: {status}", this);
    }

    private void SetConnectButtonInteractable(bool interactable)
    {
        if (connectButton != null)
        {
            connectButton.interactable = interactable;
        }
    }
}
