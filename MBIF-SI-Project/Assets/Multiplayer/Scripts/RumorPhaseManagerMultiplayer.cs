// File: RumorPhaseManagerMultiplayer.cs
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RumorPhaseManagerMultiplayer : MonoBehaviourPunCallbacks
{
    public static RumorPhaseManagerMultiplayer Instance;

    [Header("Rumor Deck Setup")]
    // Daftar ini akan diisi oleh fungsi InitializeRumorBlueprints()
    private List<RumorEffectData> rumorCardBlueprints = new List<RumorEffectData>();

    // Daftar ini akan dibuat secara otomatis oleh skrip GenerateFullRumorDeck()
    private List<RumorEffectData> allRumorEffects = new List<RumorEffectData>();

    [Header("3D Card References")]
    public GameObject rumorCardKonsumer;
    public GameObject rumorCardInfrastruktur;
    public GameObject rumorCardKeuangan;
    public GameObject rumorCardTambang;
    public GameObject rumorCardNetral;

    [System.Serializable]
    public class CardVisual { public string cardName; public Texture texture; }
    public List<CardVisual> cardVisuals = new List<CardVisual>();

    [Header("UI Visuals")]
    public GameObject rumorCardVisual; // Prefab atau objek kartu rumor yang akan ditampilkan
    private List<int> shuffledRumorDeckIndices; // Indeks dek rumor untuk ronde ini

    [Header("System References")]
    public CameraController cameraController;

    [Header("Kartu Rumor per Sektor")]
    public GameObject cardRed;
    public GameObject cardBlue;
    public GameObject cardGreen;
    public GameObject cardOrange;

    [Header("Visual Kartu Rumor")]
    public List<CardVisual> allCardVisuals = new List<CardVisual>();
  
public List<CardVisuals2D> cardVisuals2D = new List<CardVisuals2D>();


    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        // 1. Definisikan dulu semua kartu blueprint-nya
        InitializeRumorBlueprints();

        // 2. Setelah itu, baru gandakan untuk semua sektor
        GenerateFullRumorDeck();
    }

    private void InitializeRumorBlueprints()
    {
        // Cukup edit di dalam blok ini untuk menyeimbangkan permainan Anda
        rumorCardBlueprints = new List<RumorEffectData>
        {
            //ANDA BISA MENGUBAH EFFECTTYPE, VALUE, DAN DESCRIPTION DI BAWAH INI
            new RumorEffectData { cardName = "Resesi_Ekonomi",    description = "Sentimen pasar negatif, harga sedikit turun.",  effectType = RumorType.ModifyIPO, value = -1 },
            new RumorEffectData { cardName = "Revaluasi_Asset",    description = "Aset perusahaan dinilai kembali, harga naik.",    effectType = RumorType.ModifyIPO, value = 1 },
            new RumorEffectData { cardName = "Buyback",            description = "Perusahaan membeli kembali sahamnya, harga naik.",  effectType = RumorType.ModifyIPO, value = 1 },
            new RumorEffectData { cardName = "Tender_Kompetitif",  description = "Memenangkan tender proyek besar, harga naik.",     effectType = RumorType.ModifyIPO, value = 1 },
            new RumorEffectData { cardName = "Audit_Forensik",     description = "Ditemukan penyelewengan dana, harga anjlok.",    effectType = RumorType.ModifyIPO, value = -2 },
            new RumorEffectData { cardName = "Suap_Audit",         description = "Skandal suap terungkap, kepercayaan investor jatuh.", effectType = RumorType.ModifyIPO, value = -2 },
            new RumorEffectData { cardName = "Depresiasi_Rupiah",  description = "Nilai tukar melemah, biaya impor naik.",        effectType = RumorType.ModifyIPO, value = -2 },
            new RumorEffectData { cardName = "Krisis_Keuangan",    description = "Krisis likuiditas melanda, harga jatuh.",      effectType = RumorType.ModifyIPO, value = -2 },
            new RumorEffectData { cardName = "Rencana_Ekspansi",   description = "Perusahaan akan berekspansi, prospek cerah.",  effectType = RumorType.ModifyIPO, value = 2 },
            new RumorEffectData { cardName = "Stimulus_Ekonomi",   description = "Pemerintah memberi stimulus, pasar bergairah.", effectType = RumorType.ModifyIPO, value = 2 },
            new RumorEffectData { cardName = "Ekspansi_Produk",    description = "Meluncurkan produk baru yang sukses.",           effectType = RumorType.ModifyIPO, value = 2 },
            new RumorEffectData { cardName = "Investasi_Asing",    description = "Dana asing masuk, harga terdongkrak.",         effectType = RumorType.ModifyIPO, value = 2 },
            new RumorEffectData { cardName = "Kenaikan_Upah",      description = "Daya beli masyarakat meningkat.",                 effectType = RumorType.ModifyIPO, value = 2 },
            new RumorEffectData { cardName = "Siasat_Pajak",       description = "Terlibat kasus penggelapan pajak, harga anjlok.", effectType = RumorType.ModifyIPO, value = -3 },
            new RumorEffectData { cardName = "Defisit_Keuangan",   description = "Laporan keuangan menunjukkan defisit besar.",   effectType = RumorType.ModifyIPO, value = -3 },
            new RumorEffectData { cardName = "Merger",             description = "Merger dengan perusahaan raksasa, harga meroket.", effectType = RumorType.ModifyIPO, value = 3 },
            new RumorEffectData { cardName = "Reformasi_Ekonomi",  description = "Peraturan baru merombak total kondisi pasar.",    effectType = RumorType.ResetAllIPO, value = 0 },
            new RumorEffectData { cardName = "Extra_Fee",          description = "Pemain dengan kartu di sektor ini membayar denda.", effectType = RumorType.PenaltyInvestPoin, value = 1 }, // value = denda per kartu
            new RumorEffectData { cardName = "Pajak_Jalan",        description = "Semua pemain membayar pajak sesuai urutan giliran.", effectType = RumorType.TaxByTurnOrder, value = 1 }, // value = pengali
            new RumorEffectData { cardName = "Penerbitan_Saham",   description = "Menerbitkan saham baru, terjadi dilusi.",       effectType = RumorType.StockDilution, value = -1 } // value = efek ke harga
        };
    }

    // --- FUNGSI LAMA YANG SUDAH KITA BUAT, BIARKAN SEPERTI INI ---
    private void GenerateFullRumorDeck()
    {
        var allSektors = new Sektor[] { Sektor.Konsumer, Sektor.Infrastruktur, Sektor.Keuangan, Sektor.Tambang };

        foreach (var blueprint in rumorCardBlueprints)
        {
            foreach (var sektor in allSektors)
            {
                RumorEffectData newCard = new RumorEffectData
                {
                    cardName = blueprint.cardName,
                    description = blueprint.description,
                    effectType = blueprint.effectType,
                    value = blueprint.value,
                    artwork = blueprint.artwork,
                    color = sektor
                };
                allRumorEffects.Add(newCard);
            }
        }
        Debug.Log($"Dek rumor final berhasil dibuat. Total kartu: {allRumorEffects.Count} (dari {rumorCardBlueprints.Count} blueprint)");
    }

    // Fungsi ini dipanggil dari SellingPhaseManager
    public void StartRumorPhase(List<Player> players)
    {
        if (GameStatusUI.Instance != null)
        {
            GameStatusUI.Instance.photonView.RPC("UpdateStatusText", RpcTarget.All, "Fase Rumor: Kartu rumor akan segera diungkap!");
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient memulai Fase Rumor...");
            InitializeRumorDeck();
        }
    }

    public void PrepareNextRumorDeck()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Logika ini disalin dari InitializeRumorDeck() untuk men-generate 4 kartu rumor.
        List<int> finalDeckIndices = new List<int>();
        var SektorOrder = new Sektor[] { Sektor.Konsumer, Sektor.Infrastruktur, Sektor.Keuangan, Sektor.Tambang }; //

        foreach (var sektor in SektorOrder)
        {
            var rumorsInSektor = allRumorEffects
                .Select((data, index) => new { data, index })
                .Where(x => x.data.color == sektor)
                .ToList();

            if (rumorsInSektor.Any())
            {
                finalDeckIndices.Add(rumorsInSektor[Random.Range(0, rumorsInSektor.Count)].index);
            }
        }

        // Simpan dek yang sudah di-generate ke Room Custom Properties agar bisa diakses nanti
        Hashtable roomProps = new Hashtable { { "nextRumorDeck", finalDeckIndices.ToArray() } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        Debug.Log("[MasterClient] Dek rumor berikutnya telah disiapkan dan disimpan di Room Properties.");
    }

    private void InitializeRumorDeck()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // JALUR UTAMA: Gunakan dek yang sudah disiapkan sebelumnya jika ada.
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("nextRumorDeck", out object deckIndicesObj))
        {
            int[] rumorIndices = (int[])deckIndicesObj;
            Debug.Log("[MasterClient] Menggunakan dek rumor yang sudah disiapkan untuk 'Insider Trade'.");

            // Kirim dek ini ke semua pemain untuk memulai fase rumor
            photonView.RPC("Rpc_SetRumorDeck", RpcTarget.All, rumorIndices);

            // Hapus properti setelah digunakan agar tidak menumpuk
            Hashtable propsToClear = new Hashtable { { "nextRumorDeck", null } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(propsToClear);

            // PENTING: Hentikan eksekusi fungsi di sini agar tidak membuat dek baru.
            return;
        }

        // JALUR CADANGAN (FALLBACK): Kode ini hanya akan berjalan jika 'nextRumorDeck' tidak ditemukan.
        // Ini seharusnya tidak terjadi dalam alur normal, tapi bagus sebagai pengaman.
        Debug.LogError("[MasterClient] GAGAL memulai fase rumor: 'nextRumorDeck' tidak ditemukan. Men-generate dek baru secara darurat.");

        List<int> finalDeckIndices = new List<int>();
        var SektorOrder = new Sektor[] { Sektor.Konsumer, Sektor.Infrastruktur, Sektor.Keuangan, Sektor.Tambang };

        foreach (var sektor in SektorOrder)
        {
            var rumorsInSektor = allRumorEffects
                .Select((data, index) => new { data, index })
                .Where(x => x.data.color == sektor)
                .ToList();

            if (rumorsInSektor.Any())
            {
                finalDeckIndices.Add(rumorsInSektor[Random.Range(0, rumorsInSektor.Count)].index);
            }
        }

        // Logika untuk mengganti kartu ResetAllIPO (jika ada) tetap diperlukan di sini
        int resetEffectIndex = finalDeckIndices.FindIndex(i => allRumorEffects[i].effectType == RumorType.ResetAllIPO);
        if (resetEffectIndex != -1)
        {
            var neutralReplacements = allRumorEffects
                .Select((data, index) => new { data, index })
                .Where(x => x.data.color == Sektor.Netral && x.data.effectType != RumorType.ResetAllIPO)
                .ToList();

            if (neutralReplacements.Any())
            {
                finalDeckIndices[resetEffectIndex] = neutralReplacements[Random.Range(0, neutralReplacements.Count)].index;
                Debug.Log("Kartu Reset IPO diganti dengan kartu Netral.");
            }
        }

        // Kirim dek yang baru dibuat ini karena dek yang disiapkan tidak ada.
        photonView.RPC("Rpc_SetRumorDeck", RpcTarget.All, finalDeckIndices.ToArray());
    }
    public string GetCardNameFromTexture(Texture texture3D)
    {
        var visual = cardVisuals.FirstOrDefault(v => v.texture == texture3D);
        return visual?.cardName; // Menggunakan null-conditional operator untuk keringkasan
    }
