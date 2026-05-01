using FishNet.Object;
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
            if (!base.IsServerInitialized)
            {
                return;
            }

            transform.position += transform.forward * speed * Time.deltaTime;

            aliveTime += Time.deltaTime;
            if (aliveTime >= lifetime)
            {
                base.Despawn();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!base.IsServerInitialized)
            {
                return;
            }

            PlayerNetwork target = other.GetComponent<PlayerNetwork>();
            if (target != null)
            {
                if (!target.IsAlive.Value || target.OwnerId == OwnerId)
                {
                    return;
                }

                target.ApplyDamage(damage);
            }

            base.Despawn();
        }
    }
}
