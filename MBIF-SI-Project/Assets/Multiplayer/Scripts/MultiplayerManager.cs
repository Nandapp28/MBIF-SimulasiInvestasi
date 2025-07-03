using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class MultiplayerManager : MonoBehaviourPunCallbacks
{
    public static MultiplayerManager Instance;

    public enum GameState
    {
        WaitingForPlayers, TicketChoice, CardPhase,
        HelpCardPhase, SellingPhase, RumorPhase,
        ResolutionPhase, SemesterEnd
    }

    [Header("Game State")]
    private GameState currentState;
    private int helpCardTurnIndex = 0;
    private int playersSubmittedSellOrder = 0;

    [Header("Manager References")]
    public HelpCardPhaseManagerMultiplayer helpCardManager;
    public SellingPhaseManagerMultiplayer sellingManager;
    public RumorPhaseManagerMultiplayer rumorManager;
    public ResolutionPhaseManagerMultiplayer resolutionManager;
    public TicketManagerMultiplayer ticketManagerMultiplayer;

    [Header("UI References")]
    public GameObject playerEntryPrefab;
    public Transform playerListContainer;
    public GameObject cardPrefab;
    public Transform cardHolderParent;
    public GameObject ticketButtonPrefab;
    public Transform ticketListContainer;
    public GameObject activateButtonPrefab;
    public GameObject saveButtonPrefab;
    public Transform ActiveSaveContainer;
    public GameObject leaderboardPanel;
    public Transform leaderboardContainer;
    public GameObject leaderboardEntryPrefab;
    public TMP_Text statusText;
    public TMP_Text semesterText;

    [Header("Button References")]
    public Button skipButton;
    public Button resetSemesterButton;

    [Header("Game Logic Data")]
    private List<RumorEffect> shuffledRumorDeck = new List<RumorEffect>();
    private Dictionary<string, List<int>> ramalanTokens = new Dictionary<string, List<int>>();
    private Dictionary<string, int> dividendIndices = new Dictionary<string, int> { { "Red", 0 }, { "Blue", 0 }, { "Green", 0 }, { "Orange", 0 } };

    [System.Serializable]
    public class RumorEffect { public string color, cardName, description; }

    private Dictionary<int, PlayerProfileMultiplayer> players = new Dictionary<int, PlayerProfileMultiplayer>();
    private List<int> turnOrder = new List<int>();
    private List<GameObject> cardObjects = new List<GameObject>();
    private HashSet<int> takenCardIndices = new HashSet<int>();
    private Dictionary<int, Button> ticketButtons = new Dictionary<int, Button>();

    private GameObject currentlySelectedCard = null;
    private int currentTurnIndex = 0;
    private int totalCardsInPlay = 0;
    private int skipCount = 0;
    public int resetCount = 0;
    public int maxResetCount = 3;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int GetPlayerCount()
    {
        // Mengembalikan jumlah pemain yang saat ini ada di dalam room/permainan
        return players.Count; 
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            InitializePlayers();
            StartCoroutine(StartGameAfterDelay());
        }
        skipButton.gameObject.SetActive(false);
        resetSemesterButton.gameObject.SetActive(false);
        leaderboardPanel.SetActive(false);
    }

    private IEnumerator StartGameAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);
        photonView.RPC(nameof(RPC_ChangeGameState), RpcTarget.AllBuffered, GameState.TicketChoice);
    }

    #region PLAYER AND STATE MANAGEMENT

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        // Hanya MasterClient yang bertanggung jawab mengelola daftar pemain
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"Pemain baru masuk: {newPlayer.NickName}. MasterClient akan memproses.");

            // 1. Beri tahu SEMUA pemain (termasuk yang lama) untuk membuat profil bagi pemain BARU.
            //    Menggunakan AllBuffered agar pemain yang masuk berikutnya juga mendapat info ini.
            photonView.RPC(nameof(RPC_CreatePlayerProfile), RpcTarget.AllBuffered, newPlayer.NickName, newPlayer.ActorNumber);

            // 2. Kirim data pemain LAMA HANYA kepada pemain BARU.
            //    Ini agar pemain baru bisa langsung "catch-up" dan menampilkan UI semua pemain.
            foreach (var p in players.Values)
            {
                // Jangan kirim data pemain baru ke dirinya sendiri lagi, karena sudah tercakup di RPC pertama.
                if (p.actorNumber == newPlayer.ActorNumber) continue;
                
                // Kirim data pemain yang sudah ada ke pemain yang baru masuk.
                // Target RPC diubah ke 'newPlayer' secara spesifik.
                photonView.RPC(nameof(RPC_CreatePlayerProfile), newPlayer, p.playerName, p.actorNumber);
            }
        }
    }

    private void InitializePlayers()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            photonView.RPC(nameof(RPC_CreatePlayerProfile), RpcTarget.AllBuffered, p.NickName, p.ActorNumber);
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        currentState = newState;
        if (semesterText != null) semesterText.text = $"Semester {resetCount + 1}";

        switch (newState)
        {
            case GameState.TicketChoice: HandleTicketChoicePhase(); break;
            case GameState.CardPhase: HandleCardPhase(); break;
            case GameState.HelpCardPhase: HandleHelpCardPhase(); break;
            case GameState.SellingPhase: HandleSellingPhase(); break;
            case GameState.RumorPhase: HandleRumorPhase(); break;
            case GameState.ResolutionPhase: HandleResolutionPhase(); break;
            case GameState.SemesterEnd: HandleSemesterEnd(); break;
        }
    }

    public void GoToNextPhase()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        GameState nextState = currentState;
        switch (currentState)
        {
            case GameState.TicketChoice: nextState = GameState.CardPhase; break;
            case GameState.CardPhase: nextState = GameState.HelpCardPhase; break;
            case GameState.HelpCardPhase: nextState = GameState.SellingPhase; break;
            case GameState.SellingPhase: nextState = GameState.RumorPhase; break;
            case GameState.RumorPhase: nextState = GameState.ResolutionPhase; break;
            case GameState.ResolutionPhase: nextState = GameState.SemesterEnd; break;
            case GameState.SemesterEnd:
                if (resetCount < maxResetCount - 1)
                {
                    resetCount++;
                    photonView.RPC(nameof(RPC_ResetForNewSemester), RpcTarget.AllBuffered);
                    nextState = GameState.TicketChoice;
                }
                else { photonView.RPC(nameof(RPC_ShowLeaderboard), RpcTarget.All); }
                break;
        }
        if (nextState != currentState)
        {
            photonView.RPC(nameof(RPC_ChangeGameState), RpcTarget.AllBuffered, nextState);
        }
    }

    #endregion

    #region PHASE HANDLERS
    private void HandleTicketChoicePhase()
    {
        ticketListContainer.gameObject.SetActive(true);
        if (statusText != null) statusText.text = "Choose a starting ticket...";
        if (PhotonNetwork.IsMasterClient)
        {
            int playerCount = PhotonNetwork.PlayerList.Length;
            List<int> availableTickets = Enumerable.Range(1, playerCount).ToList();
            TicketManagerMultiplayer.ShuffleList(availableTickets);
            photonView.RPC(nameof(RPC_DisplayTicketChoices), RpcTarget.All, availableTickets.ToArray());
        }
    }

    private void HandleCardPhase()
    {
        ticketListContainer.gameObject.SetActive(false);
        if (PhotonNetwork.IsMasterClient)
        {
            List<PlayerProfileMultiplayer> sortedPlayers = players.Values.OrderBy(p => p.ticketNumber).ToList();
            turnOrder = sortedPlayers.Select(p => p.actorNumber).ToList();
            totalCardsInPlay = 2 * players.Count;
            InitializeDeck(out string[] cardNames, out string[] cardColors, out int[] cardValues);
            photonView.RPC(nameof(RPC_DrawCardsAndSetTurnOrder), RpcTarget.All, cardNames, cardColors, cardValues, turnOrder.ToArray());
            currentTurnIndex = 0;
            StartNextCardTurn_Master();
        }
    }
    
    // INI FUNGSI YANG HILANG SEBELUMNYA
    private void HandleHelpCardPhase()
    {
        if(statusText != null) statusText.text = "Help Card Phase";
        if(PhotonNetwork.IsMasterClient)
        {
            helpCardTurnIndex = 0;
            StartNextHelpCardTurn_Master();
        }
    }
    
    // INI FUNGSI YANG HILANG SEBELUMNYA
    private void HandleSellingPhase()
    {
        if(statusText != null) statusText.text = "Selling Phase: Choose cards to sell";
        playersSubmittedSellOrder = 0;
        sellingManager.StartSellingPhase(players, turnOrder);
    }
    
    private void HandleRumorPhase()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            InitializeRumorDeck_Master();
            StartCoroutine(RunRumorSequence_Master());
        }
    }

    private void HandleResolutionPhase()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            InitializeRamalanTokens_Master();
            StartCoroutine(RunResolutionSequence_Master());
        }
    }

    private void HandleSemesterEnd()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_ShowResetButton), RpcTarget.All);
        }
    }

    #endregion

    #region TURN MANAGEMENT
    private void StartNextCardTurn_Master()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (takenCardIndices.Count >= totalCardsInPlay || skipCount >= players.Count)
        {
            GoToNextPhase();
            return;
        }
        photonView.RPC(nameof(RPC_SetCurrentTurn), RpcTarget.All, currentTurnIndex);
    }
    
    private void StartNextHelpCardTurn_Master()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Jika semua pemain sudah dapat giliran
        if (helpCardTurnIndex >= turnOrder.Count)
        {
            GoToNextPhase();
            return;
        }

        int actorNumber = turnOrder[helpCardTurnIndex];
        PlayerProfileMultiplayer currentPlayer = players[actorNumber];

        // Jika pemain tidak punya kartu bantuan, lewati
        if (currentPlayer.helpCards.Count == 0)
        {
            helpCardTurnIndex++;
            StartNextHelpCardTurn_Master();
            return;
        }

        // Ambil kartu bantuan pertama (bisa diubah untuk memilih)
        HelpCardMultiplayer cardToUse = currentPlayer.helpCards[0];
        
        // Perintahkan client yang bersangkutan untuk menampilkan pilihan
        photonView.RPC(nameof(RPC_ShowHelpCardChoice), RpcTarget.All, actorNumber, cardToUse.cardName, cardToUse.description, cardToUse.effectType);
    }
    #endregion
    
    #region DECK AND INITIALIZATION
    private void InitializeDeck(out string[] cardNames, out string[] cardColors, out int[] cardValues)
    {
        List<CardMultiplayer> deck = new List<CardMultiplayer>();
        List<string> colors = new List<string> { "Red", "Blue", "Green", "Orange" };
        deck.Add(new CardMultiplayer("Trade Offer", "", 4, colors[Random.Range(0, colors.Count)]));
        deck.Add(new CardMultiplayer("Heal", "", 2, colors[Random.Range(0, colors.Count)]));
        // ... (dan seterusnya, isi deck sesuai game Anda)
        while (deck.Count < totalCardsInPlay) deck.Add(deck[Random.Range(0, deck.Count)]);
        cardNames = deck.Select(c => c.cardName).ToArray();
        cardColors = deck.Select(c => c.color).ToArray();
        cardValues = deck.Select(c => c.value).ToArray();
    }
    private void InitializeRumorDeck_Master()
    {
        shuffledRumorDeck.Clear();
        shuffledRumorDeck.Add(new RumorEffect { color = "Red", cardName = "Resesi_Ekonomi", description = "IPO Merah turun!" });
        //... (Isi sesuai game Anda)
    }

    private void InitializeRamalanTokens_Master()
    {
        ramalanTokens.Clear();
        dividendIndices = new Dictionary<string, int> { { "Red", 0 }, { "Blue", 0 }, { "Green", 0 }, { "Orange", 0 } };
        int[] possibleTokens = { -2, -1, 1, 2 };
        foreach (string color in new[] { "Red", "Blue", "Green", "Orange" })
        {
            ramalanTokens[color] = new List<int>();
            for (int i = 0; i < 4; i++) { ramalanTokens[color].Add(possibleTokens[Random.Range(0, possibleTokens.Length)]); }
        }
    }
    #endregion

    #region COROUTINE SEQUENCES (MASTERCLIENT ONLY)
    private IEnumerator RunRumorSequence_Master()
    {
        yield return new WaitForSeconds(2f);
        foreach (var rumor in shuffledRumorDeck)
        {
            photonView.RPC(nameof(RPC_ShowRumorCard), RpcTarget.All, rumor.color, rumor.cardName);
            yield return new WaitForSeconds(3f);
            // TODO: Terapkan efek rumor dan sinkronkan hasilnya via RPC
            photonView.RPC(nameof(RPC_HideRumorCards), RpcTarget.All);
            yield return new WaitForSeconds(1.5f);
        }
        GoToNextPhase();
    }

    private IEnumerator RunResolutionSequence_Master()
    {
        yield return new WaitForSeconds(2f);
        List<string> resolutionOrder = new List<string> { "Red", "Blue", "Green", "Orange" };
        foreach (string color in resolutionOrder)
        {
            int tokenIndex = resetCount;
            if (tokenIndex < ramalanTokens[color].Count)
            {
                int tokenValue = ramalanTokens[color][tokenIndex];
                dividendIndices[color] += tokenValue;
                photonView.RPC(nameof(RPC_RevealTokenAndUpdateDividend), RpcTarget.All, color, tokenValue, tokenIndex + 1, dividendIndices[color]);
                yield return new WaitForSeconds(2.5f);
            }
        }
        // ... (Logika distribusi dividen)
        GoToNextPhase();
    }
    #endregion

    #region UI EVENT HANDLERS
    public void OnTicketButtonClicked(int ticketNumber, Button clickedButton)
    {
        foreach (var btn in ticketButtons.Values) btn.interactable = false;
        photonView.RPC(nameof(Cmd_SelectTicket), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, ticketNumber);
    }

    public void OnCardClicked(GameObject cardObj, int cardIndex)
    {
        if (turnOrder.Count == 0 || turnOrder[currentTurnIndex] != PhotonNetwork.LocalPlayer.ActorNumber || takenCardIndices.Contains(cardIndex)) return;
        if (currentlySelectedCard != null) ResetCardSelection();
        currentlySelectedCard = cardObj;
        cardObj.transform.localScale = Vector3.one * 1.1f;
        GameObject activateBtn = Instantiate(activateButtonPrefab, ActiveSaveContainer);
        activateBtn.GetComponent<Button>().onClick.AddListener(() => OnPlayerAction(cardIndex, true));
        GameObject saveBtn = Instantiate(saveButtonPrefab, ActiveSaveContainer);
        saveBtn.GetComponent<Button>().onClick.AddListener(() => OnPlayerAction(cardIndex, false));
    }

    private void OnPlayerAction(int cardIndex, bool isActivating)
    {
        ResetCardSelection();
        skipButton.gameObject.SetActive(false);
        photonView.RPC(nameof(Cmd_PlayerAction), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, cardIndex, isActivating);
    }

    public void OnSkipButtonClicked()
    {
        skipButton.gameObject.SetActive(false);
        photonView.RPC(nameof(Cmd_PlayerSkips), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public void OnResetSemesterClicked()
    {
        if (PhotonNetwork.IsMasterClient) { GoToNextPhase(); }
    }
    #endregion
    
    #region RPCs & COMMANDS
    
    // RPCs (Dijalankan di SEMUA client)
    //------------------------------------
    [PunRPC]
    private void RPC_ChangeGameState(GameState newState) { OnGameStateChanged(newState); }

    [PunRPC]
    private void RPC_CreatePlayerProfile(string nickName, int actorNumber)
    {
        if (!players.ContainsKey(actorNumber)) { players.Add(actorNumber, new PlayerProfileMultiplayer(nickName, actorNumber)); }
        UpdatePlayerUI();
    }
    
    [PunRPC]
    private void RPC_DisplayTicketChoices(int[] ticketNumbers)
    {
        foreach (Transform child in ticketListContainer) Destroy(child.gameObject);
        ticketButtons.Clear();
        foreach (int ticketNum in ticketNumbers)
        {
            GameObject btnObj = Instantiate(ticketButtonPrefab, ticketListContainer);
            btnObj.GetComponentInChildren<Text>().text = "Choose";
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnTicketButtonClicked(ticketNum, btn));
            ticketButtons[ticketNum] = btn;
        }
    }
    
    [PunRPC]
    private void RPC_ClaimTicketAndUpdate(int actorNumber, int ticketNumber)
    {
        if (players.ContainsKey(actorNumber) && ticketButtons.ContainsKey(ticketNumber))
        {
            players[actorNumber].ticketNumber = ticketNumber;
            Button buttonToUpdate = ticketButtons[ticketNumber];
            buttonToUpdate.GetComponentInChildren<Text>().text = $"{ticketNumber} - {players[actorNumber].playerName}";
            buttonToUpdate.interactable = false;
            UpdatePlayerUI();
        }
    }

    [PunRPC]
    private void RPC_DrawCardsAndSetTurnOrder(string[] names, string[] colors, int[] values, int[] actorTurnOrder)
    {
        turnOrder = actorTurnOrder.ToList();
        takenCardIndices.Clear();
        foreach (Transform child in cardHolderParent) Destroy(child.gameObject);
        cardObjects.Clear();
        for (int i = 0; i < names.Length; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardHolderParent);
            cardObj.transform.Find("CardText").GetComponent<Text>().text = names[i];
            cardObj.transform.Find("CardColor").GetComponent<Text>().text = colors[i];
            cardObj.transform.Find("CardValue").GetComponent<Text>().text = values[i].ToString();
            int index = i;
            cardObj.GetComponent<Button>().onClick.AddListener(() => OnCardClicked(cardObj, index));
            cardObjects.Add(cardObj);
        }
    }

    [PunRPC]
    private void RPC_SetCurrentTurn(int newTurnIndex)
    {
        currentTurnIndex = newTurnIndex;
        int actorNumber = turnOrder[newTurnIndex];
        bool isMyTurn = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        if (statusText != null) statusText.text = isMyTurn ? "Your Turn!" : $"Waiting for {players[actorNumber].playerName}...";
        skipButton.gameObject.SetActive(isMyTurn);
        if (!isMyTurn) ResetCardSelection();
    }
    
    [PunRPC]
    private void RPC_ProcessPlayerAction(int actorNumber, int cardIndex, bool wasActivated, int newFinpoint)
    {
        if (cardIndex >= cardObjects.Count || !players.ContainsKey(actorNumber)) return;
        GameObject cardObj = cardObjects[cardIndex];
        PlayerProfileMultiplayer p = players[actorNumber];
        CardMultiplayer card = new CardMultiplayer(
            cardObj.transform.Find("CardText").GetComponent<Text>().text, "", 1,
            cardObj.transform.Find("CardColor").GetComponent<Text>().text);
        p.finpoint = newFinpoint;
        if (wasActivated) { Debug.Log($"{p.playerName} activated '{card.cardName}'."); }
        else { p.AddCard(card); Debug.Log($"{p.playerName} saved '{card.cardName}'."); }
        takenCardIndices.Add(cardIndex);
        var cg = cardObj.GetComponent<CanvasGroup>() ?? cardObj.AddComponent<CanvasGroup>();
        cg.alpha = 0.5f; cg.interactable = false;
        UpdatePlayerUI();
    }
    
    [PunRPC]
    private void RPC_ShowHelpCardChoice(int actorNumber, string cardName, string desc, HelpCardEffect effect)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            statusText.text = "Your turn to use a Help Card!";
            helpCardManager.ShowActivationChoice(new HelpCardMultiplayer(cardName, desc, effect), actorNumber);
        }
        else
        {
            statusText.text = $"Waiting for {players[actorNumber].playerName} to use a Help Card...";
        }
    }

    [PunRPC]
    private void RPC_ShowResetButton()
    {
        resetSemesterButton.gameObject.SetActive(true);
        resetSemesterButton.interactable = PhotonNetwork.IsMasterClient;
        statusText.text = "Semester Over.";
    }

    // Hanya ada SATU definisi untuk RPC ini.
    [PunRPC]
    private void RPC_ResetForNewSemester()
    {
        takenCardIndices.Clear();
        turnOrder.Clear();
        ticketButtons.Clear();
        skipCount = 0;
        currentTurnIndex = 0;
        foreach (var p in players.Values) { p.ticketNumber = 0; }
        foreach (Transform child in ticketListContainer) Destroy(child.gameObject);
        foreach (Transform child in cardHolderParent) Destroy(child.gameObject);
        resetSemesterButton.gameObject.SetActive(false);
        leaderboardPanel.SetActive(false);
        resolutionManager.ResetVisuals();
        rumorManager.HideAllCardObjects();
        UpdatePlayerUI();
    }
    
    [PunRPC]
    private void RPC_ShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        foreach (Transform child in leaderboardContainer) Destroy(child.gameObject);
        var rankedPlayers = players.Values.OrderByDescending(p => p.finpoint).ToList();
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            PlayerProfileMultiplayer p = rankedPlayers[i];
            GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
            Text[] texts = entry.GetComponentsInChildren<Text>();
            texts[0].text = $"{i + 1}. {p.playerName}";
            texts[1].text = $"{p.finpoint} FP";
        }
        statusText.text = "Game Over!";
    }
    
    [PunRPC]
    private void RPC_ShowRumorCard(string color, string cardName)
    {
        statusText.text = "A rumor is spreading...";
        rumorManager.ShowCardByColorAndName(color, cardName);
    }

    [PunRPC]
    private void RPC_HideRumorCards() { rumorManager.HideAllCardObjects(); }

    [PunRPC]
    private void RPC_RevealTokenAndUpdateDividend(string color, int tokenValue, int revealedCount, int newDividendIndex)
    {
        statusText.text = $"Resolving the {color} market...";
        resolutionManager.RevealToken(color, tokenValue, revealedCount);
        resolutionManager.UpdateDividendVisual(color, newDividendIndex);
    }
    
    [PunRPC]
    private void RPC_UpdateAllPlayerFinpoints(int[] actorNumbers, int[] newFinpoints)
    {
        for(int i = 0; i < actorNumbers.Length; i++)
        {
            if(players.ContainsKey(actorNumbers[i])) { players[actorNumbers[i]].finpoint = newFinpoints[i]; }
        }
        UpdatePlayerUI();
    }

    // CMDs (Dijalankan HANYA di MasterClient)
    //-------------------------------------------
    [PunRPC]
    private void Cmd_SelectTicket(int actorNumber, int ticketNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (players.Values.Any(p => p.ticketNumber == ticketNumber)) return;
        photonView.RPC(nameof(RPC_ClaimTicketAndUpdate), RpcTarget.AllBuffered, actorNumber, ticketNumber);
        if (players.Values.All(p => p.ticketNumber > 0) && players.Count == PhotonNetwork.PlayerList.Length)
        {
            GoToNextPhase();
        }
    }

    [PunRPC]
    private void Cmd_PlayerAction(int actorNumber, int cardIndex, bool isActivating)
    {
        if (!PhotonNetwork.IsMasterClient || turnOrder[currentTurnIndex] != actorNumber) return;
        PlayerProfileMultiplayer p = players[actorNumber];
        int cardValue = int.Parse(cardObjects[cardIndex].transform.Find("CardValue").GetComponent<Text>().text);
        if (p.finpoint < cardValue) return;
        p.finpoint -= cardValue;
        photonView.RPC(nameof(RPC_ProcessPlayerAction), RpcTarget.All, actorNumber, cardIndex, isActivating, p.finpoint);
        skipCount = 0;
        currentTurnIndex = (currentTurnIndex + 1) % players.Count;
        StartNextCardTurn_Master();
    }

    [PunRPC]
    private void Cmd_PlayerSkips(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || turnOrder[currentTurnIndex] != actorNumber) return;
        skipCount++;
        currentTurnIndex = (currentTurnIndex + 1) % players.Count;
        StartNextCardTurn_Master();
    }
    
    [PunRPC]
    private void Cmd_PlayerUsesHelpCard(int actorNumber, HelpCardEffect effect, bool didActivate)
    {
        if(!PhotonNetwork.IsMasterClient || turnOrder[helpCardTurnIndex] != actorNumber) return;
        
        if(didActivate)
        {
            Debug.Log($"{players[actorNumber].playerName} used Help Card: {effect}");
            // TODO: Terapkan logika efek kartu bantuan di sini
            // Misalnya: players[actorNumber].finpoint += 10;
            // Lalu sinkronkan hasilnya: photonView.RPC(nameof(RPC_UpdateAllPlayerFinpoints), ...);
        }
        
        // Hapus kartu setelah digunakan/dilewati
        players[actorNumber].helpCards.RemoveAt(0);
        
        // Lanjut ke giliran pemain berikutnya
        helpCardTurnIndex++;
        StartNextHelpCardTurn_Master();
    }
    
    [PunRPC]
    private void Cmd_SubmitSellOrder(int actorNumber, string[] colors, int[] counts)
    {
        if(!PhotonNetwork.IsMasterClient) return;

        // TODO: Simpan pilihan penjualan dari pemain ini
        // Misalnya: playerSellChoices[actorNumber] = ...;

        playersSubmittedSellOrder++;
        if(playersSubmittedSellOrder >= players.Count)
        {
            // Semua pemain sudah submit, proses semua penjualan
            // TODO: Loop semua playerSellChoices, hitung finpoint, dan update state
            // Lalu broadcast hasilnya via RPC_UpdateAllPlayerFinpoints
            
            GoToNextPhase();
        }
    }
    #endregion

    #region UTILS & UI
    public void UpdatePlayerUI()
    {
        if (playerListContainer == null)
        {
            Debug.LogError("Player List Container belum di-assign di Inspector!");
            return;
        }

        // 1. Bersihkan UI yang lama
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Urutkan pemain berdasarkan nomor tiket (jika sudah ada) atau nomor aktor
        var sortedPlayers = players.Values.OrderBy(p => p.ticketNumber > 0 ? p.ticketNumber : p.actorNumber).ToList();

        // 3. Loop setiap pemain dan buat UI-nya, LENGKAP dengan pengisian data
        foreach (var p in sortedPlayers)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);

            // Cari setiap komponen Teks berdasarkan nama GameObject-nya dan isi datanya.
            // Ini lebih aman daripada GetComponentsInChildren<Text>().

            // Menggunakan UnityEngine.UI.Text karena Anda menyebutkan menggunakan teks legacy.
            Text nameText = entry.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = p.playerName;
            }

            Text scoreText = entry.transform.Find("ScoreText")?.GetComponent<Text>();
            if (scoreText != null)
            {
                scoreText.text = $"Tiket {p.ticketNumber}";
            }

            Text cardText = entry.transform.Find("CardText")?.GetComponent<Text>();
            if (cardText != null)
            {
                cardText.text = $"{p.cardCount} kartu";
            }

            Text finpointText = entry.transform.Find("Finpoint")?.GetComponent<Text>();
            if (finpointText != null)
            {
                finpointText.text = $"FP {p.finpoint}";
            }

            // Ambil data jumlah kartu per warna dari profil pemain saat ini
            var colorCounts = p.GetCardColorCounts();

            Text redCardText = entry.transform.Find("RedCardText")?.GetComponent<Text>();
            if (redCardText != null)
            {
                redCardText.text = $"M: {(colorCounts.ContainsKey("Red") ? colorCounts["Red"] : 0)}";
            }

            Text blueCardText = entry.transform.Find("BlueCardText")?.GetComponent<Text>();
            if (blueCardText != null)
            {
                blueCardText.text = $"B: {(colorCounts.ContainsKey("Blue") ? colorCounts["Blue"] : 0)}";
            }

            Text greenCardText = entry.transform.Find("GreenCardText")?.GetComponent<Text>();
            if (greenCardText != null)
            {
                greenCardText.text = $"H: {(colorCounts.ContainsKey("Green") ? colorCounts["Green"] : 0)}";
            }

            Text orangeCardText = entry.transform.Find("OrangeCardText")?.GetComponent<Text>();
            if (orangeCardText != null)
            {
                orangeCardText.text = $"O: {(colorCounts.ContainsKey("Orange") ? colorCounts["Orange"] : 0)}";
            }
        }
    }

    private void ResetCardSelection()
    {
        if (currentlySelectedCard != null) { currentlySelectedCard.transform.localScale = Vector3.one; }
        currentlySelectedCard = null;
        if (ActiveSaveContainer == null) return;
        foreach (Transform child in ActiveSaveContainer) { Destroy(child.gameObject); }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (players.ContainsKey(otherPlayer.ActorNumber))
        {
            players.Remove(otherPlayer.ActorNumber);
            UpdatePlayerUI();
            if (PhotonNetwork.IsMasterClient && turnOrder.Count > 0 && turnOrder.Contains(otherPlayer.ActorNumber))
            {
                if (turnOrder[currentTurnIndex] == otherPlayer.ActorNumber)
                {
                   turnOrder.Remove(otherPlayer.ActorNumber);
                   currentTurnIndex %= turnOrder.Count;
                   StartNextCardTurn_Master();
                } else { turnOrder.Remove(otherPlayer.ActorNumber); }
            }
        }
    }
    #endregion
}