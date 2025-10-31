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

    [Header("Public UI")]
    public Image publicTimerBar;

    // Definisikan 'kunci' untuk Custom Properties agar tidak salah ketik
    public const string INVESTPOINT_KEY = "investpoint";
    public const string TURN_ORDER_KEY = "turn";
    public const string KONSUMER_CARDS_KEY = "konsumer_cards";
    public const string INFRASTRUKTUR_CARDS_KEY = "infrastruktur_cards";
    public const string KEUANGAN_CARDS_KEY = "keuangan_cards";
    public const string TAMBANG_CARDS_KEY = "tambang_cards";
    public const string TESTING_CARD_USED_KEY = "testing_card_used";
    public const string TESTING_CARD_INDEX_KEY = "testing_card_index";

    public const string TURN_START_TIME_KEY = "turnStartTime";
    public const string TURN_ACTOR_KEY = "turnActor";
    public const float TURN_DURATION = 10.0f;
    public const string TURN_DURATION_KEY = "turnDuration";
    private Coroutine publicTimerCoroutine;

    private MultiplayerManager multiplayerManager;
    void Awake()
    {
        multiplayerManager = MultiplayerManager.Instance;
        if (publicTimerBar != null)
        {
            publicTimerBar.gameObject.SetActive(false);
        }
    }

    void Start()
    {
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
                { TESTING_CARD_INDEX_KEY, -1 } // Indeks awal untuk Testing Card
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
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