using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DiceRoll : MonoBehaviour
{
    Rigidbody body;

    [SerializeField] private float maxRandomForceValue, startRollingForce;

    private float forceX, forceY, forceZ;

    public int diceFaceNum;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

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
            
            if (Input.GetMouseButtonDown(0))
            {
                ResetPosition();
                RollDice();
            }
        }
    }

    private void ResetPosition()
    {
        body.isKinematic = true;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        diceFaceNum = 0;
    }


    private void RollDice()
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
