using UnityEngine;

/// <summary>Particulas sencillas (sin sistema de particulas) para las explosiones.</summary>
public class Pop : MonoBehaviour
{
    public static Sprite Circle;

    Vector2 velocity;
    float life;
    float born;
    float baseScale;
    SpriteRenderer sr;

    public static void Spawn(Vector3 position, Color color, float size, int pieces)
    {
        if (Circle == null) return;
        for (int i = 0; i < pieces; i++)
        {
            var go = new GameObject("Pop");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Circle;
            renderer.color = color;
            renderer.sortingOrder = 60;

            var pop = go.AddComponent<Pop>();
            float angle = (360f / Mathf.Max(1, pieces)) * i + Random.Range(-12f, 12f);
            float speed = Random.Range(2.4f, 5.4f);
            pop.velocity = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * speed;
            pop.life = Random.Range(0.35f, 0.62f);
            pop.baseScale = size * Random.Range(0.20f, 0.42f);
            go.transform.localScale = Vector3.one * pop.baseScale;
        }
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        born = Time.time;
    }

    void Update()
    {
        float t = (Time.time - born) / life;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        velocity *= 0.94f;
        transform.localScale = Vector3.one * baseScale * (0.55f + t * 1.9f);
        var c = sr.color;
        c.a = 1f - t;
        sr.color = c;
    }
}
