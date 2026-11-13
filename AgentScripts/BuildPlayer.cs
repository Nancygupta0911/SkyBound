using System.Text;
using Skybound;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildPlayer
{
    const string Root = "Assets/_Project";
    const string FbxPath = Root + "/Art/Quaternius/Male_Casual.fbx";
    const string AnimDir = Root + "/Animations";
    const string ControllerPath = AnimDir + "/Player.controller";
    const string PrefabPath = Root + "/Prefabs/Player.prefab";
    const string ScenePath = Root + "/Scenes/Skybound.unity";

    public static string Run()
    {
        var log = new StringBuilder();
        AssetDatabase.Refresh();
        EnsureFolder(AnimDir);
        int playerLayer = EnsurePlayerLayer();
        ConfigureClipLooping(log);
        var controller = BuildController(log);
        BuildPlayerPrefab(controller, playerLayer, log);
        PlaceInScene(playerLayer, log);
        AssetDatabase.SaveAssets();
        log.AppendLine("BuildPlayer complete.");
        return log.ToString();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    static int EnsurePlayerLayer()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return 0;
        var so = new SerializedObject(assets[0]);
        var layers = so.FindProperty("layers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == "Player")
                return i;
        for (int i = 6; i < layers.arraySize; i++)
        {
            var entry = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(entry.stringValue))
            {
                entry.stringValue = "Player";
                so.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        return 0;
    }

    static void ConfigureClipLooping(StringBuilder log)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        if (importer == null) { log.AppendLine("ModelImporter missing"); return; }
        var clips = importer.defaultClipAnimations;
        bool changed = false;
        foreach (var clip in clips)
        {
            if (clip.name.Contains("Man_Idle") || clip.name.Contains("Man_Run") || clip.name.Contains("Man_Walk"))
            {
                if (!clip.loopTime)
                {
                    clip.loopTime = true;
                    changed = true;
                }
            }
        }
        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            log.AppendLine("Enabled looping on locomotion clips.");
        }
        else
        {
            log.AppendLine("Locomotion clips already looping.");
        }
    }

    static AnimationClip FindClip(string suffix)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
        {
            var clip = asset as AnimationClip;
            if (clip == null) continue;
            if (clip.name.StartsWith("__preview__")) continue;
            if (clip.name.EndsWith(suffix)) return clip;
        }
        return null;
    }

    static AnimatorController BuildController(StringBuilder log)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);

        var machine = controller.layers[0].stateMachine;
        foreach (var child in machine.states)
            machine.RemoveState(child.state);

        var idleClip = FindClip("Man_Idle");
        var runClip = FindClip("Man_Run");
        var jumpClip = FindClip("Man_Jump");
        if (idleClip == null || runClip == null || jumpClip == null)
            throw new System.Exception("Missing character animation clips");

        var idle = machine.AddState("Idle", new Vector3(240f, 0f, 0f));
        idle.motion = idleClip;
        idle.speed = 1f;

        var run = machine.AddState("Run", new Vector3(460f, 0f, 0f));
        run.motion = runClip;
        run.speed = 1.05f;

        var jump = machine.AddState("Jump", new Vector3(250f, 120f, 0f));
        jump.motion = jumpClip;
        jump.speed = 1.1f;

        var fall = machine.AddState("Fall", new Vector3(430f, 120f, 0f));
        fall.motion = jumpClip;
        fall.speed = 1.25f;
        fall.cycleOffset = 0.45f;

        machine.defaultState = idle;

        AddTransition(idle, run, 0.14f, ("Speed", AnimatorConditionMode.Greater, 0.12f));
        AddTransition(run, idle, 0.16f, ("Speed", AnimatorConditionMode.Less, 0.12f));

        var toJump = machine.AddAnyStateTransition(jump);
        Configure(toJump, 0.08f);
        toJump.canTransitionToSelf = false;
        toJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        toJump.AddCondition(AnimatorConditionMode.Greater, 0.2f, "VerticalSpeed");

        var toFall = machine.AddAnyStateTransition(fall);
        Configure(toFall, 0.12f);
        toFall.canTransitionToSelf = false;
        toFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        toFall.AddCondition(AnimatorConditionMode.Less, -0.2f, "VerticalSpeed");

        AddTransition(jump, idle, 0.12f, ("Grounded", AnimatorConditionMode.If, 0f), ("Speed", AnimatorConditionMode.Less, 0.12f));
        AddTransition(jump, run, 0.12f, ("Grounded", AnimatorConditionMode.If, 0f), ("Speed", AnimatorConditionMode.Greater, 0.12f));
        AddTransition(fall, idle, 0.14f, ("Grounded", AnimatorConditionMode.If, 0f), ("Speed", AnimatorConditionMode.Less, 0.12f));
        AddTransition(fall, run, 0.14f, ("Grounded", AnimatorConditionMode.If, 0f), ("Speed", AnimatorConditionMode.Greater, 0.12f));

        EditorUtility.SetDirty(controller);
        log.AppendLine("Animator controller created: idle/run/jump/fall");
        return controller;
    }

    static void Configure(AnimatorStateTransition transition, float duration)
    {
        transition.hasExitTime = false;
        transition.exitTime = 0f;
        transition.duration = duration;
        transition.hasFixedDuration = true;
        transition.interruptionSource = TransitionInterruptionSource.None;
    }

    static void AddTransition(AnimatorState from, AnimatorState to, float duration,
        params (string parameter, AnimatorConditionMode mode, float threshold)[] conditions)
    {
        var transition = from.AddTransition(to);
        Configure(transition, duration);
        foreach (var condition in conditions)
            transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
    }

    static void RemapMaterials(GameObject model)
    {
        var materials = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Shirt", "M_Shirt" },
            { "Pants", "M_Pants" },
            { "Skin", "M_Skin" },
            { "Hair", "M_Hair" },
            { "Socks", "M_Socks" },
            { "Eyes", "M_Eyes" }
        };

        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var shared = renderer.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] == null) continue;
                if (!materials.TryGetValue(shared[i].name, out string target)) continue;
                var replacement = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + target + ".mat");
                if (replacement != null) shared[i] = replacement;
            }
            renderer.sharedMaterials = shared;

            if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
        }
    }

    static void BuildPlayerPrefab(AnimatorController controller, int playerLayer, StringBuilder log)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (fbx == null) throw new System.Exception("Missing character FBX");

        var root = new GameObject("Player");
        root.tag = "Player";
        root.layer = playerLayer;

        var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        model.name = "Model";
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * 0.36f;
        foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = playerLayer;

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        RemapMaterials(model);

        var cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.35f;
        cc.skinWidth = 0.03f;
        cc.minMoveDistance = 0f;

        root.AddComponent<PlayerController>();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        log.AppendLine("Player prefab saved.");
    }

    static void PlaceInScene(int playerLayer, StringBuilder log)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var existing = GameObject.Find("Player");
        if (existing != null) Object.DestroyImmediate(existing);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.name = "Player";

        var spawn = GameObject.Find("SpawnPoint");
        Vector3 position = spawn != null ? spawn.transform.position : new Vector3(0f, 0.05f, -1.5f);
        player.transform.SetPositionAndRotation(position + Vector3.up * 0.05f, Quaternion.identity);

        var camera = Camera.main;
        if (camera == null) throw new System.Exception("Main Camera missing in scene");
        var follow = camera.GetComponent<CameraFollow>();
        if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;
        follow.SetCollisionMask(~(1 << playerLayer));
        follow.SnapBehind(0f);
        EditorUtility.SetDirty(follow);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.AppendLine("Player placed in scene at " + player.transform.position.ToString("F2") + " on layer " + playerLayer);
    }
}
