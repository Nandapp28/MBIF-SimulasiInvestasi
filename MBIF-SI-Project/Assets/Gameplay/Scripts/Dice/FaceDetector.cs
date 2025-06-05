using UnityEngine;

public class FaceDetector : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        DiceRoll dice = other.GetComponentInParent<DiceRoll>();

        // Cek apakah collider ini bagian dari sebuah dadu
        if (dice != null)
        {
            Rigidbody rb = dice.GetComponent<Rigidbody>();

            // Pastikan dadu sudah berhenti
            if (rb.velocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
            {
                int faceValue;
                if (int.TryParse(other.name, out faceValue))
                {
                    if (dice.diceFaceNum == 0) // hanya deteksi sekali
                    {
                        dice.diceFaceNum = faceValue;
                        Debug.Log($"Dadu {dice.name} mendeteksi face: {faceValue}");
                    }
                }
            }
        }
    }
}