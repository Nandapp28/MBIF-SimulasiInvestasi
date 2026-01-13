// File: Scripts/TutorialManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Status")]
    public int CurrentSemester = 1;

    [Header("--- KONFIGURASI TURN ORDER ---")]
    public int playerTicketSem1 = 1; // Player tiket 1 di Sem 1
    public int playerTicketSem2 = 5; // Player tiket 5 di Sem 2
    // Urutan tiket bot (misal: 2, 3, 4, 5) untuk 4 bot
    public List<int> botTicketsSem1; 
    public List<int> botTicketsSem2;
    private Queue<int> _botTicketQueue; 

    [Header("KONFIGURASI DECKvURUTAN KARTU")]
    // Tentukan kartu apa saja yang muncul di deck secara berurutan
    public List<Card> fixedDeckSem1;
    public List<Card> fixedDeckSem2;


    [System.Serializable]
    public struct BotActionScript
    {
        public string botName; // Contoh: "Bot 1", "Bot 2", dst.
        public int semester;
        public int cardIndexToTake; // Index kartu di meja (0-4). Isi -1 untuk SKIP.
        public bool shouldActivate;
    }
     [Header("KONFIGURASI AKSI BOT AMBIL KARTU")]
    public List<BotActionScript> botActions;

    
    [System.Serializable]
    public struct BotSellingScript
    {
        public string botName;
        public int semester;
        public int sellKonsumer;
        public int sellInfrastruktur;
        public int sellKeuangan;
        public int sellTambang;
    }
    [Header("KONFIGURASI JUAL BOT SELLING PHASE")]
    public List<BotSellingScript> botSellingActions;

    [Header("KONFIGURASI RUMOR")]
    public List<RumorPhaseManager.RumorEffect> fixedRumorsSem1;
    public List<RumorPhaseManager.RumorEffect> fixedRumorsSem2;
    
    // Urutan token (-2, -1, 1, 2) untuk 4 warna (Total 16 angka per semester)
    [System.Serializable]
    public struct TokenScenario
    {
        public string color; // Contoh: "Konsumer"
        
        [Tooltip("Isi dengan index 0-3. (0=-2, 1=-1, 2=1, 3=2)")]
        public List<int> tokenIndices; 
    }
    [Header("--- KONFIGURASI TOKEN RAMALAN ---")]
    public List<TokenScenario> fixedTokensSem1; 
    public List<TokenScenario> fixedTokensSem2;

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Agar data bertahan antar scene jika perlu
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    public void ActivateTutorial()
    {
        CurrentSemester = 1;
        SetupSemester(1);
    }
    public void AdvanceSemester()
    {
        CurrentSemester++;
        SetupSemester(CurrentSemester);
        Debug.Log($"[TutorialManager] Masuk ke Semester {CurrentSemester}");
    }

    public void SetupSemester(int semester)
    {
        CurrentSemester = semester;
        List<int> sourceList = (semester == 1) ? botTicketsSem1 : botTicketsSem2;
        // Copy list agar data asli tidak hilang saat di-dequeue
        _botTicketQueue = new Queue<int>(new List<int>(sourceList));
    }

    public int GetNextBotTicket()
    {
        if (_botTicketQueue != null && _botTicketQueue.Count > 0)
            return _botTicketQueue.Dequeue();
        
        Debug.LogWarning("[TutorialManager] Kehabisan tiket bot, return random.");
        return Random.Range(1, 6); 
    }

    public int GetBotActionIndex(string botName)
    {
        // Cari aksi yang cocok untuk bot ini di semester ini
        // Note: FirstOrDefault akan return default struct jika tidak ketemu
        var action = botActions.FirstOrDefault(x => x.botName == botName && x.semester == CurrentSemester);
        
        // Cek validitas (jika botName kosong berarti tidak ketemu)
        if (string.IsNullOrEmpty(action.botName)) return -2; // -2 Kode untuk "Tidak ada script, gunakan random/default"
        
        return action.cardIndexToTake;
    }
    public bool ShouldBotActivate(string botName)
    {
        var action = botActions.FirstOrDefault(x => x.botName == botName && x.semester == CurrentSemester);
        
        // Jika tidak ditemukan, defaultnya false
        if (string.IsNullOrEmpty(action.botName)) return false;

        return action.shouldActivate;
    }
    public void ConsumeBotAction(string botName)
    {
        // Cari index dari item yang cocok di list
        int index = botActions.FindIndex(x => x.botName == botName && x.semester == CurrentSemester);
        
        if (index != -1)
        {
            botActions.RemoveAt(index);
            Debug.Log($"[Tutorial] Instruksi ambil kartu untuk {botName} di Semester {CurrentSemester} telah dihapus/dikonsumsi.");
        }
    }

    public BotSellingScript GetBotSellingData(string botName)
    {
        return botSellingActions.FirstOrDefault(x => x.botName == botName && x.semester == CurrentSemester);
    }
}