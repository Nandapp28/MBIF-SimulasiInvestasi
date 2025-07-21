using UnityEngine;

public class SfxManagerDummy : MonoBehaviour
{
    // Instansiasi singleton sederhana untuk testing
    public static SfxManagerDummy Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Opsional: DontDestroyOnLoad(gameObject); jika Anda ingin ini bertahan antar scene di test
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayButtonClick()
    {
        // Tidak melakukan apapun secara nyata, hanya untuk mencegah error
        Debug.Log("SfxManagerDummy: PlayButtonClick called during test.");
    }
}