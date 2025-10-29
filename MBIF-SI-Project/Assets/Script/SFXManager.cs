using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("SFX Settings")]
    public AudioClip buttonClickSfx;
    public AudioClip buttonClickSellSfx;
    private AudioSource audioSource;

    [Header("UI Volume Control (Optional)")]
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxVolumeText;

    private const string SfxVolumeKey = "SfxVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Setup AudioSource
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        // Load saved volume
        float savedVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        audioSource.volume = savedVolume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIReferences();

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.value = audioSource.volume;
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        }

        if (sfxVolumeText != null)
        {
            UpdateSfxVolumeText(audioSource.volume);
        }
    }

private void FindUIReferences()
{
    // Find Slider
    GameObject sliderObject = GameObject.Find("SFX (Slider)");
    if (sliderObject != null)
    {
        sfxVolumeSlider = sliderObject.GetComponent<Slider>();
    }

    // Find Text
    GameObject textObject = GameObject.Find("SFX (Text)");
    if (textObject != null)
    {
        sfxVolumeText = textObject.GetComponent<TextMeshProUGUI>();
    }
}

    public void PlaySound(AudioClip clip)
    {
        // Memeriksa apakah ada audio clip yang diberikan dan audio source siap
        if (clip != null && audioSource != null)
        {
            // Mainkan clip yang diberikan dengan volume yang sudah diatur
            audioSource.PlayOneShot(clip, audioSource.volume);
        }
    }

    public void PlayButtonClick()
    {
        if (buttonClickSfx != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSfx, audioSource.volume);
        }
    }

    public void PlayButtonSellClick()
    {
        if (buttonClickSellSfx != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSellSfx, audioSource.volume);
        }
    }

    public void SetSfxVolume(float volume)
    {
        audioSource.volume = volume;
        PlayerPrefs.SetFloat(SfxVolumeKey, volume);
        PlayerPrefs.Save();

        UpdateSfxVolumeText(volume);
    }

    private void UpdateSfxVolumeText(float volume)
    {
        int percentage = Mathf.RoundToInt(volume * 100);
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = "SFX : " + percentage + "%";
        }
    }

    private void OnDestroy()
{
    SceneManager.sceneLoaded -= OnSceneLoaded;

    if (audioSource != null)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, audioSource.volume);
        PlayerPrefs.Save();
    }
}
}
