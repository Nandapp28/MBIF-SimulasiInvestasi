using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenLink : MonoBehaviour
{
    public string url = "https://youtu.be/WUOq-0oOYp0?si=ZtutAHS51VAlSiJM"; // ganti dengan linkmu

    public void OpenYouTube()
    {
        Application.OpenURL(url);
    }
}
