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
    private bool callbacksSubscribed;
    private string currentStatus = "Disconnected";

    private void Awake()
    {
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(StartClient);
        }

        SetStatus("Disconnected");
    }

    private void OnEnable()
    {
        ResolveNetworkManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromNetworkManager();
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
        if (!ResolveNetworkManager())
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

    private bool ResolveNetworkManager()
    {
        NetworkManager singleton = NetworkManager.Singleton;
        if (networkManager != singleton)
        {
            UnsubscribeFromNetworkManager();
            networkManager = singleton;
        }

        if (networkManager == null)
        {
            return false;
        }

        if (!callbacksSubscribed)
        {
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            callbacksSubscribed = true;
        }

        return true;
    }

    private void UnsubscribeFromNetworkManager()
    {
        if (callbacksSubscribed && networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= HandleClientConnected;
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        callbacksSubscribed = false;
    }

    public void BindUI(
        TMP_InputField newHostIpInput,
        Button newConnectButton,
        TMP_Text newConnectionStatusText)
    {
        if (connectButton != null)
        {
            connectButton.onClick.RemoveListener(StartClient);
        }

        hostIpInput = newHostIpInput;
        connectButton = newConnectButton;
        connectionStatusText = newConnectionStatusText;

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(StartClient);
        }

        SetStatus(currentStatus);
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
        currentStatus = status;
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
