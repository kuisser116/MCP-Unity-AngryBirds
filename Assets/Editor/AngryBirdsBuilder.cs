using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generador del nivel de Angry Birds 2D. Crea el arte (PNG procedurales),
/// la escena completa y compila un ejecutable de Windows.
/// Se puede ejecutar desde el menu "AngryBirds" o en batch mode:
///   Unity.exe -batchmode -quit -projectPath &lt;proyecto&gt; -executeMethod AngryBirdsBuilder.BuildAll
/// </summary>
public static class AngryBirdsBuilder
{
    const string Root = "Assets/AngryBirds";
    const string SpriteDir = Root + "/Sprites";
    const string ScenePath = "Assets/Scenes/AngryBirds.unity";
    const string BuildDir = "Builds/Windows";
    const string BuildExe = BuildDir + "/AngryBirds2D.exe";

    const float GroundTop = -3.2f;

    // ------------------------------------------------------------------ menu

    [MenuItem("AngryBirds/1. Generar arte + escena")]
    public static void BuildSceneOnly()
    {
        EditorUtility.DisplayProgressBar("Angry Birds", "Generando arte...", 0.2f);
        try
        {
            EnsureFolders();
            var art = GenerateArt();
            EditorUtility.DisplayProgressBar("Angry Birds", "Armando escena...", 0.7f);
            BuildScene(art);
            AssetDatabase.SaveAssets();
            Debug.Log("AB_MARKER_SCENE_OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("AngryBirds/2. Compilar ejecutable Windows")]
    public static void BuildWindowsOnly()
    {
        BuildWindows();
    }

    [MenuItem("AngryBirds/3. Todo (arte + escena + ejecutable)")]
    public static void BuildAll()
    {
        BuildSceneOnly();
        BuildWindows();
        Debug.Log("AB_MARKER_DONE");
    }

    // ------------------------------------------------------------------ setup

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "AngryBirds");
        if (!AssetDatabase.IsValidFolder(SpriteDir)) AssetDatabase.CreateFolder(Root, "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Editor")) AssetDatabase.CreateFolder("Assets", "Editor");
    }

