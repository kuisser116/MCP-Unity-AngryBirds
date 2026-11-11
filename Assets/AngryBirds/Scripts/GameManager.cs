using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Control general del nivel: aves, puntaje, victoria y derrota.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    public enum State { Ready, Flying, Win, Lose }

    [Header("Referencias")]
    public GameObject birdTemplate;
    public Transform birdAnchor;
    public Slingshot slingshot;
    public FollowCamera followCamera;
    public Sprite dotSprite;

    [Header("Nivel")]
    public int totalBirds = 3;
    public int pigsTotal = 3;

    public int birdsLeft;
    public int score;
    public int pigsAlive;
    public State state = State.Ready;

    Bird activeBird;
    float spawnTimer;
    bool pendingSpawn;
    float loseTimer;

    GUIStyle bigStyle;
    GUIStyle midStyle;
    GUIStyle smallStyle;
    GUIStyle titleStyle;
    Texture2D white;

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        if (dotSprite != null) Pop.Circle = dotSprite;

        pigsAlive = FindObjectsByType<Pig>().Length;
        if (pigsAlive <= 0) pigsAlive = pigsTotal;
        birdsLeft = totalBirds;
        score = 0;
        state = State.Ready;
        SpawnBird();
    }

    void SpawnBird()
    {
        if (birdsLeft <= 0)
        {
            pendingSpawn = false;
            return;
        }

        if (activeBird != null) Destroy(activeBird.gameObject);

        var go = Instantiate(birdTemplate, birdAnchor.position, Quaternion.identity);
        go.name = "Bird";
        go.SetActive(true);

        activeBird = go.GetComponent<Bird>();
        if (activeBird != null)
        {
            activeBird.game = this;
            activeBird.Hold(birdAnchor.position);
        }

        if (slingshot != null)
        {
            slingshot.currentBird = activeBird;
            slingshot.game = this;
            slingshot.ResetBands();
        }
        if (followCamera != null) followCamera.target = go.transform;

        birdsLeft--;
        pendingSpawn = false;
        state = State.Ready;
    }

    public void OnBirdLaunched()
    {
        state = State.Flying;
    }

    public void OnBirdSettled()
    {
        if (state == State.Win || state == State.Lose) return;
        state = State.Ready;

        if (pigsAlive <= 0)
        {
            Win();
            return;
        }

        if (birdsLeft > 0)
        {
            pendingSpawn = true;
            spawnTimer = 1.0f;
        }
        else
        {
            loseTimer = 1.8f;
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
    }

    public void OnPigKilled()
    {
        pigsAlive--;
        if (pigsAlive <= 0) Win();
    }

    void Win()
    {
        if (state == State.Win) return;
        state = State.Win;
        score += Mathf.Max(0, birdsLeft) * 10000;
    }

    void Update()
    {
        if (PointerInput.KeyPressed(PointerInput.Key.R)) Restart();

        if (pendingSpawn)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f) SpawnBird();
        }

        if (state == State.Flying && pigsAlive <= 0) Win();

        if (loseTimer > 0f)
        {
            loseTimer -= Time.deltaTime;
            if (loseTimer <= 0f && state != State.Win)
            {
                pigsAlive = FindObjectsByType<Pig>().Length;
                if (pigsAlive > 0) state = State.Lose;
            }
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void EnsureStyles()
    {
        if (bigStyle != null) return;

        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();

        bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        midStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
    }

    static void Shadowed(Rect rect, string text, GUIStyle style, Color color)
    {
        var previous = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.55f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
        style.normal.textColor = previous;
    }

    void OnGUI()
    {
        EnsureStyles();

        float w = Screen.width;
        float h = Screen.height;

        Shadowed(new Rect(24, 18, 420, 46), "PUNTOS  " + score.ToString("N0"), bigStyle, Color.white);
        Shadowed(new Rect(24, 66, 420, 30), "AVES  " + birdsLeft, midStyle, new Color(1f, 0.95f, 0.75f));
        Shadowed(new Rect(24, 96, 480, 26), "Cerdos vivos: " + Mathf.Max(0, pigsAlive), smallStyle, new Color(1f, 0.9f, 0.9f, 0.9f));

        if (state == State.Ready)
        {
            Shadowed(new Rect(0, 132, w, 26),
                "Arrastra el ave desde la resortera y suelta para disparar   |   R = reiniciar",
                new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter }, Color.white);
        }

        if (state == State.Win || state == State.Lose)
        {
            bool win = state == State.Win;
            var panel = new Rect(w * 0.5f - 260f, h * 0.5f - 150f, 520f, 300f);
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(panel, white);
            GUI.color = Color.white;

            Shadowed(new Rect(panel.x, panel.y + 26f, panel.width, 80f), win ? "¡GANASTE!" : "TE QUEDASTE SIN AVES",
                titleStyle, win ? new Color(1f, 0.86f, 0.25f) : new Color(1f, 0.45f, 0.4f));

            Shadowed(new Rect(panel.x, panel.y + 120f, panel.width, 40f), "Puntaje final: " + score.ToString("N0"),
                new GUIStyle(midStyle) { alignment = TextAnchor.MiddleCenter }, Color.white);

            var button = new Rect(panel.x + panel.width * 0.5f - 130f, panel.y + 190f, 260f, 62f);
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };
            if (GUI.Button(button, "Reintentar  (R)", buttonStyle)) Restart();
        }
    }
}
