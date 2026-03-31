using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    public class PlayerView : NetworkBehaviour
    {
        [SerializeField] private PlayerNetwork playerNetwork;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text hpText;

        public override void OnNetworkSpawn()
        {
            if (playerNetwork == null)
            {
                Debug.LogError("PlayerView requires a PlayerNetwork reference.");
                return;
            }

            playerNetwork.Nickname.OnValueChanged += OnNicknameChanged;
            playerNetwork.HP.OnValueChanged += OnHpChanged;

            OnNicknameChanged(default, playerNetwork.Nickname.Value);
            OnHpChanged(0, playerNetwork.HP.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (playerNetwork == null)
            {
                return;
            }

            playerNetwork.Nickname.OnValueChanged -= OnNicknameChanged;
            playerNetwork.HP.OnValueChanged -= OnHpChanged;
        }

        private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
        {
            if (nicknameText != null)
            {
                nicknameText.text = newValue.ToString();
            }
        }

        private void OnHpChanged(int oldValue, int newValue)
        {
            if (hpText != null)
            {
                hpText.text = $"HP: {newValue}";
            }
        }

        private void Reset()
        {
            playerNetwork = GetComponent<PlayerNetwork>();
        }

        private void OnValidate()
        {
            if (playerNetwork == null)
            {
                playerNetwork = GetComponent<PlayerNetwork>();
            }
        }
    }
}
