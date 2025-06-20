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

    [Header("Manager References")]
    public TicketManager ticketManager;
    [SerializeField] private SellingPhaseManager sellingManager;

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

    [Header("Button References")]
    public Button skipButton;
    public Button resetSemesterButton;

    private Dictionary<int, PlayerProfile> players = new Dictionary<int, PlayerProfile>();
    private List<int> turnOrder = new List<int>();
    private List<GameObject> cardObjects = new List<GameObject>();
    private HashSet<int> takenCardIndices = new HashSet<int>();
    private Dictionary<int, Button> ticketButtons = new Dictionary<int, Button>();
    
    private GameObject currentlySelectedCard = null;
    private int currentTurnIndex = 0;
    private int totalCardsInPlay = 0;
    private int skipCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            InitializePlayers();
            ShowTicketChoices();
        }
        skipButton.gameObject.SetActive(false);
        resetSemesterButton.gameObject.SetActive(false);
        leaderboardPanel.SetActive(false);
    }
    
    private void InitializePlayers()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            photonView.RPC(nameof(RPC_CreatePlayerProfile), RpcTarget.AllBuffered, p.NickName, p.ActorNumber);
        }
    }

    private void ShowTicketChoices()
    {
        int playerCount = PhotonNetwork.PlayerList.Length;
        List<int> availableTickets = new List<int>();
        for (int i = 1; i <= playerCount; i++) availableTickets.Add(i);
        TicketManager.ShuffleList(availableTickets);
        photonView.RPC(nameof(RPC_DisplayTicketChoices), RpcTarget.All, availableTickets.ToArray());
    }

    public void OnTicketButtonClicked(int ticketNumber, Button clickedButton)
    {
        foreach(var btn in ticketButtons.Values) btn.interactable = false;
        photonView.RPC(nameof(Cmd_SelectTicket), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, ticketNumber);
    }
    
    private void StartCardPhase()
    {
        StartCoroutine(StartCardPhaseAfterDelay(2.5f)); 
    }

    private IEnumerator StartCardPhaseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.IsMasterClient)
        {
            List<PlayerProfile> sortedPlayers = players.Values.OrderBy(p => p.ticketNumber).ToList();
            turnOrder = sortedPlayers.Select(p => p.actorNumber).ToList();
            
            totalCardsInPlay = 2 * players.Count;
            InitializeDeck(out string[] cardNames, out string[] cardColors, out int[] cardValues);
            photonView.RPC(nameof(RPC_DrawCardsAndSetTurnOrder), RpcTarget.All, cardNames, cardColors, cardValues, turnOrder.ToArray());
            
            currentTurnIndex = 0;
            StartNextTurn(); // Panggil fungsi baru yang lebih aman
        }
    }
    
    private void InitializeDeck(out string[] cardNames, out string[] cardColors, out int[] cardValues)
    {
        List<Card> deck = new List<Card>();
        List<string> colors = new List<string> { "Red", "Blue", "Green", "Orange" };
        deck.Add(new Card("Trade Offer", "", 4, colors[Random.Range(0, colors.Count)]));
        deck.Add(new Card("Heal", "", 2, colors[Random.Range(0, colors.Count)]));
        deck.Add(new Card("Stock Split", "", 3, colors[Random.Range(0, colors.Count)]));
        deck.Add(new Card("Steal", "", 5, colors[Random.Range(0, colors.Count)]));
        deck.Add(new Card("Flashbuy", "", 8, colors[Random.Range(0, colors.Count)]));
        
        while (deck.Count < totalCardsInPlay) deck.Add(deck[Random.Range(0, deck.Count)]);
        
        for (int i = 0; i < deck.Count; i++) { Card temp = deck[i]; int r = Random.Range(i, deck.Count); deck[i] = deck[r]; deck[r] = temp; }
        
        if (deck.Count > totalCardsInPlay)
        {
            deck = deck.GetRange(0, totalCardsInPlay);
        }
        
        cardNames = deck.Select(c => c.cardName).ToArray();
        cardColors = deck.Select(c => c.color).ToArray();
        cardValues = deck.Select(c => c.value).ToArray();
    }

    // --- LOGIKA GILIRAN YANG DIPERBAIKI ---
    
    // Fungsi baru yang tugasnya HANYA mengumumkan giliran
    private void StartNextTurn()
    {
        // Cek kondisi akhir fase dulu
        if (takenCardIndices.Count >= totalCardsInPlay || skipCount >= players.Count)
        {
            photonView.RPC(nameof(RPC_StartSellingPhase), RpcTarget.All);
            return;
        }
        
        // Ambil pemain saat ini dan umumkan gilirannya
        PlayerProfile currentPlayer = players[turnOrder[currentTurnIndex]];
        photonView.RPC(nameof(RPC_SetCurrentTurn), RpcTarget.All, currentPlayer.actorNumber);
    }
    
    // IEnumerator NextTurn() yang lama telah dihapus karena logikanya salah.

    public void OnCardClicked(GameObject cardObj, int cardIndex)
    {
        if (turnOrder.Count == 0 || turnOrder[currentTurnIndex] != PhotonNetwork.LocalPlayer.ActorNumber) return;
        if (takenCardIndices.Contains(cardIndex)) return;
        
        if (currentlySelectedCard != null) ResetCardSelection();
        
        currentlySelectedCard = cardObj;
        cardObj.transform.localScale = Vector3.one * 1.1f;
        
        if (activateButtonPrefab != null)
        {
            GameObject activateBtn = Instantiate(activateButtonPrefab, ActiveSaveContainer);
            activateBtn.GetComponent<Button>().onClick.AddListener(() => OnPlayerAction(cardIndex, true));
        }
        if (saveButtonPrefab != null)
        {
            GameObject saveBtn = Instantiate(saveButtonPrefab, ActiveSaveContainer);
            saveBtn.GetComponent<Button>().onClick.AddListener(() => OnPlayerAction(cardIndex, false));
        }
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

    [PunRPC]
    private void RPC_CreatePlayerProfile(string nickName, int actorNumber)
    {
        if (!players.ContainsKey(actorNumber))
        {
            players.Add(actorNumber, new PlayerProfile(nickName, actorNumber));
        }
        UpdatePlayerUI();
    }
    
    [PunRPC]
    private void RPC_DisplayTicketChoices(int[] ticketNumbers)
    {
        if (ticketListContainer == null) { Debug.LogError("FATAL ERROR: 'Ticket List Container' belum di-assign!", this); return; }
        ticketListContainer.gameObject.SetActive(true);
        if(statusText != null) statusText.text = "Choose a starting ticket...";
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
            string playerName = players[actorNumber].playerName;
            Button buttonToUpdate = ticketButtons[ticketNumber];
            buttonToUpdate.GetComponentInChildren<Text>().text = $"{ticketNumber} - {playerName}";
            buttonToUpdate.interactable = false;
            UpdatePlayerUI();
        }
    }

    [PunRPC]
    private void RPC_DrawCardsAndSetTurnOrder(string[] names, string[] colors, int[] values, int[] actorTurnOrder)
    {
        ticketListContainer.gameObject.SetActive(false);
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
    private void RPC_SetCurrentTurn(int actorNumber)
    {
        bool isMyTurn = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        if(statusText != null) statusText.text = isMyTurn ? "Your Turn!" : $"Waiting for {players[actorNumber].playerName}...";
        skipButton.gameObject.SetActive(isMyTurn);
    }

    [PunRPC]
    private void RPC_ProcessPlayerAction(int actorNumber, int cardIndex, bool wasActivated)
    {
        if (cardIndex >= cardObjects.Count || !players.ContainsKey(actorNumber)) return;
        GameObject cardObj = cardObjects[cardIndex];
        PlayerProfile p = players[actorNumber];
        int cardValue = int.Parse(cardObj.transform.Find("CardValue").GetComponent<Text>().text);
        if (p.finpoint < cardValue) return;

        p.finpoint -= cardValue;
        Card card = new Card(
            cardObj.transform.Find("CardText").GetComponent<Text>().text,
            "",
            cardValue,
            cardObj.transform.Find("CardColor").GetComponent<Text>().text
        );

        if (wasActivated)
        {
            CardEffectManager.ApplyEffect(card.cardName, p, card.color);
        }
        else
        {
            p.AddCard(card);
        }

        takenCardIndices.Add(cardIndex);
        cardObj.GetComponent<CanvasGroup>().alpha = 0.5f;
        cardObj.GetComponent<Button>().interactable = false;
        UpdatePlayerUI();
    }
    
    [PunRPC]
    private void RPC_StartSellingPhase()
    {
        if (sellingManager != null)
        {
            sellingManager.StartSellingPhase(players.Values.ToList(), 0, 1, resetSemesterButton.gameObject);
        }
    }

    [PunRPC]
    private void Cmd_SelectTicket(int actorNumber, int ticketNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        bool isTicketAlreadyTaken = players.Values.Any(p => p.ticketNumber == ticketNumber);
        if (isTicketAlreadyTaken) return;

        photonView.RPC(nameof(RPC_ClaimTicketAndUpdate), RpcTarget.AllBuffered, actorNumber, ticketNumber);

        bool allPlayersChose = players.Values.All(p => p.ticketNumber > 0);
        if (allPlayersChose)
        {
            StartCardPhase();
        }
    }
    
    // --- PERBAIKAN LOGIKA UTAMA ADA DI SINI ---

    [PunRPC]
    private void Cmd_PlayerAction(int actorNumber, int cardIndex, bool isActivating)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        // Validasi giliran: apakah pemain yang mengirim perintah ini memang yang sedang giliran?
        if (turnOrder.Count == 0 || turnOrder[currentTurnIndex] != actorNumber) return;

        // Proses aksi pemain saat ini
        photonView.RPC(nameof(RPC_ProcessPlayerAction), RpcTarget.All, actorNumber, cardIndex, isActivating);
        
        // Setelah aksi diproses, baru kita tentukan giliran selanjutnya
        PlayerProfile p = players[actorNumber];
        if (p.bonusActions > 0)
        {
            p.bonusActions--;
            Debug.Log($"Pemain {p.playerName} menggunakan bonus action, sisa {p.bonusActions}. Giliran tetap.");
        }
        else
        {
            skipCount = 0; // Reset skip count karena ada aksi
            currentTurnIndex = (currentTurnIndex + 1) % players.Count;
        }

        // Panggil fungsi untuk mengumumkan giliran berikutnya
        StartNextTurn();
    }

    [PunRPC]
    private void Cmd_PlayerSkips(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (turnOrder.Count == 0 || turnOrder[currentTurnIndex] != actorNumber) return;

        skipCount++;
        Debug.Log($"{players[actorNumber].playerName} has skipped.");
        
        // Langsung pindah ke giliran berikutnya
        currentTurnIndex = (currentTurnIndex + 1) % players.Count;
        StartNextTurn();
    }
    
    public void UpdatePlayerUI()
    {
        if (playerListContainer == null) return;
        foreach (Transform child in playerListContainer) Destroy(child.gameObject);
        var sortedPlayers = players.Values.OrderBy(p => p.ticketNumber > 0 ? p.ticketNumber : p.actorNumber).ToList();
        
        foreach (var p in sortedPlayers)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
            Text[] texts = entry.GetComponentsInChildren<Text>();

            var nameText = texts.FirstOrDefault(t => t.name == "NameText");
            if (nameText != null) nameText.text = p.playerName;
            
            var scoreText = texts.FirstOrDefault(t => t.name == "ScoreText");
            if (scoreText != null) scoreText.text = $"Tiket {p.ticketNumber}";

            var cardText = texts.FirstOrDefault(t => t.name == "CardText");
            if (cardText != null) cardText.text = $"{p.cardCount} kartu";

            var finpointText = texts.FirstOrDefault(t => t.name == "Finpoint");
            if (finpointText != null) finpointText.text = $"FP {p.finpoint}";
        }
    }

    private void ResetCardSelection()
    {
        if (currentlySelectedCard != null)
        {
            currentlySelectedCard.transform.localScale = Vector3.one;
        }
        currentlySelectedCard = null;
        if (ActiveSaveContainer == null) return;
        foreach(Transform child in ActiveSaveContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (players.ContainsKey(otherPlayer.ActorNumber))
        {
            players.Remove(otherPlayer.ActorNumber);
            UpdatePlayerUI();
            
            if (PhotonNetwork.IsMasterClient && players.Count > 0 && turnOrder.Count > 0)
            {
                if (currentTurnIndex < turnOrder.Count && turnOrder[currentTurnIndex] == otherPlayer.ActorNumber)
                {
                   currentTurnIndex %= players.Count;
                   StartNextTurn();
                }
            }
        }
    }
}
