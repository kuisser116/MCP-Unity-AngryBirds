using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resortera: se arrastra el ave, se muestra la trayectoria y al soltar se lanza.
/// </summary>
public class Slingshot : MonoBehaviour
{
    public Transform anchor;
    public Bird currentBird;
    public GameManager game;

    [Header("Ajustes de tiro")]
    public float maxStretch = 2.0f;
    public float power = 7.2f;
    public float grabRadius = 1.4f;
    public float minStretchToLaunch = 0.18f;

    [Header("Visuales")]
    public LineRenderer bandFront;
    public LineRenderer bandBack;
    public Transform forkFront;
    public Transform forkBack;
    public GameObject dotPrefab;
    public int dotCount = 16;
    public float dotStep = 0.085f;

    readonly List<SpriteRenderer> dots = new List<SpriteRenderer>();
    bool dragging;

    void Start()
    {
        CreateDots();
        UpdateBands();
    }

    void CreateDots()
    {
        if (dotPrefab == null) return;
        for (int i = 0; i < dotCount; i++)
        {
            var go = Instantiate(dotPrefab);
            go.name = "TrajectoryDot";
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 45;
            dots.Add(sr);
        }
    }

    public void ResetBands()
    {
        dragging = false;
        HideDots();
        UpdateBands();
    }

    void Update()
    {
        PointerInput.Poll();

        if (currentBird == null || currentBird.HasLaunched || game == null || game.state != GameManager.State.Ready)
        {
            HideDots();
            UpdateBands();
            return;
        }

        Vector2 world = ScreenToWorld(PointerInput.Screen);

        if (!dragging && PointerInput.DownThisFrame)
        {
            if (Vector2.Distance(world, currentBird.transform.position) < grabRadius)
            {
                dragging = true;
                currentBird.Hold(anchor.position);
            }
        }

        if (dragging)
        {
            if (PointerInput.Down)
            {
                Vector2 basePos = anchor.position;
                Vector2 pull = Vector2.ClampMagnitude(world - basePos, maxStretch);
                currentBird.transform.position = basePos + pull;
                currentBird.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(pull.y, pull.x) * Mathf.Rad2Deg);
                ShowTrajectory(currentBird.transform.position, (basePos - (Vector2)currentBird.transform.position) * power);
            }

            if (PointerInput.UpThisFrame)
            {
                dragging = false;
                HideDots();
                Vector2 direction = (Vector2)anchor.position - (Vector2)currentBird.transform.position;
                if (direction.magnitude >= minStretchToLaunch)
                {
                    currentBird.transform.rotation = Quaternion.identity;
                    currentBird.Launch(direction * power);
                }
                else
                {
                    currentBird.Hold(anchor.position);
                }
            }
        }

        UpdateBands();
    }

    void ShowTrajectory(Vector2 from, Vector2 velocity)
    {
        Vector2 gravity = Physics2D.gravity;
        for (int i = 0; i < dots.Count; i++)
        {
            if (dots[i] == null) continue;
            float t = (i + 1) * dotStep;
            Vector2 point = from + velocity * t + 0.5f * gravity * t * t;
            var dot = dots[i];
            dot.transform.position = point;
            dot.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.45f, (float)i / dots.Count);
            var c = dot.color;
            c.a = Mathf.Lerp(0.85f, 0.15f, (float)i / dots.Count);
            dot.color = c;
            dot.gameObject.SetActive(true);
        }
    }

    void HideDots()
    {
        for (int i = 0; i < dots.Count; i++)
        {
            if (dots[i] != null && dots[i].gameObject.activeSelf) dots[i].gameObject.SetActive(false);
        }
    }

    void UpdateBands()
    {
        if (bandFront == null || bandBack == null) return;

        Vector3 birdPos = currentBird != null ? currentBird.transform.position : anchor.position;
        Vector3 front = forkFront != null ? forkFront.position : anchor.position + new Vector3(0.28f, 0f, 0f);
        Vector3 back = forkBack != null ? forkBack.position : anchor.position + new Vector3(-0.12f, 0.18f, 1f);

        bool visible = currentBird != null && !currentBird.HasLaunched;
        bandFront.enabled = visible;
        bandBack.enabled = visible;
        if (!visible) return;

        bandFront.positionCount = 2;
        bandFront.SetPosition(0, front);
        bandFront.SetPosition(1, birdPos);

        bandBack.positionCount = 2;
        bandBack.SetPosition(0, back);
        bandBack.SetPosition(1, birdPos + new Vector3(0f, 0f, 0.2f));
    }

    static Vector2 ScreenToWorld(Vector2 screen)
    {
        var cam = Camera.main;
        if (cam == null) return Vector2.zero;
        Vector3 p = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        return new Vector2(p.x, p.y);
    }
}
