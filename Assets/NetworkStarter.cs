using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;

public class NetworkStarter : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions().SetEnvironmentName("production");
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            var session = AppSession.Instance;
            if (session == null)
            {
                Debug.LogError("NetworkStarter: AppSession is null!");
                return;
            }

            if (session.isHost)
            {
                Debug.Log($"NetworkStarter: Starting HOST, relayCode={session.relayJoinCode}");

                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(session.relayJoinCode);
                var relayData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
                NetworkManager.Singleton.StartHost();

                Debug.Log("NetworkStarter: HOST started successfully");
            }
            else
            {
                Debug.Log($"NetworkStarter: Starting CLIENT, relayCode={session.relayJoinCode}");

                if (string.IsNullOrWhiteSpace(session.relayJoinCode))
                {
                    Debug.LogError("NetworkStarter: relayJoinCode is empty for client!");
                    return;
                }

                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(session.relayJoinCode);
                var relayData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
                NetworkManager.Singleton.StartClient();

                Debug.Log("NetworkStarter: CLIENT started successfully");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("NetworkStarter failed: " + e);
        }
    }
}
