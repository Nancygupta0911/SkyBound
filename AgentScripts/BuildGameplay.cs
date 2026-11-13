using System.IO;
using System.Text;
using Skybound;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class BuildGameplay
{
    const string Root = "Assets/_Project";
    const string UIDir = Root + "/UI";
    const string SpriteDir = UIDir + "/Sprites";
    const string FontDir = UIDir + "/Fonts";
    const string Materials = Root + "/Materials";
    const string Prefabs = Root + "/Prefabs";
    const string ScenePath = Root + "/Scenes/Skybound.unity";
    const string BurstMaterialPath = Materials + "/M_CoinBurst.mat";
    const string BurstPrefabPath = Prefabs + "/CoinBurst.prefab";

    public static string Run()
    {
        var log = new StringBuilder();
        AssetDatabase.Refresh();

        EnsureFolder(UIDir);
        EnsureFolder(SpriteDir);
        EnsureFolder(FontDir);

        TMP_FontAsset font = EnsureFont(log);
        Sprite panelSprite = EnsurePanelSprite(log);
        Sprite coinIcon = EnsureCoinIcon(log);
        Material burstMaterial = EnsureBurstMaterial(log);
        GameObject burstPrefab = EnsureBurstPrefab(burstMaterial, log);
        UpdateCoinPrefab(burstPrefab, log);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureGameManager(log);
        EnsureHud(font, panelSprite, coinIcon, log);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        log.AppendLine("BuildGameplay complete.");
        return log.ToString();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static TMP_FontAsset EnsureFont(StringBuilder log)
    {
        string path = FontDir + "/KenneyFuture SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null) return existing;

        var ttf = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Art/Kenney/Kenney Future.ttf");
        if (ttf == null)
        {
            log.AppendLine("Kenney Future font missing, falling back to TMP default.");
            return TMP_Settings.defaultFontAsset;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, true);
        fontAsset.name = "KenneyFuture SDF";
        AssetDatabase.CreateAsset(fontAsset, path);

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "KenneyFuture SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "KenneyFuture SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        log.AppendLine("Created TMP font asset: " + path);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
    }

    static Sprite EnsurePanelSprite(StringBuilder log)
    {
        string path = SpriteDir + "/UI_Panel.png";
        if (!File.Exists(path)) GenerateRoundedPanel(path, 128, 30f);
        ConfigureSprite(path, new Vector4(38f, 38f, 38f, 38f), log, "UI_Panel");
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite EnsureCoinIcon(StringBuilder log)
    {
        string path = SpriteDir + "/UI_CoinIcon.png";
        if (!File.Exists(path)) GenerateCoinIcon(path, 128);
        ConfigureSprite(path, Vector4.zero, log, "UI_CoinIcon");
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void ConfigureSprite(string path, Vector4 border, StringBuilder log, string label)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            log.AppendLine("Missing importer for " + label);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = border;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        log.AppendLine("Configured sprite: " + label);
    }

    static void GenerateRoundedPanel(string path, int size, float radius)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float half = size * 0.5f;
        float inner = half - radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - inner, 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - inner, 0f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        WriteTexture(path, texture, pixels);
    }

    static void GenerateCoinIcon(string path, int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float center = size * 0.5f;

        var rim = new Color(0.93f, 0.62f, 0.13f);
        var face = new Color(1f, 0.80f, 0.27f);
        var inner = new Color(1f, 0.90f, 0.55f);
        var ring = new Color(0.98f, 0.71f, 0.18f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                Color color = Color.clear;

                if (distance <= 60f)
                {
                    Color surface = rim;
                    if (distance <= 55f) surface = face;
                    if (distance <= 46f) surface = inner;
                    if (distance >= 38f && distance <= 43f) surface = ring;

                    float hx = x + 0.5f - (center - 15f);
                    float hy = y + 0.5f - (center + 17f);
                    float highlight = Mathf.Sqrt(hx * hx + hy * hy);
                    if (highlight < 12f)
                        surface = Color.Lerp(surface, Color.white, Mathf.Clamp01((12f - highlight) / 5f) * 0.7f);

                    color = surface;
                    color.a = Mathf.Clamp01(60f - distance);
                }

                pixels[y * size + x] = color;
            }
        }

        WriteTexture(path, texture, pixels);
    }

    static void WriteTexture(string path, Texture2D texture, Color32[] pixels)
    {
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    static Material EnsureBurstMaterial(StringBuilder log)
    {
        var shader = Shader.Find("Skybound/ParticleAdditive");
        if (shader == null)
        {
            log.AppendLine("Particle shader missing!");
            return null;
        }

        var existing = AssetDatabase.LoadAssetAtPath<Material>(BurstMaterialPath);
        if (existing != null)
        {
            existing.shader = shader;
            existing.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Kenney/ParticlePack/star_01.png"));
            existing.SetColor("_Color", new Color(1f, 0.85f, 0.42f, 1f));
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var material = new Material(shader) { name = "M_CoinBurst" };
        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Kenney/ParticlePack/star_01.png"));
        material.SetColor("_Color", new Color(1f, 0.85f, 0.42f, 1f));
        AssetDatabase.CreateAsset(material, BurstMaterialPath);
        log.AppendLine("Created coin burst material.");
        return material;
    }

    static GameObject EnsureBurstPrefab(Material material, StringBuilder log)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BurstPrefabPath);
        if (existing != null) return existing;

        var root = new GameObject("CoinBurst");
        var system = root.AddComponent<ParticleSystem>();

        var main = system.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.6f, 5.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.96f, 0.68f), new Color(1f, 0.66f, 0.1f));
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.86f, 0.42f), 0.45f),
                new GradientColorKey(new Color(1f, 0.58f, 0.08f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

        var rotationOverLifetime = system.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sharedMaterial = material;

        PrefabUtility.SaveAsPrefabAsset(root, BurstPrefabPath);
        Object.DestroyImmediate(root);
        log.AppendLine("Created coin burst prefab.");
        return AssetDatabase.LoadAssetAtPath<GameObject>(BurstPrefabPath);
    }

    static void UpdateCoinPrefab(GameObject burstPrefab, StringBuilder log)
    {
        string path = Prefabs + "/Coin.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        var coin = contents.GetComponent<Coin>();
        if (coin == null) coin = contents.AddComponent<Coin>();

        var serialized = new SerializedObject(coin);
        serialized.FindProperty("collectEffect").objectReferenceValue = burstPrefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        log.AppendLine("Coin prefab updated with Coin behaviour.");
    }

    static void EnsureGameManager(StringBuilder log)
    {
        var existing = GameObject.Find("GameManager");
        if (existing == null) existing = new GameObject("GameManager");

        var manager = existing.GetComponent<GameManager>();
        if (manager == null) manager = existing.AddComponent<GameManager>();

        var spawn = GameObject.Find("SpawnPoint");
        var player = Object.FindAnyObjectByType<PlayerController>();

        var serialized = new SerializedObject(manager);
        if (spawn != null) serialized.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
        if (player != null) serialized.FindProperty("player").objectReferenceValue = player;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);

        log.AppendLine("GameManager ready (spawn=" + (spawn != null) + ", player=" + (player != null) + ").");
    }

    static void EnsureHud(TMP_FontAsset font, Sprite panelSprite, Sprite coinIcon, StringBuilder log)
    {
        var existing = GameObject.Find("UI");
        if (existing != null) Object.DestroyImmediate(existing);

        var canvasObject = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var uiManager = canvasObject.AddComponent<UIManager>();

        var hud = NewRect("HUD", canvasObject.transform);
        hud.anchorMin = new Vector2(0f, 1f);
        hud.anchorMax = new Vector2(0f, 1f);
        hud.pivot = new Vector2(0f, 1f);
        hud.anchoredPosition = new Vector2(32f, -32f);
        hud.sizeDelta = new Vector2(390f, 100f);

        var shadow = NewRect("Shadow", hud);
        shadow.anchorMin = Vector2.zero;
        shadow.anchorMax = Vector2.one;
        shadow.offsetMin = new Vector2(4f, -8f);
        shadow.offsetMax = new Vector2(4f, -8f);
        var shadowImage = shadow.gameObject.AddComponent<Image>();
        shadowImage.sprite = panelSprite;
        shadowImage.type = Image.Type.Sliced;
        shadowImage.color = new Color(0.02f, 0.04f, 0.09f, 0.35f);
        shadowImage.raycastTarget = false;

        var panel = hud.gameObject.AddComponent<Image>();
        panel.sprite = panelSprite;
        panel.type = Image.Type.Sliced;
        panel.color = new Color(0.07f, 0.11f, 0.21f, 0.86f);
        panel.raycastTarget = false;

        var icon = NewRect("CoinIcon", hud);
        icon.anchorMin = new Vector2(0f, 0.5f);
        icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = new Vector2(56f, 0f);
        icon.sizeDelta = new Vector2(58f, 58f);
        var iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.sprite = coinIcon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var labelRect = NewRect("CoinLabel", hud);
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(100f, 6f);
        labelRect.offsetMax = new Vector2(-16f, -6f);

        var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = "COINS: 0";
        label.fontSize = 40f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(1f, 0.94f, 0.79f, 1f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        var serialized = new SerializedObject(uiManager);
        serialized.FindProperty("coinLabel").objectReferenceValue = label;
        serialized.FindProperty("coinPulseTarget").objectReferenceValue = labelRect;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);

        log.AppendLine("HUD created with coin counter.");
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
}
