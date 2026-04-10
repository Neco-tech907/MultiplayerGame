using Unity.Netcode;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    [RequireComponent(typeof(NetworkObject))]
    public class HealthPickup : NetworkBehaviour
    {
        [SerializeField] private int healAmount = 40;

        private PickupManager manager;
        private Vector3 spawnPosition;

        public void Init(PickupManager pickupManager)
        {
            manager = pickupManager;
            spawnPosition = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            PlayerNetwork player = other.GetComponent<PlayerNetwork>();
            if (player == null || !player.IsAlive.Value)
            {
                return;
            }

            if (player.HP.Value >= player.MaxHp)
            {
                return;
            }

            player.ApplyHeal(healAmount);
            manager?.OnPickedUp(spawnPosition);
            NetworkObject.Despawn(true);
        }
    }
}
