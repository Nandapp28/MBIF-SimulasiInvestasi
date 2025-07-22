// File: TicketManagerMultiplayer.cs (Versi dengan Debugging)

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
    private PhotonView gameStatusView; // Referensi ke PhotonView di GameStatusUI

    void Start()
    {
        Debug.Log("✅ TicketManagerMultiplayer.cs - Start() berhasil dijalankan.");
        // Cari PhotonView dari GameStatusUI saat start
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

        Debug.Log($"✅ MasterClient menyiapkan {ticketNumbers.Length} tiket. Mengirim RPC StartBiddingPhase...");
        photonView.RPC("StartBiddingPhase", RpcTarget.All, (object)ticketNumbers);
    }

    [PunRPC]
    void StartBiddingPhase(int[] ticketNumbers)
    {
        Debug.Log($"✅ RPC StartBiddingPhase diterima. Membuat {ticketNumbers.Length} tombol tiket.");
        biddingPanel.SetActive(true);
        localTicketButtons.Clear();

        for (int i = 0; i < ticketNumbers.Length; i++)
        {
            if (i < ticketButtonPositions.Count)
            {
                // Baris ini sudah benar, membuat tombol sebagai anak dari penanda posisi
                GameObject buttonObj = Instantiate(ticketButtonPrefab, ticketButtonPositions[i]);
                
                // --- TAMBAHKAN DUA BARIS INI ---
                // 1. Atur posisi lokalnya ke tengah induk (0,0,0)
                buttonObj.transform.localPosition = Vector3.zero; 
                // 2. Atur skalanya menjadi normal (1,1,1)
                buttonObj.transform.localScale = Vector3.one;
                // --- AKHIR PENAMBAHAN ---

                TicketButtonMultiplayer ticketButton = buttonObj.GetComponent<TicketButtonMultiplayer>();
                
                ticketButton.Setup(ticketNumbers[i], this, defaultTicketSprite); 
                localTicketButtons.Add(ticketNumbers[i], ticketButton);
            }
        }
    }

    public void OnTicketButtonClicked(int ticketNumber)
    {
        Debug.Log($"Anda mengklik tiket #{ticketNumber}. Mengunci pilihan dan mengirim permintaan...");

        // --- TAMBAHAN LOGIKA DI SINI ---
        // Nonaktifkan SEMUA tombol tiket secara lokal untuk mencegah klik ganda.
        foreach (var ticketButton in localTicketButtons.Values)
        {
            if (ticketButton != null)
            {
                ticketButton.button.interactable = false;
            }
        }
        // --- AKHIR PENAMBAHAN ---

        // Kirim permintaan ke MasterClient seperti biasa.
        photonView.RPC("RequestTicket", RpcTarget.MasterClient, ticketNumber, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    void RequestTicket(int ticketNumber, Player requestingPlayer)
    {
        if (!PhotonNetwork.IsMasterClient || !availableTickets.Contains(ticketNumber)) return;

        availableTickets.Remove(ticketNumber);
        
        Debug.Log($"MasterClient menyetujui tiket #{ticketNumber} untuk {requestingPlayer.NickName}. Mengirim RPC RevealTicketVisuals...");
        photonView.RPC("RevealTicketVisuals", RpcTarget.All, ticketNumber);

        Hashtable turnProp = new Hashtable { { PlayerProfileMultiplayer.TURN_ORDER_KEY, ticketNumber } };
        requestingPlayer.SetCustomProperties(turnProp);

        if (availableTickets.Count == 0)
        {
            // Panggil RPC untuk update teks status
            if (gameStatusView != null)
            {
                gameStatusView.RPC("UpdateStatusText", RpcTarget.All, "Bidding Selesai! Menunggu penempatan pemain...");
            }

            Debug.Log("✅ Semua tiket telah diambil. Mengakhiri fase bidding...");
            Invoke(nameof(EndBiddingRPC), 2f);
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
            Destroy(button.gameObject);
        }
        localTicketButtons.Clear();
    }
}