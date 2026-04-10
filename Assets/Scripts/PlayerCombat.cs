using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame.Practice1
{
    public class PlayerCombat : NetworkBehaviour
    {
        [SerializeField] private PlayerNetwork playerNetwork;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float cooldown = 0.4f;
        [SerializeField] private int maxAmmo = 10;

        public NetworkVariable<int> CurrentAmmo = new(
            10,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private float lastShotTime;

        private void Update()
        {
            if (!IsOwner || !IsSpawned || playerNetwork == null || !playerNetwork.IsAlive.Value)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Transform muzzle = firePoint != null ? firePoint : transform;
                ShootServerRpc(muzzle.position, muzzle.forward);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentAmmo.Value = maxAmmo;
            }

            if (playerNetwork != null)
            {
                playerNetwork.IsAlive.OnValueChanged += OnIsAliveChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (playerNetwork != null)
            {
                playerNetwork.IsAlive.OnValueChanged -= OnIsAliveChanged;
            }
        }

        [ServerRpc]
        private void ShootServerRpc(Vector3 position, Vector3 direction, ServerRpcParams rpcParams = default)
        {
            if (playerNetwork == null || !playerNetwork.IsAlive.Value)
            {
                return;
            }

            if (CurrentAmmo.Value <= 0)
            {
                return;
            }

            if (Time.time < lastShotTime + cooldown)
            {
                return;
            }

            if (projectilePrefab == null)
            {
                Debug.LogError("PlayerCombat requires a projectile prefab.");
                return;
            }

            Vector3 normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            GameObject projectileObject = Instantiate(
                projectilePrefab,
                position + normalizedDirection * 1.2f,
                Quaternion.LookRotation(normalizedDirection));

            NetworkObject projectileNetworkObject = projectileObject.GetComponent<NetworkObject>();
            if (projectileNetworkObject == null)
            {
                Debug.LogError("Projectile prefab must contain a NetworkObject component.");
                Destroy(projectileObject);
                return;
            }

            lastShotTime = Time.time;
            CurrentAmmo.Value--;
            projectileNetworkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
        }

        private void OnIsAliveChanged(bool previousValue, bool currentValue)
        {
            if (!IsServer || currentValue)
            {
                return;
            }

            CurrentAmmo.Value = maxAmmo;
        }

        private void Reset()
        {
            playerNetwork = GetComponent<PlayerNetwork>();
            firePoint = transform;
        }

        private void OnValidate()
        {
            if (playerNetwork == null)
            {
                playerNetwork = GetComponent<PlayerNetwork>();
            }

            if (firePoint == null)
            {
                firePoint = transform;
            }
        }
    }
}
