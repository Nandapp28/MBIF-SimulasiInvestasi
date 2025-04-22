using UnityEngine;
using System.Collections;
[RequireComponent(typeof(Rigidbody))]
public class DiceRoll : MonoBehaviour
{
    Rigidbody body;

    [SerializeField] private float maxRandomForceValue, startRollingForce;

    private float forceX, forceY, forceZ;

    public int diceFaceNum;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float actionCooldown = 3f;  // Waktu cooldown untuk ResetPosition dan RollDice
    private bool isActionCooldown = false;

    private void Awake()
    {
        Initialize();
        initialPosition = transform.position;
        initialRotation = transform.rotation;

    }

    private void Update()
    {
        if (body != null)
        {
            // Mengecek apakah mouse sedang ditekan
            if (Input.GetMouseButton(0) && !isActionCooldown)
            {
                // Jalankan fungsi ResetPosition dan RollDice tanpa cooldown selama mouse ditahan
                ResetPosition();
                RollDice();
            }

            // Mengecek apakah mouse dilepaskan
            if (Input.GetMouseButtonUp(0) && !isActionCooldown)
            {
                // Setelah mouse dilepas, mulai cooldown
                StartCoroutine(HandleActionsCooldown());
            }
        }
    }

    private IEnumerator HandleActionsCooldown()
    {
        // Memulai cooldown untuk ResetPosition dan RollDice
        isActionCooldown = true;

        // Menjalankan fungsi-fungsi yang diinginkan
        ResetPosition();
        RollDice();

        // Menunggu selama cooldownTime detik
        yield return new WaitForSeconds(actionCooldown);

        // Setelah cooldown selesai, reset flag isActionCooldown
        isActionCooldown = false;
    }

    public void ResetPosition()
    {
        body.isKinematic = true;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.position = initialPosition;

        diceFaceNum = 0;
    }


    public void RollDice()
    {
        body.isKinematic = false;

        forceX = Random.Range(0, maxRandomForceValue);
        forceY = Random.Range(0, maxRandomForceValue);
        forceZ = Random.Range(0, maxRandomForceValue);

        body.AddForce(Vector3.up * startRollingForce);
        body.AddTorque(forceX, forceY, forceZ);
    }

    private void Initialize()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        transform.rotation = new Quaternion(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360), 0);
    }
}
