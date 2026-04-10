using Unity.Netcode;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    [RequireComponent(typeof(NetworkObject))]
    public class Projectile : NetworkBehaviour
    {
        [SerializeField] private float speed = 18f;
        [SerializeField] private int damage = 20;
        [SerializeField] private float lifetime = 3f;

        private float aliveTime;

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            transform.position += transform.forward * speed * Time.deltaTime;

            aliveTime += Time.deltaTime;
            if (aliveTime >= lifetime)
            {
                NetworkObject.Despawn(true);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            PlayerNetwork target = other.GetComponent<PlayerNetwork>();
            if (target != null)
            {
                if (!target.IsAlive.Value || target.OwnerClientId == OwnerClientId)
                {
                    return;
                }

                target.ApplyDamage(damage);
            }

            NetworkObject.Despawn(true);
        }
    }
}
