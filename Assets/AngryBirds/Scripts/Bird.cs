using UnityEngine;

/// <summary>
/// Ave del juego: se mantiene quieta en la resortera hasta que se lanza.
/// Avisa al GameManager cuando ya se detuvo para cargar la siguiente.
/// </summary>
public class Bird : MonoBehaviour
{
    public GameManager game;
    public Rigidbody2D body;
    public float settleSpeed = 0.5f;
    public float settleTime = 0.6f;
    public float maxFlightTime = 9f;

    bool launched;
    bool settled;

    public bool HasLaunched { get { return launched; } }
    float airTime;
    float slowTime;

    void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
    }

    public void Hold(Vector3 position)
    {
        launched = false;
        settled = false;
        airTime = 0f;
        slowTime = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        transform.position = position;
        transform.rotation = Quaternion.identity;
    }

    public void Launch(Vector2 velocity)
    {
        body.bodyType = RigidbodyType2D.Dynamic;
        body.linearVelocity = velocity;
        body.angularVelocity = -velocity.x * 4f;
        launched = true;
        settled = false;
        airTime = 0f;
        slowTime = 0f;
        if (game != null) game.OnBirdLaunched();
    }

    void Update()
    {
        if (!launched || settled) return;

        airTime += Time.deltaTime;
        float speed = body.linearVelocity.magnitude;
        if (speed < settleSpeed) slowTime += Time.deltaTime;
        else slowTime = 0f;

        if (transform.position.y < -10f || Mathf.Abs(transform.position.x) > 40f) Settle();
        else if (airTime > 0.75f && slowTime > settleTime) Settle();
        else if (airTime > maxFlightTime) Settle();
    }

    void Settle()
    {
        if (settled) return;
        settled = true;
        if (game != null) game.OnBirdSettled();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!launched) return;
        if (collision.relativeVelocity.magnitude > 8f)
        {
            Pop.Spawn(transform.position, new Color(1f, 0.9f, 0.6f, 0.9f), 0.5f, 4);
        }
    }
}
