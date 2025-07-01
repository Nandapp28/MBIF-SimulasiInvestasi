using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CardMultiplayer : MonoBehaviourPunCallbacks
{
    public string cardName;
    public string description;
    public int value;
    public string color; // 🔴 Tambahkan warna

    public CardMultiplayer(string name, string desc, int val = 0, string color = "Red")
    {
        cardName = name;
        description = desc;
        value = val;
        this.color = color;
    }
}
