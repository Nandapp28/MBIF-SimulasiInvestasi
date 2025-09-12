using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RumorPhaseManager : MonoBehaviour
{
    [Header("Game References")]
    public GameManager gameManager;
    public SellingPhaseManager sellingPhaseManager;
    public ResolutionPhaseManager resolutionPhaseManager;
    [Header("Posisi Spesial")] // <-- TAMBAHKAN HEADER BARU
    public Transform predictionCardStage;
    public GameObject predictionCardObject; // <-- TAMBAHKAN BARIS INI
    public Renderer predictionCardRenderer;
    [Header("System References")]
    public CameraController cameraController;
    [System.Serializable]
    public class RumorEffect
    {
        public string color;
        public string description;
        public string cardName;

        public enum EffectType
        {
            ModifyIPO,
            BonusFinpoint,
            PenaltyFinpoint,
            ResetAllIPO,
            TaxByTurnOrder,
            StockDilution
            // Tambahkan efek lain jika perlu
        }

        public EffectType effectType;
        public int value;
        public bool affectAllPlayers = true;
    }

    public List<RumorEffect> rumorEffects = new List<RumorEffect>();

    private Dictionary<string, GameObject> faceUpRumorCards = new Dictionary<string, GameObject>();
    // SOLUSI: Menggunakan backing field
    [Header("Debug - Urutan Kartu Rumor TKonsumerilih")]
    [SerializeField] // <-- Tambahkan ini agar field private bisa dilihat di Inspector
    private List<RumorEffect> _shuffledRumorDeck = new List<RumorEffect>();

    // Property publik untuk dibaca oleh skrip lain (misal: HelpCardPhaseManager)
    public List<RumorEffect> shuffledRumorDeck => _shuffledRumorDeck; private bool rumorRunning = false;

    private List<PlayerProfile> players;
    [Header("Kartu Rumor Per Warna")]
    public GameObject cardRed;
    public GameObject cardBlue;
    public GameObject cardGreen;
    public GameObject cardOrange;

    [System.Serializable]
    public class CardVisual
    {
        public string cardName;
        public Texture texture;
    }
    [System.Serializable] // <-- TAMBAHKAN CLASS BARU INI
    public class CardVisuals2D
    {
        public string cardName;
        public Sprite sprite; // Menggunakan Sprite, bukan Texture
    }


    public List<CardVisual> cardVisuals = new List<CardVisual>();
    public List<CardVisuals2D> cardVisuals2D = new List<CardVisuals2D>();

    public Renderer rendererRed;
    public Renderer rendererBlue;
    public Renderer rendererGreen;
    public Renderer rendererOrange;
    private void Start()
    {
        rumorEffects = new List<RumorEffect>
    {

        new RumorEffect { color = "Konsumer",cardName = "Resesi_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Konsumer",cardName = "Resesi_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Konsumer",cardName = "Revaluasi_Asset", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Buyback", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Buyback", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Tender_Kompetitif", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Audit_Forensik", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Suap_Audit", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Depresiasi_Rupiah", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Rencana_Ekspansi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Ekspansi_Produk", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Investasi_Asing", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Kenaikan_Upah", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Siasat_Pajak", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Konsumer",cardName = "Defisit_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Konsumer",cardName = "Merger", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Merger", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Konsumer",cardName = "Reformasi_Ekonomi", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Konsumer",cardName = "Reformasi_Ekonomi", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Konsumer",cardName = "Extra_Fee", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Konsumer",cardName = "Extra_Fee", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Konsumer",cardName = "Pajak_Jalan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },
        new RumorEffect { color = "Konsumer",cardName = "Pajak_Jalan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },
        new RumorEffect { color = "Konsumer",cardName = "Penerbitan_Saham", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Konsumer",cardName = "Penerbitan_Saham", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Ref   ormasi ekonomi" },



        new RumorEffect { color = "Infrastruktur",cardName = "Resesi_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Infrastruktur",cardName = "Resesi_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Infrastruktur",cardName = "Revaluasi_Asset", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Buyback", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Buyback", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Tender_Kompetitif", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Audit_Forensik", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Suap_Audit", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Depresiasi_Rupiah", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Rencana_Ekspansi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Ekspansi_Produk", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Investasi_Asing", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Kenaikan_Upah", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Siasat_Pajak", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Infrastruktur",cardName = "Defisit_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Infrastruktur",cardName = "Merger", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Merger", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Infrastruktur",cardName = "Reformasi_Ekonomi", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Infrastruktur",cardName = "Reformasi_Ekonomi", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Infrastruktur",cardName = "Extra_Fee", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Infrastruktur",cardName = "Extra_Fee", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Infrastruktur",cardName = "Pajak_Jalan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },
        new RumorEffect { color = "Infrastruktur",cardName = "Pajak_Jalan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },
        new RumorEffect { color = "Infrastruktur",cardName = "Penerbitan_Saham", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Infrastruktur",cardName = "Penerbitan_Saham", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Reformasi ekonomi" },

        new RumorEffect { color = "Keuangan",cardName = "Resesi_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Keuangan",cardName = "Resesi_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Keuangan",cardName = "Revaluasi_Asset", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Buyback", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Buyback", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Tender_Kompetitif", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Audit_Forensik", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Suap_Audit", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Depresiasi_Rupiah", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Rencana_Ekspansi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Ekspansi_Produk", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Investasi_Asing", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Kenaikan_Upah", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Siasat_Pajak", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Keuangan",cardName = "Defisit_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Keuangan",cardName = "Merger", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Merger", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Keuangan",cardName = "Reformasi_Ekonomi", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Keuangan",cardName = "Reformasi_Ekonomi", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Keuangan",cardName = "Extra_Fee", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Keuangan",cardName = "Extra_Fee", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Keuangan",cardName = "Pajak_Jalan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },
        new RumorEffect { color = "Keuangan",cardName = "Pajak_Jalan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },
        new RumorEffect { color = "Keuangan",cardName = "Penerbitan_Saham", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Keuangan",cardName = "Penerbitan_Saham", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Reformasi ekonomi" },

        new RumorEffect { color = "Tambang",cardName = "Resesi_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Tambang",cardName = "Resesi_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Tambang",cardName = "Revaluasi_Asset", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Buyback", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Buyback", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Tender_Kompetitif", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Audit_Forensik", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Suap_Audit", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Depresiasi_Rupiah", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Rencana_Ekspansi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Ekspansi_Produk", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Investasi_Asing", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Kenaikan_Upah", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Siasat_Pajak", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Tambang",cardName = "Defisit_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Tambang",cardName = "Merger", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Merger", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Tambang",cardName = "Reformasi_Ekonomi", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Tambang",cardName = "Reformasi_Ekonomi", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Tambang",cardName = "Extra_Fee", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Tambang",cardName = "Extra_Fee", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Tambang",cardName = "Pajak_Jalan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },
        new RumorEffect { color = "Tambang",cardName = "Pajak_Jalan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },
        new RumorEffect { color = "Tambang",cardName = "Penerbitan_Saham", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Tambang",cardName = "Penerbitan_Saham", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Reformasi ekonomi" },
    };
        InitializeRumorDeck(); // Atau panggil dari GameManager saat game dimulai
    }


    public void InitializeRumorDeck()
    {
        shuffledRumorDeck.Clear();

        // Ambil satu kartu acak dari tiap warna
        List<string> colors = new List<string> { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };

        foreach (string color in colors)
        {
            var possibleRumors = rumorEffects.Where(r => r.color == color).ToList();
            if (possibleRumors.Count > 0)
            {
                RumorEffect chosen = possibleRumors[Random.Range(0, possibleRumors.Count)];
                shuffledRumorDeck.Add(chosen);
            }
        }


        Debug.Log("[RumorDeck] Kartu rumor telah diacak dan disiapkan:");
        foreach (var effect in shuffledRumorDeck)
        {
            Debug.Log($"- {effect.color}: {effect.cardName} ({effect.description})");
        }
    }


    public void StartRumorPhase(List<PlayerProfile> currentPlayers)
    {
        if (rumorRunning) return; // Jangan mulai dua kali
        rumorRunning = true;

        players = currentPlayers;
        Debug.Log("Memulai fase rumor...");

        StartCoroutine(RunRumorSequence());
    }
    private IEnumerator RunRumorSequence()
    {
        yield return new WaitForSeconds(2f);
        UITransitionAnimator.Instance.StartTransition("Rumour Phase");
        yield return new WaitForSeconds(4f);

        foreach (var selected in shuffledRumorDeck)
        {

            // Tentukan posisi kamera berdasarkan warna kartu
            CameraController.CameraPosition targetPos = CameraController.CameraPosition.Normal;
            switch (selected.color)
            {
                case "Konsumer": targetPos = CameraController.CameraPosition.KonsumerRumour; break;
                case "Infrastruktur": targetPos = CameraController.CameraPosition.InfrastrukturRumour; break;
                case "Keuangan": targetPos = CameraController.CameraPosition.KeuanganRumour; break;
                case "Tambang": targetPos = CameraController.CameraPosition.TambangRumour; break;
            }

            // 1. GERAKKAN KAMERA KE KARTU
            if (cameraController) yield return cameraController.MoveTo(targetPos);

            yield return new WaitForSeconds(0.5f); // Sedikit jeda setelah kamera sampai

            // Tampilkan kartu
            ShowCardByColorAndName(selected.color, selected.cardName);
            Debug.Log($"[Rumor] Warna {selected.color}: {selected.description}");


            yield return new WaitForSeconds(2f); // Tunggu sebelum sembunyikan kartu & reset kamera




            // Terapkan efek
            yield return StartCoroutine(ApplyRumorEffect(selected));
            gameManager.UpdatePlayerUI();
            sellingPhaseManager.UpdateIPOVisuals();



            // 2. KEMBALIKAN KAMERA KE POSISI NORMAL
            if (cameraController && cameraController.CurrentPosition != CameraController.CameraPosition.Normal)
            {
                yield return cameraController.MoveTo(CameraController.CameraPosition.Normal);
            }

            yield return new WaitForSeconds(1.0f); // Jeda sebelum kartu berikutnya
        }

        rumorRunning = false;
        UITransitionAnimator.Instance.StartTransition("Resolution Phase");
        yield return new WaitForSeconds(4f);
        resolutionPhaseManager.StartResolutionPhase(players);
    }


    private void ShowCardByColorAndName(string color, string cardName)
    {

        if (faceUpRumorCards.ContainsKey(color))
        {
            if (faceUpRumorCards[color] != null)
            {
                faceUpRumorCards[color].SetActive(false);
            }
            faceUpRumorCards.Remove(color);
        }// Sembunyikan dulu semua kartu

        Texture frontTexture = cardVisuals.FirstOrDefault(v => v.cardName == cardName)?.texture;
        if (frontTexture == null)
        {
            Debug.LogWarning($"[RumorPhase] Texture untuk cardName '{cardName}' tidak ditemukan!");
            return;
        }

        GameObject card = null;
        Renderer renderer = null;

        switch (color)
        {
            case "Konsumer":
                card = cardRed;
                renderer = rendererRed;
                break;
            case "Infrastruktur":
                card = cardBlue;
                renderer = rendererBlue;
                break;
            case "Keuangan":
                card = cardGreen;
                renderer = rendererGreen;
                break;
            case "Tambang":
                card = cardOrange;
                renderer = rendererOrange;
                break;
        }

        if (card && renderer)
        {
            renderer.material.mainTexture = frontTexture; // ⬅️ langsung set texture di awal
            StartCoroutine(FlipCard(card));
        }
    }

    public IEnumerator MoveObjectToTargetAndBack(GameObject objectToMove)
    {
        // Pemeriksaan keamanan
        if (objectToMove == null)
        {
            Debug.LogError("Objek yang ingin digerakkan tidak valid (null).");
            yield break;
        }

        // --- 1. Persiapan ---
        Vector3 originalPosition = objectToMove.transform.position;

        // [DIUBAH] Hitung posisi target dengan menambah 10 dari posisi awal
        Vector3 targetPosition = originalPosition + new Vector3(-1.96f, 2.72f, 0f);

        float moveDuration = 1f;
        float elapsedTime = 0f;

        // --- 2. Pergerakan ke Posisi Target ---
        Debug.Log($"'{objectToMove.name}' bergerak ke {targetPosition}...");
        while (elapsedTime < moveDuration)
        {
            objectToMove.transform.position = Vector3.Lerp(originalPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        objectToMove.transform.position = targetPosition;
        Debug.Log($"'{objectToMove.name}' tiba di target.");

        // --- 3. Tunggu selama 5 detik ---
        Debug.Log("Menunggu 5 detik...");
        yield return new WaitForSeconds(3f);

        // --- 4. Pergerakan Kembali ke Posisi Awal ---
        Debug.Log($"'{objectToMove.name}' kembali ke posisi awal...");
        elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            objectToMove.transform.position = Vector3.Lerp(targetPosition, originalPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        objectToMove.transform.position = originalPosition;
        Debug.Log($"'{objectToMove.name}' telah kembali.");
    }
    private IEnumerator FlipCard(GameObject cardObject)
    {
        cardObject.SetActive(true);

        // Mulai dari kondisi terbalik
        cardObject.transform.rotation = Quaternion.Euler(0, -180, 180);

        float duration = 0.5f;
        float elapsed = 0f;

        Quaternion startRot = cardObject.transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, -180, 0); // Menghadap depan

        yield return new WaitForSeconds(0.5f); // jeda sejenak sebelum animasi

        while (elapsed < duration)
        {
            cardObject.transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cardObject.transform.rotation = endRot;
    }
    // Tambahkan metode ini di dalam kelas RumorPhaseManager.cs

    public IEnumerator ShowPredictionCardAtCenter(RumorEffect rumorToShow)
    {
        Debug.Log($"Menampilkan bocoran kartu rumor: {rumorToShow.cardName}");

        // Langkah 1: Tentukan objek kartu dan renderer yang akan digunakan
        GameObject cardObject = predictionCardObject;
        Renderer cardRenderer = predictionCardRenderer;

        if (cardObject == null || cardRenderer == null)
        {
            Debug.LogError("Referensi 'Prediction Card Object' atau 'Prediction Card Renderer' belum di-assign di Inspector!");
            yield break;
        }

        // Validasi texture
        Texture frontTexture = cardVisuals.FirstOrDefault(v => v.cardName == rumorToShow.cardName)?.texture;
        if (frontTexture == null)
        {
            Debug.LogWarning($"Texture untuk '{rumorToShow.cardName}' tidak ditemukan!");
            yield break;
        }

        if (cameraController) yield return cameraController.MoveTo(CameraController.CameraPosition.Center);

        Vector3 originalPosition = cardObject.transform.position;

        // Pindahkan kartu ke stage dan balik
        cardObject.transform.position = predictionCardStage.position;
        cardObject.transform.rotation = predictionCardStage.rotation;
        cardRenderer.material.mainTexture = frontTexture;
        yield return StartCoroutine(FlipCard(cardObject));

        yield return new WaitForSeconds(3f); // Waktu untuk pemain melihat

        cardObject.SetActive(false);
        cardObject.transform.position = originalPosition; // Kembalikan posisi logisnya

        // 2. KEMBALIKAN KAMERA KE NORMAL
        if (cameraController) yield return cameraController.MoveTo(CameraController.CameraPosition.Normal);

        Debug.Log($"Kartu {cardObject.name} telah disembunyikan dan kamera kembali normal.");
    }


    public void HideAllCardObjects()
    {
        if (cardRed) cardRed.SetActive(false);
        if (cardBlue) cardBlue.SetActive(false);
        if (cardGreen) cardGreen.SetActive(false);
        if (cardOrange) cardOrange.SetActive(false);
    }
    public void HideAllFaceUpRumorCards()
    {
        if (faceUpRumorCards == null) return;

        foreach (var cardObject in faceUpRumorCards.Values)
        {
            if (cardObject != null)
            {
                cardObject.SetActive(false);
            }
        }
        faceUpRumorCards.Clear();
        Debug.Log("[RumorPhase] Semua kartu rumor yang terbuka telah disembunyikan.");
    }



    private IEnumerator ApplyRumorEffect(RumorEffect effect)
    {
        if (effect.effectType == RumorEffect.EffectType.ModifyIPO)
        {
            yield return StartCoroutine(sellingPhaseManager.ModifyIPOIndexWithCamera(effect.color, effect.value));
            yield break;
        }
        if (effect.effectType == RumorEffect.EffectType.ResetAllIPO)
        {
            yield return StartCoroutine(sellingPhaseManager.ResetAllIPOIndexesWithCamera());
            yield break;
        }
        if (effect.effectType == RumorEffect.EffectType.StockDilution)
        {
            List<PlayerProfile> affectedPlayers = new List<PlayerProfile>();
            foreach (var p in players)
            {
                if (p.cards.Any(c => c.color == effect.color))
                {
                    affectedPlayers.Add(p);
                }
            }

            yield return StartCoroutine(sellingPhaseManager.ModifyIPOIndexWithCamera(effect.color, effect.value));

            foreach (var p in affectedPlayers)
            {
                var newCard = new Card($"{effect.color}_Extra", $"Kartu tambahan warna {effect.color}", 0, effect.color);
                p.AddCard(newCard);
                Debug.Log($"{p.playerName} menerima 1 kartu tambahan warna {effect.color}");
            }
            yield break;
        }

        // Efek lain yang tidak memengaruhi IPO
        foreach (var player in players)
        {
            bool playerHasColor = player.cards.Any(c => c.color == effect.color);
            if (!effect.affectAllPlayers && !player.isBot) continue;

            switch (effect.effectType)
            {
                case RumorEffect.EffectType.BonusFinpoint:
                    if (playerHasColor)
                    {
                        player.finpoint += effect.value;
                        Debug.Log($"{player.playerName} mendapat bonus {effect.value} finpoint karena memegang kartu {effect.color}");
                    }
                    break;
                case RumorEffect.EffectType.PenaltyFinpoint:
                    { // <-- KURUNG KURAWAL DITAMBAHKAN
                        int cardCount = player.cards.Count(c => c.color == effect.color);
                        if (cardCount > 0)
                        {
                            int penalty = cardCount * effect.value;
                            player.finpoint -= penalty;
                            Debug.Log($"{player.playerName} membayar {penalty} finpoint karena memiliki {cardCount} kartu {effect.color}");
                        }
                        break;
                    } // <-- KURUNG KURAWAL DITAMBAHKAN
                case RumorEffect.EffectType.TaxByTurnOrder:
                    { // <-- KURUNG KURAWAL DITAMBAHKAN
                        int penalty = player.ticketNumber * effect.value;
                        player.finpoint -= penalty;
                        Debug.Log($"{player.playerName} membayar pajak jalan sebesar {penalty} finpoint (turnOrder: {player.ticketNumber})");
                        break;
                    }
            }
        }
    }

    public void ResetAllIPOIndexes()
    {
        // Fungsi ini sekarang didelegasikan ke SellingPhaseManager
        StartCoroutine(sellingPhaseManager.ResetAllIPOIndexesWithCamera());
    }

    public IEnumerator DisplayAndHidePrediction(RumorEffect predictionCard)
    {
        Debug.Log($"Menampilkan bocoran kartu rumor: {predictionCard.cardName}");

        // Panggil fungsi yang sudah ada untuk menampilkan kartu di tengah
        yield return StartCoroutine(ShowPredictionCardAtCenter(predictionCard));

        // Tunggu selama beberapa detik agar pemain bisa melihat
        yield return new WaitForSeconds(1f); // Anda bisa sesuaikan durasi  
    }




    private void ModifyIPOIndex(string color, int delta)
    {
        // Fungsi ini sekarang didelegasikan ke SellingPhaseManager
        StartCoroutine(sellingPhaseManager.ModifyIPOIndexWithCamera(color, delta));
    }
    
    public string GetCardNameFromTexture(Texture texture3D)
{
    var visual = cardVisuals.FirstOrDefault(v => v.texture == texture3D);
    if (visual != null)
    {
        return visual.cardName;
    }
    return null; // Return null jika tidak ditemukan
}
/// Mencari Sprite 2D berdasarkan nama kartunya.
public Sprite GetCardSprite2D(string cardName)
{
    var visual2D = cardVisuals2D.FirstOrDefault(v => v.cardName == cardName);
    if (visual2D != null)
    {
        return visual2D.sprite;
    }
    Debug.LogWarning($"Sprite 2D untuk '{cardName}' tidak ditemukan!");
    return null; // Return null jika tidak ditemukan
}
}
