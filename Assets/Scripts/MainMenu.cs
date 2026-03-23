using FishNet;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;

    [Header("UI")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Transform lobbyListContainer;
    [SerializeField] private GameObject lobbyItemPrefab;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    // Callbacks для Steam (ДОЛЖНЫ быть полями класса, чтобы GC не удалил)
    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<LobbyEnter_t> lobbyEnterCallback;
    private Callback<LobbyMatchList_t> lobbyMatchListCallback;

    private CSteamID currentLobbyID;
    private List<GameObject> lobbyItems = new List<GameObject>();

    private void Start()
    {
        if (SteamManager.Initialized)
        {
            // Регистрируем Callbacks (ВАЖНО: сохраняем в поля класса!)
            lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            lobbyMatchListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);

            hostButton.onClick.AddListener(HostGame);
            joinButton.onClick.AddListener(RequestLobbyList);
            if (refreshButton != null) refreshButton.onClick.AddListener(RequestLobbyList);
            if (backButton != null) backButton.onClick.AddListener(BackToMain);

            statusText.text = "✓ Steam ready";
        }
        else
        {
            statusText.text = "✗ Steam not initialized!";
            hostButton.interactable = false;
            joinButton.interactable = false;
            if (refreshButton != null) refreshButton.interactable = false;
        }
    }

    // ==================== СОЗДАНИЕ ЛОББИ ====================

    private void HostGame()
    {
        statusText.text = "Creating lobby...";

        // Правильная сигнатура: CreateLobby(тип, макс_игроков)
        // Результат придет в OnLobbyCreated через Callback
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 2);
    }

    // Callback при создании лобби (вызывается автоматически SteamAPI)
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            statusText.text = $"Failed to create lobby: {callback.m_eResult}";
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Сохраняем данные лобби (для поиска другими игроками)
        SteamMatchmaking.SetLobbyData(currentLobbyID, "name", "Pong Game");
        SteamMatchmaking.SetLobbyData(currentLobbyID, "appID", "480");

        statusText.text = "Lobby created! Starting server...";

        // Показываем код лобби (Steam ID)
        if (lobbyCodeText != null)
            lobbyCodeText.text = $"Lobby ID: {currentLobbyID.m_SteamID}";

        lobbyPanel.SetActive(true);
        mainPanel.SetActive(false);

        // Запускаем сервер FishNet
        InstanceFinder.ServerManager.StartConnection();

        // Загружаем игровую сцену
        SceneManager.LoadScene("Game");
    }

    // ==================== ПОИСК ЛОББИ ====================

    private void RequestLobbyList()
    {
        statusText.text = "Searching for lobbies...";

        // Очищаем старый список UI
        ClearLobbyList();

        // Добавляем фильтры для поиска (ищем только лобби с нашим App ID)
        SteamMatchmaking.AddRequestLobbyListStringFilter("appID", "480", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(10);

        // Запрашиваем список лобби
        // Результат придет в OnLobbyMatchList через Callback
        SteamMatchmaking.RequestLobbyList();
    }

    // Callback при получении списка лобби
    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        int lobbiesCount = (int)callback.m_nLobbiesMatching;

        if (lobbiesCount == 0)
        {
            statusText.text = "No lobbies found. Create one!";
            return;
        }

        statusText.text = $"Found {lobbiesCount} lobby(s)";

        for (int i = 0; i < lobbiesCount; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);

            // Получаем данные лобби для отображения
            string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, "name");
            if (string.IsNullOrEmpty(lobbyName))
                lobbyName = "Unnamed Lobby";

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);

            // Создаем UI элемент для лобби
            if (lobbyListContainer != null && lobbyItemPrefab != null)
            {
                GameObject item = Instantiate(lobbyItemPrefab, lobbyListContainer);
                lobbyItems.Add(item);

                TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0)
                    texts[0].text = $"{lobbyName} ({memberCount}/2)";

                Button btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    CSteamID capturedId = lobbyId;
                    btn.onClick.AddListener(() => JoinLobby(capturedId));
                }
            }
        }
    }

    private void ClearLobbyList()
    {
        foreach (GameObject item in lobbyItems)
        {
            Destroy(item);
        }
        lobbyItems.Clear();
    }

    // ==================== ПРИСОЕДИНЕНИЕ К ЛОББИ ====================

    private void JoinLobby(CSteamID lobbyId)
    {
        statusText.text = "Joining lobby...";

        // Правильная сигнатура: JoinLobby(SteamID лобби)
        // Результат придет в OnLobbyEntered через Callback
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    // Callback при входе в лобби
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // m_EChatRoomEnterResponse == 1 означает успех
        if (callback.m_EChatRoomEnterResponse != 1)
        {
            statusText.text = $"Failed to join lobby: code {callback.m_EChatRoomEnterResponse}";
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        statusText.text = "Joined lobby! Connecting...";

        // Запускаем клиент FishNet
        InstanceFinder.ClientManager.StartConnection();

        // Загружаем игровую сцену
        SceneManager.LoadScene("Game");
    }

    private void BackToMain()
    {
        lobbyPanel.SetActive(false);
        mainPanel.SetActive(true);

        if (currentLobbyID.m_SteamID != 0)
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
        }

        ClearLobbyList();
    }

    private void OnDestroy()
    {
        // Callback'и автоматически отписываются при уничтожении объекта
        // Дополнительной очистки не требуется
    }
}