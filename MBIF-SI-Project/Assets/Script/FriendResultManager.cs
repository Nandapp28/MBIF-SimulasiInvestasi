using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Threading.Tasks;

public class FriendResultManager : MonoBehaviour
{
    [Header("UI Pencarian")]
    public GameObject addFriendPopUp;
    public TMP_InputField searchInput;
    public Button searchButton;

    [Header("UI Hasil Pencarian")]
    public GameObject searchResultPanel;
    public TextMeshProUGUI resultUserNameText;
    public TextMeshProUGUI resultPlayerIdText;
    public Image resultAvatarImage;
    public Button addFriendButton;

    // MODIFIKASI: Header dan variabel notifikasi diubah
    [Header("UI Feedback")]
    public GameObject addSuccessPanel;
    public GameObject warningAlertPanel;

    // DIHAPUS: public TextMeshProUGUI notificationText;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private string currentUserUID;
    private string foundUserUID;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;
        
        if (auth.CurrentUser == null)
        {
            Debug.LogError("User belum login!");
            return;
        }
        currentUserUID = auth.CurrentUser.UserId;

        // Sembunyikan semua panel di awal
        searchResultPanel.SetActive(false);
        addSuccessPanel.SetActive(false); 
        warningAlertPanel.SetActive(false);

        searchButton.onClick.AddListener(OnSearchButtonClicked);
        addFriendButton.onClick.AddListener(OnAddFriendButtonClicked);
    }

    private void OnSearchButtonClicked()
    {
        string playerIdToSearch = searchInput.text;
        if (string.IsNullOrEmpty(playerIdToSearch))
        {
            Debug.LogWarning("Kolom pencarian tidak boleh kosong.");
            return;
        }
        StartCoroutine(FindUserByPlayerId(playerIdToSearch));
    }

    public void OnSearchInputChanged()
    {
        // Mengambil teks yang ada dan mengubahnya menjadi huruf kecil
        string lowerCaseText = searchInput.text.ToLower();
        if (searchInput.text != lowerCaseText)
        {
            // Setel kembali teks input field dengan versi huruf kecil
            searchInput.text = lowerCaseText;
        }
    }

    private IEnumerator FindUserByPlayerId(string playerId)
    {
        Debug.Log("Mencari pemain...");
        var query = dbRef.Child("users").OrderByChild("playerId").EqualTo(playerId).GetValueAsync();
        yield return new WaitUntil(() => query.IsCompleted);

        if (query.Exception != null)
        {
            Debug.LogError("Pencarian gagal: " + query.Exception);
            yield break;
        }

        DataSnapshot snapshot = query.Result;
        if (!snapshot.Exists)
        {
            Debug.LogWarning("Pemain dengan ID tersebut tidak ditemukan.");
            searchResultPanel.SetActive(false);
            warningAlertPanel.SetActive(true);
            StartCoroutine(HidePanelAfterDelay(warningAlertPanel, 2.0f));
        }
        else
        {
            foreach (var childSnapshot in snapshot.Children)
            {
                var foundUser = JsonUtility.FromJson<DataToSave>(childSnapshot.GetRawJsonValue());
                foundUserUID = childSnapshot.Key;

                if (foundUserUID == currentUserUID)
                {
                    Debug.LogWarning("Anda tidak bisa menambahkan diri sendiri.");
                    searchResultPanel.SetActive(false);
                    warningAlertPanel.SetActive(true);
                    StartCoroutine(HidePanelAfterDelay(warningAlertPanel, 2.0f));
                    yield break;
                }

                DisplaySearchResult(foundUser);
                Debug.Log("Pemain ditemukan!");
                break;
            }
        }
    }

    private void DisplaySearchResult(DataToSave userData)
    {
        searchResultPanel.SetActive(true);
        
        resultUserNameText.text = userData.userName;
        resultPlayerIdText.text = "ID: " + userData.playerId;
        
        if (!string.IsNullOrEmpty(userData.avatarName))
        {
            Sprite avatarSprite = Resources.Load<Sprite>("Avatars/" + userData.avatarName);
            if(avatarSprite != null)
            {
                resultAvatarImage.sprite = avatarSprite;
            }
            else
            {
                Debug.LogWarning($"Avatar '{userData.avatarName}' tidak ditemukan di folder Resources/Avatars.");
            }
        }
        addFriendButton.interactable = true; 
    }

    private void OnAddFriendButtonClicked()
    {
        if (string.IsNullOrEmpty(foundUserUID))
        {
            Debug.LogError("Tidak ada user yang dipilih untuk ditambahkan.");
            return;
        }
        StartCoroutine(SendFriendRequest());
    }

    private IEnumerator SendFriendRequest()
    {
        addFriendButton.interactable = false;

        var userProfileTask = dbRef.Child("users").Child(currentUserUID).GetValueAsync();
        yield return new WaitUntil(() => userProfileTask.IsCompleted);

        if(userProfileTask.Exception != null)
        {
            Debug.LogError("Gagal mendapatkan data Anda.");
            addFriendButton.interactable = true;
            yield break;
        }

        DataSnapshot snapshot = userProfileTask.Result;
        string senderName = snapshot.Child("userName").Value.ToString();
        
        var requestData = new System.Collections.Generic.Dictionary<string, object>
        {
            ["senderName"] = senderName,
            ["status"] = "pending"
        };

        var requestTask = dbRef.Child("friendRequests").Child(foundUserUID).Child(currentUserUID).SetValueAsync(requestData);
        yield return new WaitUntil(() => requestTask.IsCompleted);

        if (requestTask.Exception != null)
        {
            Debug.LogError("Gagal mengirim permintaan: " + requestTask.Exception);
            addFriendButton.interactable = true;
        }
        else
        {
            // --- MODIFIKASI UTAMA ADA DI SINI ---
            Debug.Log("Permintaan pertemanan terkirim!");
            
            // 1. Sembunyikan panel hasil pencarian
            searchResultPanel.SetActive(false);

            // 2. Tampilkan panel "ADD FRIEND SUCCESS"
            addSuccessPanel.SetActive(true);

            // 3. Mulai coroutine untuk menyembunyikan panel sukses setelah 2 detik
            StartCoroutine(HidePanelAfterDelay(addSuccessPanel, 2.0f));
        }
    }
    
    // Coroutine untuk menyembunyikan panel setelah durasi tertentu
    private IEnumerator HidePanelAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
    }

    public void BackToPrevious()
    {
        addFriendPopUp.SetActive(false);
    }
}