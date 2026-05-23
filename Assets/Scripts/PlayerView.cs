using TMPro;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    public class PlayerView : NetworkBehaviour
    {
        [SerializeField] private PlayerNetwork playerNetwork;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text scoreText;

        public override void OnStartNetwork()
        {
            if (playerNetwork != null)
            {
                SetNickname(playerNetwork.Nickname.Value);
                SetHp(playerNetwork.HP.Value);
                SetScore(playerNetwork.Score.Value);
            }
        }

        public void SetNickname(string value)
        {
            if (nicknameText != null)
            {
                nicknameText.text = value;
            }
        }

        public void SetHp(int value)
        {
            if (hpText != null)
            {
                hpText.text = $"HP: {value}";
            }
        }

        public void SetScore(int value)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {value}";
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
