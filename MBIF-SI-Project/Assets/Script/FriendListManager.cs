using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;

public class FriendListManager : MonoBehaviour
{
    [Header("UI Referensi")]
    public Transform friendListParent;   // GameObject "Content" di dalam ScrollView
    public GameObject friendItemPrefab;  // Prefab yang kita buat di Langkah 1

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private string currentUserUID;
    private Dictionary<string, GameObject> instantiatedFriends = new Dictionary<string, GameObject>();

    IEnumerator Start()
    {
        // Selalu tunggu Firebase siap
        yield return new WaitUntil(() => FirebaseInitializer.Instance != null && FirebaseInitializer.Instance.IsFirebaseReady);
        
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser == null) {
            Debug.LogError("User belum login.");
            yield break;
        }
        currentUserUID = auth.CurrentUser.UserId;

        LoadFriendList();
    }

    private void LoadFriendList()
    {
        // Path: /friends/{currentUserUID}
        DatabaseReference friendsRef = dbRef.Child("friends").Child(currentUserUID);
        friendsRef.ValueChanged += HandleFriendListChanged;
    }

    private void HandleFriendListChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Hapus UI lama sebelum refresh
        foreach (var item in instantiatedFriends.Values) {
            Destroy(item);
        }
        instantiatedFriends.Clear();

        if (args.Snapshot == null || !args.Snapshot.Exists) {
            Debug.Log("Pengguna ini belum memiliki teman.");
            return;
        }

        // Ulangi untuk setiap teman yang ada di daftar
        foreach (var childSnapshot in args.Snapshot.Children)
        {
            string friendUID = childSnapshot.Key;
            CreateFriendItemUI(friendUID);
        }
    }

    private async void CreateFriendItemUI(string friendUID)
    {
        // Ambil profil lengkap dari teman berdasarkan UID-nya dari node "users"
        var userSnapshot = await dbRef.Child("users").Child(friendUID).GetValueAsync();
        if (!userSnapshot.Exists) return;

        var friendData = JsonUtility.FromJson<DataToSave>(userSnapshot.GetRawJsonValue());

        // Buat instance prefab
        GameObject newFriendItem = Instantiate(friendItemPrefab, friendListParent);
        
        // Atur agar teman baru muncul di paling atas
        newFriendItem.transform.SetAsFirstSibling();

        // Panggil fungsi Setup pada skrip prefab
        var itemUI = newFriendItem.GetComponent<FriendItemUI>();
        if (itemUI != null)
        {
            itemUI.Setup(friendData);
        }

        instantiatedFriends[friendUID] = newFriendItem;
    }

    private void OnDestroy()
    {
        if (auth?.CurrentUser != null && dbRef != null)
        {
            dbRef.Child("friends").Child(currentUserUID).ValueChanged -= HandleFriendListChanged;
        }
    }
}