public Sprite GetCardSprite2D(string cardName)
{
    var visual2D = cardVisuals2D.FirstOrDefault(v => v.cardName == cardName);
    if (visual2D == null)
    {
        Debug.LogWarning($"Sprite 2D untuk '{cardName}' tidak ditemukan!");
    }
    return visual2D?.sprite;
}

    [PunRPC]
    private void Rpc_SetRumorDeck(int[] rumorIndices)
    {
        shuffledRumorDeckIndices = new List<int>(rumorIndices);
        Debug.Log($"[{PhotonNetwork.LocalPlayer.NickName}] menerima dek rumor. Jumlah: {shuffledRumorDeckIndices.Count} kartu.");

        // MasterClient yang menerima RPC ini akan langsung memulai urutan rumor
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(RunRumorSequence());
        }
    }

    public RumorEffectData GetRumorCardData(int index)
    {
        // Pastikan indeksnya valid untuk menghindari eror
        if (index >= 0 && index < allRumorEffects.Count)
        {
            return allRumorEffects[index];
        }

        // Jika indeks tidak valid, kembalikan null
        Debug.LogError($"Permintaan data kartu dengan indeks di luar jangkauan: {index}");
        return null;
    }

    private IEnumerator RunRumorSequence()
    {
        Debug.Log("MasterClient memulai urutan rumor dengan pergerakan kamera...");
        yield return new WaitForSeconds(2f);

        foreach (int index in shuffledRumorDeckIndices)
        {
            RumorEffectData effectData = allRumorEffects[index];

            // 1. Tentukan posisi kamera tujuan berdasarkan warna kartu
            CameraController.CameraPosition targetPos = CameraController.CameraPosition.Normal;
            switch (effectData.color)
            {
                case Sektor.Konsumer: targetPos = CameraController.CameraPosition.Konsumer; break;
                case Sektor.Infrastruktur: targetPos = CameraController.CameraPosition.Infrastruktur; break;
                case Sektor.Keuangan: targetPos = CameraController.CameraPosition.Keuangan; break;
                case Sektor.Tambang: targetPos = CameraController.CameraPosition.Tambang; break;
                // Untuk Netral, kamera bisa tetap di Normal atau pindah ke tengah (Center)
                case Sektor.Netral: targetPos = CameraController.CameraPosition.Center; break;
            }

            // 2. Perintahkan SEMUA client untuk menggerakkan kamera mereka ke target
            photonView.RPC("Rpc_MoveCamera", RpcTarget.All, targetPos);
            // Tunggu animasi kamera selesai sebelum melanjutkan
            float waitDuration = (cameraController != null) ? cameraController.moveDuration : 0.8f;
            yield return new WaitForSeconds(waitDuration);

            // 3. Tampilkan kartu rumor di perangkat semua pemain
            photonView.RPC("Rpc_ShowRumorCard", RpcTarget.All, index);
            yield return new WaitForSeconds(3f); // Waktu untuk pemain membaca kartu

            
            // 5. BARULAH MasterClient menerapkan efek dari rumor
            ApplyRumorEffect(effectData); // <<< SEKARANG EFEK DITERAPKAN SETELAH KARTU HILANG
            yield return new WaitForSeconds(2f); // Jeda agar pemain bisa melihat pergerakan IPO

            // 6. Perintahkan SEMUA client untuk mengembalikan kamera ke posisi Normal
            photonView.RPC("Rpc_MoveCamera", RpcTarget.All, CameraController.CameraPosition.Normal);
            // Tunggu animasi kamera kembali selesai
            yield return new WaitForSeconds(waitDuration);
        }

        // 1. Panggil RPC untuk memulai transisi di SEMUA klien
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.photonView.RPC(
                "Rpc_StartFadeTransition",
                RpcTarget.All,
                MultiplayerManager.TransitionType.Resolution // Kirim tipe transisi yang benar
            );
        }

        // 2. Tunggu selama total durasi transisi agar tidak tumpang tindih
        // Durasi = fadeIn + hold + fadeOut = 0.5s + 1s + 0.5s = 2.0s
        yield return new WaitForSeconds(2.0f);

        Debug.Log("✅ Fase Rumor Selesai. Memulai fase berikutnya...");

        if (ResolutionPhaseManagerMultiplayer.Instance != null)
        {
            ResolutionPhaseManagerMultiplayer.Instance.StartResolutionPhase();
        }
    }

    #region Visuals & Animation
    public IEnumerator AnimatePrivateRumorPreview(string sectorName)
    {
        Debug.Log($"[PRIVATE PREVIEW] Menjalankan animasi privat untuk sektor: {sectorName}");

        // Dapatkan dek rumor yang akan datang dari Room Properties
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("nextRumorDeck", out object deckIndicesObj))
        {
            Debug.LogError("[PRIVATE PREVIEW] Gagal: 'nextRumorDeck' tidak ditemukan.");
            yield break;
        }

        int[] rumorIndices = (int[])deckIndicesObj;
        Sektor targetSektor = (Sektor)System.Enum.Parse(typeof(Sektor), sectorName);

        // Tentukan indeks kartu yang sesuai dengan sektor yang dipilih
        int sectorIndexInDeck = -1;
        switch (targetSektor)
        {
            case Sektor.Konsumer: sectorIndexInDeck = 0; break;
            case Sektor.Infrastruktur: sectorIndexInDeck = 1; break;
            case Sektor.Keuangan: sectorIndexInDeck = 2; break;
            case Sektor.Tambang: sectorIndexInDeck = 3; break;
        }

        if (sectorIndexInDeck == -1 || rumorIndices.Length <= sectorIndexInDeck)
        {
            Debug.LogError($"[PRIVATE PREVIEW] Indeks sektor tidak valid untuk {sectorName}.");
            yield break;
        }

        // Dapatkan data kartu rumor yang akan ditampilkan
        int cardToShowIndex = rumorIndices[sectorIndexInDeck];
        if (cardToShowIndex < 0 || cardToShowIndex >= allRumorEffects.Count) yield break;
        RumorEffectData effectData = allRumorEffects[cardToShowIndex];

        // Salin dan tempel logika animasi dari AnimateSingleRumorCard
        // Bagian 1: Gerakkan Kamera
        CameraController.CameraPosition targetPos = (CameraController.CameraPosition)System.Enum.Parse(typeof(CameraController.CameraPosition), sectorName);
        if (cameraController != null)
        {
            cameraController.MoveTo(targetPos);
            yield return new WaitForSeconds(cameraController.moveDuration);
        }

        // Bagian 2: Tampilkan Kartu 3D
        HideAllCardObjects();
        Texture frontTexture = cardVisuals.FirstOrDefault(v => v.cardName == effectData.cardName)?.texture;
        if (frontTexture != null)
        {
            GameObject cardToDisplay = GetCardObjectByColor(sectorName);
            if (cardToDisplay != null)
            {
                Renderer cardRenderer = cardToDisplay.GetComponent<Renderer>();
                cardRenderer.material.mainTexture = frontTexture;
                StartCoroutine(FlipCard(cardToDisplay));
            }
        }

        // Bagian 3: Kembalikan Kamera
        HideAllCardObjects();
        yield return new WaitForSeconds(0.5f);
        if (cameraController != null)
        {
            cameraController.MoveTo(CameraController.CameraPosition.Normal);
            yield return new WaitForSeconds(cameraController.moveDuration);
        }
        Debug.Log($"[PRIVATE PREVIEW] Animasi untuk {sectorName} selesai.");
    }

    [PunRPC]
    private void Rpc_MoveCamera(CameraController.CameraPosition targetPosition)
    {
        if (cameraController != null)
        {
            // Setiap client akan menggerakkan kamera lokalnya masing-masing
            cameraController.MoveTo(targetPosition);
        }
        else
        {
            Debug.LogWarning("Referensi CameraController tidak ditemukan, kamera tidak akan bergerak.");
        }
    }
    private IEnumerator FlipCard(GameObject cardObject)
    {
        // --- Persiapan ---
        Vector3 finalPosition = cardObject.transform.position;
        Vector3 flipStartPosition = finalPosition;
        flipStartPosition.y -= 0.01f; // Mulai sedikit dari bawah

        // Set rotasi awal (terbalik) dan aktifkan kartu
        Quaternion startRotation = Quaternion.Euler(0, 180, 180);
        Quaternion finalRotation = Quaternion.Euler(0, 180, 0);

        cardObject.transform.position = flipStartPosition;
        cardObject.transform.rotation = startRotation;
        cardObject.SetActive(true);

        float flipDuration = 0.7f;
        float flipHeight = 0.5f; // Ketinggian lengkungan saat flip
        float flipElapsed = 0f;

        // --- Loop Animasi ---
        while (flipElapsed < flipDuration)
        {
            float progress = flipElapsed / flipDuration;

            // Gerakkan posisi dari awal ke akhir dengan lengkungan ke atas
            Vector3 currentPos = Vector3.Lerp(flipStartPosition, finalPosition, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * flipHeight;
            cardObject.transform.position = currentPos;

            // Rotasikan kartu secara Slerp
            cardObject.transform.rotation = Quaternion.Slerp(startRotation, finalRotation, progress);

            flipElapsed += Time.deltaTime;
            yield return null;
        }

        // --- Finalisasi ---
        // Pastikan posisi dan rotasi akhir sudah tepat
        cardObject.transform.position = finalPosition;
        cardObject.transform.rotation = finalRotation;
    }
private IEnumerator HideCard(GameObject cardObject)
{
    // --- Persiapan ---
    Vector3 originalPosition = cardObject.transform.position;
    float moveDuration = 0.5f; // Durasi animasi hide
    float sideOffset = -2.0f;  // Jarak lengkungan ke samping
    float backOffset = 0.05f;  // Seberapa jauh turun ke belakang
    float moveElapsed = 0f;

    Vector3 moveStartPos = originalPosition;
    Vector3 moveEndPos = originalPosition;
    moveEndPos.y -= backOffset;

    // --- Loop Animasi ---
    while (moveElapsed < moveDuration)
    {
        float progress = moveElapsed / moveDuration;

        // 1. Posisi turun secara linear
        Vector3 currentPos = Vector3.Lerp(moveStartPos, moveEndPos, progress);

        // 2. Tambahkan gerakan melengkung ke samping menggunakan Sin
        currentPos.x += Mathf.Sin(progress * Mathf.PI) * sideOffset;

        cardObject.transform.position = currentPos;

        moveElapsed += Time.deltaTime;
        yield return null;
    }

    // --- Finalisasi ---
    // Pastikan posisi akhir tepat dan sembunyikan objek
    cardObject.transform.position = moveEndPos;
    cardObject.SetActive(false);
    cardObject.transform.position = moveStartPos; // Kembalikan transform ke posisi semula
}
    // [BARU] Menambahkan coroutine untuk gerak maju-mundur, disalin dari RumorPhaseManager.cs

    [PunRPC]
   private void Rpc_ShowRumorCard(int rumorIndex)
{
    // Sekarang, tugas RPC ini hanya untuk memulai Coroutine di setiap client.
    StartCoroutine(AnimateCardRevealSequence(rumorIndex));
}
    private IEnumerator AnimateCardRevealSequence(int rumorIndex)
{
    if (rumorIndex < 0 || rumorIndex >= allRumorEffects.Count)
    {
        yield break; // Keluar jika indeks tidak valid
    }

    RumorEffectData effect = allRumorEffects[rumorIndex];
    
    // Cari texture yang sesuai
    Texture frontTexture = cardVisuals.FirstOrDefault(v => v.cardName == effect.cardName)?.texture;
    if (frontTexture == null)
    {
        Debug.LogWarning($"[RumorPhase] Texture untuk '{effect.cardName}' tidak ditemukan!");
        yield break;
    }

    // Tentukan GameObject dan Renderer mana yang akan digunakan
    GameObject cardToDisplay = null;
    switch (effect.color)
    {
        case Sektor.Konsumer: cardToDisplay = rumorCardKonsumer; break;
        case Sektor.Infrastruktur: cardToDisplay = rumorCardInfrastruktur; break;
        case Sektor.Keuangan: cardToDisplay = rumorCardKeuangan; break;
        case Sektor.Tambang: cardToDisplay = rumorCardTambang; break;
        case Sektor.Netral: cardToDisplay = rumorCardNetral; break;
    }

    if (cardToDisplay != null)
    {
        // === INI ADALAH LOGIKA INTI YANG ANDA INGINKAN ===
        Renderer cardRenderer = cardToDisplay.GetComponent<Renderer>();

        // 1. Jika kartu untuk sektor ini sedang aktif, sembunyikan dulu dan tunggu selesai.
        if (cardToDisplay.activeInHierarchy)
        {
            yield return StartCoroutine(HideCard(cardToDisplay));
        }

        // 2. Ganti texture-nya.
        cardRenderer.material.mainTexture = frontTexture;

        // 3. Jalankan animasi untuk membalik kartu.
        yield return StartCoroutine(FlipCard(cardToDisplay));
    }
}

    // Fungsi ini berjalan di SEMUA pemain untuk menyembunyikan visual
    [PunRPC]
    private void Rpc_HideRumorCards()
    {
        Debug.Log("[SEMUA PEMAIN] Menyembunyikan semua kartu rumor.");
        HideAllCardObjects();
    }

    // Fungsi bantuan untuk menyembunyikan semua objek kartu 3D
    private void HideAllCardObjects()
{
    // Cek setiap kartu, jika aktif, jalankan animasi HideCard
    if (rumorCardKonsumer && rumorCardKonsumer.activeInHierarchy)
        StartCoroutine(HideCard(rumorCardKonsumer));

    if (rumorCardInfrastruktur && rumorCardInfrastruktur.activeInHierarchy)
        StartCoroutine(HideCard(rumorCardInfrastruktur));

    if (rumorCardKeuangan && rumorCardKeuangan.activeInHierarchy)
        StartCoroutine(HideCard(rumorCardKeuangan));

    if (rumorCardTambang && rumorCardTambang.activeInHierarchy)
        StartCoroutine(HideCard(rumorCardTambang));

    if (rumorCardNetral && rumorCardNetral.activeInHierarchy)
        StartCoroutine(HideCard(rumorCardNetral));
}

    [PunRPC]
    private void Rpc_AnimateInsiderTradePreview(int rumorIndex)
    {
        StartCoroutine(AnimateSingleRumorCard(rumorIndex));
    }
    // COROUTINE BARU: Ini adalah inti dari fitur baru Anda.
    // Coroutine ini berjalan LOKAL di perangkat pemain untuk menjalankan semua animasi.
    private IEnumerator AnimateSingleRumorCard(int rumorIndex)
    {
        // Validasi dasar
        if (rumorIndex < 0 || rumorIndex >= allRumorEffects.Count) yield break;

        RumorEffectData effectData = allRumorEffects[rumorIndex];
        Debug.Log($"[INSIDER TRADE] Anda melihat preview animasi untuk: {effectData.description}");

        // --- Bagian 1: Gerakkan Kamera ke Target ---
        CameraController.CameraPosition targetPos = CameraController.CameraPosition.Normal;
        switch (effectData.color)
        {
            case Sektor.Konsumer: targetPos = CameraController.CameraPosition.Konsumer; break;
            case Sektor.Infrastruktur: targetPos = CameraController.CameraPosition.Infrastruktur; break;
            case Sektor.Keuangan: targetPos = CameraController.CameraPosition.Keuangan; break;
            case Sektor.Tambang: targetPos = CameraController.CameraPosition.Tambang; break;
            case Sektor.Netral: targetPos = CameraController.CameraPosition.Center; break;
        }

        if (cameraController != null)
        {
            cameraController.MoveTo(targetPos);
            yield return new WaitForSeconds(cameraController.moveDuration);
        }

        // --- Bagian 2: Tampilkan dan Animasikan Kartu 3D ---
        HideAllCardObjects();

        Texture frontTexture = cardVisuals.FirstOrDefault(v => v.cardName == effectData.cardName)?.texture;
        if (frontTexture != null)
        {
            GameObject cardToDisplay = null;
            switch (effectData.color)
            {
                case Sektor.Konsumer: cardToDisplay = rumorCardKonsumer; break;
                case Sektor.Infrastruktur: cardToDisplay = rumorCardInfrastruktur; break;
                case Sektor.Keuangan: cardToDisplay = rumorCardKeuangan; break;
                case Sektor.Tambang: cardToDisplay = rumorCardTambang; break;
                case Sektor.Netral: cardToDisplay = rumorCardNetral; break;
            }

            if (cardToDisplay != null)
            {
                Renderer cardRenderer = cardToDisplay.GetComponent<Renderer>();
                if (cardRenderer != null)
                {
                    cardRenderer.material.mainTexture = frontTexture;
                    StartCoroutine(FlipCard(cardToDisplay));

                }
            }
        }
        else
        {
            Debug.LogWarning($"[RumorPhase] Texture untuk '{effectData.cardName}' tidak ditemukan!");
        }

        // --- Bagian 3: Sembunyikan Kartu & Kembalikan Kamera ---
        HideAllCardObjects();
        yield return new WaitForSeconds(0.5f); // Beri jeda singkat

        if (cameraController != null)
        {
            cameraController.MoveTo(CameraController.CameraPosition.Normal);
            yield return new WaitForSeconds(cameraController.moveDuration);
        }

        // >> PINDAHKAN KE SINI <<
        // --- Bagian 4: Kirim Sinyal Selesai ---
        // Setelah SEMUA animasi lokal selesai, baru kirim sinyal ke MasterClient.
        if (ActionPhaseManager.Instance != null)
        {
            ActionPhaseManager.Instance.photonView.RPC("Rpc_SignalInsiderTradeAnimationComplete", RpcTarget.MasterClient);
        }

        Debug.Log($"[INSIDER TRADE] Animasi preview selesai. Mengirim sinyal ke MasterClient.");
    }
    #endregion

    // Fungsi ini HANYA berjalan di MasterClient untuk mengubah state game
    private void ApplyRumorEffect(RumorEffectData effect)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[MasterClient] Menerapkan efek RUMOR: {effect.effectType} untuk {effect.color}");

        Hashtable roomPropsToSet = new Hashtable();

        switch (effect.effectType)
        {
            case RumorType.ModifyIPO:
                // Cukup panggil fungsi terpusat. Tidak perlu logika tambahan.
                SellingPhaseManagerMultiplayer.Instance.ModifyIPOIndex(effect.color.ToString(), effect.value);
                Debug.Log($"[RUMOR IPO] Efek {effect.cardName} diterapkan pada {effect.color} sebesar {effect.value}.");
                break;

            case RumorType.ResetAllIPO:
                string[] colors = { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };
                foreach (string c in colors)
                {
                    roomPropsToSet["ipo_index_" + c] = 0;
                    roomPropsToSet["ipo_bonus_" + c] = 0;
                }
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomPropsToSet);
                Debug.Log("[RUMOR IPO] Semua harga pasar direset.");
                break;

            case RumorType.StockDilution:
                string dilutionIpoKey = "ipo_index_" + effect.color;
                int currentDilutionIndex = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(dilutionIpoKey) ? (int)PhotonNetwork.CurrentRoom.CustomProperties[dilutionIpoKey] : 0;
                roomPropsToSet[dilutionIpoKey] = currentDilutionIndex + effect.value;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomPropsToSet);
                Debug.Log($"[RUMOR IPO] {effect.color} diubah sebesar {effect.value} karena dilusi saham.");

                // --- PERBAIKAN DI SINI ---
                string dilutionCardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(effect.color.ToString());

                foreach (Player p in PhotonNetwork.PlayerList)
                {
                    if (p.CustomProperties.ContainsKey(dilutionCardKey) && (int)p.CustomProperties[dilutionCardKey] > 0)
                    {
                        int currentCards = (int)p.CustomProperties[dilutionCardKey];
                        Hashtable playerProp = new Hashtable { { dilutionCardKey, currentCards + 1 } };
                        p.SetCustomProperties(playerProp);
                        Debug.Log($"{p.NickName} mendapat 1 kartu {effect.color} tambahan karena dilusi.");
                    }
                }
                break;

            case RumorType.PenaltyInvestPoin:
                // --- PERBAIKAN DI SINI ---
                string penaltyCardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(effect.color.ToString());

                foreach (Player p in PhotonNetwork.PlayerList)
                {
                    if (p.CustomProperties.ContainsKey(penaltyCardKey))
                    {
                        int cardCount = (int)p.CustomProperties[penaltyCardKey];
                        if (cardCount > 0)
                        {
                            int penalty = cardCount * effect.value;
                            // --- PERBAIKAN NAMA VARIABEL & LOG ---
                            int currentInvestpoint = (int)p.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];
                            Hashtable playerProp = new Hashtable { { PlayerProfileMultiplayer.INVESTPOINT_KEY, currentInvestpoint - penalty } };
                            p.SetCustomProperties(playerProp);
                            Debug.Log($"{p.NickName} membayar penalti {penalty} InvestPoin.");
                        }
                    }
                }
                break;

            case RumorType.TaxByTurnOrder:
                foreach (Player p in PhotonNetwork.PlayerList)
                {
                    int turnOrder = (int)p.CustomProperties[PlayerProfileMultiplayer.TURN_ORDER_KEY];
                    int penalty = turnOrder * effect.value;
                    // --- PERBAIKAN NAMA VARIABEL & LOG ---
                    int currentInvestpoint = (int)p.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];
                    Hashtable playerProp = new Hashtable { { PlayerProfileMultiplayer.INVESTPOINT_KEY, currentInvestpoint - penalty } };
                    p.SetCustomProperties(playerProp);
                    Debug.Log($"{p.NickName} membayar pajak jalan {penalty} InvestPoin.");
                }
                break;
        }
    }

    public List<int> GetShuffledDeck()
    {
        return shuffledRumorDeckIndices;
    }

    public GameObject GetCardObjectByColor(string colorName)
    {
        switch (colorName)
        {
            case "Konsumer": return cardRed;
            case "Infrastruktur": return cardBlue;
            case "Keuangan": return cardGreen;
            case "Tambang": return cardOrange;
            default: return null;
        }
    }

    public Texture GetCardTextureByName(string cardName)
    {
        return allCardVisuals.FirstOrDefault(v => v.cardName == cardName)?.texture;
    }

    // Ubah FlipCard menjadi publik dan tambahkan parameter durasi
    
}