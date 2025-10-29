// File: Scripts/InputManagerMultiplayer.cs

using UnityEngine;
using UnityEngine.EventSystems;

public class InputManagerMultiplayer : MonoBehaviour
{
    // Variabel ini disertakan agar struktur kodenya sama persis
    public LayerMask clickableLayer;

    void Update()
    {
        // Cek apakah ada input (baik klik mouse maupun sentuhan)
        if (Input.GetMouseButtonDown(0))
        {
            // --- LOGIKA UNTUK MENCEGAH KLIK "TEMBUS" UI ---
            
            // Untuk PC, pengecekan ini sudah cukup
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Untuk Mobile, kita perlu pengecekan tambahan berdasarkan ID sentuhan
            // Jika ada sentuhan dan sentuhan itu mengenai UI, hentikan
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                return;
            }

            // --- LOGIKA RAYCAST (TETAP SAMA) ---
            
            // Buat "sinar" dari posisi input (berfungsi untuk mouse dan sentuhan)
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Lakukan Raycast
            if (Physics.Raycast(ray, out hit))
            {
                // --- BAGIAN YANG DISESUAIKAN UNTUK MULTIPLAYER ---
                // Coba dapatkan komponen RumorCardClickHandlerMultiplayer
                RumorCardClickHandlerMultiplayer cardClickHandler = hit.collider.GetComponent<RumorCardClickHandlerMultiplayer>();

                // Jika ditemukan, panggil fungsinya
                if (cardClickHandler != null)
                {
                    cardClickHandler.TriggerCardView();
                }
            }
        }
    }
}