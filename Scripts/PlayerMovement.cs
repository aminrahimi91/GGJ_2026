using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Jump Settings")]
    public int maxJumps = 2;

    [Header("Ground Check (no GroundCheck object needed)")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.18f;
    public float groundCheckMargin = 0.02f;

    [Header("Squash & Stretch Scales")]
    public Vector2 stretchScale = new Vector2(0.8f, 1.2f); // jump
    public Vector2 squashScale  = new Vector2(1.15f, 0.85f); // land

    [Header("Stretch Timing (Jump)")]
    public float jumpInDuration = 0.08f;
    public float jumpHoldTime = 0.00f;
    public float jumpOutDuration = 0.12f;

    [Header("Squash Timing (Land)")]
    public float landInDuration = 0.05f;
    public float landHoldTime = 0.02f;
    public float landOutDuration = 0.13f;

    [Header("Ease Curve")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Gravity Flip")]
    public KeyCode gravityFlipKey = KeyCode.LeftShift;
    public float flipAnimDuration = 0.22f;
    public Color normalColor = Color.white;
    public Color invertedColor = Color.white;

    [Header("Air Trail (splats in air)")]
    public GameObject slimeSplatPrefab;
    public float splatDistance = 0.2f;
    public float minAirSpeed = 1.0f;
    public float cornerBackMargin = 0.02f;
    public float cornerDownMargin = 0.02f;

    [Header("Renderer (optional)")]
    public SpriteRenderer spriteRenderer; // assign your visible sprite renderer

    private Rigidbody2D rb;
    private Collider2D col;

    private bool isGrounded;
    private bool wasGrounded;
    private int jumpCount;

    private bool gravityInverted = false;
    private Coroutine flipRoutine;

    private Vector3 originalScale;
    private Coroutine scaleRoutine;

    private bool trailStarted = false;
    private Vector3 lastSplatPos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        originalScale = transform.localScale;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyColor(normalColor);
    }

    void Update()
    {
        HandleMovement();
        UpdateGrounded();
        HandleJump();
        HandleGravityFlip();
        HandleAirTrail();

        wasGrounded = isGrounded;
    }

    // ---------------- Movement ----------------
    void HandleMovement()
    {
        float move = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(move * moveSpeed, rb.velocity.y);
    }

    // ---------------- Ground Check (bounds-based) ----------------
    void UpdateGrounded()
    {
        if (col == null)
        {
            isGrounded = false;
            return;
        }

        Bounds b = col.bounds;

        // If gravity is normal, "feet" are at bottom. If inverted, "feet" are at top.
        float y = gravityInverted ? (b.max.y + groundCheckMargin) : (b.min.y - groundCheckMargin);
        Vector2 checkPos = new Vector2(b.center.x, y);

        isGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);

        // Landing event
        if (isGrounded && !wasGrounded)
        {
            jumpCount = 0;
            PlaySquash();
            trailStarted = false;
        }
    }

    // ---------------- Jump / Double Jump ----------------
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            float jumpVel = gravityInverted ? -jumpForce : jumpForce;
            rb.velocity = new Vector2(rb.velocity.x, jumpVel);

            jumpCount++;
            PlayStretch();
        }
    }

    // ---------------- Gravity Flip ----------------
    void HandleGravityFlip()
    {
        if (Input.GetKeyDown(gravityFlipKey))
        {
            gravityInverted = !gravityInverted;
            rb.gravityScale *= -1f;

            if (flipRoutine != null) StopCoroutine(flipRoutine);
            flipRoutine = StartCoroutine(FlipRoutine(gravityInverted));
        }
    }

    IEnumerator FlipRoutine(bool inverted)
    {
        float t = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(0f, 0f, inverted ? 180f : 0f);

        Color startColor = (spriteRenderer != null) ? spriteRenderer.color : normalColor;
        Color endColor = inverted ? invertedColor : normalColor;

        while (t < flipAnimDuration)
        {
            float u = easeCurve.Evaluate(t / flipAnimDuration);
            transform.rotation = Quaternion.Lerp(startRot, endRot, u);
            ApplyColor(Color.Lerp(startColor, endColor, u));

            t += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRot;
        ApplyColor(endColor);

        flipRoutine = null;
    }

    void ApplyColor(Color c)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = c;
    }

    // ---------------- Air Trail (bottom corner in air) ----------------
    void HandleAirTrail()
    {
        if (slimeSplatPrefab == null) return;
        if (col == null) return;
        if (isGrounded) return;

        Vector2 vel = rb.velocity;
        float speed = vel.magnitude;
        if (speed < minAirSpeed) return;

        float dirX = Mathf.Sign(vel.x);
        if (Mathf.Abs(dirX) < 0.01f) dirX = 1f;

        Bounds b = col.bounds;

        // Ground side depends on gravity
        float y = (!gravityInverted)
            ? (b.min.y - cornerDownMargin)   // normal gravity -> bottom
            : (b.max.y + cornerDownMargin);  // inverted gravity -> top

        // Back corner depends on horizontal direction
        float x = (dirX > 0f)
            ? (b.min.x - cornerBackMargin)   // moving right -> back is left
            : (b.max.x + cornerBackMargin);  // moving left  -> back is right

        Vector3 currentPos = new Vector3(x, y, transform.position.z);

        if (!trailStarted)
        {
            SpawnSplat(currentPos);
            lastSplatPos = currentPos;
            trailStarted = true;
            return;
        }

        if (Vector2.Distance(currentPos, lastSplatPos) >= splatDistance)
        {
            SpawnSplat(currentPos);
            lastSplatPos = currentPos;
        }
    }

    void SpawnSplat(Vector3 pos)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        Instantiate(slimeSplatPrefab, pos, rot);
    }

    // ---------------- Squash & Stretch ----------------
    void PlayStretch()
    {
        Vector3 target = new Vector3(
            originalScale.x * stretchScale.x,
            originalScale.y * stretchScale.y,
            originalScale.z
        );
        StartScalePulse(target, jumpInDuration, jumpHoldTime, jumpOutDuration);
    }

    void PlaySquash()
    {
        Vector3 target = new Vector3(
            originalScale.x * squashScale.x,
            originalScale.y * squashScale.y,
            originalScale.z
        );
        StartScalePulse(target, landInDuration, landHoldTime, landOutDuration);
    }

    void StartScalePulse(Vector3 targetScale, float inDur, float hold, float outDur)
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScalePulseRoutine(targetScale, inDur, hold, outDur));
    }

    IEnumerator ScalePulseRoutine(Vector3 targetScale, float inDur, float hold, float outDur)
    {
        Vector3 startScale = transform.localScale;

        // In
        float t = 0f;
        while (t < inDur)
        {
            float u = easeCurve.Evaluate(t / inDur);
            transform.localScale = Vector3.Lerp(startScale, targetScale, u);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;

        // Hold
        if (hold > 0f) yield return new WaitForSeconds(hold);

        // Out
        t = 0f;
        while (t < outDur)
        {
            float u = easeCurve.Evaluate(t / outDur);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, u);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;

        scaleRoutine = null;
    }

    // ---------------- Gizmos ----------------
    void OnDrawGizmosSelected()
    {
        if (col != null)
        {
            Bounds b = col.bounds;
            float y = gravityInverted ? (b.max.y + groundCheckMargin) : (b.min.y - groundCheckMargin);
            Vector2 checkPos = new Vector2(b.center.x, y);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
        }
    }
}
