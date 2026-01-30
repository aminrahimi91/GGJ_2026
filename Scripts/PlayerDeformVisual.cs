using UnityEngine;

public class PlayerDeformVisual : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public Transform visual;     // Visual child (the sprite)
    public Transform nose;       // optional: Nose child (triangle sprite)

    [Header("Speed Settings")]
    public float maxSpeed = 8f;  // speed where effect reaches max

    [Header("Lean / Stretch")]
    public float maxTiltZ = 10f;         // degrees
    public float maxForwardShift = 0.08f; // units
    public float maxStretchX = 0.12f;    // +X scale
    public float maxSquashY = 0.08f;     // -Y scale

    [Header("Nose (Pointy Corner)")]
    public float noseMaxScaleX = 0.7f;   // how pointy it becomes
    public float noseMaxScaleY = 0.25f;
    public float noseForwardShift = 0.05f;
    public float noseUpShift = 0.03f;

    [Header("Smoothing")]
    public float smooth = 14f;

    private Vector3 visualStartPos;
    private Vector3 visualStartScale;
    private Quaternion visualStartRot;

    private Vector3 noseStartPos;
    private Vector3 noseStartScale;

    private float lastDir = 1f;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (visual != null)
        {
            visualStartPos = visual.localPosition;
            visualStartScale = visual.localScale;
            visualStartRot = visual.localRotation;
        }

        if (nose != null)
        {
            noseStartPos = nose.localPosition;
            noseStartScale = nose.localScale;
        }
    }

    void LateUpdate()
    {
        if (rb == null || visual == null) return;

        float vx = rb.velocity.x;
        float speed = Mathf.Abs(vx);

        // direction (keep last dir when stopping)
        if (Mathf.Abs(vx) > 0.05f) lastDir = Mathf.Sign(vx);

        float t = Mathf.Clamp01(speed / maxSpeed); // 0..1

        // --- Visual: tilt forward, shift forward, stretch forward ---
        float tilt = -lastDir * maxTiltZ * t; // negative feels like leaning into motion
        Quaternion targetRot = Quaternion.Euler(0f, 0f, tilt);

        Vector3 targetPos = visualStartPos + new Vector3(lastDir * maxForwardShift * t, 0f, 0f);

        float sx = visualStartScale.x * (1f + maxStretchX * t);
        float sy = visualStartScale.y * (1f - maxSquashY * t);
        Vector3 targetScale = new Vector3(sx, sy, visualStartScale.z);

        // Smooth
        float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, targetRot, k);
        visual.localPosition = Vector3.Lerp(visual.localPosition, targetPos, k);
        visual.localScale = Vector3.Lerp(visual.localScale, targetScale, k);

        // --- Nose: make top-front corner sharp ---
        if (nose != null)
        {
            // Put nose on the moving-forward side and slightly up
            Vector3 nPos = noseStartPos + new Vector3(lastDir * noseForwardShift * t, noseUpShift * t, 0f);

            // Make it pointier with speed
            Vector3 nScale = new Vector3(
                noseStartScale.x * (1f + noseMaxScaleX * t),
                noseStartScale.y * (1f + noseMaxScaleY * t),
                noseStartScale.z
            );

            // Flip nose direction by scaling X sign (so it points forward both ways)
            nScale.x = Mathf.Abs(nScale.x) * lastDir;

            nose.localPosition = Vector3.Lerp(nose.localPosition, nPos, k);
            nose.localScale = Vector3.Lerp(nose.localScale, nScale, k);
        }
    }
}
