using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class AudioManager : MonoBehaviour
{
    public AudioSource audioSource; // Drag your AudioSource here in the Inspector
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText; // Drag your Text UI element here in the Inspector

    private const string VolumeKey = "MusicVolume";

    void Start()
    {
        // Load the saved volume from PlayerPrefs
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f); // Default to 1 if not set
        SetVolume(savedVolume);
        volumeSlider.value = savedVolume;
        
        // Add listener to the slider
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float volume)
    {
        audioSource.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume); // Save the volume to PlayerPrefs
        PlayerPrefs.Save(); // Ensure the data is saved

        // Update the volume text
        UpdateVolumeText(volume);
    }

    private void UpdateVolumeText(float volume)
    {
        // Convert volume to percentage and update the text
        int percentage = Mathf.RoundToInt(volume * 100);
        volumeText.text = "Volume: " + percentage + "%";
    }

    private void OnDestroy()
    {
        // Optionally, you can save the volume when the object is destroyed
        PlayerPrefs.SetFloat(VolumeKey, audioSource.volume);
        PlayerPrefs.Save();
    }
}