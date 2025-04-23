using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    [SerializeField] public DiceRoll dice1;  // Dadu pertama
    [SerializeField] public DiceRoll dice2;  // Dadu kedua

    [SerializeField]
    public Text scoreText;  // UI untuk menampilkan skor

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
    public int GetDiceTotal()
{
    if (dice1 == null || dice2 == null) return 0;

    if (dice1.GetComponent<Rigidbody>().velocity.magnitude <= stopThreshold &&
        dice1.GetComponent<Rigidbody>().angularVelocity.magnitude <= stopThreshold &&
        dice2.GetComponent<Rigidbody>().velocity.magnitude <= stopThreshold &&
        dice2.GetComponent<Rigidbody>().angularVelocity.magnitude <= stopThreshold)
    {
        return dice1.diceFaceNum + dice2.diceFaceNum;
    }

    return 0;
}
public bool AreDiceStopped()
    {
        if (dice1 == null || dice2 == null) return false;

        Rigidbody rb1 = dice1.GetComponent<Rigidbody>();
        Rigidbody rb2 = dice2.GetComponent<Rigidbody>();

        return rb1.velocity.magnitude <= stopThreshold &&
               rb1.angularVelocity.magnitude <= stopThreshold &&
               rb2.velocity.magnitude <= stopThreshold &&
               rb2.angularVelocity.magnitude <= stopThreshold;
    }

}