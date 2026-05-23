using FishNet.Object;
using FishNet.Object.Synchronizing;
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
        [SerializeField] private PlayerView playerView;

        public readonly SyncVar<string> Nickname = new("Player");

        public readonly SyncVar<int> HP = new(100);

        public readonly SyncVar<bool> IsAlive = new(true);

        public readonly SyncVar<int> Score = new(0);

        private Coroutine respawnRoutine;

        private void Awake()
        {
            CacheReferences();
            Nickname.OnChange += OnNicknameChanged;
            HP.OnChange += OnHpChanged;
            IsAlive.OnChange += OnIsAliveChanged;
            Score.OnChange += OnScoreChanged;
        }

        public override void OnStartNetwork()
        {
            CacheReferences();

            if (base.IsServerInitialized)
            {
                HP.Value = maxHp;
                IsAlive.Value = true;
                Score.Value = 0;
                AssignSpawnPosition();
            }

            ApplyViewState();
            ApplyAliveState(IsAlive.Value);
        }

        public override void OnStartClient()
        {
            if (base.IsOwner)
            {
                SetNicknameServerRpc(ConnectionUI.PlayerNickname);
            }
        }

        public int MaxHp => maxHp;

        public void ApplyDamage(int damage, PlayerNetwork attacker = null)
        {
            if (!base.IsServerInitialized || !IsAlive.Value)
            {
                return;
            }

            int clampedDamage = Mathf.Max(0, damage);
            int nextHp = Mathf.Max(0, HP.Value - clampedDamage);
            bool wasEliminated = nextHp == 0 && HP.Value > 0;

            HP.Value = nextHp;

            if (wasEliminated && attacker != null && attacker != this)
            {
                attacker.AddScore(1);
            }
        }

        public void ApplyHeal(int amount)
        {
            if (!base.IsServerInitialized || !IsAlive.Value)
            {
                return;
            }

            int clampedHeal = Mathf.Max(0, amount);
            HP.Value = Mathf.Min(maxHp, HP.Value + clampedHeal);
        }

        public void AddScore(int amount)
        {
            if (!base.IsServerInitialized)
            {
                return;
            }

            Score.Value = Mathf.Max(0, Score.Value + Mathf.Max(0, amount));
            GameManager.Instance?.NotifyScoreChanged(this);
        }

        public void ResetForLobby()
        {
            if (!base.IsServerInitialized)
            {
                return;
            }

            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }

            Score.Value = 0;
            HP.Value = maxHp;
            IsAlive.Value = true;
            AssignSpawnPosition();
            ResetCombatState();
        }

        public void ResetForMatchStart()
        {
            if (!base.IsServerInitialized)
            {
                return;
            }

            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }

            HP.Value = maxHp;
            IsAlive.Value = true;
            AssignSpawnPosition();
            ResetCombatState();
        }

        [ServerRpc]
        private void SetNicknameServerRpc(string nickname)
        {
            Nickname.Value = NormalizeNickname(nickname);
        }

        private string NormalizeNickname(string rawNickname)
        {
            return string.IsNullOrWhiteSpace(rawNickname)
                ? $"Player_{OwnerId}"
                : rawNickname.Trim();
        }

        private void AssignSpawnPosition()
        {
            if (TryGetSpawnPosition(out Vector3 spawnPosition))
            {
                TeleportTo(spawnPosition);
                return;
            }

            float xOffset = OwnerId * spawnSpacing;
            TeleportTo(new Vector3(xOffset, transform.position.y, 0f));
        }

        private void OnNicknameChanged(string previousValue, string newValue, bool asServer)
        {
            if (playerView != null)
            {
                playerView.SetNickname(newValue);
            }
        }

        private void OnHpChanged(int previousValue, int newValue, bool asServer)
        {
            if (playerView != null)
            {
                playerView.SetHp(newValue);
            }

            if (!asServer || newValue > 0 || !IsAlive.Value)
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

        private void OnIsAliveChanged(bool previousValue, bool newValue, bool asServer)
        {
            ApplyAliveState(newValue);
        }

        private void OnScoreChanged(int previousValue, int newValue, bool asServer)
        {
            if (playerView != null)
            {
                playerView.SetScore(newValue);
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

            int index = Mathf.Abs(OwnerId % spawnPoints.Length);
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

        private void CacheReferences()
        {
            if (playerView == null)
            {
                playerView = GetComponent<PlayerView>();
            }
        }

        private void ApplyViewState()
        {
            if (playerView == null)
            {
                return;
            }

            playerView.SetNickname(Nickname.Value);
            playerView.SetHp(HP.Value);
            playerView.SetScore(Score.Value);
        }

        private void ApplyAliveState(bool newValue)
        {
            if (modelRoot != null)
            {
                modelRoot.SetActive(newValue);
            }
        }

        private void Reset()
        {
            playerView = GetComponent<PlayerView>();
        }

        private void OnValidate()
        {
            if (playerView == null)
            {
                playerView = GetComponent<PlayerView>();
            }
        }

        private void ResetCombatState()
        {
            PlayerCombat combat = GetComponent<PlayerCombat>();
            if (combat != null)
            {
                combat.ServerResetAmmo();
            }
        }
    }
}
