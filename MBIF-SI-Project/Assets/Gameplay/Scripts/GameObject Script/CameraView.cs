using UnityEngine;

public class TampilkanArah : MonoBehaviour
{
    // Fungsi ini akan menggambar di Scene View
    void OnDrawGizmos()
    {
        // Tentukan warna untuk garis
        Gizmos.color = Color.blue;

        // Gambar sebuah garis dari posisi objek ini ke arah depannya (transform.forward)
        // Garis ini akan memiliki panjang 2 unit
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}