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
        Debug.Log("MasterClient memulai urutan rumor...");
        yield return new WaitForSeconds(2f);

        foreach (int index in shuffledRumorDeckIndices)
        {
            // Beri tahu semua pemain untuk menampilkan kartu ini
            photonView.RPC("Rpc_ShowRumorCard", RpcTarget.All, index);
            yield return new WaitForSeconds(3f); // Waktu untuk pemain membaca kartu

            // MasterClient menjalankan efeknya
            ApplyRumorEffect(allRumorEffects[index]);
            yield return new WaitForSeconds(2f); // Jeda setelah efek

            // Beri tahu semua pemain untuk menyembunyikan kartu
            photonView.RPC("Rpc_HideRumorCards", RpcTarget.All);
            yield return new WaitForSeconds(1.5f);
        }

        Debug.Log("✅ Fase Rumor Selesai. Memulai fase berikutnya...");
        if (ResolutionPhaseManagerMultiplayer.Instance != null)
        {
            ResolutionPhaseManagerMultiplayer.Instance.StartResolutionPhase();
        }
    }

    #region Visuals & Animation
    // --- MENGGUNAKAN FlipCard UNTUK OBJEK 3D ---
    private IEnumerator FlipCard(GameObject cardObject)
    {
        cardObject.SetActive(true);
        cardObject.transform.rotation = Quaternion.Euler(0, -180, 180);
        float duration = 0.5f;
        float elapsed = 0f;
        Quaternion startRot = cardObject.transform.rotation;
        Quaternion endRot = Quaternion.Euler(20, -180, 0);
        yield return new WaitForSeconds(0.5f);
        while (elapsed < duration)
        {
            cardObject.transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cardObject.transform.rotation = endRot;
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
                cardToDisplay.SetActive(true);
                StartCoroutine(FlipCard(cardToDisplay));
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
                            int currentFP = (int)p.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY];
                            Hashtable playerProp = new Hashtable { { PlayerProfileMultiplayer.FINPOINT_KEY, currentFP - penalty } };
                            p.SetCustomProperties(playerProp);
                            Debug.Log($"{p.NickName} membayar penalti {penalty} FP.");
                        }
                    }
                }
                break;

            case RumorType.TaxByTurnOrder:
                foreach (Player p in PhotonNetwork.PlayerList)
                {
                    int turnOrder = (int)p.CustomProperties[PlayerProfileMultiplayer.TURN_ORDER_KEY];
                    int penalty = turnOrder * effect.value;
                    int currentFP = (int)p.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY];
                    Hashtable playerProp = new Hashtable { { PlayerProfileMultiplayer.FINPOINT_KEY, currentFP - penalty } };
                    p.SetCustomProperties(playerProp);
                    Debug.Log($"{p.NickName} membayar pajak jalan {penalty} FP.");
                }
                break;
        }
    }
    
    public List<int> GetShuffledDeck()
    {
        return shuffledRumorDeckIndices;
    }
}