using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target; // drag Player here

    [Header("Offsets (player lower vs higher)")]
    public Vector3 normalOffset   = new Vector3(0f, 3f, -10f);  // gravity down -> camera up
    public Vector3 invertedOffset = new Vector3(0f, -3f, -10f); // gravity up   -> camera down
    public float offsetSmoothTime = 0.15f;

    [Header("Follow Smooth")]
    public float followSmoothTime = 0.12f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 1.5f;
    public float lookAheadSmooth = 0.2f;

    private Vector3 followVel = Vector3.zero;

    private Vector3 offsetVel = Vector3.zero;
    private Vector3 currentOffset;

    private float currentLookAhead = 0f;
    private float lookAheadVel = 0f;
    private float lastTargetX = 0f;

    private Rigidbody2D targetRb;

    void Start()
    {
        if (target != null)
        {
            lastTargetX = target.position.x;
            targetRb = target.GetComponent<Rigidbody2D>();
        }

        currentOffset = normalOffset;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (targetRb == null)
            targetRb = target.GetComponent<Rigidbody2D>();

        // Gravity state (we assume you flip rb.gravityScale in your player script)
        bool gravityInverted = (targetRb != null && targetRb.gravityScale < 0f);

        Vector3 desiredOffset = gravityInverted ? invertedOffset : normalOffset;

        // Smoothly blend offset so it doesn't snap
        currentOffset = Vector3.SmoothDamp(currentOffset, desiredOffset, ref offsetVel, offsetSmoothTime);

        // Look-ahead based on horizontal movement
        float deltaX = target.position.x - lastTargetX;
        lastTargetX = target.position.x;

        float desiredLookAhead = Mathf.Clamp(deltaX * 10f, -1f, 1f) * lookAheadDistance;
        currentLookAhead = Mathf.SmoothDamp(currentLookAhead, desiredLookAhead, ref lookAheadVel, lookAheadSmooth);

        Vector3 desiredPos = new Vector3(
            target.position.x + currentLookAhead,
            target.position.y,
            0f
        ) + currentOffset;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref followVel, followSmoothTime);
    }
}
