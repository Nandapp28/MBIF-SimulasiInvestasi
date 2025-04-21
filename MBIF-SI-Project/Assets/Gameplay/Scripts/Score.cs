using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    [SerializeField] private DiceRoll dice1;  // Dadu pertama
    [SerializeField] private DiceRoll dice2;  // Dadu kedua

    [SerializeField]
    private Text scoreText;  // UI untuk menampilkan skor

    [SerializeField]
    private float stopThreshold = 0.1f;  // Ambang batas kecepatan untuk dianggap berhenti

    private void Update()
    {
        if (dice1 != null && dice2 != null)
        {
            int total = 0;
            bool allDiceStopped = true;

            // Cek apakah kedua dadu sudah berhenti
            if (dice1.GetComponent<Rigidbody>().velocity.magnitude > stopThreshold || dice1.GetComponent<Rigidbody>().angularVelocity.magnitude > stopThreshold)
            {
                allDiceStopped = false;
            }
            if (dice2.GetComponent<Rigidbody>().velocity.magnitude > stopThreshold || dice2.GetComponent<Rigidbody>().angularVelocity.magnitude > stopThreshold)
            {
                allDiceStopped = false;
            }

            // Jika kedua dadu sudah berhenti, hitung skor
            if (allDiceStopped)
            {
                total = dice1.diceFaceNum + dice2.diceFaceNum;
                scoreText.text = "Total: " + total.ToString();
            }
        }
    }
}
