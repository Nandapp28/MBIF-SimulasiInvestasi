using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Ubah warisan menjadi MonoBehaviourPunCallbacks agar bisa terima update realtime
public class PlayerTabManagerMultiplayer : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject playerTabPanel;
    public Button togglePlayerTabButton;
    public Button closeButton;
    public Button sortButton;
    public Transform playerListContainer;
    public GameObject playerInfoPrefab;

    [Header("Local Player Visual")]
    public string localIndicatorChildName = "LocalPlayerIndicator"; 

    // Referensi Manager
    private SellingPhaseManagerMultiplayer sellingManager;

    private bool isPanelActive = false;
    private bool isSortedByWorth = false;

    void Start()
    {
        // Cari SellingPhaseManager untuk harga saham
        if (SellingPhaseManagerMultiplayer.Instance != null)
        {
            sellingManager = SellingPhaseManagerMultiplayer.Instance;
        }

        if (playerTabPanel != null) playerTabPanel.SetActive(false);

        if (togglePlayerTabButton != null)
            togglePlayerTabButton.onClick.AddListener(TogglePlayerTab);

        if (closeButton != null)
            closeButton.onClick.AddListener(TogglePlayerTab);

        if (sortButton != null)
            sortButton.onClick.AddListener(ToggleSort);
    }

    // --- BAGIAN PENTING 1: UPDATE OTOMATIS SAAT DATA BERUBAH ---
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Jika panel sedang terbuka, refresh list agar angkanya sinkron realtime
        if (isPanelActive)
        {
            UpdatePlayerList();
        }
    }
    // ------------------------------------------------------------

    public void TogglePlayerTab()
    {
        isPanelActive = !isPanelActive;
        if (playerTabPanel != null) playerTabPanel.SetActive(isPanelActive);

        if (isPanelActive)
        {
            UpdatePlayerList();
        }
    }

    private void ToggleSort()
    {
        isSortedByWorth = !isSortedByWorth;
        UpdatePlayerList();
    }

    public void UpdatePlayerList()
    {
        if (sellingManager == null) sellingManager = SellingPhaseManagerMultiplayer.Instance;

        // Bersihkan list lama
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        Player[] allPlayers = PhotonNetwork.PlayerList;

        var playersWithData = allPlayers.Select(player => {
            var props = player.CustomProperties;

            // --- BAGIAN PENTING 2: MAPPING YANG BENAR ---
            // Pastikan Key ini SAMA PERSIS dengan PlayerProfileMultiplayer.cs
            // Jika salah satu salah, data akan masuk ke sektor yang salah.
            
            // Konsumer (Merah)
            int valKonsumer = props.ContainsKey(PlayerProfileMultiplayer.KONSUMER_CARDS_KEY) ? (int)props[PlayerProfileMultiplayer.KONSUMER_CARDS_KEY] : 0;
            
            // Infrastruktur (Oranye)
            int valInfra = props.ContainsKey(PlayerProfileMultiplayer.INFRASTRUKTUR_CARDS_KEY) ? (int)props[PlayerProfileMultiplayer.INFRASTRUKTUR_CARDS_KEY] : 0;
            
            // Keuangan (Biru)
            int valKeuangan = props.ContainsKey(PlayerProfileMultiplayer.KEUANGAN_CARDS_KEY) ? (int)props[PlayerProfileMultiplayer.KEUANGAN_CARDS_KEY] : 0;
            
            // Tambang (Hijau)
            int valTambang = props.ContainsKey(PlayerProfileMultiplayer.TAMBANG_CARDS_KEY) ? (int)props[PlayerProfileMultiplayer.TAMBANG_CARDS_KEY] : 0;

            int investPoint = props.ContainsKey(PlayerProfileMultiplayer.INVESTPOINT_KEY) ? (int)props[PlayerProfileMultiplayer.INVESTPOINT_KEY] : 0;
            int turnOrder = props.ContainsKey(PlayerProfileMultiplayer.TURN_ORDER_KEY) ? (int)props[PlayerProfileMultiplayer.TURN_ORDER_KEY] : 0;

            // Hitung Aset
            int assetValue = 0;
            if (sellingManager != null)
            {
                assetValue += valKonsumer * sellingManager.GetFullCardPrice("Konsumer");
                assetValue += valInfra * sellingManager.GetFullCardPrice("Infrastruktur");
                assetValue += valKeuangan * sellingManager.GetFullCardPrice("Keuangan");
                assetValue += valTambang * sellingManager.GetFullCardPrice("Tambang");
            }
            int totalWorth = investPoint + assetValue;

            return new
            {
                Player = player,
                NickName = player.NickName,
                TurnOrder = turnOrder,
                InvestPoint = investPoint,
                Konsumer = valKonsumer,
                Infra = valInfra,
                Keuangan = valKeuangan,
                Tambang = valTambang,
                AssetValue = assetValue,
                TotalWorth = totalWorth,
                IsLocal = player.IsLocal
            };
        }).AsEnumerable();

        // Sorting Logic
        if (isSortedByWorth)
        {
            playersWithData = playersWithData.OrderByDescending(p => p.TotalWorth);
        }
        else
        {
            playersWithData = playersWithData.OrderBy(p => p.TurnOrder == 0 ? 99 : p.TurnOrder).ThenBy(p => p.NickName);
        }

        // Render ke UI
        foreach (var data in playersWithData)
        {
            GameObject entryObj = Instantiate(playerInfoPrefab, playerListContainer);

            SetTextComponent(entryObj, "NameText", data.NickName);
            SetTextComponent(entryObj, "ScoreText", data.TurnOrder.ToString());
            SetTextComponent(entryObj, "Finpoint", data.InvestPoint.ToString());

            // --- BAGIAN PENTING 3: MAPPING KE NAMA TEXT DI PREFAB ---
            // Pastikan nama string di sini ("RedCardText" dll) SAMA dengan nama GameObject Text di Unity Editor Anda
            
            // Konsumer -> Red
            SetTextComponent(entryObj, "RedCardText", data.Konsumer.ToString());
            
            // Infrastruktur -> Orange (Sering tertukar dengan Blue di beberapa versi, pastikan ini benar)
            SetTextComponent(entryObj, "OrangeCardText", data.Infra.ToString());
            
            // Keuangan -> Blue (Di PlayerProfile: BlueCardText = Keuangan)
            SetTextComponent(entryObj, "BlueCardText", data.Keuangan.ToString());
            
            // Tambang -> Green (Di PlayerProfile: GreenCardText = Tambang)
            SetTextComponent(entryObj, "GreenCardText", data.Tambang.ToString()); 

            SetTextComponent(entryObj, "AssetValueText", data.AssetValue.ToString());
            SetTextComponent(entryObj, "TotalWorthText", data.TotalWorth.ToString());

            // Indikator Diri Sendiri
            Transform localIndicator = entryObj.transform.Find(localIndicatorChildName);
            if (localIndicator != null)
            {
                localIndicator.gameObject.SetActive(data.IsLocal);
            }
        }
    }

    private bool SetTextComponent(GameObject parentObject, string childName, string value)
    {
        Transform childTransform = parentObject.transform.Find(childName);
        if (childTransform == null)
        {
            // Debug.LogWarning($"Object '{childName}' tidak ditemukan di prefab PlayerInfo."); 
            return false;
        }

        TextMeshProUGUI textComponent = childTransform.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = value;
            return true;
        }
        return false;
    }
}