using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;

public class LobbyUI : MonoBehaviour
{
    private string joinCode = "";
    private string statusMessage = "";
    private bool isInitialized = false;

    async void Start()
    {
        if (NetworkManager.Singleton != null)
            DontDestroyOnLoad(NetworkManager.Singleton.gameObject);

        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        isInitialized = true;

        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (NetworkManager.Singleton.IsHost)
            {
                int totalPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
                statusMessage = "Host - Players: " + totalPlayers + "/2";
            }
            else
            {
                statusMessage = "Player 2 Connected!";
            }
        };
    }

    void OnGUI()
    {
        if (!isInitialized)
        {
            GUI.Label(new Rect(10, 10, 300, 30), "Connecting to Unity Services...");
            return;
        }

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            GUI.Label(new Rect(10, 10, 300, 30), statusMessage);
            return;
        }

        if (GUI.Button(new Rect(10, 10, 200, 60), "Host Game"))
        {
            CreateRelay();
        }

        GUI.Label(new Rect(10, 80, 200, 30), "Join Code:");
        joinCode = GUI.TextField(new Rect(10, 110, 200, 30), joinCode);

        if (GUI.Button(new Rect(10, 150, 200, 60), "Join Game"))
        {
            JoinRelay();
        }

        GUI.Label(new Rect(10, 220, 300, 30), statusMessage);
    }

    async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            statusMessage = "Join Code: " + code;
            Debug.Log("Relay Host started. Code: " + code);
        }
        catch (System.Exception e)
        {
            statusMessage = "Error: " + e.Message;
            Debug.LogError(e);
        }
    }

    async void JoinRelay()
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            statusMessage = "Connecting...";
            Debug.Log("Joining relay with code: " + joinCode);
        }
        catch (System.Exception e)
        {
            statusMessage = "Error: " + e.Message;
            Debug.LogError(e);
        }
    }
}