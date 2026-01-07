// File: Script/GlobalUserData.cs
using UnityEngine;

public static class GlobalUserData
{
    // Menyimpan data user di memori agar bisa diakses instan antar scene
    public static DataToSave cachedData = null;

    // Helper untuk mengecek apakah kita punya data
    public static bool HasData => cachedData != null;
    public static void Clear()
    {
        cachedData = null;
    }
}