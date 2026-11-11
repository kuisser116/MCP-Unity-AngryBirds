using UnityEngine;

/// <summary>Bloque destructible (madera, piedra, hielo).</summary>
public class Destructible : MonoBehaviour
{
    public float toughness = 6f;
    public int points = 500;
    public Color popColor = new Color(0.80f, 0.56f, 0.30f, 1f);

    bool broken;

    void OnCollisionEnter2D(Collision2D collision)
    {
        float speed = collision.relativeVelocity.magnitude;
        float mass = collision.rigidbody != null ? collision.rigidbody.mass : 1f;
        if (speed > toughness || speed * mass > toughness * 1.6f) Break();
    }

    void Break()
    {
        if (broken) return;
        broken = true;
        if (GameManager.I != null) GameManager.I.AddScore(points);
        Pop.Spawn(transform.position, popColor, 1.1f, 9);
        Destroy(gameObject);
    }
}
