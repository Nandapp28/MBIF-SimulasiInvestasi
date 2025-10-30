// File: TicketManagerMultiplayer.cs (Dengan Perbaikan Race Condition)

using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using System.Collections;
using TMPro;

using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class TicketManagerMultiplayer : MonoBehaviourPunCallbacks
{
    [Header("Bidding Setup")]
    public GameObject biddingPanel;
    public GameObject ticketButtonPrefab;
    public List<Transform> ticketButtonPositions;

    [Header("Ticket Sprites")]
    public Sprite defaultTicketSprite;
    public List<Sprite> numberTicketSprites;

    [Header("Timer UI (Opsional)")]
    public GameObject timerPanel;
    public Image timerBar; // Opsional: Tarik UI Image (Tipe: Filled) ke sini
    public TextMeshProUGUI timerText;
    public const float BIDDING_TIME = 20.0f; // Waktu dalam detik
    private Coroutine biddingTimerCoroutine;
    private bool hasChosenTicket = false;

    private Dictionary<int, TicketButtonMultiplayer> localTicketButtons = new Dictionary<int, TicketButtonMultiplayer>();
    private List<int> availableTickets;
    private PhotonView gameStatusView;

    void Start()
    {
        if (GameStatusUI.Instance != null)
        {
            gameStatusView = GameStatusUI.Instance.photonView;
        }
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
        }
    }

    public void InitializeBidding()
    {
        if (gameStatusView != null)
        {
            gameStatusView.RPC("UpdateStatusText", RpcTarget.All, "Fase Bidding: Pilih Kartu Urutan Anda!");
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient me-reset TURN_ORDER_KEY semua pemain dan menyiapkan tiket...");

            // 3. (PERBAIKAN) Reset TURN_ORDER_KEY semua pemain ke 0
            Hashtable resetProp = new Hashtable { { PlayerProfileMultiplayer.TURN_ORDER_KEY, 0 } };
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                p.SetCustomProperties(resetProp);
            }

            // 4. Siapkan daftar tiket baru
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            availableTickets = Enumerable.Range(1, playerCount).ToList();

            // 5. Acak tiket dan kirim ke semua pemain
            System.Random rnd = new System.Random();
            int[] ticketNumbers = availableTickets.OrderBy(x => rnd.Next()).ToArray();
            
            photonView.RPC("StartBiddingPhase", RpcTarget.All, (object)ticketNumbers);
        }
    }

    [PunRPC]
    void StartBiddingPhase(int[] ticketNumbers)
    {
        biddingPanel.SetActive(true);
        localTicketButtons.Clear();
        if (timerPanel != null) timerPanel.SetActive(true);
        hasChosenTicket = false;
        if (timerBar != null) timerBar.fillAmount = 1;
        if (timerText != null) timerText.text = ((int)BIDDING_TIME).ToString();

        // Hentikan coroutine lama jika ada dan mulai yang baru
        if (biddingTimerCoroutine != null) StopCoroutine(biddingTimerCoroutine);
        biddingTimerCoroutine = StartCoroutine(BiddingTimer());

        for (int i = 0; i < ticketNumbers.Length; i++)
        {
            if (i < ticketButtonPositions.Count)
            {
                GameObject buttonObj = Instantiate(ticketButtonPrefab, ticketButtonPositions[i]);
                buttonObj.transform.localPosition = Vector3.zero;
                buttonObj.transform.localScale = Vector3.one;

                TicketButtonMultiplayer ticketButton = buttonObj.GetComponent<TicketButtonMultiplayer>();
                ticketButton.Setup(ticketNumbers[i], this, defaultTicketSprite);
                localTicketButtons.Add(ticketNumbers[i], ticketButton);
            }
        }
    }
    private IEnumerator BiddingTimer()
    {
        float timeLeft = BIDDING_TIME;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            // --- BARU --- Update Teks Timer
            // Gunakan CeilToInt agar hitungan mundurnya pas (20, 19, 18...)
            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(timeLeft).ToString();
            }

            // --- LAMA --- Update Bar Timer
            if (timerBar != null)
            {
                timerBar.fillAmount = timeLeft / BIDDING_TIME;
            }
            yield return null;
        }

        // Waktu habis
        // --- BARU --- Pastikan teks menampilkan 0
        if (timerText != null)
        {
            timerText.text = "0";
        }
        
        Debug.Log("Waktu bidding habis! Meminta tiket acak...");
        DisableAllLocalButtons(); // Nonaktifkan tombol agar tidak bisa diklik manual
        photonView.RPC("RequestRandomTicket", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }
    private void DisableAllLocalButtons()
    {
        foreach (var ticketButton in localTicketButtons.Values)
        {
            if (ticketButton != null)
            {
                ticketButton.button.interactable = false;
            }
        }
    }

    public void OnTicketButtonClicked(int ticketNumber)
    {
        Debug.Log($"Anda mengklik tiket #{ticketNumber}. Mengunci pilihan dan mengirim permintaan...");
        DisableAllLocalButtons(); // --- MODIFIKASI --- Nonaktifkan semua tombol saat memilih
        
        // --- BARU --- Hentikan timer karena pemain sudah memilih
        if (biddingTimerCoroutine != null)
        {
            StopCoroutine(biddingTimerCoroutine);
            biddingTimerCoroutine = null;
        }
        //
        photonView.RPC("RequestTicket", RpcTarget.MasterClient, ticketNumber, PhotonNetwork.LocalPlayer);
    }

    // --- FUNGSI INI DIMODIFIKASI ---
    [PunRPC]
    void RequestTicket(int ticketNumber, Player requestingPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Cek apakah tiket masih tersedia
        if (availableTickets.Contains(ticketNumber))
        {
            // --- PERBAIKAN ---
            // Langsung panggil helper. JANGAN tambahkan logika
            // SetCustomProperties atau availableTickets.Remove() di sini.
            AssignTicketToPlayer(ticketNumber, requestingPlayer, false);
        }
        else
        {
            // --- JALUR GAGAL (INI SUDAH BENAR) ---
            // Tiket sudah diambil orang lain. Beri tahu pemain yang meminta untuk mencoba lagi.
            Debug.LogWarning($"Tiket #{ticketNumber} sudah diambil. Mengirim pesan gagal ke {requestingPlayer.NickName}.");
            photonView.RPC("Rpc_TicketRequestFailed", requestingPlayer);
        }
    }
    [PunRPC]
    void RequestRandomTicket(Player requestingPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Cek apakah pemain ini SUDAH punya tiket (mungkin dari klik manual yg nyaris bersamaan)
        if (requestingPlayer.CustomProperties.ContainsKey(PlayerProfileMultiplayer.TURN_ORDER_KEY) && (int)requestingPlayer.CustomProperties[PlayerProfileMultiplayer.TURN_ORDER_KEY] > 0)
        {
            Debug.Log("Pemain sudah punya tiket");
            return; // Pemain sudah punya tiket, abaikan permintaan acak
        }

        // Jika tiket masih tersedia, berikan satu secara acak
        if (availableTickets.Count > 0)
        {
            int randomIndex = Random.Range(0, availableTickets.Count);
            int randomTicket = availableTickets[randomIndex];
            
            // Panggil helper baru untuk menetapkan tiket
            AssignTicketToPlayer(randomTicket, requestingPlayer, true);
        }
    }

    // --- BARU --- Fungsi helper internal di MasterClient untuk menghindari duplikasi kode
    private void AssignTicketToPlayer(int ticketNumber, Player requestingPlayer, bool isRandom)
    {
        // Fungsi ini hanya boleh jalan di MasterClient
        if (!PhotonNetwork.IsMasterClient) return;

        // Cek ganda (penting jika isRandom=false, tapi aman untuk isRandom=true)
        if (!availableTickets.Contains(ticketNumber)) return;

        availableTickets.Remove(ticketNumber);
        
        string logMsg = isRandom ? "MENETAPKAN TIKET ACAK" : "menyetujui tiket";
        Debug.Log($"MasterClient {logMsg} #{ticketNumber} untuk {requestingPlayer.NickName}.");

        photonView.RPC("RevealTicketVisuals", RpcTarget.All, ticketNumber);
        Hashtable turnProp = new Hashtable { { PlayerProfileMultiplayer.TURN_ORDER_KEY, ticketNumber } };
        requestingPlayer.SetCustomProperties(turnProp);

        if (availableTickets.Count == 0)
        {
            if (gameStatusView != null)
            {
                gameStatusView.RPC("UpdateStatusText", RpcTarget.All, "Bidding Selesai! Menunggu penempatan pemain...");
            }
            Debug.Log("✅ Semua tiket telah diambil. Mengakhiri fase bidding...");
            Invoke(nameof(EndBiddingRPC), 2f);
        }
    }

    // --- FUNGSI RPC BARU UNTUK MENANGANI KEGAGALAN ---
    [PunRPC]
    void Rpc_TicketRequestFailed()
    {
        Debug.Log("Permintaan tiket Anda gagal (sudah diambil orang lain). Silakan pilih lagi.");

        // Aktifkan kembali HANYA tombol yang tiketnya belum diambil.
        foreach (var ticketButton in localTicketButtons.Values)
        {
            if (ticketButton.buttonImage.sprite == defaultTicketSprite)
            {
                ticketButton.button.interactable = true;
            }
        }

        // --- BARU --- Mulai lagi timer-nya!
        if (biddingTimerCoroutine != null) StopCoroutine(biddingTimerCoroutine);
        biddingTimerCoroutine = StartCoroutine(BiddingTimer());
    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Cek apakah update ini adalah untuk SAYA dan apakah properti TURN_ORDER_KEY berubah
        if (targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey(PlayerProfileMultiplayer.TURN_ORDER_KEY))
        {
            int turnOrder = (int)changedProps[PlayerProfileMultiplayer.TURN_ORDER_KEY];
            
            // Jika turn order > 0 (artinya tiket sudah diset) dan kita belum menandainya
            if (turnOrder > 0 && !hasChosenTicket)
            {
                hasChosenTicket = true;
                Debug.Log($"Tiket #{turnOrder} dikonfirmasi! Menghentikan timer bidding.");

                if (biddingTimerCoroutine != null)
                {
                    StopCoroutine(biddingTimerCoroutine);
                    biddingTimerCoroutine = null;
                }
                
                if (timerPanel != null) timerPanel.SetActive(false);
                
                // Nonaktifkan semua tombol (jika belum) setelah pilihan dikonfirmasi
                DisableAllLocalButtons();
            }
        }
    }

    void EndBiddingRPC()
    {
        photonView.RPC("EndBiddingPhase", RpcTarget.All);
        MultiplayerManager.Instance.StartPlayerPlacementPhase();
    }

    [PunRPC]
    void RevealTicketVisuals(int ticketNumber)
    {
        if (localTicketButtons.ContainsKey(ticketNumber))
        {
            if (ticketNumber - 1 < numberTicketSprites.Count)
            {
                Sprite revealedSprite = numberTicketSprites[ticketNumber - 1];
                localTicketButtons[ticketNumber].RevealTicket(revealedSprite);
            }
        }
    }

    [PunRPC]
    void EndBiddingPhase()
    {
        if (biddingTimerCoroutine != null)
        {
            StopCoroutine(biddingTimerCoroutine);
            biddingTimerCoroutine = null;
        }
        hasChosenTicket = true;
        if (timerPanel != null) timerPanel.SetActive(false);

        biddingPanel.SetActive(false);
        foreach (var button in localTicketButtons.Values)
        {
            if(button != null) Destroy(button.gameObject);
        }
        localTicketButtons.Clear();
    }
}