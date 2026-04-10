using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace MultiplayerGame.Practice1
{
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerNetwork : NetworkBehaviour
    {
        [Header("Stats")]
        [SerializeField] private int maxHp = 100;
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private GameObject modelRoot;

        [Header("Spawn")]
        [SerializeField] private float spawnSpacing = 2.5f;
        [SerializeField] private bool useSceneSpawnPoints = true;

        public NetworkVariable<FixedString32Bytes> Nickname = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public NetworkVariable<int> HP = new(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsAlive = new(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Coroutine respawnRoutine;

        public override void OnNetworkSpawn()
        {
            HP.OnValueChanged += OnHpChanged;
            IsAlive.OnValueChanged += OnIsAliveChanged;

            if (IsServer)
            {
                HP.Value = maxHp;
                IsAlive.Value = true;
                AssignSpawnPosition();
            }

            if (IsOwner)
            {
                SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
            }

            OnIsAliveChanged(IsAlive.Value, IsAlive.Value);
        }

        public override void OnNetworkDespawn()
        {
            HP.OnValueChanged -= OnHpChanged;
            IsAlive.OnValueChanged -= OnIsAliveChanged;
        }

        public int MaxHp => maxHp;

        public void ApplyDamage(int damage)
        {
            if (!IsServer || !IsAlive.Value)
            {
                return;
            }

            int clampedDamage = Mathf.Max(0, damage);
            HP.Value = Mathf.Max(0, HP.Value - clampedDamage);
        }

        public void ApplyHeal(int amount)
        {
            if (!IsServer || !IsAlive.Value)
            {
                return;
            }

            int clampedHeal = Mathf.Max(0, amount);
            HP.Value = Mathf.Min(maxHp, HP.Value + clampedHeal);
        }

        [ServerRpc]
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
            if (TryGetSpawnPosition(out Vector3 spawnPosition))
            {
                TeleportTo(spawnPosition);
                return;
            }

            float xOffset = (int)OwnerClientId * spawnSpacing;
            TeleportTo(new Vector3(xOffset, transform.position.y, 0f));
        }

        private void OnHpChanged(int previousValue, int newValue)
        {
            if (!IsServer)
            {
                return;
            }

            if (newValue > 0 || !IsAlive.Value)
            {
                return;
            }

            IsAlive.Value = false;

            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
            }

            respawnRoutine = StartCoroutine(RespawnRoutine());
        }

        private void OnIsAliveChanged(bool previousValue, bool newValue)
        {
            if (modelRoot != null)
            {
                modelRoot.SetActive(newValue);
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            AssignSpawnPosition();
            HP.Value = maxHp;
            IsAlive.Value = true;
            respawnRoutine = null;
        }

        private bool TryGetSpawnPosition(out Vector3 spawnPosition)
        {
            spawnPosition = default;
            if (!useSceneSpawnPoints)
            {
                return false;
            }

            PlayerSpawnPoint[] spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return false;
            }

            int index = Mathf.Abs((int)(OwnerClientId % (ulong)spawnPoints.Length));
            spawnPosition = spawnPoints[index].transform.position;
            return true;
        }

        private void TeleportTo(Vector3 worldPosition)
        {
            CharacterController characterController = GetComponent<CharacterController>();
            bool restoreCharacterController = characterController != null && characterController.enabled;

            if (restoreCharacterController)
            {
                characterController.enabled = false;
            }

            transform.position = worldPosition;

            if (restoreCharacterController)
            {
                characterController.enabled = true;
            }
        }
    }
}
