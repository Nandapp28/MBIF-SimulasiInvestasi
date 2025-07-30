// File: HistoryManager.cs

using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro; 

public class HistoryManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject historyListPopup;
    public GameObject historyDetailPopup;
    public Button openHistoryButton;

    [Header("History List")]
    public GameObject historyEntryPrefab;
    public Transform historyListContainer;

    [Header("History Detail")]
    public GameObject detailEntryPrefab;
    public Transform detailListContainer;

    private DatabaseReference dbReference;
    private FirebaseAuth auth;
    private string currentUserId;
    private string localPlayerId;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (auth.CurrentUser != null)
        {
            currentUserId = auth.CurrentUser.UserId;
            openHistoryButton.onClick.AddListener(OnOpenHistoryClicked);
        }
        else
        {
            Debug.LogError("User belum login!");
            openHistoryButton.interactable = false;
        }
    }

    public async void OnOpenHistoryClicked()
    {
        var userProfileSnapshot = await dbReference.Child("users").Child(currentUserId).Child("playerId").GetValueAsync();
        if (!userProfileSnapshot.Exists)
        {
            Debug.LogError("Gagal mendapatkan playerId dari profil user.");
            return;
        }
        localPlayerId = userProfileSnapshot.Value.ToString();

        historyListPopup.SetActive(true);
        foreach (Transform child in historyListContainer)
        {
            Destroy(child.gameObject);
        }

        var snapshot = await dbReference.Child("playerMatchHistories").Child(currentUserId).GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.Log("Tidak ada riwayat pertandingan.");
            return;
        }

        List<string> matchIds = new List<string>();
        foreach (var child in snapshot.Children)
        {
            matchIds.Add(child.Key);
        }

        matchIds.Reverse(); // history yang paling baru akan selalu berada di list paling atas

        foreach (string matchId in matchIds)
        {
            await FetchAndDisplayHistoryEntry(matchId);
        }
    }

    private async Task FetchAndDisplayHistoryEntry(string matchId)
    {
        var matchSnapshot = await dbReference.Child("matchHistories").Child(matchId).GetValueAsync();
        if (!matchSnapshot.Exists) return;

        long timestamp = 0;
        int myRank = 0;

        if (matchSnapshot.Child("timestamp").Exists)
        {
            timestamp = long.Parse(matchSnapshot.Child("timestamp").Value.ToString());
        }

        foreach (var playerEntry in matchSnapshot.Child("players").Children)
        {
            if (playerEntry.Child("playerId").Value.ToString() == localPlayerId)
            {
                myRank = int.Parse(playerEntry.Child("rank").Value.ToString());
                break;
            }
        }

        if (myRank > 0)
        {
            GameObject entryGO = Instantiate(historyEntryPrefab, historyListContainer);
            // --- PERBAIKAN DI SINI ---
            // Menggunakan TextMeshProUGUI bukan Text
            entryGO.GetComponentInChildren<TextMeshProUGUI>().text = $"Rank - {myRank} ( {System.DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime:dd MMM yyyy} )";
            entryGO.GetComponent<Button>().onClick.AddListener(() => OnHistoryEntryClicked(matchId));
        }
    }

    public async void OnHistoryEntryClicked(string matchId)
    {
        historyDetailPopup.SetActive(true);
        foreach (Transform child in detailListContainer)
        {
            Destroy(child.gameObject);
        }

        var matchSnapshot = await dbReference.Child("matchHistories").Child(matchId).GetValueAsync();
        if (!matchSnapshot.Exists) return;
        
        List<DataSnapshot> sortedPlayers = new List<DataSnapshot>();
        foreach (var player in matchSnapshot.Child("players").Children)
        {
            sortedPlayers.Add(player);
        }
        sortedPlayers.Sort((a, b) =>
            int.Parse(a.Child("rank").Value.ToString()).CompareTo(int.Parse(b.Child("rank").Value.ToString()))
        );

        foreach (var playerEntry in sortedPlayers)
        {
            string rank = playerEntry.Child("rank").Value.ToString();
            string userName = playerEntry.Child("userName").Value.ToString();
            string investPoin = playerEntry.Child("investPoin").Value.ToString();

            GameObject detailEntryGO = Instantiate(detailEntryPrefab, detailListContainer);
            // --- PERBAIKAN DI SINI JUGA ---
            // Menggunakan TextMeshProUGUI bukan Text
            TextMeshProUGUI[] texts = detailEntryGO.GetComponentsInChildren<TextMeshProUGUI>();
            texts[0].text = rank;
            texts[1].text = userName;
            texts[2].text = $"{investPoin} IP";
        }
    }
    
    public void CloseHistoryListPopup()
    {
        historyListPopup.SetActive(false);
    }

    public void CloseDetailPopup()
    {
        historyDetailPopup.SetActive(false);
    }
}