using UnityEngine;

public class Splash : MonoBehaviour
{
    public float lifetime = 2f;       // How long the splat stays fully visible
    public float fadeDuration = 1f;   // How long it takes to fade out after lifetime

    private SpriteRenderer sr;
    private float timer = 0f;
    private bool fading = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!fading && timer >= lifetime)
        {
            fading = true;
            timer = 0f; // Reset timer for fade
        }

        if (fading)
        {
            float alpha = 1f - (timer / fadeDuration);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);

            if (alpha <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}