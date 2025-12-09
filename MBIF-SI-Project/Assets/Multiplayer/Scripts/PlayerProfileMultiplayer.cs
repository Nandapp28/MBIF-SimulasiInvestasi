// File: PlayerProfileMultiplayer.cs

using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime; // Diperlukan untuk mengakses Player dan callback
using ExitGames.Client.Photon;// Diperlukan untuk Hashtable
using System.Collections;

using Hashtable = ExitGames.Client.Photon.Hashtable;
// Ganti warisan ke MonoBehaviourPunCallbacks untuk bisa menerima update properti
public class PlayerProfileMultiplayer : MonoBehaviourPunCallbacks
{
    [Header("UI References (Legacy Text)")]
    public Text nameText;
    public Text turnOrderText;  // Teks untuk urutan giliran, misal: ScoreText
    public Text investpointText;   // Teks untuk Investpoint
    public Text redCardText;    // Teks untuk jumlah kartu merah
    public Text orangeCardText; // Teks untuk jumlah kartu oranye
    public Text blueCardText;   // Teks untuk jumlah kartu biru
    public Text greenCardText;  // Teks untuk jumlah kartu hijau

    [Header("New Stats")]
    public Text totalShareValueText;

    [Header("Public UI")]
    public Image publicTimerBar;
    public GameObject tenderOfferTargetButton; 
    
    private Button _tenderButtonComponent;

    // Definisikan 'kunci' untuk Custom Properties agar tidak salah ketik
    public const string INVESTPOINT_KEY = "investpoint";
    public const string TURN_ORDER_KEY = "turn";
    public const string KONSUMER_CARDS_KEY = "konsumer_cards";
    public const string INFRASTRUKTUR_CARDS_KEY = "infrastruktur_cards";
    public const string KEUANGAN_CARDS_KEY = "keuangan_cards";
    public const string TAMBANG_CARDS_KEY = "tambang_cards";
    public const string TESTING_CARD_USED_KEY = "testing_card_used";
    public const string TESTING_CARD_INDEX_KEY = "testing_card_index";
    public const string IS_BOT_MODE_KEY = "isBotMode";

