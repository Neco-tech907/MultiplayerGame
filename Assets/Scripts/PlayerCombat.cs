using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
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
        [SerializeField] private int maxAmmo = 30;

        public readonly SyncVar<int> CurrentAmmo = new(30);

        private float lastShotTime;

        public override void OnStartNetwork()
        {
            if (base.IsServerInitialized)
            {
                CurrentAmmo.Value = maxAmmo;
            }
        }

        [ServerRpc]
        private void ShootServerRpc(Vector3 position, Vector3 direction, NetworkConnection sender = null)
        {
            if (playerNetwork == null || !playerNetwork.IsAlive.Value || GameManager.Instance == null || !GameManager.Instance.IsMatchInProgress)
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

            FishNet.Object.NetworkObject projectileNetworkObject = projectileObject.GetComponent<FishNet.Object.NetworkObject>();
            if (projectileNetworkObject == null)
            {
                Debug.LogError("Projectile prefab must contain a NetworkObject component.");
                Destroy(projectileObject);
                return;
            }

            lastShotTime = Time.time;
            CurrentAmmo.Value--;
            Projectile projectile = projectileObject.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetShooter(playerNetwork);
            }
            base.ServerManager.Spawn(projectileNetworkObject, sender);
        }

        private void Update()
        {
            if (!base.IsOwner || !base.IsClientInitialized || playerNetwork == null || !playerNetwork.IsAlive.Value)
            {
                return;
            }

            if (GameManager.Instance == null || !GameManager.Instance.IsMatchInProgress)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Transform muzzle = firePoint != null ? firePoint : transform;
                ShootServerRpc(muzzle.position, muzzle.forward);
            }
        }

        private void LateUpdate()
        {
            if (!base.IsServerInitialized || playerNetwork == null || playerNetwork.IsAlive.Value || CurrentAmmo.Value == maxAmmo)
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

        public void ServerResetAmmo()
        {
            if (!base.IsServerInitialized)
            {
                return;
            }

            CurrentAmmo.Value = maxAmmo;
        }
    }
}
