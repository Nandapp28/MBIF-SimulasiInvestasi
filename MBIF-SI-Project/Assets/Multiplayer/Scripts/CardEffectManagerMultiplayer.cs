using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CardEffectManagerMultiplayer
{
    // Untuk sekarang, fungsi ini tidak melakukan apa-apa selain memberi pesan debug.
    // Semua kartu akan diperlakukan sama oleh MultiplayerManager.
    public static void ApplyEffect(string cardName, PlayerProfile player, string color)
    {
        // Pesan ini hanya akan muncul jika ada bagian dari kode yang masih memanggilnya.
        // Dalam logika sederhana kita, ini seharusnya tidak terjadi.
        Debug.Log($"ApplyEffect dipanggil untuk '{cardName}', tetapi semua efek dinonaktifkan sementara.");
    }
}
