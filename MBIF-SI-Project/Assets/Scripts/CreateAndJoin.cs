using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro UI
using System.Collections;
using System.Collections.Generic;

public class CreateAndJoin : MonoBehaviourPunCallbacks
{
    public TMP_InputField roomNameInput;
    public GameObject roomListContainer;
    public GameObject roomListItemPrefab;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    public void CreateRoom()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 5;
        PhotonNetwork.CreateRoom(roomNameInput.text, options);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform child in roomListContainer.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (RoomInfo room in roomList)
        {
            if (!room.RemovedFromList && room.PlayerCount < room.MaxPlayers)
            {
                GameObject item = Instantiate(roomListItemPrefab, roomListContainer.transform);
                item.GetComponentInChildren<Text>().text = room.Name;
                item.GetComponent<Button>().onClick.AddListener(() => PhotonNetwork.JoinRoom(room.Name));
            }
        }
    }

    public override void OnJoinedRoom()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        if (autoRefreshCoroutine != null)
        {
            StopCoroutine(autoRefreshCoroutine);
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
    }

    public void RefreshRoomList()
    {
        // Tinggalkan lobby lalu join lagi untuk memicu OnRoomListUpdate
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby(); // Harus keluar dulu
        }

        PhotonNetwork.JoinLobby(); // Join lagi untuk memperbarui room list
    }


    private Coroutine autoRefreshCoroutine;

    private void Start()
    {
        if (PhotonNetwork.InLobby == false)
        {
            PhotonNetwork.JoinLobby(); // Pastikan sudah di lobby
        }

        autoRefreshCoroutine = StartCoroutine(AutoRefreshRoomList());
    }

    private IEnumerator AutoRefreshRoomList()
    {
        while (true)
        {
            RefreshRoomList();
            yield return new WaitForSeconds(1f); // Ulangi setiap 1 detik
        }
    }

}