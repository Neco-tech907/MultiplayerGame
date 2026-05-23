using System.Text;
using TMPro;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    public class MatchUI : MonoBehaviour
    {
        [SerializeField] private GameObject waitingRoot;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject resultsRoot;
        [SerializeField] private TMP_Text waitingText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text resultsText;

        private GameManager gameManager;

        private void Update()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager == null)
                {
                    SetActive(waitingRoot, false);
                    SetActive(hudRoot, false);
                    SetActive(resultsRoot, false);
                    return;
                }
            }

            RefreshState();
        }

        private void RefreshState()
        {
            switch (gameManager.CurrentState.Value)
            {
                case GameManager.GameState.WaitingForPlayers:
                    SetActive(waitingRoot, true);
                    SetActive(hudRoot, false);
                    SetActive(resultsRoot, false);
                    if (waitingText != null)
                    {
                        waitingText.text =
                            $"Waiting for players: {gameManager.ConnectedPlayers.Value}/{gameManager.RequiredPlayers}";
                    }
                    break;

                case GameManager.GameState.InProgress:
                    SetActive(waitingRoot, false);
                    SetActive(hudRoot, true);
                    SetActive(resultsRoot, false);
                    if (timerText != null)
                    {
                        timerText.text = $"Time: {Mathf.CeilToInt(gameManager.MatchTimer.Value)}";
                    }
                    break;

                case GameManager.GameState.ShowingResults:
                    SetActive(waitingRoot, false);
                    SetActive(hudRoot, false);
                    SetActive(resultsRoot, true);
                    if (resultsText != null)
                    {
                        resultsText.text = BuildResultsText();
                    }
                    break;
            }
        }

        private string BuildResultsText()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Results");

            foreach (PlayerNetwork player in gameManager.GetSortedPlayers())
            {
                builder.AppendLine($"{player.Nickname.Value}: {player.Score.Value}");
            }

            return builder.ToString().TrimEnd();
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }
    }
}
