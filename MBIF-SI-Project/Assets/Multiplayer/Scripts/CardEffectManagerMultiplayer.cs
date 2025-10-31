// File: CardEffectManagerMultiplayer.cs (Versi Adaptasi Awal)
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Diperlukan untuk LINQ seperti Select
using ExitGames.Client.Photon; // Diperlukan untuk Hashtable

using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class CardEffectManagerMultiplayer
{
    // Pintu gerbang utama, akan dipanggil oleh MasterClient
    public static IEnumerator ApplyEffect(string cardName, Player activator, Sektor color)
    {
        Debug.Log($"🧪 [MULTIPLAYER] Menjalankan efek untuk kartu: {cardName} [{color}]");

        switch (cardName)
        {
            case "StockSplit":
                yield return StockSplitEffect(activator, color);
                break;
            case "TenderOffer":
                yield return TenderOfferEffect(activator, color);
                break;
            case "TradeFee":
                yield return TradeFeeEffect(activator, color);
                break;
            case "Flashbuy":
                yield return FlashbuyEffect(activator);
                break;
            case "InsiderTrade":
                yield return InsiderTradeEffect(activator, color);
                break;
            default:
                Debug.LogWarning($"Efek multiplayer belum tersedia untuk kartu: {cardName}");
                ActionPhaseManager.Instance.ForceNextTurn();
                yield break;
        }
    }

    private static IEnumerator TenderOfferEffect(Player activator, Sektor color)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[Tender Offer] {activator.NickName} mengaktifkan untuk sektor {color}. Mencari target...");

            // --- PERBAIKAN 1: Menggunakan nama fungsi yang benar ---
            string colorCardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(color.ToString());
            if (string.IsNullOrEmpty(colorCardKey)) yield break;
            
            int activatorCardCount = activator.CustomProperties.ContainsKey(colorCardKey) ? (int)activator.CustomProperties[colorCardKey] : 0;
            
            List<Player> validTargets = new List<Player>();
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p == activator) continue; // Tidak bisa menargetkan diri sendiri

                int targetCardCount = p.CustomProperties.ContainsKey(colorCardKey) ? (int)p.CustomProperties[colorCardKey] : 0;

                if (targetCardCount > 0)
                {
                    validTargets.Add(p);
                }
            }

            if (validTargets.Count > 0)
            {
                int[] validTargetActorNumbers = validTargets.Select(p => p.ActorNumber).ToArray();
                
                // Kirim RPC ke pengaktif untuk meminta mereka memilih target
                ActionPhaseManager.Instance.photonView.RPC(
                    "Rpc_RequestTenderOfferTarget", 
                    activator, 
                    validTargetActorNumbers, 
                    color.ToString() // <-- TAMBAHKAN INI: Kirim warna kartunya
                );
            }
            else
            {
                Debug.LogWarning($"[Tender Offer] Tidak ada target yang valid untuk {activator.NickName}.");
                ActionPhaseManager.Instance.ForceNextTurn();
            }
        }
        yield return null;
    }

    private static IEnumerator TradeFeeEffect(Player activator, Sektor color)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            string colorCardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(color.ToString());
            if (string.IsNullOrEmpty(colorCardKey)) yield break;
            
            // Cek berapa banyak kartu yang dimiliki pemain
            int cardsOwned = activator.CustomProperties.ContainsKey(colorCardKey) ? (int)activator.CustomProperties[colorCardKey] : 0;
            
            if (cardsOwned > 0)
            {
                // Kirim RPC ke pemain untuk meminta input
                ActionPhaseManager.Instance.photonView.RPC(
                    "Rpc_RequestTradeFeeInput",
                    activator, // Target RPC
                    color.ToString(), // Data 1: Warna kartu
                    cardsOwned      // Data 2: Jumlah maksimal yang bisa dijual
                );
            }
            else
            {
                Debug.LogWarning($"[Trade Fee] {activator.NickName} tidak punya kartu sektor {color} untuk dijual.");
                // Karena tidak ada aksi, majukan giliran secara manual
                ActionPhaseManager.Instance.ForceNextTurn();
            }
        }
        yield return null;
    }
    
    private static IEnumerator FlashbuyEffect(Player activator)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[Flashbuy] {activator.NickName} mengaktifkan Flashbuy. Memulai mode pemilihan kartu...");
            // Beri tahu semua pemain untuk masuk ke mode pemilihan Flashbuy
            // dan kirim ID pemain yang bisa memilih.
            ActionPhaseManager.Instance.photonView.RPC(
                "Rpc_StartFlashbuyMode",
                RpcTarget.All, // Kirim ke semua untuk sinkronisasi visual dan UI
                activator.ActorNumber 
            );

            // PENTING: JANGAN LANGSUNG PANGGIL AdvanceToNextTurn() DI SINI.
            // Giliran akan dilanjutkan setelah pemain pengaktif mengirimkan pilihannya
            // melalui Rpc_SubmitFlashbuyChoices di ActionPhaseManager.
            yield return null; // Kembali ke dispatcher
        }
    }

    private static IEnumerator StockSplitEffect(Player activator, Sektor color)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[Stock Split] Menggandakan kartu dan menyesuaikan harga untuk sektor '{color}'.");

            // --- Bagian 1: Duplikasi Kartu (Logika dari Multiplayer) ---
            string cardColorKey = PlayerProfileMultiplayer.GetCardKeyFromColor(color.ToString());
            if (string.IsNullOrEmpty(cardColorKey)) yield break;

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                int currentCardCount = p.CustomProperties.ContainsKey(cardColorKey) ? (int)p.CustomProperties[cardColorKey] : 0;
                if (currentCardCount > 0)
                {
                    Hashtable propsToSet = new Hashtable { { cardColorKey, currentCardCount * 2 } };
                    p.SetCustomProperties(propsToSet);
                    Debug.Log($"Pemain {p.NickName} kartu '{color}'-nya digandakan menjadi {currentCardCount * 2}.");
                }
            }

            SellingPhaseManagerMultiplayer.Instance.ModifyIPOIndex(color.ToString(), -2);
            Debug.Log($"[Stock Split] Harga pasar {color} diturunkan 2 level.");
            yield return new WaitForSeconds(3.0f); 
            
            // Panggil giliran berikutnya HANYA SETELAH jeda
            ActionPhaseManager.Instance.ForceNextTurn();
            // --- AKHIR MODIFIKASI ---
        }
        else
        {
            // --- MODIFIKASI --- Pastikan coroutine non-MasterClient juga keluar
            yield return null;
        }
        // Hapus 'yield return new WaitForSeconds(1f);' yang lama
    }

    private static IEnumerator InsiderTradeEffect(Player activator, Sektor color)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[Insider Trade] {activator.NickName} mengintip rumor untuk sektor {color}.");

            // >> TAMBAHKAN INI <<
            // LANGKAH 1: Perintahkan SEMUA pemain untuk menyembunyikan UI Fase Aksi.
            ActionPhaseManager.Instance.photonView.RPC("Rpc_SetActionPhaseUIVisibility", RpcTarget.All, false);

            // Logika untuk menemukan kartu yang benar (ini sudah benar)
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("nextRumorDeck", out object deckIndicesObj))
            {
                int[] rumorIndices = (int[])deckIndicesObj;
                int sectorIndex = -1;
                switch (color)
                {
                    case Sektor.Konsumer: sectorIndex = 0; break;
                    case Sektor.Infrastruktur: sectorIndex = 1; break;
                    case Sektor.Keuangan: sectorIndex = 2; break;
                    case Sektor.Tambang: sectorIndex = 3; break;
                }

                if (sectorIndex != -1 && rumorIndices.Length > sectorIndex)
                {
                    int cardToShowIndex = rumorIndices[sectorIndex];

                    // LANGKAH 2: Kirim perintah animasi HANYA ke pemain pengaktif.
                    if (RumorPhaseManagerMultiplayer.Instance != null)
                    {
                        RumorPhaseManagerMultiplayer.Instance.photonView.RPC(
                            "Rpc_AnimateInsiderTradePreview",
                            activator,
                            cardToShowIndex
                        );
                    }
                }
            }
            else
            {
                Debug.LogError("[Insider Trade] Gagal: 'nextRumorDeck' tidak ditemukan.");
                // Fallback: Jika gagal, tampilkan UI lagi agar permainan tidak macet.
                ActionPhaseManager.Instance.photonView.RPC("Rpc_SetActionPhaseUIVisibility", RpcTarget.All, true);
                ActionPhaseManager.Instance.ForceNextTurn();
            }
        }
        yield return null;
    }

    // Fungsi bantuan untuk mengubah enum Sektor menjadi string kunci
    private static string GetCardKeyFromSektor(Sektor sektor)
    {
        switch (sektor)
        {
            case Sektor.Konsumer: return PlayerProfileMultiplayer.KONSUMER_CARDS_KEY;
            case Sektor.Infrastruktur: return PlayerProfileMultiplayer.INFRASTRUKTUR_CARDS_KEY;
            case Sektor.Keuangan: return PlayerProfileMultiplayer.KEUANGAN_CARDS_KEY;
            case Sektor.Tambang: return PlayerProfileMultiplayer.TAMBANG_CARDS_KEY;
            default: return null;
        }
    }
}