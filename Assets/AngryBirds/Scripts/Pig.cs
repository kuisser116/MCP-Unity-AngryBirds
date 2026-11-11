using UnityEngine;

/// <summary>Cerdo objetivo: muere al recibir un golpe fuerte.</summary>
public class Pig : MonoBehaviour
{
    public float tolerance = 3.2f;
    public int points = 5000;

    bool dead;

    void OnCollisionEnter2D(Collision2D collision)
    {
        float speed = collision.relativeVelocity.magnitude;
        float mass = collision.rigidbody != null ? collision.rigidbody.mass : 0.5f;
        if (speed > tolerance || speed * mass > 5.5f) Hit();
    }

    public void Hit()
    {
        if (dead) return;
        dead = true;
        if (GameManager.I != null)
        {
            GameManager.I.AddScore(points);
            GameManager.I.OnPigKilled();
        }
        Pop.Spawn(transform.position, new Color(0.55f, 0.85f, 0.35f, 1f), 0.9f, 10);
        Destroy(gameObject);
    }
}
