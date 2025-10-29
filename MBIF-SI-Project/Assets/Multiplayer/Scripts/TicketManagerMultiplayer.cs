// File: TicketManagerMultiplayer.cs (Dengan Perbaikan Race Condition)

using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;

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

    private Dictionary<int, TicketButtonMultiplayer> localTicketButtons = new Dictionary<int, TicketButtonMultiplayer>();
    private List<int> availableTickets;
    private PhotonView gameStatusView;

    void Start()
    {
        if (GameStatusUI.Instance != null)
        {
            gameStatusView = GameStatusUI.Instance.photonView;
        }
    }

    public void InitializeBidding()
    {
        if (gameStatusView != null)
        {
            gameStatusView.RPC("UpdateStatusText", RpcTarget.All, "Fase Bidding: Pilih Kartu Urutan Anda!");
        }

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        availableTickets = Enumerable.Range(1, playerCount).ToList();

        System.Random rnd = new System.Random();
        int[] ticketNumbers = availableTickets.OrderBy(x => rnd.Next()).ToArray();

        photonView.RPC("StartBiddingPhase", RpcTarget.All, (object)ticketNumbers);
    }

    [PunRPC]
    void StartBiddingPhase(int[] ticketNumbers)
    {
        biddingPanel.SetActive(true);
        localTicketButtons.Clear();

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

    public void OnTicketButtonClicked(int ticketNumber)
    {
        Debug.Log($"Anda mengklik tiket #{ticketNumber}. Mengunci pilihan dan mengirim permintaan...");
        foreach (var ticketButton in localTicketButtons.Values)
        {
            if (ticketButton != null)
            {
                ticketButton.button.interactable = false;
            }
        }
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
            // --- JALUR SUKSES (Tidak Berubah) ---
            availableTickets.Remove(ticketNumber);
            Debug.Log($"MasterClient menyetujui tiket #{ticketNumber} untuk {requestingPlayer.NickName}.");
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
        else
        {
            // --- JALUR GAGAL (BARU) ---
            // Tiket sudah diambil orang lain. Beri tahu pemain yang meminta untuk mencoba lagi.
            Debug.LogWarning($"Tiket #{ticketNumber} sudah diambil. Mengirim pesan gagal ke {requestingPlayer.NickName}.");
            photonView.RPC("Rpc_TicketRequestFailed", requestingPlayer);
        }
    }

    // --- FUNGSI RPC BARU UNTUK MENANGANI KEGAGALAN ---
    [PunRPC]
    void Rpc_TicketRequestFailed()
    {
        Debug.Log("Permintaan tiket Anda gagal (sudah diambil orang lain). Silakan pilih lagi.");
        
        // Aktifkan kembali HANYA tombol yang tiketnya belum diambil.
        // Kita bisa mengetahuinya karena tombol yang sudah diambil akan `interactable = false` dari RPC RevealTicketVisuals.
        foreach (var ticketButton in localTicketButtons.Values)
        {
            // Jika sebuah tombol masih bisa diinteraksi sebelumnya (artinya belum ada yg ambil),
            // biarkan tetap bisa diinteraksi. Jika sudah nonaktif (karena sudah diambil), biarkan nonaktif.
            // Pengecekan sprite juga bisa jadi indikator. Tombol yang sudah diambil spritenya akan berubah.
            if (ticketButton.buttonImage.sprite == defaultTicketSprite)
            {
                ticketButton.button.interactable = true;
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
        biddingPanel.SetActive(false);
        foreach (var button in localTicketButtons.Values)
        {
            if(button != null) Destroy(button.gameObject);
        }
        localTicketButtons.Clear();
    }
}