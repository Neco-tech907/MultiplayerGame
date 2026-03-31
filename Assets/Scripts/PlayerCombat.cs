using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame.Practice1
{
    public class PlayerCombat : NetworkBehaviour
    {
        [SerializeField] private PlayerNetwork playerNetwork;
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackRange = 3f;

        private void Update()
        {
            if (!IsOwner || !IsSpawned)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                RequestAttackServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestAttackServerRpc()
        {
            if (playerNetwork == null || !playerNetwork.IsAlive)
            {
                return;
            }

            PlayerNetwork target = FindNearestTarget();
            if (target == null)
            {
                return;
            }

            target.ApplyDamage(damage);
            Debug.Log($"{playerNetwork.Nickname.Value} dealt {damage} damage to {target.Nickname.Value}. Target HP: {target.HP.Value}");
        }

        private PlayerNetwork FindNearestTarget()
        {
            PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
            PlayerNetwork bestTarget = null;
            float bestDistance = attackRange;

            foreach (PlayerNetwork candidate in players)
            {
                if (candidate == null || candidate == playerNetwork || !candidate.IsSpawned || !candidate.IsAlive)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance > bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestTarget = candidate;
            }

            return bestTarget;
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
