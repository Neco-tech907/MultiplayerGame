using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    [RequireComponent(typeof(NetworkObject))]
    public class GameManager : NetworkBehaviour
    {
        public enum GameState
        {
            WaitingForPlayers,
            InProgress,
            ShowingResults
        }

        [Header("Match Flow")]
        [SerializeField] private int requiredPlayers = 2;
        [SerializeField] private float matchDuration = 60f;
        [SerializeField] private float resultsDuration = 5f;
        [SerializeField] private int killsToWin = 3;

        public readonly SyncVar<GameState> CurrentState = new(GameState.WaitingForPlayers);
        public readonly SyncVar<int> ConnectedPlayers = new(0);
        public readonly SyncVar<float> MatchTimer = new(0f);

        public static GameManager Instance { get; private set; }

        public int RequiredPlayers => requiredPlayers;
        public bool IsMatchInProgress => CurrentState.Value == GameState.InProgress;

        private void Awake()
        {
            if (Instance == null || Instance == this)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Duplicate GameManager found. The latest instance will be used.");
                Instance = this;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            base.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            ConnectedPlayers.Value = base.ServerManager.Clients.Count;
            CurrentState.Value = GameState.WaitingForPlayers;
            MatchTimer.Value = matchDuration;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            base.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (base.ServerManager != null)
            {
                base.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            }
        }

        private void Update()
        {
            if (!base.IsServerInitialized)
            {
                return;
            }

            if (CurrentState.Value != GameState.InProgress)
            {
                return;
            }

            MatchTimer.Value = Mathf.Max(0f, MatchTimer.Value - Time.deltaTime);
            if (MatchTimer.Value <= 0f)
            {
                EndMatch();
            }
        }

        public void NotifyScoreChanged(PlayerNetwork player)
        {
            if (!base.IsServerInitialized || CurrentState.Value != GameState.InProgress || player == null)
            {
                return;
            }

            if (killsToWin > 0 && player.Score.Value >= killsToWin)
            {
                EndMatch();
            }
        }

        public List<PlayerNetwork> GetSortedPlayers()
        {
            return FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None)
                .OrderByDescending(p => p.Score.Value)
                .ThenBy(p => p.Nickname.Value)
                .ToList();
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (!base.IsServerInitialized)
            {
                return;
            }

            ConnectedPlayers.Value = base.ServerManager.Clients.Count;

            if (CurrentState.Value == GameState.WaitingForPlayers &&
                ConnectedPlayers.Value >= requiredPlayers)
            {
                StartMatch();
            }
        }

        private void StartMatch()
        {
            CancelInvoke(nameof(ResetToLobby));
            PreparePlayersForMatch();
            MatchTimer.Value = matchDuration;
            CurrentState.Value = GameState.InProgress;
            Debug.Log("[Server] Match started.");
        }

        private void EndMatch()
        {
            if (CurrentState.Value == GameState.ShowingResults)
            {
                return;
            }

            CurrentState.Value = GameState.ShowingResults;
            MatchTimer.Value = 0f;
            Debug.Log("[Server] Match ended. Showing results.");
            Invoke(nameof(ResetToLobby), resultsDuration);
        }

        private void ResetToLobby()
        {
            foreach (PlayerNetwork player in FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None))
            {
                player.ResetForLobby();
            }

            ConnectedPlayers.Value = base.ServerManager.Clients.Count;
            MatchTimer.Value = matchDuration;
            CurrentState.Value = GameState.WaitingForPlayers;
            Debug.Log("[Server] Lobby reset. Waiting for players.");

            if (ConnectedPlayers.Value >= requiredPlayers)
            {
                StartMatch();
            }
        }

        private void PreparePlayersForMatch()
        {
            foreach (PlayerNetwork player in FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None))
            {
                player.ResetForMatchStart();
            }
        }
    }
}
