// CameraController.cs
using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Camera Positions")]
    public Transform normalView;
    public Transform centerCardView;
    public Transform konsumerCardView;      // Posisi untuk kartu Merah
    public Transform infrastrukturCardView;  // Posisi untuk kartu Biru
    public Transform keuanganCardView;        // Posisi untuk kartu Hijau
    public Transform tambangCardView;         // Posisi untuk kartu Oranye

    [Header("Movement Settings")]
    public float moveDuration = 0.8f; // Durasi pergerakan kamera dalam detik

    private Coroutine _moveCoroutine;

    // Enum untuk mendefinisikan target posisi dengan lebih jelas
    public enum CameraPosition
    {
        Normal,
        Center,
        Konsumer,
        Infrastruktur,
        Keuangan,
        Tambang
    }
    public CameraPosition CurrentPosition { get; private set; }


    /// <summary>
    /// Memulai pergerakan kamera ke posisi yang ditentukan.
    /// </summary>
    /// <param name="targetPosition">Enum posisi tujuan.</param>
    /// <returns>Coroutine untuk ditunggu jika perlu.</returns>
    public Coroutine MoveTo(CameraPosition targetPosition)
    {
        Transform destination = GetTransformForPosition(targetPosition);
        if (destination == null)
        {
            Debug.LogError($"Posisi kamera untuk '{targetPosition}' belum diatur!");
            return null;
        }

        // Hentikan pergerakan sebelumnya jika ada
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }

        // Mulai pergerakan baru
        _moveCoroutine = StartCoroutine(MoveAndRotateCoroutine(destination, targetPosition));
        return _moveCoroutine;
    }

    private IEnumerator MoveAndRotateCoroutine(Transform destination, CameraPosition newPosition)
    {
        float elapsedTime = 0f;
        Vector3 startingPos = transform.position;
        Quaternion startingRot = transform.rotation;

        while (elapsedTime < moveDuration)
        {
            // Interpolasi posisi dan rotasi secara halus (smooth)
            transform.position = Vector3.Lerp(startingPos, destination.position, elapsedTime / moveDuration);
            transform.rotation = Quaternion.Slerp(startingRot, destination.rotation, elapsedTime / moveDuration);

            elapsedTime += Time.deltaTime;
            yield return null; // Tunggu frame berikutnya
        }

        // Pastikan posisi dan rotasi tepat di tujuan pada akhir animasi
        transform.position = destination.position;
        transform.rotation = destination.rotation;
        CurrentPosition = newPosition; 
        _moveCoroutine = null;
    }

    /// <summary>
    /// Helper untuk mendapatkan Transform berdasarkan enum.
    /// </summary>
    private Transform GetTransformForPosition(CameraPosition position)
    {
        switch (position)
        {
            case CameraPosition.Normal: return normalView;
            case CameraPosition.Center: return centerCardView;
            case CameraPosition.Konsumer: return konsumerCardView;
            case CameraPosition.Infrastruktur: return infrastrukturCardView;
            case CameraPosition.Keuangan: return keuanganCardView;
            case CameraPosition.Tambang: return tambangCardView;
            default: return null;
        }
    }
}