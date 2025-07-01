using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExitGame : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Exit();
        }
    }

    public void Exit()
    {
#if UNITY_EDITOR
        Debug.Log("Keluar dari play mode di Editor.");
        EditorApplication.isPlaying = false;
#else
        Debug.Log("Keluar dari aplikasi build.");
        Application.Quit();
#endif
    }
}