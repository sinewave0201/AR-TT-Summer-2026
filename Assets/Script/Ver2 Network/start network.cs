using UnityEngine;
using Unity.Netcode;

public class startnetwork : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Networking")]
    [SerializeField] private NetworkObject robotControlBridgePrefab;
    [SerializeField] private AnimationManager animationManager;
    [SerializeField] private SoundManager soundManager;
    [SerializeField] private TraceManager traceManager;

    private NetworkObject spawnedControlBridge;
    void Start()
    {
        traceManager?.Initialize(animationManager);
        StartHostAndSpawnControlBridge(animationManager, soundManager, traceManager);
    }

    private void StartHostAndSpawnControlBridge(
        AnimationManager animationManager,
        SoundManager soundManager,
        TraceManager traceManager)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("Cannot start Host because no NetworkManager exists in the scene.", this);
            return;
        }

        if (!networkManager.IsListening && !networkManager.StartHost())
        {
            Debug.LogError("NetworkManager failed to start as Host.", this);
            return;
        }

        if (!networkManager.IsServer)
        {
            Debug.LogError("Robot was placed on a non-Host instance.", this);
            return;
        }


        if (spawnedControlBridge == null)
        {
            spawnedControlBridge = Instantiate(robotControlBridgePrefab);

            InheritNetworkBehavior bridge =
                spawnedControlBridge.GetComponent<InheritNetworkBehavior>();
            if (bridge == null)
            {
                Debug.LogError(
                    "RobotControlBridge Prefab is missing InheritNetworkBehavior.",
                    this);
                Destroy(spawnedControlBridge.gameObject);
                spawnedControlBridge = null;
                return;
            }

            if (animationManager == null || soundManager == null)
            {
                Debug.LogError(
                    "The spawned Robot needs AnimationManager and SoundManager.",
                    this);
                Destroy(spawnedControlBridge.gameObject);
                spawnedControlBridge = null;
                return;
            }

            bridge.Initialize(animationManager, soundManager, traceManager);
            spawnedControlBridge.Spawn(destroyWithScene: false);
        }
    }
}
