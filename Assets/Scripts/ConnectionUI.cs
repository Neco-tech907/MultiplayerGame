using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    public class ConnectionUI : MonoBehaviour
    {
        private const ushort DefaultPort = 7777;

        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private TMP_InputField portInput;
        [SerializeField] private GameObject menuRoot;

        public static string PlayerNickname { get; private set; } = "Player";

        public void StartAsHost()
        {
            SaveNickname();
            ConfigureTransportForLocalSession();

            if (NetworkManager.Singleton.StartHost())
            {
                HideMenu();
            }
            else
            {
                Debug.LogError("Failed to start Host. Check NetworkManager and UnityTransport configuration.");
            }
        }

        public void StartAsClient()
        {
            SaveNickname();
            ConfigureTransportForClient();

            if (NetworkManager.Singleton.StartClient())
            {
                HideMenu();
            }
            else
            {
                Debug.LogError("Failed to start Client. Check the host address, port, and transport configuration.");
            }
        }

        private void SaveNickname()
        {
            string rawValue = nicknameInput != null ? nicknameInput.text : string.Empty;
            PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
        }

        private void ConfigureTransportForLocalSession()
        {
            if (!TryGetTransport(out UnityTransport transport))
            {
                return;
            }

            ushort port = ParsePort(DefaultPort);
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        }

        private void ConfigureTransportForClient()
        {
            if (!TryGetTransport(out UnityTransport transport))
            {
                return;
            }

            string address = addressInput != null && !string.IsNullOrWhiteSpace(addressInput.text)
                ? addressInput.text.Trim()
                : "127.0.0.1";

            ushort port = ParsePort(DefaultPort);
            transport.SetConnectionData(address, port);
        }

        private ushort ParsePort(ushort fallback)
        {
            if (portInput == null)
            {
                return fallback;
            }

            return ushort.TryParse(portInput.text, out ushort parsedPort) ? parsedPort : fallback;
        }

        private bool TryGetTransport(out UnityTransport transport)
        {
            transport = NetworkManager.Singleton != null ? NetworkManager.Singleton.GetComponent<UnityTransport>() : null;

            if (transport != null)
            {
                return true;
            }

            Debug.LogError("UnityTransport component was not found on the NetworkManager object.");
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
