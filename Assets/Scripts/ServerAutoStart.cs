using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    public class ServerAutoStart : MonoBehaviour
    {
        [SerializeField] private ushort port = 7770;
        [SerializeField] private string bindAddress = "0.0.0.0";

        private void Start()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null)
            {
                Debug.LogError("[Server] NetworkManager not found.");
                return;
            }

            Transport transport = networkManager.TransportManager.Transport;
            if (transport == null)
            {
                Debug.LogError("[Server] Transport not found.");
                return;
            }

            transport.SetServerBindAddress(bindAddress, IPAddressType.IPv4);
            transport.SetPort(port);

            if (networkManager.ServerManager.StartConnection())
            {
                Debug.Log($"[Server] Headless mode detected. Server started on port {port}.");
            }
            else
            {
                Debug.LogError("[Server] Failed to start server in headless mode.");
            }
        }
    }
}