    public const string TURN_START_TIME_KEY = "turnStartTime";
    public const string TURN_ACTOR_KEY = "turnActor";
    public const float TURN_DURATION = 10.0f;
    public const string TURN_DURATION_KEY = "turnDuration";
    private Coroutine publicTimerCoroutine;

    
    void Awake()
    {
        
        if (publicTimerBar != null)
        {
            publicTimerBar.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        PlaceProfileInContainer();
        if (photonView.IsMine)
        {
            Hashtable initialProps = new Hashtable
            {
                { INVESTPOINT_KEY, 100 },
                { TURN_ORDER_KEY, 0 },
                { KONSUMER_CARDS_KEY, 0 },
                { INFRASTRUKTUR_CARDS_KEY, 0 },
                { KEUANGAN_CARDS_KEY, 0 },
                { TAMBANG_CARDS_KEY, 0 },
                { TESTING_CARD_USED_KEY, false },
                { TESTING_CARD_INDEX_KEY, -1 },

                { IS_BOT_MODE_KEY, false }// Indeks awal untuk Testing Card
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
        }
        if (tenderOfferTargetButton != null)
        {
            _tenderButtonComponent = tenderOfferTargetButton.GetComponent<Button>();
            tenderOfferTargetButton.SetActive(false); // Sembunyikan saat mulai
        }
    }
    private void PlaceProfileInContainer()
{
    // 1. Temukan MultiplayerManager
    MultiplayerManager manager = MultiplayerManager.Instance;
    if (manager == null)
    {
        // Tampilkan error dengan nama pemilik jika ada, jika tidak, tampilkan "Owner N/A"
        string ownerName = (photonView.Owner != null) ? photonView.Owner.NickName : "Owner N/A";
        Debug.LogError($"[PlayerProfile] Gagal menemukan MultiplayerManager.Instance untuk {ownerName}");
        return;
    }

    Transform targetContainer;

    // --- PERBAIKAN LOGIKA INTI ---
    // Jangan gunakan "IsMine". Cek apakah "Owner" dari prefab ini
    // adalah "LocalPlayer" (pemain yang menjalankan game di komputer ini).
    if (photonView.Owner != null && photonView.Owner == PhotonNetwork.LocalPlayer)
    {
        targetContainer = manager.localPlayerContainer;
    }
    else
    {
        targetContainer = manager.onlinePlayerContainer;
    }
    // --- AKHIR PERBAIKAN ---

    // 3. Pindahkan prefab ini (transform) ke dalam kontainer tersebut
    if (targetContainer != null)
    {
        transform.SetParent(targetContainer, false);
        transform.localScale = Vector3.one; // Pastikan skala tidak berubah
        gameObject.SetActive(true); // Pastikan objeknya aktif
        
        // Tambahkan pengecekan null untuk NickName saat logging
        string ownerName = (photonView.Owner != null) ? photonView.Owner.NickName : "Disconnecting Player";
        Debug.Log($"[PlayerProfile] {ownerName} telah ditempatkan di kontainer UI.");
    }
    else
    {
        string ownerName = (photonView.Owner != null) ? photonView.Owner.NickName : "Owner N/A";
        Debug.LogWarning($"[PlayerProfile] Gagal menemukan targetContainer untuk {ownerName}");
    }
}

    #region Photon Callbacks

    // Fungsi ini otomatis dipanggil saat pertama kali terhubung dan setiap kali ada update
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer != null && targetPlayer == photonView.Owner)
        {
            UpdateAllUI(targetPlayer);
        }
    }
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Cek apakah properti timer giliran berubah
        if (propertiesThatChanged.ContainsKey(TURN_ACTOR_KEY))
        {
            // Hentikan timer lama jika ada
            if (publicTimerCoroutine != null)
            {
                StopCoroutine(publicTimerCoroutine);
                publicTimerCoroutine = null;
            }

            // Ambil data giliran baru
            int turnActorNumber = (int)propertiesThatChanged[TURN_ACTOR_KEY];
            
            // Cek apakah giliran ini MILIK profil ini?
            if (photonView.Owner != null && photonView.Owner.ActorNumber == turnActorNumber)
            {
                // Ya, ini giliran pemain ini. Mulai timer.
                // Ambil waktu mulai dari properti
                float duration = propertiesThatChanged.ContainsKey(TURN_DURATION_KEY) 
                    ? (float)propertiesThatChanged[TURN_DURATION_KEY] 
                    : TURN_DURATION;

                if (propertiesThatChanged.ContainsKey(TURN_START_TIME_KEY))
                {
                    double startTime = (double)propertiesThatChanged[TURN_START_TIME_KEY];
                    // Mulai timer dengan durasi yang benar
                    publicTimerCoroutine = StartCoroutine(AnimatePublicTimer(startTime, duration));
                }
            }
            else if (photonView.Owner == null || photonView.Owner.ActorNumber != turnActorNumber || turnActorNumber < 1)
            {
                if (publicTimerBar != null)
                {
                    publicTimerBar.gameObject.SetActive(false);
                }
            }
        }
        bool priceChanged = false;
        foreach (object key in propertiesThatChanged.Keys)
        {
            string keyStr = key.ToString();
            // Cek prefix key yang digunakan di SellingPhaseManager
            if (keyStr.StartsWith("ipo_index_") || keyStr.StartsWith("ipo_bonus_"))
            {
                priceChanged = true;
                break;
            }
        }

