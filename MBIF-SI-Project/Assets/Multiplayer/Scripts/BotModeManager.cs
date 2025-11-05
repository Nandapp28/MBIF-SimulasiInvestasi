// File: Scripts/BotModeManager.cs
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BotModeManager : MonoBehaviourPunCallbacks
{
    public static BotModeManager Instance;

    [Header("UI References")]
    public GameObject botModePanel; // Panel/Tombol yang muncul saat mode bot aktif
    public Button returnToManualButton; // Tombol di dalam panel itu

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); } else { Instance = this; }
    }

    void Start()
    {
        if (botModePanel != null)
        {
            botModePanel.SetActive(false); // Sembunyikan di awal
        }

        if (returnToManualButton != null)
        {
            returnToManualButton.onClick.AddListener(OnReturnToManualClicked);
        }

        // Cek status saat ini jika kita baru bergabung/load
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
        {
            UpdateUI(PhotonNetwork.LocalPlayer.CustomProperties);
        }
    }

    // Fungsi ini dipanggil oleh manajer lain (Action, Bidding, Selling) saat timer habis
    public static void SetBotMode(bool isBot)
    {
        if (!PhotonNetwork.IsConnected) return;

        Debug.Log($"[BotModeManager] Mengatur mode bot ke: {isBot}");
        Hashtable props = new Hashtable { { PlayerProfileMultiplayer.IS_BOT_MODE_KEY, isBot } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // Dipanggil saat tombol 'Kembali Manual' diklik
    private void OnReturnToManualClicked()
    {
        SetBotMode(false);
    }

    // Otomatis memonitor perubahan properti
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Hanya peduli pada perubahan properti milik pemain lokal
        if (targetPlayer != null && targetPlayer.IsLocal)
        {
            UpdateUI(changedProps);
        }
    }

    // Fungsi terpusat untuk mengupdate UI berdasarkan properti
    private void UpdateUI(Hashtable props)
    {
        if (props.ContainsKey(PlayerProfileMultiplayer.IS_BOT_MODE_KEY))
        {
            bool isBot = (bool)props[PlayerProfileMultiplayer.IS_BOT_MODE_KEY];
            if (botModePanel != null)
            {
                botModePanel.SetActive(isBot);
            }
        }
    }
}