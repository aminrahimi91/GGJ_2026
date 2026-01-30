using UnityEngine;

public class TutorialLRHints : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject hintLeft;     // Hint_Left in scene
    public GameObject hintRight;    // Hint_Right in scene
    public string playerTag = "Player";

    [Header("Always visible + pulsing")]
    public Color startColor = new Color(1f, 1f, 1f, 1f); // visible from the start

    [Header("Change color after moving a little")]
    public float changeDistance = 0.10f; // world units (try 0.06 to 0.15)
    public Color leftChangedColor  = new Color(1f, 0.25f, 0.25f, 1f); // example: red
    public Color rightChangedColor = new Color(0.25f, 1f, 0.25f, 1f); // example: green

    [Header("Pulse (idle breathing)")]
    public float pulseSpeed = 5f;     // higher = faster pulse
    public float pulseAmount = 0.08f; // 0.05-0.12 is nice

    [Header("Optional: Pop when you press the key")]
    public bool popOnPress = true;
    public float popScale = 1.25f;
    public float popDuration = 0.10f;

    private SpriteRenderer leftSR;
    private SpriteRenderer rightSR;

    private Vector3 leftBaseScale;
    private Vector3 rightBaseScale;
    private float leftXSign = 1f;
    private float rightXSign = 1f;

    private Transform playerTf;
    private float startX;
    private bool startedTracking = false;

    private bool leftChanged = false;
    private bool rightChanged = false;

    private bool leftPopLock = false;
    private bool rightPopLock = false;

    void Start()
    {
        if (hintLeft != null)
        {
            leftSR = hintLeft.GetComponent<SpriteRenderer>();
            leftBaseScale = hintLeft.transform.localScale;
            leftXSign = Mathf.Sign(leftBaseScale.x);
            if (leftSR != null) leftSR.color = startColor;
        }

        if (hintRight != null)
        {
            rightSR = hintRight.GetComponent<SpriteRenderer>();
            rightBaseScale = hintRight.transform.localScale;
            rightXSign = Mathf.Sign(rightBaseScale.x);
            if (rightSR != null) rightSR.color = startColor;
        }

        // If the player starts inside the trigger, OnTriggerEnter might not fire.
        // So we also try to find player at Start (safe fallback).
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
        {
            playerTf = p.transform;
            startX = playerTf.position.x;
            startedTracking = true;
        }
    }

    void Update()
    {
        // Pulse effect always (even before tracking)
        DoPulse();

        if (!startedTracking || playerTf == null) return;

        float dx = playerTf.position.x - startX;

        // Change LEFT arrow color after moving left a little
        if (!leftChanged && dx <= -changeDistance)
        {
            leftChanged = true;
            if (leftSR != null) leftSR.color = leftChangedColor;
        }

        // Change RIGHT arrow color after moving right a little
        if (!rightChanged && dx >= changeDistance)
        {
            rightChanged = true;
            if (rightSR != null) rightSR.color = rightChangedColor;
        }

        // Optional: pop when pressed (does NOT change color; color comes from movement)
        if (popOnPress)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                TriggerPop(true);

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                TriggerPop(false);
        }

        // If you want: stop tracking after both colors changed (but keep visuals)
        // if (leftChanged && rightChanged) startedTracking = false;
    }

    void DoPulse()
    {
        float s = 1f + pulseAmount * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));

        if (hintLeft != null)
        {
            Vector3 baseS = leftBaseScale;
            hintLeft.transform.localScale = new Vector3(Mathf.Abs(baseS.x) * leftXSign * s, baseS.y * s, baseS.z);
        }

        if (hintRight != null)
        {
            Vector3 baseS = rightBaseScale;
            hintRight.transform.localScale = new Vector3(Mathf.Abs(baseS.x) * rightXSign * s, baseS.y * s, baseS.z);
        }
    }

    void TriggerPop(bool isLeft)
    {
        if (isLeft)
        {
            if (leftPopLock || hintLeft == null) return;
            StartCoroutine(PopRoutine(hintLeft.transform, leftBaseScale, () => leftPopLock = false));
            leftPopLock = true;
        }
        else
        {
            if (rightPopLock || hintRight == null) return;
            StartCoroutine(PopRoutine(hintRight.transform, rightBaseScale, () => rightPopLock = false));
            rightPopLock = true;
        }
    }

    System.Collections.IEnumerator PopRoutine(Transform t, Vector3 baseScale, System.Action onDone)
    {
        // Keep X sign if the arrow was flipped
        float signX = Mathf.Sign(baseScale.x);
        Vector3 baseS = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        Vector3 targetS = baseS * popScale;

        float timer = 0f;

        // Up
        while (timer < popDuration)
        {
            float u = timer / popDuration;
            Vector3 s = Vector3.Lerp(baseS, targetS, u);
            t.localScale = new Vector3(s.x * signX, s.y, s.z);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;

        // Down
        while (timer < popDuration)
        {
            float u = timer / popDuration;
            Vector3 s = Vector3.Lerp(targetS, baseS, u);
            t.localScale = new Vector3(s.x * signX, s.y, s.z);
            timer += Time.deltaTime;
            yield return null;
        }

        // Restore
        t.localScale = new Vector3(baseS.x * signX, baseS.y, baseS.z);
        onDone?.Invoke();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerTf = other.transform;

        // Start measuring movement from the moment the player enters this tutorial area
        startX = playerTf.position.x;
        startedTracking = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Handles the case where player starts already inside the trigger.
        if (startedTracking) return;
        if (!other.CompareTag(playerTag)) return;

        playerTf = other.transform;
        startX = playerTf.position.x;
        startedTracking = true;
    }
}
