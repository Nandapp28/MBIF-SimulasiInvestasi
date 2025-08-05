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

            // 4. SEMBUNYIKAN kartu rumor terlebih dahulu
            photonView.RPC("Rpc_HideRumorCards", RpcTarget.All); // <<< PINDAHKAN KE SINI
            yield return new WaitForSeconds(0.5f); // Beri jeda singkat agar kartu sempat hilang

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
        cardObject.SetActive(true);

        // Mulai dari kondisi terbalik
        cardObject.transform.rotation = Quaternion.Euler(0, -180, 180);

        float duration = 0.5f;
        float elapsed = 0f;

        Quaternion startRot = cardObject.transform.rotation;
        // Rotasi akhir diubah dari (20, -180, 0) menjadi (0, -180, 0) agar lurus
        Quaternion endRot = Quaternion.Euler(0, -180, 0);

        yield return new WaitForSeconds(0.5f); // Jeda sejenak sebelum animasi

        while (elapsed < duration)
        {
            cardObject.transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cardObject.transform.rotation = endRot;
    }

    // [BARU] Menambahkan coroutine untuk gerak maju-mundur, disalin dari RumorPhaseManager.cs
    public IEnumerator MoveObjectToTargetAndBack(GameObject objectToMove)
    {
        if (objectToMove == null)
        {
            yield break;
        }

        // [BARU] Simpan posisi dan SKALA awal kartu
        Vector3 originalPosition = objectToMove.transform.position;
        Vector3 originalScale = objectToMove.transform.localScale;

        // [BARU] Definisikan posisi dan SKALA target saat kartu di depan kamera
        Vector3 targetPosition = originalPosition + new Vector3(-1.96f, 2.72f, 0f);
        // Kartu akan menjadi 70% dari ukuran aslinya. Anda bisa mengubah nilai 0.7f ini.
        // Contoh: 0.5f untuk setengah ukuran, 1.0f untuk ukuran asli.
        Vector3 targetScale = originalScale * 0.7f;

        float moveDuration = 1f;    // Durasi pergerakan
        float waitDuration = 3f;    // Durasi kartu diam di depan kamera
        float elapsedTime = 0f;

        // --- Pergerakan ke Posisi Target (Posisi & Skala) ---
        while (elapsedTime < moveDuration)
        {
            // Animasikan posisi
            objectToMove.transform.position = Vector3.Lerp(originalPosition, targetPosition, elapsedTime / moveDuration);

            // [BARU] Animasikan skala secara bersamaan
            objectToMove.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / moveDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // Pastikan posisi dan skala tepat di tujuan
        objectToMove.transform.position = targetPosition;
        objectToMove.transform.localScale = targetScale;

        // --- Tunggu ---
        yield return new WaitForSeconds(waitDuration);

        // --- Pergerakan Kembali ke Posisi Awal (Posisi & Skala) ---
        elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            // Animasikan posisi kembali
            objectToMove.transform.position = Vector3.Lerp(targetPosition, originalPosition, elapsedTime / moveDuration);

            // [BARU] Animasikan skala kembali ke ukuran aslinya
            objectToMove.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / moveDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // Pastikan kartu kembali tepat ke posisi dan skala semula
        objectToMove.transform.position = originalPosition;
        objectToMove.transform.localScale = originalScale;
    }

    [PunRPC]
    private void Rpc_ShowRumorCard(int rumorIndex)
    {
        if (rumorIndex < 0 || rumorIndex >= allRumorEffects.Count) return;

        RumorEffectData effect = allRumorEffects[rumorIndex];
        Debug.Log($"[SEMUA PEMAIN] Menampilkan rumor: {effect.description} ({effect.color})");

        HideAllCardObjects(); // Sembunyikan dulu kartu sebelumnya

        // Cari texture yang sesuai
        Texture frontTexture = cardVisuals.FirstOrDefault(v => v.cardName == effect.cardName)?.texture;
        if (frontTexture == null)
        {
            Debug.LogWarning($"[RumorPhase] Texture untuk cardName '{effect.cardName}' tidak ditemukan!");
            return;
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
            Renderer cardRenderer = cardToDisplay.GetComponent<Renderer>();
            if (cardRenderer != null)
            {
                cardRenderer.material.mainTexture = frontTexture;
                StartCoroutine(FlipCard(cardToDisplay));
                StartCoroutine(MoveObjectToTargetAndBack(cardToDisplay));
            }
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
        if (rumorCardKonsumer) rumorCardKonsumer.SetActive(false);
        if (rumorCardInfrastruktur) rumorCardInfrastruktur.SetActive(false);
        if (rumorCardKeuangan) rumorCardKeuangan.SetActive(false);
        if (rumorCardTambang) rumorCardTambang.SetActive(false);
        if (rumorCardNetral) rumorCardNetral.SetActive(false);
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
                    // TUNGGU SAMPAI ANIMASI KARTU SELESAI
                    yield return StartCoroutine(MoveObjectToTargetAndBack(cardToDisplay));
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
    public IEnumerator FlipCard(GameObject cardObject, float duration)
    {
        cardObject.SetActive(true);
        cardObject.transform.rotation = Quaternion.Euler(0, -180, 180); // Mulai dari terbalik

        float elapsed = 0f;
        Quaternion startRot = cardObject.transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, -180, 0); // Menghadap depan

        yield return new WaitForSeconds(0.5f); 

        while (elapsed < duration)
        {
            cardObject.transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cardObject.transform.rotation = endRot;
    }
}