        if (priceChanged && photonView.Owner != null)
        {
            // Panggil update khusus untuk menghitung ulang nilai saham
            UpdateTotalShareValue(photonView.Owner);
        }
    }

    // --- BARU --- Coroutine untuk menganimasikan timer publik
    private IEnumerator AnimatePublicTimer(double startTime, float duration)
    {
        if (publicTimerBar == null) yield break;

        publicTimerBar.gameObject.SetActive(true);
        double elapsed = 0;

        while (elapsed < duration)
        {
            elapsed = PhotonNetwork.Time - startTime;
            // Gunakan 'duration' yang diterima
            float fillAmount = 1.0f - (float)(elapsed / duration);
            publicTimerBar.fillAmount = Mathf.Clamp01(fillAmount);

            yield return null;
        }

        publicTimerBar.gameObject.SetActive(false);
    }
    
    public void SetupTenderOfferButton(bool isTarget)
    {
        if (tenderOfferTargetButton == null || _tenderButtonComponent == null) return;

        tenderOfferTargetButton.SetActive(isTarget);

        // Hapus listener lama untuk menghindari panggilan ganda
        _tenderButtonComponent.onClick.RemoveAllListeners();

        if (isTarget)
        {
            // Jika diaktifkan, tambahkan listener baru
            _tenderButtonComponent.onClick.AddListener(OnTenderTargetClicked);
        }
    }

    /// <summary>
    /// Saat tombol target di-klik, tombol ini akan memberi tahu ActionPhaseManager
    /// dan mengirimkan "Owner" (Pemain) dari profil ini sebagai target.
    /// </summary>
    private void OnTenderTargetClicked()
    {
        if (ActionPhaseManager.Instance != null && photonView.Owner != null)
        {
            // Panggil fungsi publik di ActionPhaseManager (yang akan kita buat di Langkah 2)
            ActionPhaseManager.Instance.OnTenderOfferTargetClicked(photonView.Owner);
        }
    }
    #endregion

    #region UI Update

    // Saat Start, langsung coba update UI dengan data yang ada
    public override void OnEnable()
    {
        base.OnEnable();
        UpdateAllUI(photonView.Owner);
    }
    
    // Fungsi untuk memperbarui semua teks di UI
    private void UpdateAllUI(Player player)
    {
        if (player == null) return;

        // Update Nama
        if (nameText != null) nameText.text = player.NickName;

        // Update Investpoint
        if (investpointText != null)
        {
            object investpointValue;
            if (player.CustomProperties.TryGetValue(INVESTPOINT_KEY, out investpointValue))
                investpointText.text = investpointValue.ToString();
            else
                investpointText.text = "100"; // Nilai default
        }

        // Update Urutan Giliran
        if (turnOrderText != null)
        {
            object turnOrderValue;
            if (player.CustomProperties.TryGetValue(TURN_ORDER_KEY, out turnOrderValue))
                turnOrderText.text = "Turn " + turnOrderValue.ToString();
            else
                turnOrderText.text = "Turn 0"; // Nilai default
        }

        // --- BAGIAN YANG DIPERBAIKI ---
        // Update Jumlah Kartu berdasarkan Warna
        object cardCount;

        // Konsumer (Merah)
        if (redCardText != null)
        {
            if (player.CustomProperties.TryGetValue(KONSUMER_CARDS_KEY, out cardCount))
                redCardText.text = cardCount.ToString();
            else
                redCardText.text = "0";
        }

        // Infrastruktur (Oranye)
        if (orangeCardText != null)
        {
            if (player.CustomProperties.TryGetValue(INFRASTRUKTUR_CARDS_KEY, out cardCount))
                orangeCardText.text = cardCount.ToString();
            else
                orangeCardText.text = "0";
        }

        // Keuangan (Biru)
        if (blueCardText != null)
        {
            if (player.CustomProperties.TryGetValue(KEUANGAN_CARDS_KEY, out cardCount))
                blueCardText.text = cardCount.ToString();
            else
                blueCardText.text = "0";
        }

        // Tambang (Hijau)
        if (greenCardText != null)
        {
            if (player.CustomProperties.TryGetValue(TAMBANG_CARDS_KEY, out cardCount))
                greenCardText.text = cardCount.ToString();
            else
                greenCardText.text = "0";
        }
        UpdateCardCountUI(player, KONSUMER_CARDS_KEY, redCardText);
        UpdateCardCountUI(player, INFRASTRUKTUR_CARDS_KEY, orangeCardText);
        UpdateCardCountUI(player, KEUANGAN_CARDS_KEY, blueCardText);
        UpdateCardCountUI(player, TAMBANG_CARDS_KEY, greenCardText);
        UpdateTotalShareValue(player);
    }
    private void UpdateCardCountUI(Player player, string key, Text uiText)
    {
        if (uiText != null)
        {
            if (player.CustomProperties.TryGetValue(key, out object count))
                uiText.text = count.ToString();
            else
                uiText.text = "0";
        }
    }

    // [BARU] Fungsi Logika Menghitung Total Aset
    private void UpdateTotalShareValue(Player player)
    {
        if (totalShareValueText == null) return;

        // Cek apakah SellingManager ada untuk mengambil harga. 
        // Jika tidak ada (misal belum load), set 0 atau strip.
        if (SellingPhaseManagerMultiplayer.Instance == null)
        {
            // Debug.LogWarning("SellingPhaseManager belum siap, tidak bisa menghitung nilai saham.");
            totalShareValueText.text = "0"; 
            return;
        }

        int totalValue = 0;
        string[] colors = { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };

        foreach (string color in colors)
        {
            // 1. Ambil jumlah kartu yang dimiliki
            string cardKey = GetCardKeyFromColor(color);
            int cardCount = 0;
            if (player.CustomProperties.TryGetValue(cardKey, out object countObj))
            {
                cardCount = (int)countObj;
            }

            // 2. Ambil harga pasar saat ini dari SellingPhaseManager
            int currentPrice = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(color);

            // 3. Kalikan dan tambahkan ke total
            totalValue += (cardCount * currentPrice);
        }

        // Tampilkan ke UI
        totalShareValueText.text = totalValue.ToString();
    }
    
    public static string GetCardKeyFromColor(string color)
    {
        switch(color)
        {
            case "Konsumer": return KONSUMER_CARDS_KEY;
            case "Infrastruktur": return INFRASTRUKTUR_CARDS_KEY;
            case "Keuangan": return KEUANGAN_CARDS_KEY;
            case "Tambang": return TAMBANG_CARDS_KEY;
            default: return "";
        }
    }
    #endregion
}