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
            case "InsiderTrade":
                yield return InsiderTradeEffect(activator, color);
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
            default:
                Debug.LogWarning($"Efek multiplayer belum tersedia untuk kartu: {cardName}");
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
                if (p == activator) continue;
                int targetCardCount = p.CustomProperties.ContainsKey(colorCardKey) ? (int)p.CustomProperties[colorCardKey] : 0;
                if (targetCardCount > 0 && targetCardCount < activatorCardCount)
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

            // --- Bagian 2: Penyesuaian Harga (Logika dari Single-player) ---
            // Ini adalah logika yang disederhanakan untuk multiplayer.
            // Kita akan menurunkan indeks IPO sebanyak 2 level.
            string ipoIndexKey = "ipo_index_" + color;
            int currentIndex = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ipoIndexKey) ? (int)PhotonNetwork.CurrentRoom.CustomProperties[ipoIndexKey] : 0;

            int newIndex = currentIndex - 2; // Turun 2 level sebagai efek stock split

            // Batasi agar tidak lebih rendah dari -3 (atau -2 untuk Tambang)
            int minIndex = (color == Sektor.Tambang) ? -2 : -3;
            newIndex = Mathf.Max(newIndex, minIndex);

            Hashtable ipoProp = new Hashtable { { ipoIndexKey, newIndex } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(ipoProp);
            Debug.Log($"[Stock Split] Harga pasar {color} diturunkan. Indeks baru: {newIndex}");
            ActionPhaseManager.Instance.ForceNextTurn();
        }
        yield return new WaitForSeconds(1f);
    }

    private static IEnumerator InsiderTradeEffect(Player activator, Sektor color)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[Insider Trade] MasterClient mencari rumor AKURAT untuk sektor {color}...");
            
            var rumorManager = RumorPhaseManagerMultiplayer.Instance;
            var deck = rumorManager.GetShuffledDeck();

            if (deck != null && deck.Count > 0)
            {
                // Cari data rumor yang sesuai dari dek yang diacak
                RumorEffectData secretRumor = null;
                foreach(int index in deck)
                {
                    if (rumorManager.allRumorEffects[index].color == color)
                    {
                        secretRumor = rumorManager.allRumorEffects[index];
                        break;
                    }
                }

                if (secretRumor != null)
                {
                    Debug.Log($"[Insider Trade] Rumor ditemukan: {secretRumor.cardName}. Mengirim RPC ke {activator.NickName} untuk animasi.");
                    // Kirim NAMA KARTU dan WARNA untuk keperluan visual
                    ActionPhaseManager.Instance.photonView.RPC(
                        "Rpc_ShowInsiderTradePrediction", // Nama RPC baru yang lebih deskriptif
                        activator, 
                        secretRumor.cardName, 
                        secretRumor.color.ToString()
                    );
                    // PENTING: MasterClient berhenti di sini. Giliran akan dilanjutkan
                    // setelah menerima RPC konfirmasi dari klien.
                }
                else
                {
                    Debug.LogWarning($"[Insider Trade] Tidak ada rumor untuk sektor {color}. Giliran langsung dilanjutkan.");
                    // Jika tidak ada rumor, tidak ada animasi, maka MasterClient harus melanjutkan giliran secara manual.
                    ActionPhaseManager.Instance.ForceNextTurn();
                }
            }
        }
        yield return null; // Coroutine harus selalu yield sesuatu.
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