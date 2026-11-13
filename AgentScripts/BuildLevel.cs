using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class BuildLevel
{
    const string Root = "Assets/_Project";
    const string Art = Root + "/Art/Kenney";
    const string Platformer = Art + "/PlatformerKit";
    const string Nature = Art + "/NatureKit";
    const string Prefabs = Root + "/Prefabs";
    const string Materials = Root + "/Materials";
    const string Scenes = Root + "/Scenes";

    static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

    static Transform GroupIslands, GroupCoins, GroupProps, GroupClouds, GroupDistant;

    static readonly Dictionary<string, string> NatureMap = new Dictionary<string, string>
    {
        {"grass","M_Grass"}, {"dirt","M_Dirt"}, {"stone","M_Stone"}, {"stoneDark","M_StoneDark"},
        {"woodBark","M_Wood"}, {"woodBarkDark","M_WoodDark"}, {"woodInner","M_WoodLight"},
        {"leafsGreen","M_Leaf"}, {"leafsDark","M_LeafDark"},
        {"colorRed","M_FlowerRed"}, {"colorYellow","M_FlowerYellow"}, {"colorPurple","M_FlowerPurple"},
        {"colorTan","M_Wood"}, {"_defaultMat","M_Stone"}
    };

    public static string Run()
    {
        var log = new StringBuilder();
        AssetDatabase.Refresh();
        EnsureFolders();
        CreateMaterials();
        ConfigureUrp(log);
        CreatePrefabs(log);
        BuildScene(log);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        log.AppendLine("BuildLevel complete.");
        return log.ToString();
    }

    static void EnsureFolders()
    {
        foreach (var f in new[] { Root + "/Art", Prefabs, Materials, Scenes, Root + "/Scripts", Root + "/UI" })
            if (!AssetDatabase.IsValidFolder(f))
            {
                var parent = System.IO.Path.GetDirectoryName(f).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(f));
            }
    }

    static Material Mat(string name, string hex, float smoothness = 0.12f, float metallic = 0f, float emission = 0f)
    {
        string path = Materials + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", metallic);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emission);
        }
        else
        {
            m.DisableKeyword("_EMISSION");
        }
        EditorUtility.SetDirty(m);
        Mats[name] = m;
        return m;
    }

    static void CreateMaterials()
    {
        Mat("M_Grass", "#74C365", 0.08f);
        Mat("M_GrassDark", "#4E9E54", 0.08f);
        Mat("M_Dirt", "#A9744F", 0.05f);
        Mat("M_Stone", "#8E9AA8", 0.12f);
        Mat("M_StoneLight", "#B8C3CE", 0.12f);
        Mat("M_StoneDark", "#6E7986", 0.1f);
        Mat("M_Wood", "#C28B5B", 0.1f);
        Mat("M_WoodDark", "#8A5A3B", 0.1f);
        Mat("M_WoodLight", "#D9A876", 0.1f);
        Mat("M_Leaf", "#5FBE68", 0.08f);
        Mat("M_LeafDark", "#3E9A56", 0.08f);
        Mat("M_Rock", "#7E8A97", 0.08f);
        Mat("M_RockDark", "#66717D", 0.08f);
        Mat("M_Cloud", "#FFFFFF", 0f);
        Mat("M_Gold", "#FFC83D", 0.65f, 0.45f, 0.35f);
        Mat("M_Gem", "#55D6E8", 0.75f, 0.2f, 0.5f);
        Mat("M_FlowerRed", "#FF6B6B", 0.1f);
        Mat("M_FlowerYellow", "#FFD166", 0.1f);
        Mat("M_FlowerPurple", "#B388EB", 0.1f);
        Mat("M_Shirt", "#FF6B6B", 0.15f);
        Mat("M_Pants", "#33475B", 0.15f);
        Mat("M_Skin", "#F3C39C", 0.15f);
        Mat("M_Hair", "#4A2F27", 0.15f);
        Mat("M_Socks", "#F5F5F5", 0.15f);
        Mat("M_Eyes", "#2B2B2B", 0.3f);
    }

    static void ConfigureUrp(StringBuilder log)
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            var so = new SerializedObject(urp);
            var dist = so.FindProperty("m_ShadowDistance");
            if (dist != null) dist.floatValue = 140f;
            var msaa = so.FindProperty("m_MSAA");
            if (msaa != null) msaa.intValue = 4;
            var soft = so.FindProperty("m_SoftShadowQuality");
            if (soft != null) soft.intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("URP configured: shadows=" + (dist != null ? dist.floatValue.ToString() : "?") + " msaa=" + (msaa != null ? msaa.intValue.ToString() : "?"));
        }

        var rd = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/PC_Renderer.asset");
        if (rd != null)
        {
            var so = new SerializedObject(rd);
            var arr = so.FindProperty("m_RendererFeatures");
            for (int i = 0; i < arr.arraySize; i++)
            {
                var feat = arr.GetArrayElementAtIndex(i).objectReferenceValue as ScreenSpaceAmbientOcclusion;
                if (feat == null) continue;
                var fso = new SerializedObject(feat);
                var intensity = fso.FindProperty("m_Settings.Intensity");
                var radius = fso.FindProperty("m_Settings.Radius");
                var samples = fso.FindProperty("m_Settings.Samples");
                var falloff = fso.FindProperty("m_Settings.Falloff");
                var active = fso.FindProperty("m_Active");
                if (intensity != null) intensity.floatValue = 0.9f;
                if (radius != null) radius.floatValue = 0.55f;
                if (samples != null) samples.enumValueIndex = 2;
                if (falloff != null) falloff.floatValue = 100f;
                if (active != null) active.boolValue = true;
                fso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(feat);
                log.AppendLine("SSAO tuned on PC_Renderer.");
            }
            EditorUtility.SetDirty(rd);
        }
    }

    static GameObject Fbx(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new Exception("Missing FBX: " + path);
        return (GameObject)PrefabUtility.InstantiatePrefab(asset);
    }

    static void Remap(GameObject go, Dictionary<string, string> map, string fallback = null)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var sm = r.sharedMaterials;
            for (int i = 0; i < sm.Length; i++)
            {
                if (sm[i] == null) continue;
                string key = sm[i].name;
                if (map.TryGetValue(key, out var target) && Mats.ContainsKey(target)) sm[i] = Mats[target];
                else if (fallback != null && Mats.ContainsKey(fallback)) sm[i] = Mats[fallback];
            }
            r.sharedMaterials = sm;
        }
    }

    static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
    }

    static void AddUnderside(Transform parent, float sx, float sz, int seed)
    {
        string[] rocks = { "rock_largeA", "rock_largeB", "rock_largeC", "rock_largeD", "rock_largeE", "rock_largeF" };
        var rnd = new System.Random(seed);
        int count = 4;
        for (int i = 0; i < count; i++)
        {
            var rock = Fbx(Nature + "/" + rocks[i % rocks.Length] + ".fbx");
            Remap(rock, NatureMap, "M_Rock");
            rock.transform.SetParent(parent, false);
            float px = ((float)rnd.NextDouble() - 0.5f) * sx * 0.45f;
            float pz = ((float)rnd.NextDouble() - 0.5f) * sz * 0.45f;
            rock.transform.localPosition = new Vector3(px, -1.25f - 0.28f * i, pz);
            rock.transform.localRotation = Quaternion.Euler(180f, (float)rnd.Next(0, 360), 0f);
            rock.transform.localScale = new Vector3(sx * 0.5f, 1.6f + 0.3f * i, sz * 0.5f);
        }
    }

    static void MakeGrass(string name, float sx, float sz, bool hex)
    {
        var root = new GameObject(name);
        var block = Fbx(Platformer + (hex ? "/block-grass-hexagon.fbx" : "/block-grass-large.fbx"));
        block.name = "Top";
        block.transform.SetParent(root.transform, false);
        float baseW = hex ? 1.10f : 2.08f;
        float baseD = hex ? 1.27f : 2.08f;
        block.transform.localScale = new Vector3(sx / baseW, 1f, sz / baseD);
        block.transform.localPosition = new Vector3(0f, -1f, 0f);
        AddUnderside(root.transform, sx, sz, name.GetHashCode());
        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, -0.5f, 0f);
        bc.size = new Vector3(sx, 1f, sz);
        SavePrefab(root, Prefabs + "/" + name + ".prefab");
    }

    static void MakeStone(string name, float sx, float sz)
    {
        var root = new GameObject(name);
        var block = Fbx(Nature + "/cliff_block_stone.fbx");
        block.name = "Top";
        block.transform.SetParent(root.transform, false);
        var map = new Dictionary<string, string>(NatureMap);
        map["grass"] = "M_StoneLight";
        Remap(block, map, "M_Stone");
        block.transform.localScale = new Vector3(sx, 1.1f, sz);
        block.transform.localPosition = new Vector3(0f, -1.05f, 0f);
        AddUnderside(root.transform, sx, sz, name.GetHashCode());
        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, -0.5f, 0f);
        bc.size = new Vector3(sx, 1f, sz);
        SavePrefab(root, Prefabs + "/" + name + ".prefab");
    }

    static void MakeWood(string name, float sx, float sz)
    {
        var root = new GameObject(name);
        int nx = Mathf.Max(1, Mathf.RoundToInt(sx));
        int nz = Mathf.Max(1, Mathf.RoundToInt(sz));
        float px = sx / nx, pz = sz / nz;
        var deck = new GameObject("Deck");
        deck.transform.SetParent(root.transform, false);
        for (int x = 0; x < nx; x++)
        for (int z = 0; z < nz; z++)
        {
            var plank = Fbx(Platformer + "/platform.fbx");
            plank.name = "Plank";
            plank.transform.SetParent(deck.transform, false);
            plank.transform.localPosition = new Vector3(-sx * 0.5f + px * (x + 0.5f), -0.2f, -sz * 0.5f + pz * (z + 0.5f));
            plank.transform.localScale = new Vector3(px, 1f, pz);
            plank.transform.localRotation = Quaternion.Euler(0f, (x + z) % 2 == 0 ? 0f : 90f, 0f);
        }
        var beam = Fbx(Platformer + "/platform-overhang.fbx");
        beam.name = "Beam";
        beam.transform.SetParent(root.transform, false);
        beam.transform.localPosition = new Vector3(0f, -0.45f, -sz * 0.5f + 0.3f);
        beam.transform.localScale = new Vector3(sx, 1f, 1f);
        for (int i = 0; i < 2; i++)
        {
            var crate = Fbx(Platformer + "/crate.fbx");
            crate.name = "Support";
            crate.transform.SetParent(root.transform, false);
            crate.transform.localPosition = new Vector3((i == 0 ? -1f : 1f) * sx * 0.3f, -0.75f, -sz * 0.25f);
            crate.transform.localScale = Vector3.one * 0.9f;
        }
        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, -0.1f, 0f);
        bc.size = new Vector3(sx, 0.2f, sz);
        SavePrefab(root, Prefabs + "/" + name + ".prefab");
    }

    static void MakeProp(string fbx, string name, float scale = 1f, string fallback = "M_Rock")
    {
        var go = Fbx(fbx);
        go.name = name;
        Remap(go, NatureMap, fallback);
        go.transform.localScale *= scale;
        SavePrefab(go, Prefabs + "/" + name + ".prefab");
    }

    static void MakeCloud()
    {
        var root = new GameObject("Cloud");
        string[] rocks = { "rock_smallFlatA", "rock_smallFlatB", "rock_smallFlatC", "rock_largeD", "rock_smallTopA" };
        var offsets = new[] {
            new Vector3(0f, 0f, 0f), new Vector3(1.9f, 0.05f, 0.35f), new Vector3(-1.8f, -0.02f, -0.25f),
            new Vector3(0.5f, 0.55f, -1.0f), new Vector3(-0.7f, 0.32f, 1.05f)
        };
        var scales = new[] {
            new Vector3(4.2f, 1.15f, 3.4f), new Vector3(3.0f, 1.0f, 2.6f), new Vector3(2.8f, 0.95f, 2.4f),
            new Vector3(2.2f, 1.35f, 2.0f), new Vector3(2.0f, 1.1f, 1.9f)
        };
        for (int i = 0; i < rocks.Length; i++)
        {
            var puff = Fbx(Nature + "/" + rocks[i] + ".fbx");
            puff.name = "Puff";
            Remap(puff, NatureMap, "M_Cloud");
            puff.transform.SetParent(root.transform, false);
            puff.transform.localPosition = offsets[i];
            puff.transform.localRotation = Quaternion.Euler(0f, i * 63f, 0f);
            puff.transform.localScale = scales[i];
        }
        SavePrefab(root, Prefabs + "/Cloud.prefab");
    }

    static void MakeCoin()
    {
        var root = new GameObject("Coin");
        var coin = Fbx(Platformer + "/coin-gold.fbx");
        coin.name = "Visual";
        coin.transform.SetParent(root.transform, false);
        var sc = root.AddComponent<SphereCollider>();
        sc.center = new Vector3(0f, 0.22f, 0f);
        sc.radius = 0.55f;
        sc.isTrigger = true;
        SavePrefab(root, Prefabs + "/Coin.prefab");
    }

    static void CreatePrefabs(StringBuilder log)
    {
        MakeGrass("Platform_Grass_S", 3.2f, 3.2f, false);
        MakeGrass("Platform_Grass_M", 5.0f, 5.0f, false);
        MakeGrass("Platform_Grass_L", 7.5f, 7.5f, false);
        MakeGrass("Platform_Grass_Hex", 4.2f, 4.2f, true);
        MakeStone("Platform_Stone_S", 3.2f, 3.2f);
        MakeStone("Platform_Stone_M", 5.0f, 5.0f);
        MakeWood("Platform_Wood_S", 3.2f, 3.2f);
        MakeWood("Platform_Wood_M", 4.5f, 4.5f);

        MakeProp(Nature + "/tree_default.fbx", "Tree_Default", 1.5f, "M_Leaf");
        MakeProp(Nature + "/tree_detailed.fbx", "Tree_Detailed", 1.8f, "M_Leaf");
        MakeProp(Nature + "/tree_pineTallA.fbx", "Tree_Pine", 1.7f, "M_LeafDark");
        MakeProp(Nature + "/tree_palm.fbx", "Tree_Palm", 1.8f, "M_Leaf");
        MakeProp(Nature + "/rock_largeA.fbx", "Rock_A", 1.3f, "M_Rock");
        MakeProp(Nature + "/rock_largeB.fbx", "Rock_B", 1.1f, "M_Rock");
        MakeProp(Nature + "/rock_largeC.fbx", "Rock_C", 1.0f, "M_Rock");
        MakeProp(Nature + "/plant_bushLarge.fbx", "Bush", 1.8f, "M_Leaf");
        MakeProp(Nature + "/flower_redA.fbx", "Flowers_Red", 2.2f, "M_FlowerRed");
        MakeProp(Nature + "/flower_yellowA.fbx", "Flowers_Yellow", 2.2f, "M_FlowerYellow");
        MakeProp(Nature + "/mushroom_red.fbx", "Mushroom_Red", 1.8f, "M_FlowerRed");
        MakeProp(Nature + "/grass_large.fbx", "Grass_Tuft", 2.2f, "M_GrassDark");
        MakeProp(Platformer + "/crate.fbx", "Crate", 1f, null);
        MakeProp(Platformer + "/barrel.fbx", "Barrel", 1f, null);
        MakeProp(Platformer + "/flag.fbx", "Flag", 1.2f, null);
        MakeProp(Nature + "/log.fbx", "Log", 1.2f, "M_Wood");
        MakeProp(Nature + "/stump_round.fbx", "Stump", 1.4f, "M_Wood");
        MakeCloud();
        MakeCoin();
        log.AppendLine("Prefabs created.");
    }

    static GameObject AddIsland(string prefabName, float x, float y, float z, float yaw, string label)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + prefabName + ".prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.name = label;
        go.transform.position = new Vector3(x, y, z);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (GroupIslands != null) go.transform.SetParent(GroupIslands, true);
        return go;
    }

    static void AddCoin(float x, float y, float z, float yaw = 0f)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Coin.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.name = "Coin";
        go.transform.position = new Vector3(x, y, z);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (GroupCoins != null) go.transform.SetParent(GroupCoins, true);
    }

    static void CoinArc(Vector3 a, Vector3 b, int count = 3, float peak = 1.1f)
    {
        for (int i = 1; i <= count; i++)
        {
            float t = i / (float)(count + 1);
            var p = Vector3.Lerp(a, b, t);
            p.y = Mathf.Lerp(a.y, b.y, t) + Mathf.Sin(t * Mathf.PI) * peak + 0.9f;
            AddCoin(p.x, p.y, p.z, t * 180f);
        }
    }

    static void ScatterDecor(GameObject island, float sx, float sz, int seed, bool dense)
    {
        var rnd = new System.Random(seed);
        string[] trees = { "Tree_Default", "Tree_Detailed", "Tree_Pine", "Tree_Palm" };
        string[] small = { "Bush", "Flowers_Red", "Flowers_Yellow", "Mushroom_Red", "Grass_Tuft", "Rock_A", "Rock_B" };
        int treeCount = dense ? 3 : 1;
        for (int i = 0; i < treeCount; i++)
        {
            var t = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + trees[rnd.Next(trees.Length)] + ".prefab")) as GameObject;
            t.transform.SetParent(island.transform, true);
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(0.28f, 0.42f, (float)rnd.NextDouble());
            t.transform.localPosition = new Vector3(Mathf.Cos(ang) * sx * rad, 0f, Mathf.Sin(ang) * sz * rad);
            t.transform.localRotation = Quaternion.Euler(0f, (float)rnd.Next(0, 360), 0f);
            t.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.15f, (float)rnd.NextDouble());
        }
        int smallCount = dense ? 8 : 4;
        for (int i = 0; i < smallCount; i++)
        {
            var s = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + small[rnd.Next(small.Length)] + ".prefab")) as GameObject;
            s.transform.SetParent(island.transform, true);
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(0.3f, 0.46f, (float)rnd.NextDouble());
            s.transform.localPosition = new Vector3(Mathf.Cos(ang) * sx * rad, 0.02f, Mathf.Sin(ang) * sz * rad);
            s.transform.localRotation = Quaternion.Euler(0f, (float)rnd.Next(0, 360), 0f);
            s.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.2f, (float)rnd.NextDouble());
        }
    }

    static void BuildScene(StringBuilder log)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var level = new GameObject("Level");
        var env = new GameObject("Environment");

        GroupIslands = new GameObject("Islands").transform;
        GroupIslands.SetParent(level.transform);
        GroupCoins = new GameObject("Coins").transform;
        GroupCoins.SetParent(level.transform);
        GroupProps = new GameObject("Props").transform;
        GroupProps.SetParent(level.transform);
        GroupClouds = new GameObject("Clouds").transform;
        GroupClouds.SetParent(env.transform);
        GroupDistant = new GameObject("Distant Islands").transform;
        GroupDistant.SetParent(env.transform);

        // Skybox
        var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Skyboxes/skybox-day.png");
        var skyPath = Materials + "/M_Skybox.mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Panoramic"));
            AssetDatabase.CreateAsset(sky, skyPath);
        }
        sky.shader = Shader.Find("Skybox/Panoramic");
        sky.SetTexture("_MainTex", skyTex);
        sky.SetFloat("_Mapping", 1f);
        sky.SetFloat("_ImageType", 0f);
        sky.SetFloat("_Exposure", 0.55f);
        sky.SetColor("_Tint", new Color(0.85f, 0.92f, 1f, 1f));
        EditorUtility.SetDirty(sky);
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.9f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.70f, 0.87f, 0.97f);
        RenderSettings.fogStartDistance = 65f;
        RenderSettings.fogEndDistance = 190f;

        // Sun
        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(env.transform);
        sunGo.transform.rotation = Quaternion.Euler(46f, -34f, 0f);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.86f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.82f;
        sun.shadowBias = 0.04f;
        sun.shadowNormalBias = 0.3f;

        // Global post processing
        var profilePath = Materials + "/SkyboundVolume.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null)
            AssetDatabase.DeleteAsset(profilePath);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, profilePath);
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.55f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.65f);
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);
        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.08f);
        ca.contrast.Override(8f);
        ca.saturation.Override(14f);
        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.Override(10f);
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.2f);
        vig.smoothness.Override(0.45f);
        foreach (var c in profile.components)
        {
            c.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
        }
        EditorUtility.SetDirty(profile);

        var volGo = new GameObject("Global Volume");
        volGo.transform.SetParent(env.transform);
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.sharedProfile = profile;

        // Islands: (prefab, x, topY, z, yaw, label, size)
        AddIsland("Platform_Grass_L", 0f, 0f, 0f, 0f, "Island_Start");
        AddIsland("Platform_Grass_M", 0.5f, 0.3f, 9.5f, 10f, "Island_01");
        AddIsland("Platform_Grass_S", 5.8f, 1.4f, 14.6f, -18f, "Island_02");
        AddIsland("Platform_Wood_M", 11.4f, 2.4f, 18.4f, 22f, "Island_03");
        AddIsland("Platform_Stone_M", 15.2f, 3.6f, 24.2f, -8f, "Island_04");
        AddIsland("Platform_Grass_S", 10.6f, 4.8f, 29.2f, 30f, "Island_05");
        AddIsland("Platform_Grass_Hex", 4.0f, 5.5f, 33.4f, 12f, "Island_06");
        AddIsland("Platform_Grass_M", -3.8f, 5.0f, 37.2f, -22f, "Island_07");
        AddIsland("Platform_Stone_S", -9.6f, 6.6f, 42.4f, 16f, "Island_08");
        AddIsland("Platform_Grass_S", -7.0f, 8.0f, 48.2f, -12f, "Island_09");
        AddIsland("Platform_Wood_M", 0.2f, 9.0f, 52.4f, 26f, "Island_10");
        AddIsland("Platform_Grass_L", 8.4f, 10.0f, 57.4f, -14f, "Island_Goal");

        // Optional hard-jump islands
        AddIsland("Platform_Grass_S", 18.6f, 4.4f, 21.0f, 35f, "Island_Bonus_01");
        AddIsland("Platform_Stone_S", -13.2f, 8.6f, 33.0f, -25f, "Island_Bonus_02");
        AddIsland("Platform_Grass_S", 14.8f, 12.6f, 47.5f, 20f, "Island_Bonus_03");

        // Coins along the main route
        CoinArc(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.3f, 9.5f), 3, 1.2f);
        CoinArc(new Vector3(0.5f, 0.3f, 9.5f), new Vector3(5.8f, 1.4f, 14.6f), 3, 1.3f);
        CoinArc(new Vector3(5.8f, 1.4f, 14.6f), new Vector3(11.4f, 2.4f, 18.4f), 3, 1.1f);
        CoinArc(new Vector3(11.4f, 2.4f, 18.4f), new Vector3(15.2f, 3.6f, 24.2f), 3, 1.2f);
        CoinArc(new Vector3(15.2f, 3.6f, 24.2f), new Vector3(10.6f, 4.8f, 29.2f), 3, 1.4f);
        CoinArc(new Vector3(10.6f, 4.8f, 29.2f), new Vector3(4.0f, 5.5f, 33.4f), 3, 1.1f);
        CoinArc(new Vector3(4.0f, 5.5f, 33.4f), new Vector3(-3.8f, 5.0f, 37.2f), 3, 1.2f);
        CoinArc(new Vector3(-3.8f, 5.0f, 37.2f), new Vector3(-9.6f, 6.6f, 42.4f), 3, 1.3f);
        CoinArc(new Vector3(-9.6f, 6.6f, 42.4f), new Vector3(-7.0f, 8.0f, 48.2f), 3, 1.4f);
        CoinArc(new Vector3(-7.0f, 8.0f, 48.2f), new Vector3(0.2f, 9.0f, 52.4f), 3, 1.2f);
        CoinArc(new Vector3(0.2f, 9.0f, 52.4f), new Vector3(8.4f, 10.0f, 57.4f), 4, 1.3f);

        // Coins on platforms
        AddCoin(0f, 1.0f, 2.4f, 20f); AddCoin(-1.6f, 1.0f, 1.0f, 60f); AddCoin(1.7f, 1.0f, 1.1f, 100f);
        AddCoin(0.5f, 1.4f, 11.0f, 45f); AddCoin(1.6f, 1.4f, 9.2f, 80f);
        AddCoin(5.8f, 2.5f, 15.4f, 15f); AddCoin(11.4f, 3.5f, 19.0f, 70f);
        AddCoin(15.2f, 4.7f, 24.8f, 25f); AddCoin(14.2f, 4.7f, 23.4f, 95f);
        AddCoin(4.0f, 6.6f, 33.4f, 55f); AddCoin(-3.8f, 6.1f, 37.6f, 15f);
        AddCoin(-9.6f, 7.7f, 42.6f, 65f); AddCoin(-7.0f, 9.1f, 48.6f, 30f);
        AddCoin(0.2f, 10.1f, 52.8f, 80f); AddCoin(8.4f, 11.1f, 57.8f, 10f);

        // Bonus coins
        AddCoin(18.6f, 5.5f, 21.0f, 0f); AddCoin(18.6f, 5.5f, 21.9f, 60f); AddCoin(17.8f, 5.5f, 21.0f, 120f);
        AddCoin(-13.2f, 9.7f, 33.0f, 0f); AddCoin(-13.2f, 9.7f, 33.9f, 60f); AddCoin(-12.4f, 9.7f, 33.0f, 120f);
        AddCoin(14.8f, 13.7f, 47.5f, 0f); AddCoin(14.8f, 13.7f, 48.4f, 60f); AddCoin(14.0f, 13.7f, 47.5f, 120f);

        // Decorate islands
        var names = new List<string> { "Island_Start", "Island_01", "Island_02", "Island_03", "Island_04", "Island_05", "Island_06", "Island_07", "Island_08", "Island_09", "Island_10", "Island_Goal", "Island_Bonus_01", "Island_Bonus_02", "Island_Bonus_03" };
        int idx = 0;
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go == null) continue;
            float sx = 4.5f, sz = 4.5f;
            if (n.Contains("_L")) { sx = 7.5f; sz = 7.5f; }
            else if (n.Contains("_S")) { sx = 3.2f; sz = 3.2f; }
            else if (n.Contains("Hex")) { sx = 4.2f; sz = 4.2f; }
            else if (n.Contains("Wood_M") || n.Contains("Stone_M")) { sx = 4.5f; sz = 4.5f; }
            ScatterDecor(go, sx, sz, idx * 7919 + 17, n == "Island_Start" || n == "Island_Goal");
            idx++;
        }

        // Flags at start and goal
        var startFlag = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Flag.prefab")) as GameObject;
        startFlag.transform.position = new Vector3(-3.0f, 0f, -2.6f);
        startFlag.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
        startFlag.transform.SetParent(GroupProps, true);
        var goalFlag = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Flag.prefab")) as GameObject;
        goalFlag.transform.position = new Vector3(8.4f, 10f, 54.6f);
        goalFlag.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
        goalFlag.transform.localScale = Vector3.one * 2.2f;
        goalFlag.transform.SetParent(GroupProps, true);

        // Crates and barrels on wooden platforms
        var crate = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Crate.prefab")) as GameObject;
        crate.transform.position = new Vector3(12.6f, 2.4f, 17.2f);
        crate.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
        crate.transform.SetParent(GroupProps, true);
        var barrel = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Barrel.prefab")) as GameObject;
        barrel.transform.position = new Vector3(10.2f, 2.4f, 19.4f);
        barrel.transform.rotation = Quaternion.Euler(0f, 42f, 0f);
        barrel.transform.SetParent(GroupProps, true);

        // Clouds
        var cloudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Cloud.prefab");
        var rnd = new System.Random(4242);
        for (int i = 0; i < 16; i++)
        {
            var c = (GameObject)PrefabUtility.InstantiatePrefab(cloudPrefab);
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(28f, 85f, (float)rnd.NextDouble());
            c.transform.position = new Vector3(Mathf.Cos(ang) * rad, Mathf.Lerp(6f, 26f, (float)rnd.NextDouble()), Mathf.Sin(ang) * rad + 26f);
            float s = Mathf.Lerp(0.9f, 2.2f, (float)rnd.NextDouble());
            c.transform.localScale = new Vector3(s, s * 0.8f, s);
            c.transform.rotation = Quaternion.Euler(0f, (float)rnd.Next(0, 360), 0f);
            c.transform.SetParent(GroupClouds, true);
        }

        // Distant floating islands
        var distant = new[] { "Platform_Grass_M", "Platform_Stone_M", "Platform_Wood_M", "Platform_Grass_S" };
        for (int i = 0; i < 12; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(38f, 95f, (float)rnd.NextDouble());
            var d = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + distant[i % distant.Length] + ".prefab"));
            d.transform.position = new Vector3(Mathf.Cos(ang) * rad, Mathf.Lerp(-16f, 16f, (float)rnd.NextDouble()), Mathf.Sin(ang) * rad + 26f);
            d.transform.rotation = Quaternion.Euler(0f, (float)rnd.Next(0, 360), 0f);
            float s = Mathf.Lerp(1.2f, 2.8f, (float)rnd.NextDouble());
            d.transform.localScale = new Vector3(s, s, s);
            d.transform.SetParent(GroupDistant, true);
            foreach (var col in d.GetComponentsInChildren<Collider>(true)) col.enabled = false;
        }

        // Spawn point marker
        var spawn = new GameObject("SpawnPoint");
        spawn.transform.position = new Vector3(0f, 0.1f, -1.5f);
        spawn.transform.SetParent(level.transform);

        // Camera placeholder (replaced by CameraFollow rig later)
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 400f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 4.2f, -8.5f);
        camGo.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

        EditorSceneManager.SaveScene(scene, Scenes + "/Skybound.unity");

        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in buildScenes) if (s.path == Scenes + "/Skybound.unity") found = true;
        if (!found) buildScenes.Insert(0, new EditorBuildSettingsScene(Scenes + "/Skybound.unity", true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        log.AppendLine("Scene built and saved.");
    }
}
