using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerNetwork : NetworkBehaviour
    {
        [Header("Stats")]
        [SerializeField] private int maxHp = 100;

        [Header("Spawn")]
        [SerializeField] private float spawnSpacing = 2.5f;

        public NetworkVariable<FixedString32Bytes> Nickname = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public NetworkVariable<int> HP = new(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                HP.Value = maxHp;
                AssignSpawnPosition();
            }

            if (IsOwner)
            {
                SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
            }
        }

        public bool IsAlive => HP.Value > 0;

        public void ApplyDamage(int damage)
        {
            if (!IsServer)
            {
                return;
            }

            int clampedDamage = Mathf.Max(0, damage);
            HP.Value = Mathf.Max(0, HP.Value - clampedDamage);
        }

        [Rpc(SendTo.Server)]
        private void SubmitNicknameServerRpc(string nickname)
        {
            Nickname.Value = NormalizeNickname(nickname);
        }

        private string NormalizeNickname(string rawNickname)
        {
            return string.IsNullOrWhiteSpace(rawNickname)
                ? $"Player_{OwnerClientId}"
                : rawNickname.Trim();
        }

        private void AssignSpawnPosition()
        {
            float xOffset = (int)OwnerClientId * spawnSpacing;
            transform.position = new Vector3(xOffset, transform.position.y, 0f);
        }
    }
}
