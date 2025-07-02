using UnityEngine;

[CreateAssetMenu(fileName = "New Avatar Item", menuName = "Shop/Avatar Item")]
public class AvatarItemData : ScriptableObject
{
    [Header("Info Item")]
    public string itemName; // Contoh: "Avatar Nezuko"
    public string avatarAssetName; // Nama file di folder Resources, contoh: "nezuko_avatar"
    public int price;

    [Header("Aset Visual")]
    public Sprite avatarSprite; // Sprite untuk ditampilkan di UI
}