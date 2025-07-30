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
    public List<RumorEffectData> allRumorEffects; // Isi di Inspector dengan semua kemungkinan rumor

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

    [Header("Posisi Spesial")]
    public Transform predictionCardStage;

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
    }

    // Fungsi ini dipanggil dari SellingPhaseManager
    public void StartRumorPhase(List<Player> players)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient memulai Fase Rumor...");
            InitializeRumorDeck();
        }
    }

    private void InitializeRumorDeck()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        List<int> finalDeckIndices = new List<int>();

        // 1. Tentukan urutan sektor yang kita inginkan
        var SektorOrder = new Sektor[] { Sektor.Konsumer, Sektor.Infrastruktur, Sektor.Keuangan, Sektor.Tambang };

        // 2. Pilih satu rumor acak dari setiap sektor utama
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

        // 3. Cek apakah ada kartu ResetAllIPO di antara yang terpilih.
        int resetEffectIndex = finalDeckIndices.FindIndex(i => allRumorEffects[i].effectType == RumorType.ResetAllIPO);

        // Jika ada, ganti dengan kartu Netral (non-reset) acak.
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

        // Pastikan kita mengirim tepat 4 kartu
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
                case Sektor.Konsumer:      targetPos = CameraController.CameraPosition.Konsumer; break;
                case Sektor.Infrastruktur: targetPos = CameraController.CameraPosition.Infrastruktur; break;
                case Sektor.Keuangan:      targetPos = CameraController.CameraPosition.Keuangan; break;
                case Sektor.Tambang:       targetPos = CameraController.CameraPosition.Tambang; break;
                // Untuk Netral, kamera bisa tetap di Normal atau pindah ke tengah (Center)
                case Sektor.Netral:        targetPos = CameraController.CameraPosition.Center; break;
            }

            // 2. Perintahkan SEMUA client untuk menggerakkan kamera mereka ke target
            photonView.RPC("Rpc_MoveCamera", RpcTarget.All, targetPos);
            // Tunggu animasi kamera selesai sebelum melanjutkan
            float waitDuration = (cameraController != null) ? cameraController.moveDuration : 0.8f;
            yield return new WaitForSeconds(waitDuration);

            // 3. Tampilkan kartu rumor di perangkat semua pemain
            photonView.RPC("Rpc_ShowRumorCard", RpcTarget.All, index);
            yield return new WaitForSeconds(3f); // Waktu untuk pemain membaca kartu

            // 4. MasterClient menerapkan efek dari rumor
            ApplyRumorEffect(effectData);
            yield return new WaitForSeconds(2f); // Jeda setelah efek diterapkan

            // 5. Sembunyikan kartu rumor di perangkat semua pemain
            photonView.RPC("Rpc_HideRumorCards", RpcTarget.All);
            yield return new WaitForSeconds(1.0f);

            // 6. Perintahkan SEMUA client untuk mengembalikan kamera ke posisi Normal
            photonView.RPC("Rpc_MoveCamera", RpcTarget.All, CameraController.CameraPosition.Normal);
            // Tunggu animasi kamera kembali selesai
            yield return new WaitForSeconds(waitDuration);
        }

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
                string ipoIndexKey = "ipo_index_" + effect.color;
                int currentIndex = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ipoIndexKey) ? (int)PhotonNetwork.CurrentRoom.CustomProperties[ipoIndexKey] : 0;
                int newIndex = currentIndex + effect.value;

                roomPropsToSet[ipoIndexKey] = newIndex;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomPropsToSet);
                Debug.Log($"[RUMOR IPO] {effect.color} diubah sebesar {effect.value}. Indeks baru: {newIndex}");
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

            case RumorType.PenaltyFinpoint:
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