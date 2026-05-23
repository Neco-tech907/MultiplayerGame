using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using TMPro;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    public class ConnectionUI : MonoBehaviour
    {
        private const string DefaultAddress = "127.0.0.1";
        private const ushort DefaultPort = 7770;

        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private TMP_InputField portInput;
        [SerializeField] private GameObject menuRoot;

        public static string PlayerNickname { get; private set; } = "Player";

        private void Awake()
        {
            if (addressInput != null && string.IsNullOrWhiteSpace(addressInput.text))
            {
                addressInput.text = DefaultAddress;
            }

            if (portInput != null && string.IsNullOrWhiteSpace(portInput.text))
            {
                portInput.text = DefaultPort.ToString();
            }
        }

        public void StartAsHost()
        {
            SaveNickname();
            if (!TryGetNetworkManager(out NetworkManager networkManager))
            {
                return;
            }

            string address = addressInput != null && !string.IsNullOrWhiteSpace(addressInput.text)
                ? addressInput.text.Trim()
                : DefaultAddress;

            ConfigureTransport(networkManager, address, ParsePort(DefaultPort));

            bool serverStarted = networkManager.ServerManager.StartConnection();
            bool clientStarted = serverStarted && networkManager.ClientManager.StartConnection();

            if (serverStarted && clientStarted)
            {
                HideMenu();
            }
            else
            {
                if (serverStarted)
                {
                    networkManager.ClientManager.StopConnection();
                    networkManager.ServerManager.StopConnection(true);
                }

                Debug.LogError("Failed to start Host. Check FishNet NetworkManager and Tugboat configuration.");
            }
        }

        public void StartAsClient()
        {
            SaveNickname();
            if (!TryGetNetworkManager(out NetworkManager networkManager))
            {
                return;
            }

            string address = addressInput != null && !string.IsNullOrWhiteSpace(addressInput.text)
                ? addressInput.text.Trim()
                : DefaultAddress;

            ConfigureTransport(networkManager, address, ParsePort(DefaultPort));

            if (networkManager.ClientManager.StartConnection())
            {
                HideMenu();
            }
            else
            {
                Debug.LogError("Failed to start Client. Check the host address, port, and Tugboat configuration.");
            }
        }

        private void SaveNickname()
        {
            string rawValue = nicknameInput != null ? nicknameInput.text : string.Empty;
            PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
        }

        private void ConfigureTransport(NetworkManager networkManager, string address, ushort port)
        {
            if (!TryGetTransport(networkManager, out Transport transport))
            {
                return;
            }

            transport.SetClientAddress(address);
            transport.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
            transport.SetPort(port);
        }

        private ushort ParsePort(ushort fallback)
        {
            if (portInput == null)
            {
                return fallback;
            }

            return ushort.TryParse(portInput.text, out ushort parsedPort) ? parsedPort : fallback;
        }

        private bool TryGetNetworkManager(out NetworkManager networkManager)
        {
            networkManager = InstanceFinder.NetworkManager;

            if (networkManager != null)
            {
                return true;
            }

            Debug.LogError("FishNet NetworkManager was not found in the scene.");
            return false;
        }

        private bool TryGetTransport(NetworkManager networkManager, out Transport transport)
        {
            transport = networkManager != null ? networkManager.TransportManager.Transport : null;

            if (transport != null)
            {
                return true;
            }

            Debug.LogError("FishNet transport was not found on the NetworkManager object.");
            return false;
        }

        private void HideMenu()
        {
            if (menuRoot != null)
            {
                menuRoot.SetActive(false);
            }
        }
    }
}
