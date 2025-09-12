using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class TutorialController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject bullishMascot;
    public GameObject bearyMascot;
    public GameObject bullishBubble;
    public TextMeshProUGUI bullishText;
    public GameObject bearyBubble;
    public TextMeshProUGUI bearyText;
    public GameObject choiceButtons;
    
    // TAMBAHKAN INI: Referensi ke panel UI untuk EventTrigger dan menonaktifkannya
    public Image tutorialPanelImage; 

    [Header("Scene Names")]
    public string playSceneName = "Play";
    public string tutorialSceneName = "PlayTutorial";
    
    [Header("Camera Zoom")]
    public Camera mainCamera;
    public float zoomDuration = 1.5f;
    public float targetZoomSize = 3f;

    private int conversationStep = 0;

    void Start()
    {
        InitializeTutorial();
    }

    void InitializeTutorial()
    {
        bullishBubble.SetActive(false);
        bearyBubble.SetActive(false);
        bearyMascot.SetActive(false);
        choiceButtons.SetActive(false);
        
        // Pastikan panel bisa diklik di awal
        if (tutorialPanelImage != null)
        {
            tutorialPanelImage.raycastTarget = true;
        }
        
        conversationStep = 0;
        StartCoroutine(StartFirstDialogue());
    }

    IEnumerator StartFirstDialogue()
    {
        yield return new WaitForSeconds(0.5f);
        ShowBullishLine1();
    }

    public void OnPanelClicked()
    {
        switch (conversationStep)
        {
            case 1:
                ShowBearyLine();
                break;
            case 3:
                StartCoroutine(ZoomAndLoadScene(tutorialSceneName));
                break;
        }
    }

    void ShowBullishLine1()
    {
        bullishText.text = "Hi! You look new here";
        bullishBubble.SetActive(true);
        conversationStep = 1;
    }

    void ShowBearyLine()
    {
        bearyMascot.SetActive(true);
        bearyBubble.SetActive(true);
        bearyText.text = "Have you ever play this type of game before?";
        choiceButtons.SetActive(true);
        conversationStep = 2;
    }

    public void OnYesButtonClicked()
    {
        SceneManager.LoadScene(playSceneName);
        PlayerPrefs.SetString("hasCompletedTutorial", "yes");
        PlayerPrefs.Save();
    }

    public void OnNoButtonClicked()
    {
        bearyMascot.SetActive(false);
        bearyBubble.SetActive(false);
        choiceButtons.SetActive(false);
        bullishText.text = "Let me give some tour in this game";
        conversationStep = 3;
    }

    IEnumerator ZoomAndLoadScene(string sceneName)
    {
        float initialSize = mainCamera.orthographicSize;
        float elapsedTime = 0f;

        // UBAH BAGIAN INI
        // Menonaktifkan panel agar tidak bisa diklik lagi selama zoom
        if (tutorialPanelImage != null)
        {
            tutorialPanelImage.raycastTarget = false; 
        }

        while (elapsedTime < zoomDuration)
        {
            mainCamera.orthographicSize = Mathf.Lerp(initialSize, targetZoomSize, elapsedTime / zoomDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.orthographicSize = targetZoomSize;
        SceneManager.LoadScene(sceneName);
    }
}