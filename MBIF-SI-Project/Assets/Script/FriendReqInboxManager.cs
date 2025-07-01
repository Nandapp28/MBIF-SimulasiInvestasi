using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections; // MODIFIKASI: Diperlukan untuk IEnumerator
using System.Collections.Generic;
using System.Threading.Tasks;

public class FriendReqInboxManager : MonoBehaviour
{
    [Header("UI Referensi")]
    public GameObject inboxPopupPanel;
    public Transform requestListParent;
    public GameObject requestItemPrefab;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private string currentUserUID;
    private Dictionary<string, GameObject> instantiatedRequests = new Dictionary<string, GameObject>();


    IEnumerator Start()
    {
        yield return new WaitUntil(() => FirebaseInitializer.Instance != null && FirebaseInitializer.Instance.IsFirebaseReady);
        
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser == null) {
            Debug.LogError("User belum login.");
            yield break; // Gunakan yield break karena ini Coroutine
        }
        currentUserUID = auth.CurrentUser.UserId;

        inboxPopupPanel.SetActive(false);
        StartListeningForRequests();
    }

    public void OpenInboxPopup() { inboxPopupPanel.SetActive(true); }
    public void CloseInboxPopup() { inboxPopupPanel.SetActive(false); }

    private void StartListeningForRequests()
    {
        dbRef.Child("friendRequests").Child(currentUserUID).ValueChanged += HandleRequestsChanged;
    }

    private void HandleRequestsChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        foreach (var item in instantiatedRequests.Values) {
            Destroy(item);
        }
        instantiatedRequests.Clear();

        if (args.Snapshot == null || !args.Snapshot.Exists) {
            Debug.Log("Tidak ada permintaan pertemanan yang pending.");
            return;
        }

        foreach (var childSnapshot in args.Snapshot.Children)
        {
            string senderUID = childSnapshot.Key;
            CreateRequestItemUI(senderUID);
        }
    }
    
    private async void CreateRequestItemUI(string senderUID)
    {
        var userSnapshot = await dbRef.Child("users").Child(senderUID).GetValueAsync();
        if (!userSnapshot.Exists) return;

        // Baris ini sekarang akan menggunakan kelas 'DataToSave' dari file DataStructures.cs
        var userData = JsonUtility.FromJson<DataToSave>(userSnapshot.GetRawJsonValue());

        GameObject newRequestItem = Instantiate(requestItemPrefab, requestListParent);
        var itemUI = newRequestItem.GetComponent<FriendReqItemUI>();

        if (itemUI != null)
        {
            itemUI.Setup(
                userData,
                () => OnAcceptClicked(senderUID),
                async () => await OnIgnoreClicked(senderUID)
            );
        }

        instantiatedRequests[senderUID] = newRequestItem;
    }

    private async void OnAcceptClicked(string senderUID)
    {
        Debug.Log($"Menerima permintaan dari {senderUID}");
        Task friends1 = dbRef.Child("friends").Child(currentUserUID).Child(senderUID).SetValueAsync(true);
        Task friends2 = dbRef.Child("friends").Child(senderUID).Child(currentUserUID).SetValueAsync(true);
        await Task.WhenAll(friends1, friends2);
        await OnIgnoreClicked(senderUID);
        Debug.Log("Permintaan pertemanan diterima.");
    }

    private async Task OnIgnoreClicked(string senderUID)
    {
        Debug.Log($"Mengabaikan permintaan dari {senderUID}");
        await dbRef.Child("friendRequests").Child(currentUserUID).Child(senderUID).RemoveValueAsync();
    }

    private void OnDestroy()
    {
        if (auth?.CurrentUser != null && dbRef != null)
        {
            dbRef.Child("friendRequests").Child(currentUserUID).ValueChanged -= HandleRequestsChanged;
        }
    }
}