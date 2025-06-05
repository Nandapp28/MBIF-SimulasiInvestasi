using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource musicAudioSource; // Hanya untuk musik latar

    [Header("UI Elements")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText;

    private const string VolumeKey = "MusicVolume";

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
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        musicAudioSource.volume = savedVolume;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIReferences();

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.value = musicAudioSource.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }


    }

    private void FindUIReferences()
{
    GameObject sliderObject = GameObject.Find("Music (Slider)");
    if (sliderObject != null)
    {
        volumeSlider = sliderObject.GetComponent<Slider>();
    }

    GameObject textObject = GameObject.Find("Music (Text)");
    if (textObject != null)
    {
        volumeText = textObject.GetComponent<TextMeshProUGUI>();
    }
}

    public void SetVolume(float volume)
    {
        musicAudioSource.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();

    }


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        PlayerPrefs.SetFloat(VolumeKey, musicAudioSource.volume);
        PlayerPrefs.Save();
    }
}
