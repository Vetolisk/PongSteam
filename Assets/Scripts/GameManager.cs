using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI leftScoreText;
    [SerializeField] private TextMeshProUGUI rightScoreText;
    [SerializeField] private GameObject gameOverPanel;

    // НОВЫЙ СПОСОБ: SyncVar<T> вместо [SyncVar]
    public readonly SyncVar<int> leftScore = new SyncVar<int>();
    public readonly SyncVar<int> rightScore = new SyncVar<int>();
    public readonly SyncVar<bool> gameActive = new SyncVar<bool>(true);

    private void Awake()
    {
        // Подписываемся на изменения значений
        leftScore.OnChange += OnLeftScoreChanged;
        rightScore.OnChange += OnRightScoreChanged;
        gameActive.OnChange += OnGameActiveChanged;
    }

    private void Start()
    {
        UpdateUI();
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    [Server]
    public void AddScore(int player)
    {
        if (!gameActive.Value) return;

        if (player == 0)
            leftScore.Value++;
        else
            rightScore.Value++;

        if (leftScore.Value >= 10)
        {
            EndGame(0); // победил левый
        }
        else if (rightScore.Value >= 10)
        {
            EndGame(1); // победил правый
        }
    }

    // Callback при изменении счета слева
    private void OnLeftScoreChanged(int prev, int next, bool asServer)
    {
        if (leftScoreText != null)
            leftScoreText.text = next.ToString();
    }

    // Callback при изменении счета справа
    private void OnRightScoreChanged(int prev, int next, bool asServer)
    {
        if (rightScoreText != null)
            rightScoreText.text = next.ToString();
    }

    // Callback при изменении состояния игры
    private void OnGameActiveChanged(bool prev, bool next, bool asServer)
    {
        if (!next)
        {
            // Игра окончена
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }
    }

    // Вспомогательный метод для обновления UI (если нужен)
    private void UpdateUI()
    {
        if (leftScoreText != null)
            leftScoreText.text = leftScore.Value.ToString();
        if (rightScoreText != null)
            rightScoreText.text = rightScore.Value.ToString();
    }

    [Server]
    private void EndGame(int winner)
    {
        gameActive.Value = false;

        // Показываем экран победы всем клиентам
        ShowGameOver(winner);

        // Запускаем таймер на отключение
        Invoke(nameof(DisconnectAllPlayers), 5f);
    }

    [ObserversRpc]
    private void ShowGameOver(int winner)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            TextMeshProUGUI winnerText = gameOverPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (winnerText != null)
            {
                winnerText.text = winner == 0 ? "LEFT PLAYER WINS!" : "RIGHT PLAYER WINS!";
            }
        }
    }

    [Server]
    private void DisconnectAllPlayers()
    {
        foreach (NetworkConnection conn in ServerManager.Clients.Values)
        {
            conn.Disconnect(true);
        }
    }
}