    static Material GetSpriteMaterial()
    {
        string path = Root + "/Sprites/SpriteUnlit.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        var mat = new Material(shader) { name = "SpriteUnlit" };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static PhysicsMaterial2D GetPhysicsMaterial()
    {
        string path = Root + "/Sprites/Slick.physicsMaterial2D";
        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (existing != null) return existing;

        var pm = new PhysicsMaterial2D("Slick") { friction = 0.55f, bounciness = 0.02f };
        AssetDatabase.CreateAsset(pm, path);
        return pm;
    }

    // ------------------------------------------------------------------ arte

    enum Kind { Wood, Stone, Ice }

    static Dictionary<string, Sprite> GenerateArt()
    {
        var art = new Dictionary<string, Sprite>();

        art["bird"] = SaveSprite(BirdTex(110), "bird");
        art["pig"] = SaveSprite(PigTex(100), "pig");
        art["dot"] = SaveSprite(DotTex(14), "dot");
        art["ground"] = SaveSprite(GroundTex(256, 160), "ground");
        art["sling"] = SaveSprite(SlingTex(80, 150), "sling");
        art["hills"] = SaveSprite(HillsTex(1024, 240), "hills");
        art["sun"] = SaveSprite(SunTex(220), "sun");

        art["wood_beam_v"] = SaveSprite(BlockTex(30, 170, Kind.Wood, 11), "wood_beam_v");
        art["wood_beam_v2"] = SaveSprite(BlockTex(26, 100, Kind.Wood, 23), "wood_beam_v2");
        art["wood_plank_h"] = SaveSprite(BlockTex(250, 28, Kind.Wood, 37), "wood_plank_h");
        art["wood_plank_top"] = SaveSprite(BlockTex(150, 26, Kind.Wood, 41), "wood_plank_top");
        art["crate"] = SaveSprite(BlockTex(70, 70, Kind.Wood, 53), "crate");
        art["stone"] = SaveSprite(BlockTex(80, 80, Kind.Stone, 67), "stone");
        art["ice"] = SaveSprite(BlockTex(70, 70, Kind.Ice, 71), "ice");

        return art;
    }

    // --- utilidades de dibujo (se dibuja al doble y se reduce: bordes suaves)

    const int SS = 2;
    static Color[] buffer;
    static int bufW, bufH;

    static void Begin(int width, int height)
    {
        bufW = width * SS;
        bufH = height * SS;
        buffer = new Color[bufW * bufH];
        for (int i = 0; i < buffer.Length; i++) buffer[i] = new Color(0f, 0f, 0f, 0f);
    }

    static void Blend(int x, int y, Color src)
    {
        if (x < 0 || y < 0 || x >= bufW || y >= bufH) return;
        if (src.a <= 0.001f) return;
        int i = y * bufW + x;
        Color dst = buffer[i];
        float a = src.a + dst.a * (1f - src.a);
        if (a <= 0.0001f)
        {
            buffer[i] = new Color(0f, 0f, 0f, 0f);
            return;
        }
        Color outColor = (src * src.a + dst * dst.a * (1f - src.a)) / a;
        outColor.a = a;
        buffer[i] = outColor;
    }

    static void Circle(float cx, float cy, float radius, Color color)
    {
        int x0 = Mathf.FloorToInt((cx - radius) * SS) - 1;
        int x1 = Mathf.CeilToInt((cx + radius) * SS) + 1;
        int y0 = Mathf.FloorToInt((cy - radius) * SS) - 1;
        int y1 = Mathf.CeilToInt((cy + radius) * SS) + 1;
        float r2 = radius * radius;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f) / SS - cx;
                float dy = (y + 0.5f) / SS - cy;
                if (dx * dx + dy * dy <= r2) Blend(x, y, color);
            }
        }
    }

    static void Ellipse(float cx, float cy, float rx, float ry, Color color)
    {
        int x0 = Mathf.FloorToInt((cx - rx) * SS) - 1;
        int x1 = Mathf.CeilToInt((cx + rx) * SS) + 1;
        int y0 = Mathf.FloorToInt((cy - ry) * SS) - 1;
        int y1 = Mathf.CeilToInt((cy + ry) * SS) + 1;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = ((x + 0.5f) / SS - cx) / rx;
                float dy = ((y + 0.5f) / SS - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Blend(x, y, color);
            }
        }
    }

    static void Rect(float x0, float y0, float width, float height, Color color)
    {
        RoundRect(x0, y0, width, height, 0f, color);
    }

    static void RoundRect(float x0, float y0, float width, float height, float radius, Color color)
    {
        radius = Mathf.Clamp(radius, 0f, Mathf.Min(width, height) * 0.5f);
        int px0 = Mathf.FloorToInt(x0 * SS);
        int px1 = Mathf.CeilToInt((x0 + width) * SS);
        int py0 = Mathf.FloorToInt(y0 * SS);
        int py1 = Mathf.CeilToInt((y0 + height) * SS);

        for (int y = py0; y <= py1; y++)
        {
            for (int x = px0; x <= px1; x++)
            {
                float px = (x + 0.5f) / SS;
                float py = (y + 0.5f) / SS;
                if (px < x0 || px > x0 + width || py < y0 || py > y0 + height) continue;

                if (radius > 0.01f)
                {
                    float nx = Mathf.Clamp(px, x0 + radius, x0 + width - radius);
                    float ny = Mathf.Clamp(py, y0 + radius, y0 + height - radius);
                    float dx = px - nx;
                    float dy = py - ny;
                    if (dx * dx + dy * dy > radius * radius) continue;
                }
                Blend(x, y, color);
            }
        }
    }

    static void Line(Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 ab = b - a;
        float length = ab.magnitude;
        if (length < 0.001f)
        {
            Circle(a.x, a.y, width * 0.5f, color);
            return;
        }
        float radius = width * 0.5f;
        int x0 = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - width) * SS) - 1;
        int x1 = Mathf.CeilToInt((Mathf.Max(a.x, b.x) + width) * SS) + 1;
        int y0 = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - width) * SS) - 1;
        int y1 = Mathf.CeilToInt((Mathf.Max(a.y, b.y) + width) * SS) + 1;
        float len2 = ab.sqrMagnitude;

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float px = (x + 0.5f) / SS;
                float py = (y + 0.5f) / SS;
                float t = Mathf.Clamp01(((px - a.x) * ab.x + (py - a.y) * ab.y) / len2);
                float dx = px - (a.x + ab.x * t);
                float dy = py - (a.y + ab.y * t);
                if (dx * dx + dy * dy <= radius * radius) Blend(x, y, color);
            }
        }
    }

    static Texture2D Finish()
    {
        int w = bufW / SS;
        int h = bufH / SS;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var cols = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color sum = new Color(0f, 0f, 0f, 0f);
                for (int sy = 0; sy < SS; sy++)
                    for (int sx = 0; sx < SS; sx++)
                        sum += buffer[(y * SS + sy) * bufW + (x * SS + sx)];
                cols[y * w + x] = sum / (SS * SS);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    static Sprite SaveSprite(Texture2D tex, string name)
    {
        string path = SpriteDir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // --- sprites concretos

    static Texture2D BirdTex(int size)
    {
        Begin(size, size);
        float c = size * 0.5f;

        // cola
        Line(new Vector2(c - 34f, c + 6f), new Vector2(c - 50f, c + 20f), 9f, new Color(0.48f, 0.10f, 0.07f));
        Line(new Vector2(c - 34f, c + 2f), new Vector2(c - 52f, c + 2f), 8f, new Color(0.48f, 0.10f, 0.07f));
        Line(new Vector2(c - 34f, c - 2f), new Vector2(c - 50f, c - 16f), 8f, new Color(0.48f, 0.10f, 0.07f));

        // cuerpo
        Circle(c, c, 48f, new Color(0.44f, 0.08f, 0.06f));
        Circle(c, c, 44f, new Color(0.86f, 0.22f, 0.16f));

        // plumaje superior y mejilla
        Circle(c - 12f, c + 14f, 22f, new Color(0.95f, 0.38f, 0.28f, 0.55f));
        Ellipse(c + 2f, c - 16f, 30f, 20f, new Color(0.98f, 0.87f, 0.76f));

        // ojos
        Circle(c - 10f, c + 6f, 14f, Color.white);
        Circle(c + 16f, c + 6f, 14f, Color.white);
        Circle(c - 7f, c + 5f, 6f, new Color(0.08f, 0.08f, 0.09f));
        Circle(c + 19f, c + 5f, 6f, new Color(0.08f, 0.08f, 0.09f));

        // cejas enojadas
        Line(new Vector2(c - 26f, c + 22f), new Vector2(c - 4f, c + 15f), 8f, new Color(0.13f, 0.10f, 0.09f));
        Line(new Vector2(c + 32f, c + 22f), new Vector2(c + 12f, c + 15f), 8f, new Color(0.13f, 0.10f, 0.09f));

        // pico
        Ellipse(c + 6f, c - 8f, 12f, 8f, new Color(0.96f, 0.65f, 0.12f));
        Line(new Vector2(c - 4f, c - 9f), new Vector2(c + 16f, c - 9f), 2.5f, new Color(0.72f, 0.44f, 0.06f));

        // plumitas de la cabeza
        Line(new Vector2(c - 12f, c + 46f), new Vector2(c - 18f, c + 56f), 7f, new Color(0.72f, 0.14f, 0.10f));
        Line(new Vector2(c + 2f, c + 47f), new Vector2(c + 10f, c + 57f), 7f, new Color(0.72f, 0.14f, 0.10f));

        return Finish();
    }

    static Texture2D PigTex(int size)
    {
        Begin(size, size);
        float c = size * 0.5f;

        Circle(c - 22f, c + 26f, 12f, new Color(0.33f, 0.55f, 0.16f));
        Circle(c + 22f, c + 26f, 12f, new Color(0.33f, 0.55f, 0.16f));

        Circle(c, c - 2f, 44f, new Color(0.24f, 0.42f, 0.11f));
        Circle(c, c - 2f, 40f, new Color(0.50f, 0.76f, 0.29f));
        Circle(c - 14f, c + 14f, 18f, new Color(0.64f, 0.86f, 0.42f, 0.6f));

        // ojos
        Circle(c - 13f, c + 8f, 11f, Color.white);
        Circle(c + 13f, c + 8f, 11f, Color.white);
        Circle(c - 11f, c + 7f, 4.6f, new Color(0.08f, 0.10f, 0.07f));
        Circle(c + 15f, c + 7f, 4.6f, new Color(0.08f, 0.10f, 0.07f));

        // hocico
        Ellipse(c, c - 16f, 17f, 12f, new Color(0.24f, 0.42f, 0.11f));
        Ellipse(c, c - 16f, 15f, 10f, new Color(0.68f, 0.88f, 0.47f));
        Ellipse(c - 5.5f, c - 16f, 3f, 4f, new Color(0.20f, 0.34f, 0.10f));
        Ellipse(c + 5.5f, c - 16f, 3f, 4f, new Color(0.20f, 0.34f, 0.10f));

        return Finish();
    }

    static Texture2D DotTex(int size)
    {
        Begin(size, size);
        Circle(size * 0.5f, size * 0.5f, size * 0.42f, new Color(1f, 1f, 1f, 0.95f));
        return Finish();
    }

    static Texture2D GroundTex(int w, int h)
    {
        Begin(w, h);
        var rng = new System.Random(7);

        Rect(0, 0, w, h, new Color(0.52f, 0.34f, 0.19f));
        Rect(0, 0, w, h * 0.22f, new Color(0.44f, 0.28f, 0.15f));

        // piedritas
        for (int i = 0; i < 60; i++)
        {
            float x = (float)rng.NextDouble() * w;
            float y = (float)rng.NextDouble() * h * 0.62f;
            float r = 1.2f + (float)rng.NextDouble() * 2.2f;
            bool light = rng.NextDouble() > 0.5;
            Circle(x, y, r, light ? new Color(0.62f, 0.43f, 0.26f, 0.7f) : new Color(0.38f, 0.24f, 0.13f, 0.7f));
        }

        // pasto
        float grassH = h * 0.34f;
        Rect(0, h - grassH, w, grassH, new Color(0.36f, 0.61f, 0.24f));
        Rect(0, h - grassH * 0.35f, w, grassH * 0.35f, new Color(0.28f, 0.48f, 0.18f));

        // briznas
        for (int i = 0; i < 90; i++)
        {
            float x = (float)rng.NextDouble() * w;
            float baseY = h - grassH + (float)rng.NextDouble() * grassH * 0.5f;
            float len = 3f + (float)rng.NextDouble() * 8f;
            Line(new Vector2(x, baseY), new Vector2(x + (float)(rng.NextDouble() - 0.5) * 4f, baseY + len),
                1.6f, new Color(0.46f, 0.72f, 0.31f, 0.9f));
        }

        Rect(0, h - grassH - 1.5f, w, 2.5f, new Color(0.25f, 0.18f, 0.10f, 0.55f));
        return Finish();
    }

    static Texture2D SlingTex(int w, int h)
    {
        Begin(w, h);
        float cx = w * 0.5f;
        var wood = new Color(0.36f, 0.22f, 0.10f);
        var woodLight = new Color(0.52f, 0.33f, 0.16f);

        Line(new Vector2(cx, 0f), new Vector2(cx, h * 0.58f), 22f, wood);
        Line(new Vector2(cx, 4f), new Vector2(cx, h * 0.56f), 11f, woodLight);
        Line(new Vector2(cx, h * 0.55f), new Vector2(w * 0.20f, h * 0.93f), 17f, wood);
        Line(new Vector2(cx, h * 0.55f), new Vector2(w * 0.80f, h * 0.93f), 17f, wood);
        Line(new Vector2(cx, h * 0.55f), new Vector2(w * 0.22f, h * 0.90f), 8f, woodLight);
        Line(new Vector2(cx, h * 0.55f), new Vector2(w * 0.78f, h * 0.90f), 8f, woodLight);

        Circle(w * 0.20f, h * 0.93f, 10f, new Color(0.24f, 0.14f, 0.06f));
        Circle(w * 0.80f, h * 0.93f, 10f, new Color(0.24f, 0.14f, 0.06f));
        Rect(0f, 0f, w, 6f, new Color(0.28f, 0.17f, 0.08f));
        return Finish();
    }

    static Texture2D HillsTex(int w, int h)
    {
        Begin(w, h);
        Ellipse(w * 0.20f, 0f, w * 0.28f, h * 0.75f, new Color(0.35f, 0.52f, 0.30f, 0.85f));
        Ellipse(w * 0.55f, 0f, w * 0.30f, h * 0.95f, new Color(0.30f, 0.46f, 0.26f, 0.9f));
        Ellipse(w * 0.85f, 0f, w * 0.26f, h * 0.68f, new Color(0.35f, 0.52f, 0.30f, 0.85f));
        return Finish();
    }

    static Texture2D SunTex(int size)
    {
        Begin(size, size);
        float c = size * 0.5f;
        for (int i = 12; i >= 1; i--)
        {
            float r = size * 0.5f * (i / 12f);
            float alpha = Mathf.Lerp(0.05f, 0.55f, 1f - (i / 12f));
            Circle(c, c, r, new Color(1f, 0.95f, 0.62f, alpha * 0.35f));
        }
        Circle(c, c, size * 0.26f, new Color(1f, 0.94f, 0.60f, 0.75f));
        return Finish();
    }

    static Texture2D BlockTex(int w, int h, Kind kind, int seed)
    {
        Begin(w, h);
        var rng = new System.Random(seed);

        Color fill, border;
        switch (kind)
        {
            case Kind.Stone:
                fill = new Color(0.62f, 0.64f, 0.66f);
                border = new Color(0.33f, 0.35f, 0.38f);
                break;
            case Kind.Ice:
                fill = new Color(0.60f, 0.84f, 0.96f, 0.78f);
                border = new Color(0.86f, 0.96f, 1f, 0.95f);
                break;
            default:
                fill = new Color(0.78f, 0.54f, 0.28f);
                border = new Color(0.44f, 0.27f, 0.11f);
                break;
        }

        float radius = Mathf.Clamp(Mathf.Min(w, h) * 0.22f, 2.5f, 10f);
        RoundRect(0.5f, 0.5f, w - 1f, h - 1f, radius, border);
        RoundRect(2.2f, 2.2f, w - 4.4f, h - 4.4f, radius * 0.75f, fill);

        if (kind == Kind.Wood)
        {
            // vetas horizontales
            int lines = Mathf.Max(2, h / 12);
            for (int i = 0; i < lines; i++)
            {
                float y = 5f + (h - 10f) * ((i + 0.5f) / lines) + (float)(rng.NextDouble() - 0.5) * 2f;
                float wobble = (float)(rng.NextDouble() - 0.5) * 3f;
                Line(new Vector2(4f, y), new Vector2(w - 4f, y + wobble), 1.3f, new Color(0.42f, 0.25f, 0.10f, 0.30f));
            }
            // nudos
            int knots = Mathf.Max(1, (w * h) / 6000);
            for (int i = 0; i < knots; i++)
            {
                float x = 8f + (float)rng.NextDouble() * (w - 16f);
                float y = 8f + (float)rng.NextDouble() * (h - 16f);
                Ellipse(x, y, 3.2f, 2.2f, new Color(0.40f, 0.24f, 0.10f, 0.45f));
            }
            // brillo superior
            RoundRect(4f, h - 6f, w - 8f, 2.4f, 1.2f, new Color(1f, 0.92f, 0.72f, 0.22f));
        }
        else if (kind == Kind.Stone)
        {
            for (int i = 0; i < 40; i++)
            {
                float x = 5f + (float)rng.NextDouble() * (w - 10f);
                float y = 5f + (float)rng.NextDouble() * (h - 10f);
                float r = 0.9f + (float)rng.NextDouble() * 1.8f;
                bool light = rng.NextDouble() > 0.5;
                Circle(x, y, r, light ? new Color(0.82f, 0.84f, 0.86f, 0.35f) : new Color(0.34f, 0.36f, 0.39f, 0.35f));
            }
            Line(new Vector2(w * 0.25f, h * 0.75f), new Vector2(w * 0.45f, h * 0.55f), 1.4f, new Color(0.28f, 0.30f, 0.33f, 0.5f));
            Line(new Vector2(w * 0.45f, h * 0.55f), new Vector2(w * 0.62f, h * 0.62f), 1.4f, new Color(0.28f, 0.30f, 0.33f, 0.5f));
            RoundRect(4f, h - 6f, w - 8f, 2f, 1f, new Color(1f, 1f, 1f, 0.18f));
        }
        else
        {
            Line(new Vector2(w * 0.15f, 4f), new Vector2(w * 0.45f, h - 4f), 3.4f, new Color(1f, 1f, 1f, 0.45f));
            Line(new Vector2(w * 0.52f, 4f), new Vector2(w * 0.78f, h - 4f), 2.2f, new Color(1f, 1f, 1f, 0.35f));
            RoundRect(4f, h - 7f, w - 8f, 3f, 1.5f, new Color(1f, 1f, 1f, 0.5f));
        }

        return Finish();
    }

    // ------------------------------------------------------------------ escena

    static void BuildScene(Dictionary<string, Sprite> art)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var mat = GetSpriteMaterial();
        var phys = GetPhysicsMaterial();

        // Camara
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.53f, 0.77f, 0.92f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        camGo.transform.position = new Vector3(0.8f, 2.4f, -10f);
        camGo.AddComponent<AudioListener>();
        var follow = camGo.AddComponent<FollowCamera>();

        // Fondo
        SpriteObject("Sun", art["sun"], new Vector3(6.6f, 4.2f, 0f), mat, -40);
        var hills = SpriteObject("Hills", art["hills"], new Vector3(1.5f, -2.6f, 0f), mat, -30);
        hills.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Tiled;
        hills.GetComponent<SpriteRenderer>().size = new Vector2(14f, 2.4f);

        // Suelo
        var ground = SpriteObject("Ground", art["ground"], new Vector3(2f, GroundTop - 0.8f, 0f), mat, -10);
        var groundSr = ground.GetComponent<SpriteRenderer>();
        groundSr.drawMode = SpriteDrawMode.Tiled;
        groundSr.size = new Vector2(48f, 1.6f);
        var groundCol = ground.AddComponent<BoxCollider2D>();
        groundCol.size = new Vector2(48f, 1.6f);
        groundCol.sharedMaterial = phys;

        // Resortera
        var slingGo = SpriteObject("Slingshot", art["sling"], new Vector3(-6.65f, GroundTop + 0.75f, 0f), mat, 1);
        var anchorGo = new GameObject("Anchor");
        anchorGo.transform.position = new Vector3(-6.36f, -1.60f, 0f);
        var forkFront = new GameObject("ForkFront");
        forkFront.transform.position = new Vector3(-6.45f, -1.78f, 0f);
        var forkBack = new GameObject("ForkBack");
        forkBack.transform.position = new Vector3(-6.87f, -1.78f, 0f);

        var sling = slingGo.AddComponent<Slingshot>();
        sling.anchor = anchorGo.transform;
        sling.forkFront = forkFront.transform;
        sling.forkBack = forkBack.transform;
        sling.bandFront = CreateBand("BandFront", mat, slingGo.transform);
        sling.bandBack = CreateBand("BandBack", mat, slingGo.transform);

        // Puntos de trayectoria (prefab en escena, inactivo)
        var dot = SpriteObject("TrajectoryDotPrefab", art["dot"], new Vector3(-20f, -20f, 0f), mat, 45);
        var dotCol = dot.GetComponent<SpriteRenderer>();
        dotCol.color = new Color(1f, 1f, 1f, 0.9f);
        dot.SetActive(false);
        sling.dotPrefab = dot;

        // Ave (plantilla)
        var bird = SpriteObject("BirdTemplate", art["bird"], new Vector3(-24f, -20f, 0f), mat, 7);
        var birdCol = bird.AddComponent<CircleCollider2D>();
        birdCol.radius = 0.5f;
        birdCol.sharedMaterial = phys;
        var birdBody = bird.AddComponent<Rigidbody2D>();
        birdBody.mass = 1.2f;
        birdBody.bodyType = RigidbodyType2D.Kinematic;
        birdBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        birdBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        birdBody.linearDamping = 0.05f;
        birdBody.angularDamping = 0.05f;
        var birdScript = bird.AddComponent<Bird>();
        birdScript.body = birdBody;
        bird.SetActive(false);

        // Estructura
        AddBlock("BeamIzq", art["wood_beam_v"], new Vector2(2.10f, GroundTop + 0.85f), 1.0f, 5.2f, mat, phys, Kind.Wood);
        AddBlock("BeamDer", art["wood_beam_v"], new Vector2(4.10f, GroundTop + 0.85f), 1.0f, 5.2f, mat, phys, Kind.Wood);
        AddBlock("PlankMedio", art["wood_plank_h"], new Vector2(3.10f, GroundTop + 1.84f), 0.9f, 5.0f, mat, phys, Kind.Wood);

        AddBlock("BeamIzq2", art["wood_beam_v2"], new Vector2(2.55f, GroundTop + 2.48f), 0.6f, 4.4f, mat, phys, Kind.Wood);
        AddBlock("BeamDer2", art["wood_beam_v2"], new Vector2(3.65f, GroundTop + 2.48f), 0.6f, 4.4f, mat, phys, Kind.Wood);
        AddBlock("PlankTecho", art["wood_plank_top"], new Vector2(3.10f, GroundTop + 3.11f), 0.7f, 4.4f, mat, phys, Kind.Wood);

        AddBlock("Hielo", art["ice"], new Vector2(5.35f, GroundTop + 0.35f), 0.7f, 3.2f, mat, phys, Kind.Ice);
        AddBlock("Piedra", art["stone"], new Vector2(5.35f, GroundTop + 1.10f), 1.7f, 8.5f, mat, phys, Kind.Stone);
        AddBlock("Caja", art["crate"], new Vector2(6.60f, GroundTop + 0.35f), 0.9f, 5.0f, mat, phys, Kind.Wood);

        // Cerdos
        AddPig("Cerdo1", art["pig"], new Vector2(3.10f, GroundTop + 0.383f), mat, phys);
        AddPig("Cerdo2", art["pig"], new Vector2(3.10f, GroundTop + 3.623f), mat, phys);
        AddPig("Cerdo3", art["pig"], new Vector2(6.60f, GroundTop + 1.083f), mat, phys);

        // GameManager
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gm.birdTemplate = bird;
        gm.birdAnchor = anchorGo.transform;
        gm.slingshot = sling;
        gm.followCamera = follow;
        gm.dotSprite = art["dot"];
        gm.totalBirds = 3;
        gm.pigsTotal = 3;
        sling.game = gm;

        // Guardar escena
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.companyName = "Sergio Garduno";
        PlayerSettings.productName = "Angry Birds 2D - MCP";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        Debug.Log("[AngryBirds] Escena creada en " + ScenePath);
    }

    static GameObject SpriteObject(string name, Sprite sprite, Vector3 position, Material mat, int order)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = mat;
        sr.sortingOrder = order;
        return go;
    }

    static LineRenderer CreateBand(string name, Material mat, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = mat;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = 0.075f;
        line.endWidth = 0.055f;
        line.numCapVertices = 4;
        line.startColor = new Color(0.28f, 0.16f, 0.09f);
        line.endColor = new Color(0.40f, 0.24f, 0.12f);
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    static void AddBlock(string name, Sprite sprite, Vector2 position, float mass, float toughness,
                         Material mat, PhysicsMaterial2D phys, Kind kind)
    {
        var go = SpriteObject(name, sprite, new Vector3(position.x, position.y, 0f), mat, 5);
        var size = sprite.bounds.size;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(size.x, size.y);
        col.sharedMaterial = phys;

        var body = go.AddComponent<Rigidbody2D>();
        body.mass = mass;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        body.linearDamping = 0.25f;
        body.angularDamping = 0.55f;
        body.sleepMode = RigidbodySleepMode2D.StartAwake;

        var destructible = go.AddComponent<Destructible>();
        destructible.toughness = toughness;
        switch (kind)
        {
            case Kind.Stone:
                destructible.popColor = new Color(0.66f, 0.68f, 0.70f, 1f);
                destructible.points = 800;
                break;
            case Kind.Ice:
                destructible.popColor = new Color(0.72f, 0.90f, 1f, 1f);
                destructible.points = 300;
                break;
            default:
                destructible.popColor = new Color(0.78f, 0.54f, 0.28f, 1f);
                destructible.points = 500;
                break;
        }
    }

    static void AddPig(string name, Sprite sprite, Vector2 position, Material mat, PhysicsMaterial2D phys)
    {
        var go = SpriteObject(name, sprite, new Vector3(position.x, position.y, 0f), mat, 6);
        go.transform.localScale = Vector3.one * 0.82f;
        var circle = go.AddComponent<CircleCollider2D>();
        circle.radius = sprite.bounds.size.x * 0.5f * 0.92f;
        circle.sharedMaterial = phys;

        var body = go.AddComponent<Rigidbody2D>();
        body.mass = 0.85f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        body.linearDamping = 0.2f;
        body.angularDamping = 0.4f;

        go.AddComponent<Pig>();
    }

    // ------------------------------------------------------------------ build

    public static void BuildWindows()
    {
        if (!File.Exists(BuildExe) && !Directory.Exists(BuildDir)) Directory.CreateDirectory(BuildDir);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = BuildExe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception("[AngryBirds] Build fallido: " + summary.result +
                                       " errores=" + summary.totalErrors);
        }
        Debug.Log("[AngryBirds] Ejecutable creado: " + BuildExe + " (" + summary.totalSize + " bytes)");
    }
}
