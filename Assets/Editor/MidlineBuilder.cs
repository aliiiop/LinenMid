// Сборщик проекта Midline. Строит с нуля обе сцены (MainMenu и Game), все префабы,
// материалы, процедурные текстуры, интерфейс, мини-карту, навигацию и настройки билда.
//
// Зачем так: вместо сотен ручных кликов в редакторе вся расстановка описана кодом,
// её можно перечитать, поменять число и пересобрать. Это и есть "исходник" карты.
//
// Использование в редакторе: меню Midline -> "Собрать всё".
// Из командной строки:  Unity.exe -batchmode -projectPath . -executeMethod MidlineBuilder.BatchBuildAll -quit
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static partial class MidlineBuilder
{
    // ----- Слои -----
    const int PlayerLayer = 6;
    const int MinimapLayer = 7;
    const int UnitsLayer = 8;

    // ----- Пути -----
    const string MatDir = "Assets/Materials";
    const string TexDir = "Assets/Textures";
    const string PrefabDir = "Assets/Prefabs";
    const string GameScenePath = "Assets/Scenes/Game.unity";
    const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    const string ShotClip = "Assets/Audio/427396__erokia__plasma-lazer-pistol-gun-shot-1.wav";
    const string ReloadClip = "Assets/Audio/688866__dacmod1__reload.wav";
    const string CoinClip = "Assets/Audio/350868__cabled_mess__coin_c_05.wav";
    const string ImpactClip = "Assets/Audio/749788__rescopicsound__cinematic-energy-impact-grainy-voltage.mp3";
    const string MusicClip = "Assets/Audio/Cyberpunk Moonlight Sonata.mp3";

    // ----- Цвета -----
    static readonly Color Cyan = new Color(0f, 0.9f, 1f);
    static readonly Color Orange = new Color(1f, 0.42f, 0f);
    static readonly Color Gold = new Color(1f, 0.82f, 0.15f);
    static readonly Color Magenta = new Color(1f, 0.15f, 0.75f);
    static readonly Color Violet = new Color(0.55f, 0.25f, 1f);
    static readonly Color UiBg = new Color(0.03f, 0.05f, 0.09f, 0.82f);
    static readonly Color UiText = new Color(0.9f, 0.95f, 1f);
    static readonly Color UiDim = new Color(0.48f, 0.53f, 0.6f);

    // Все материалы и префабы, которые создаются в BuildAssets
    static Dictionary<string, Material> M = new Dictionary<string, Material>();
    static Dictionary<string, GameObject> P = new Dictionary<string, GameObject>();

    // =====================================================================
    //  Точки входа
    // =====================================================================
    [MenuItem("Midline/Собрать всё (сцены, префабы, интерфейс)")]
    public static void BuildEverything()
    {
        ImportTMPEssentials();
        SetupLayers();
        ConfigureModels();
        BuildAssets();
        BuildGameScene();
        BuildMenuScene();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Midline: всё собрано. Открой Assets/Scenes/MainMenu.unity и нажми Play.");
    }

    [MenuItem("Midline/Собрать билд Windows")]
    public static void BuildWindows()
    {
        BuildPlayerOptions o = new BuildPlayerOptions
        {
            scenes = new[] { MenuScenePath, GameScenePath },
            locationPathName = "Builds/Windows/Midline.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(o);
        Debug.Log("Midline build: " + report.summary.result + ", size " + (report.summary.totalSize / (1024 * 1024)) + " MB");
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    // Добавляет процедурные анимации (удар, попадание, падение) в уже готовые префабы,
    // не пересобирая сцены и материалы
    [MenuItem("Midline/Добавить анимации миньонам")]
    public static void AddMinionAnimations()
    {
        SetHumanoid("Assets/Models/Mutant.fbx", "Assets/Animations/Mutant@Idle.fbx");
        Avatar mutantAvatar = AvatarOf("Assets/Models/Mutant.fbx");
        Debug.Log("Midline: аватар Mutant valid=" + (mutantAvatar != null && mutantAvatar.isValid));

        string[] prefabs = { "Minion", "NeutralMonster", "Pitmonster" };
        foreach (string name in prefabs)
        {
            string path = PrefabDir + "/" + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            MinionAnimation ma = root.GetComponent<MinionAnimation>();
            if (ma == null) ma = root.AddComponent<MinionAnimation>();
            ma.animator = root.transform.Find("Model").GetComponent<Animator>();
            ma.animator.avatar = mutantAvatar;
            if (name == "Pitmonster") { ma.attackDuration = 0.8f; ma.deathDuration = 1.1f; }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        // Миньоны-декорации в главном меню используют ту же модель
        var menu = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        foreach (Animator a in Object.FindObjectsByType<Animator>())
            if (a.avatar == null || a.avatar.name.StartsWith("Mutant")) a.avatar = mutantAvatar;
        EditorSceneManager.SaveScene(menu);

        AssetDatabase.SaveAssets();
        Debug.Log("Midline: анимации миньонов добавлены");
    }

    public static void BatchAddMinionAnimationsAndBuild()
    {
        AddMinionAnimations();
        BuildWindows();
    }

    public static void BatchImportTMP() { ImportTMPEssentials(); AssetDatabase.SaveAssets(); }

    public static void BatchBuildAll()
    {
        BuildEverything();
        BuildWindows();
    }

    // =====================================================================
    //  Подготовка проекта
    // =====================================================================
    static void ImportTMPEssentials()
    {
        if (AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null) return;
        string[] found = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
        if (found.Length == 0) { Debug.LogError("Не найден TMP Essential Resources.unitypackage"); return; }
        AssetDatabase.ImportPackage(found[0], false);
        AssetDatabase.Refresh();
    }

    static void SetupLayers()
    {
        SerializedObject tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tm.FindProperty("layers");
        layers.GetArrayElementAtIndex(PlayerLayer).stringValue = "Player";
        layers.GetArrayElementAtIndex(MinimapLayer).stringValue = "Minimap";
        layers.GetArrayElementAtIndex(UnitsLayer).stringValue = "Units";
        tm.ApplyModifiedProperties();
    }

    // Модели Mixamo: Humanoid, анимации зациклены и не двигают персонажа сами (Bake Into Pose)
    static void ConfigureModels()
    {
        string[] clips =
        {
            "Assets/Animations/Y Bot@Rifle Idle.fbx", "Assets/Animations/Y Bot@Rifle Run.fbx",
            "Assets/Animations/Mutant@Idle.fbx", "Assets/Animations/Mutant@Zombie Walk.fbx",
        };
        foreach (string path in clips)
        {
            ModelImporter mi = (ModelImporter)AssetImporter.GetAtPath(path);
            if (mi == null) continue;
            mi.animationType = ModelImporterAnimationType.Human;
            mi.importAnimation = true;
            ModelImporterClipAnimation[] list = mi.clipAnimations.Length > 0 ? mi.clipAnimations : mi.defaultClipAnimations;
            foreach (ModelImporterClipAnimation c in list)
            {
                c.loopTime = true;
                c.loopPose = true;
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = true;
            }
            mi.clipAnimations = list;
            mi.SaveAndReimport();
        }

        // Модели: Humanoid. У Mutant автоматическая разметка костей не срабатывает,
        // поэтому копируем аватар из файла его анимации (скелет там тот же).
        SetHumanoid("Assets/Models/Y Bot.fbx", null);
        SetHumanoid("Assets/Models/Mutant.fbx", "Assets/Animations/Mutant@Idle.fbx");

        MakeAnimator("Assets/Animations/PlayerAnimator.controller", "Assets/Animations/Y Bot@Rifle Idle.fbx", "Assets/Animations/Y Bot@Rifle Run.fbx", "Run");
        MakeAnimator("Assets/Animations/MinionAnimator.controller", "Assets/Animations/Mutant@Idle.fbx", "Assets/Animations/Mutant@Zombie Walk.fbx", "Walk");
    }

    // avatarSourceFbx: файл, из которого берём РАЗМЕТКУ костей (какая кость — рука, нога и т. д.).
    // Скелет при этом строим из самой модели: у Mutant пальцы и имя корня отличаются от файла анимации,
    // поэтому копировать чужой аватар целиком нельзя — он не привяжется и модель останется в T-позе.
    static void SetHumanoid(string path, string avatarSourceFbx)
    {
        ModelImporter mi = (ModelImporter)AssetImporter.GetAtPath(path);
        if (mi == null) return;
        mi.animationType = ModelImporterAnimationType.Human;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        if (avatarSourceFbx != null)
        {
            ModelImporter src = (ModelImporter)AssetImporter.GetAtPath(avatarSourceFbx);
            HumanDescription desc = src.humanDescription;

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            List<SkeletonBone> skeleton = new List<SkeletonBone>();
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
                skeleton.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            desc.skeleton = skeleton.ToArray();
            mi.humanDescription = desc;
        }
        mi.SaveAndReimport();
    }

    static Avatar AvatarOf(string fbx)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbx))
            if (o is Avatar a) return a;
        return null;
    }

    // Два состояния Idle и Run/Walk, переход по параметру Speed (его ставят скрипты)
    static void MakeAnimator(string path, string idleFbx, string moveFbx, string moveName)
    {
        AssetDatabase.DeleteAsset(path);
        AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(path);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        AnimatorStateMachine sm = ac.layers[0].stateMachine;

        AnimatorState idle = sm.AddState("Idle");
        idle.motion = FirstClip(idleFbx);
        AnimatorState move = sm.AddState(moveName);
        move.motion = FirstClip(moveFbx);
        sm.defaultState = idle;

        AnimatorStateTransition toMove = idle.AddTransition(move);
        toMove.hasExitTime = false; toMove.duration = 0.12f;
        toMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition toIdle = move.AddTransition(idle);
        toIdle.hasExitTime = false; toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
    }

    static AnimationClip FirstClip(string fbx)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbx))
            if (o is AnimationClip c && !c.name.StartsWith("__preview__")) return c;
        Debug.LogWarning("Нет клипа в " + fbx);
        return null;
    }

    // =====================================================================
    //  Материалы, текстуры, префабы
    // =====================================================================
    static void BuildAssets()
    {
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(TexDir);
        Directory.CreateDirectory(PrefabDir);
        M.Clear(); P.Clear();

        Texture2D grid = MakeTexture("NeonGrid", 256, 256, GridPixel, true);
        Texture2D windows = MakeTexture("CityWindows", 256, 512, WindowPixel, true);
        Texture2D dot = MakeTexture("SoftDot", 64, 64, DotPixel, false);
        Texture2D stripes = MakeTexture("HazardStripes", 128, 128, StripePixel, true);

        // Окружение
        M["Ground"] = Lit("Dark", new Color(0.05f, 0.06f, 0.085f), new Color(0.05f, 0.3f, 0.45f), 1f, 0.75f, 0.2f, grid, new Vector2(45, 105));
        M["Lane"] = Lit("Lane", new Color(0.075f, 0.085f, 0.11f), new Color(0.1f, 0.5f, 0.6f), 0.5f, 0.85f, 0.1f, grid, new Vector2(6, 65));
        M["Wall"] = Lit("Wall", new Color(0.1f, 0.12f, 0.17f), Color.black, 0f, 0.4f, 0.3f);
        M["WallDark"] = Lit("WallDark", new Color(0.055f, 0.065f, 0.09f), Color.black, 0f, 0.3f, 0.2f);
        M["Building"] = Lit("Building", new Color(0.04f, 0.045f, 0.06f), Color.white, 1.6f, 0.6f, 0.4f, windows, new Vector2(1, 1));
        M["Hazard"] = Lit("Hazard", new Color(0.1f, 0.1f, 0.1f), new Color(1f, 0.75f, 0.1f), 1.2f, 0.5f, 0f, stripes, new Vector2(1, 1));
        M["Crate"] = Lit("Crate", new Color(0.13f, 0.15f, 0.2f), new Color(0.1f, 0.5f, 0.9f), 0f, 0.5f, 0.5f);

        // Неон (HDR-цвета: значения больше 1 светятся через Bloom)
        M["Blue"] = Lit("TeamBlue", new Color(0.02f, 0.06f, 0.08f), Cyan, 2.5f);
        M["Red"] = Lit("TeamRed", new Color(0.08f, 0.04f, 0.02f), Orange, 2.5f);
        M["NeonPink"] = Lit("NeonPink", Color.black, Magenta, 3f);
        M["NeonViolet"] = Lit("NeonViolet", Color.black, Violet, 1.6f);
        M["NeonGold"] = Lit("Gold", new Color(0.1f, 0.08f, 0f), Gold, 2.5f);
        M["NeonGreen"] = Lit("Green", new Color(0f, 0.08f, 0f), new Color(0.2f, 1f, 0.35f), 2.5f);
        M["NeonEnergy"] = Lit("EnergyBlue", new Color(0f, 0f, 0.08f), new Color(0.25f, 0.5f, 1f), 3f);
        M["NeonWhite"] = Lit("NeonWhite", Color.black, new Color(0.8f, 0.9f, 1f), 2f);

        // Персонажи
        M["HeroBody"] = Lit("HeroBody", new Color(0.42f, 0.45f, 0.52f), Color.black, 0f, 0.6f, 0.35f);
        M["MinionBlue"] = Lit("MinionBlue", new Color(0.05f, 0.16f, 0.2f), Cyan, 0.45f, 0.35f, 0.1f);
        M["MinionRed"] = Lit("MinionRed", new Color(0.22f, 0.09f, 0.03f), Orange, 0.45f, 0.35f, 0.1f);
        M["NeutralSkin"] = Lit("NeutralSkin", new Color(0.3f, 0.25f, 0.08f), Gold, 0.2f, 0.3f, 0.1f);
        M["CoreBlue"] = Lit("CoreBlue", new Color(0.05f, 0.2f, 0.25f), Cyan, 1.1f);
        M["CoreRed"] = Lit("CoreRed", new Color(0.25f, 0.1f, 0.03f), Orange, 1.1f);
        M["PitSkin"] = Lit("PitSkin", new Color(0.2f, 0.07f, 0.26f), Violet, 0.22f, 0.4f, 0.2f);
        M["Gun"] = Lit("GunMetal", new Color(0.08f, 0.09f, 0.11f), Color.black, 0f, 0.7f, 0.8f);

        // Без освещения: лучи, иконки мини-карты, щиты
        M["BeamBlue"] = Unlit("BeamBlue", Cyan * 4f, false);
        M["BeamRed"] = Unlit("BeamRed", Orange * 4f, false);
        M["Tracer"] = Unlit("Tracer", new Color(0.6f, 1f, 1f) * 3f, false);
        M["IconBlue"] = Unlit("IconBlue", Cyan, false);
        M["IconRed"] = Unlit("IconRed", Orange, false);
        M["IconGold"] = Unlit("IconGold", Gold, false);
        M["IconWhite"] = Unlit("IconWhite", Color.white, false);
        M["IconWall"] = Unlit("IconWall", new Color(0.18f, 0.22f, 0.3f), false);
        M["IconLane"] = Unlit("IconLane", new Color(0.07f, 0.09f, 0.13f), false);
        M["Shield"] = Unlit("ShieldBubble", new Color(0.35f, 0.75f, 1f, 0.13f), true);
        M["LightShaft"] = Unlit("LightShaft", new Color(1f, 1f, 1f, 0.08f), true);
        M["Particle"] = ParticleMat("ParticleAdd", dot);

        // Эффекты (частицы)
        P["HitSpark"] = MakeBurstFx("HitSpark", new Color(0.6f, 1f, 1f), 14, 0.25f, 6f, 0.12f, 0.3f, false);
        P["Explosion"] = MakeBurstFx("Explosion", new Color(1f, 0.55f, 0.15f), 60, 1.2f, 14f, 0.9f, 1.4f, true);
        P["DeathPuff"] = MakeBurstFx("DeathPuff", new Color(0.8f, 0.9f, 1f), 24, 0.5f, 4f, 0.35f, 0.7f, false);
        P["BurstRing"] = MakeRingFx("BurstRing", Cyan);
        P["HealFx"] = MakeRingFx("HealFx", new Color(0.3f, 1f, 0.45f));

        // Пикапы
        P["HealthPack"] = MakePickup("HealthPack", PickupType.Health, 60, M["NeonGreen"], new Color(0.2f, 1f, 0.35f));
        P["EnergyCell"] = MakePickup("EnergyCell", PickupType.Energy, 40, M["NeonEnergy"], new Color(0.3f, 0.5f, 1f));
        P["HealthDrop"] = MakePickup("HealthDrop", PickupType.Health, 40, M["NeonGreen"], new Color(0.2f, 1f, 0.35f));

        // Юниты
        P["Minion"] = MakeMinionPrefab();
        P["Neutral"] = MakeNeutralPrefab("NeutralMonster", false);
        P["Pitmonster"] = MakeNeutralPrefab("Pitmonster", true);
        P["BotHero"] = MakeBotPrefab();

        AssetDatabase.SaveAssets();
    }

    // ----- Процедурные текстуры (исходник = этот код) -----
    static Color GridPixel(int x, int y, int w, int h)
    {
        // Тонкие линии по краям тайла + точка-узел в углу
        float line = (x < 2 || y < 2) ? 1f : 0f;
        float node = (x < 6 && y < 6) ? 1f : 0f;
        float v = Mathf.Max(line * 0.55f, node);
        return new Color(v, v, v, 1f);
    }

    static Color WindowPixel(int x, int y, int w, int h)
    {
        // Окна 16x24 пикселя с промежутками; часть окон "горит" тёплым или холодным светом
        int cx = x / 16, cy = y / 24;
        bool inWindow = (x % 16) >= 3 && (x % 16) <= 12 && (y % 24) >= 4 && (y % 24) <= 17;
        if (!inWindow) return Color.black;
        int hash = (cx * 73856093) ^ (cy * 19349663);
        float r = Mathf.Abs(hash % 1000) / 1000f;
        if (r > 0.42f) return Color.black;
        if (r < 0.12f) return new Color(0.2f, 0.85f, 1f);       // бирюзовые
        if (r < 0.18f) return new Color(1f, 0.3f, 0.8f);        // розовые
        return new Color(1f, 0.72f, 0.4f) * 0.8f;               // тёплые
    }

    static Color DotPixel(int x, int y, int w, int h)
    {
        float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
        float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
        a = a * a;
        return new Color(1f, 1f, 1f, a);
    }

    static Color StripePixel(int x, int y, int w, int h)
    {
        bool s = ((x + y) / 16) % 2 == 0;
        return s ? Color.white : Color.black;
    }

    static Texture2D MakeTexture(string name, int w, int h, System.Func<int, int, int, int, Color> pixel, bool repeat)
    {
        string path = TexDir + "/" + name + ".png";
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                t.SetPixel(x, y, pixel(x, y, w, h));
        t.Apply();
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        ti.alphaIsTransparency = !repeat;
        ti.mipmapEnabled = true;
        ti.anisoLevel = 4;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ----- Материалы -----
    static Material GetMat(string name, string shader)
    {
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = Shader.Find(shader);
        return m;
    }

    static Material Lit(string name, Color baseColor, Color emission, float intensity, float smooth = 0.5f, float metal = 0f, Texture emissionMap = null, Vector2? tiling = null)
    {
        Material m = GetMat(name, "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", baseColor);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        m.SetTexture("_BaseMap", null);
        if (intensity > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emission * intensity);
            m.SetTexture("_EmissionMap", emissionMap);
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.SetTexture("_EmissionMap", null);
        }
        Vector2 tile = tiling ?? Vector2.one;
        m.SetTextureScale("_BaseMap", tile);
        m.SetTextureScale("_EmissionMap", tile);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Unlit(string name, Color color, bool transparent)
    {
        Material m = GetMat(name, "Universal Render Pipeline/Unlit");
        m.SetColor("_BaseColor", color);
        SetTransparent(m, transparent, false);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material ParticleMat(string name, Texture tex)
    {
        Material m = GetMat(name, "Universal Render Pipeline/Particles/Unlit");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", Color.white);
        SetTransparent(m, true, true);
        m.SetFloat("_Blend", 2f);   // Additive
        EditorUtility.SetDirty(m);
        return m;
    }

    static void SetTransparent(Material m, bool transparent, bool additive)
    {
        if (!transparent)
        {
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_ZWrite", 1f);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Opaque");
            m.renderQueue = -1;
            return;
        }
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_Cull", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;
    }

    // ----- Частицы -----
    static GameObject MakeBurstFx(string name, Color color, int count, float life, float speed, float size, float duration, bool withLight)
    {
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color * 2f;
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = M["Particle"];
        r.renderMode = ParticleSystemRenderMode.Billboard;

        if (withLight)
        {
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.range = 18f; l.intensity = 6f;
            go.AddComponent<LightFade>().duration = duration * 0.6f;
        }
        return SavePrefab(go, PrefabDir + "/" + name + ".prefab");
    }

    static GameObject MakeRingFx(string name, Color color)
    {
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.6f; main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 14f;
        main.startSize = 0.45f;
        main.startColor = color * 2.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 90) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        shape.rotation = new Vector3(90f, 0f, 0f);       // кольцо лежит на земле
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = M["Particle"];

        Light l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = color; l.range = 12f; l.intensity = 5f;
        go.AddComponent<LightFade>().duration = 0.4f;
        return SavePrefab(go, PrefabDir + "/" + name + ".prefab");
    }

    // ----- Пикапы -----
    static GameObject MakePickup(string name, PickupType type, int value, Material mat, Color lightColor)
    {
        GameObject root = new GameObject(name);
        SphereCollider sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true; sc.radius = 1.3f; sc.center = new Vector3(0, 0.8f, 0);

        GameObject core = Prim(PrimitiveType.Cube, "Core", root.transform, new Vector3(0, 0.8f, 0), new Vector3(0.45f, 0.45f, 0.45f), mat, false);
        core.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        if (type == PickupType.Health)
        {
            // Крестик аптечки
            Prim(PrimitiveType.Cube, "CrossH", root.transform, new Vector3(0, 0.8f, 0), new Vector3(0.9f, 0.25f, 0.25f), mat, false);
            Prim(PrimitiveType.Cube, "CrossV", root.transform, new Vector3(0, 0.8f, 0), new Vector3(0.25f, 0.9f, 0.25f), mat, false);
            Object.DestroyImmediate(core);
        }
        Prim(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0, 0.02f, 0), new Vector3(1.4f, 0.02f, 1.4f), mat, false);

        Light l = new GameObject("Glow").AddComponent<Light>();
        l.transform.SetParent(root.transform, false);
        l.transform.localPosition = new Vector3(0, 1f, 0);
        l.type = LightType.Point; l.color = lightColor; l.range = 5f; l.intensity = 2f;

        Pickup p = root.AddComponent<Pickup>();
        p.type = type; p.value = value; p.rotateSpeed = 80f;
        p.pickupSound = Clip(CoinClip);
        return SavePrefab(root, PrefabDir + "/" + name + ".prefab");
    }

    // ----- Миньон -----
    static GameObject MakeMinionPrefab()
    {
        GameObject root = new GameObject("Minion");
        root.layer = UnitsLayer;
        Team team = root.AddComponent<Team>(); team.kind = UnitKind.Minion; team.radius = 0.5f;
        Health hp = root.AddComponent<Health>(); hp.maxHealth = 80f;
        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed = 3.6f; agent.radius = 0.45f; agent.height = 1.6f; agent.stoppingDistance = 0.5f;
        agent.acceleration = 12f; agent.angularSpeed = 360f;
        agent.avoidancePriority = 60;
        CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0, 0.8f, 0); cc.radius = 0.45f; cc.height = 1.6f;

        GameObject model = AddModel(root.transform, "Assets/Models/Mutant.fbx", 1.55f, "Assets/Animations/MinionAnimator.controller");

        root.AddComponent<MinionAnimation>().animator = model.GetComponent<Animator>();

        UnitAI ai = root.AddComponent<UnitAI>();
        ai.aggroRange = 10f; ai.attackRange = 1.6f; ai.damage = 9f; ai.attackCooldown = 1.1f;

        Bounty b = root.AddComponent<Bounty>();
        b.credits = 15; b.destroyOnDeath = true; b.deathEffect = P["DeathPuff"];

        TeamTint tint = root.AddComponent<TeamTint>();
        tint.blueMaterial = M["MinionBlue"]; tint.redMaterial = M["MinionRed"];
        tint.blueIcon = M["IconBlue"]; tint.redIcon = M["IconRed"];
        tint.onlyThese = model.GetComponentsInChildren<Renderer>();

        WorldHealthBar bar = root.AddComponent<WorldHealthBar>();
        bar.height = 2.05f; bar.width = 1f; bar.onlyWhenDamaged = true;

        AddIcon(root.transform, 2.6f, null, true);
        SetLayerRecursive(root, UnitsLayer, MinimapLayer);
        return SavePrefab(root, PrefabDir + "/Minion.prefab");
    }

    static GameObject MakeNeutralPrefab(string name, bool pit)
    {
        GameObject root = new GameObject(name);
        Team team = root.AddComponent<Team>();
        team.id = TeamId.Neutral; team.kind = UnitKind.Neutral; team.radius = pit ? 2f : 0.6f;
        team.displayName = pit ? "Pitmonster" : "Нейтрал";
        Health hp = root.AddComponent<Health>();
        hp.maxHealth = pit ? 2000f : 300f; hp.regenPerSecond = pit ? 15f : 6f;
        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed = pit ? 2.6f : 3.2f; agent.radius = pit ? 1.4f : 0.55f; agent.height = pit ? 4f : 2f;
        agent.angularSpeed = 300f;
        CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0, pit ? 2.1f : 1f, 0); cc.radius = pit ? 1.4f : 0.55f; cc.height = pit ? 4.2f : 2f;

        GameObject model = AddModel(root.transform, "Assets/Models/Mutant.fbx", pit ? 4.2f : 2.0f, "Assets/Animations/MinionAnimator.controller");
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
            r.sharedMaterials = Fill(r.sharedMaterials.Length, pit ? M["PitSkin"] : M["NeutralSkin"]);

        if (pit)
        {
            Light l = new GameObject("PitGlow").AddComponent<Light>();
            l.transform.SetParent(root.transform, false);
            l.transform.localPosition = new Vector3(0, 3f, 0);
            l.type = LightType.Point; l.color = Violet; l.range = 14f; l.intensity = 4f;
        }

        MinionAnimation ma = root.AddComponent<MinionAnimation>();
        ma.animator = model.GetComponent<Animator>();
        if (pit) { ma.attackDuration = 0.8f; ma.deathDuration = 1.1f; }

        UnitAI ai = root.AddComponent<UnitAI>();
        ai.waypoints = new Transform[0];
        ai.aggroRange = pit ? 9f : 7f;
        ai.attackRange = pit ? 2.6f : 1.8f;
        ai.damage = pit ? 40f : 14f;
        ai.attackCooldown = pit ? 1.6f : 1.2f;
        ai.leashDistance = pit ? 11f : 12f;

        Bounty b = root.AddComponent<Bounty>();
        b.credits = pit ? 150 : 35; b.destroyOnDeath = true; b.grantsPitBuff = pit;
        b.dropPrefab = pit ? P["EnergyCell"] : P["HealthDrop"];
        b.deathEffect = pit ? P["Explosion"] : P["DeathPuff"];

        WorldHealthBar bar = root.AddComponent<WorldHealthBar>();
        bar.height = pit ? 5f : 2.5f; bar.width = pit ? 3f : 1.2f;

        AddIcon(root.transform, pit ? 7f : 3.5f, M["IconGold"], false);
        SetLayerRecursive(root, UnitsLayer, MinimapLayer);
        return SavePrefab(root, PrefabDir + "/" + name + ".prefab");
    }

    static GameObject MakeBotPrefab()
    {
        GameObject root = new GameObject("BotHero");
        Team team = root.AddComponent<Team>(); team.kind = UnitKind.Hero; team.radius = 0.5f;
        Health hp = root.AddComponent<Health>(); hp.maxHealth = 420f; hp.regenPerSecond = 2f;
        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed = 4.6f; agent.stoppingDistance = 1f; agent.radius = 0.4f; agent.height = 1.8f;
        agent.angularSpeed = 400f; agent.acceleration = 14f; agent.avoidancePriority = 40;
        CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0, 0.9f, 0); cc.radius = 0.4f; cc.height = 1.8f;

        GameObject model = AddModel(root.transform, "Assets/Models/Y Bot.fbx", 1.8f, "Assets/Animations/PlayerAnimator.controller");
        Transform muzzle = AddRifle(model);

        GameObject beamGo = new GameObject("Beam");
        beamGo.transform.SetParent(root.transform, false);
        LineRenderer lr = beamGo.AddComponent<LineRenderer>();
        lr.widthMultiplier = 0.07f; lr.sharedMaterial = M["BeamRed"];
        lr.shadowCastingMode = ShadowCastingMode.Off;
        BeamEffect be = beamGo.AddComponent<BeamEffect>();
        be.shotClip = Clip(ShotClip); be.pitch = 0.85f; be.volume = 0.45f;

        UnitAI ai = root.AddComponent<UnitAI>();
        ai.aggroRange = 16f; ai.attackRange = 13f; ai.damage = 13f; ai.attackCooldown = 0.8f; ai.retreatBelow = 0.3f;
        ai.beam = be; ai.muzzle = muzzle;

        Bounty bounty = root.AddComponent<Bounty>(); bounty.credits = 60; bounty.destroyOnDeath = false; bounty.deathEffect = P["DeathPuff"];
        Respawner rs = root.AddComponent<Respawner>(); rs.baseTime = 8f; rs.extraPerDeath = 4f;

        TeamTint tint = root.AddComponent<TeamTint>();
        tint.blueMaterial = M["Blue"]; tint.redMaterial = M["Red"]; tint.bodyMaterial = M["HeroBody"];
        tint.blueIcon = M["IconBlue"]; tint.redIcon = M["IconRed"];
        tint.onlyThese = model.GetComponentsInChildren<SkinnedMeshRenderer>();

        WorldHealthBar bar = root.AddComponent<WorldHealthBar>();
        bar.height = 2.3f; bar.width = 1.4f;

        GameObject icon = AddIcon(root.transform, 4.2f, null, true);
        rs.hideWhileDead = new[] { model, icon };
        SetLayerRecursive(root, UnitsLayer, MinimapLayer);
        return SavePrefab(root, PrefabDir + "/BotHero.prefab");
    }

    // Модель Mixamo нужного роста с аниматором
    static GameObject AddModel(Transform parent, string fbx, float height, string controller)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        model.name = "Model";
        model.transform.SetParent(parent, false);
        model.transform.localPosition = Vector3.zero;

        // Подгоняем рост по габаритам меша
        Bounds b = new Bounds(model.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            r.shadowCastingMode = ShadowCastingMode.On;
        }
        if (any && b.size.y > 0.01f) model.transform.localScale = Vector3.one * (height / b.size.y);

        Animator anim = model.GetComponent<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controller);
        foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.updateWhenOffscreen = false;
        return model;
    }

    // Винтовка из примитивов (свой ассет). Возвращает точку дула.
    static Transform AddRifle(GameObject model)
    {
        GameObject gun = new GameObject("Rifle");
        gun.transform.SetParent(model.transform.parent, false);
        float s = 1f;
        Prim(PrimitiveType.Cube, "Body", gun.transform, new Vector3(0, 0.02f, 0.12f) * s, new Vector3(0.07f, 0.12f, 0.5f) * s, M["Gun"], false);
        Prim(PrimitiveType.Cube, "Stock", gun.transform, new Vector3(0, -0.01f, -0.2f) * s, new Vector3(0.06f, 0.11f, 0.22f) * s, M["Gun"], false);
        Prim(PrimitiveType.Cube, "Barrel", gun.transform, new Vector3(0, 0.05f, 0.5f) * s, new Vector3(0.035f, 0.035f, 0.35f) * s, M["Gun"], false);
        Prim(PrimitiveType.Cube, "Mag", gun.transform, new Vector3(0, -0.09f, 0.14f) * s, new Vector3(0.05f, 0.14f, 0.07f) * s, M["Gun"], false);
        Prim(PrimitiveType.Cube, "Scope", gun.transform, new Vector3(0, 0.11f, 0.08f) * s, new Vector3(0.04f, 0.05f, 0.16f) * s, M["Gun"], false);
        Prim(PrimitiveType.Cube, "NeonStrip", gun.transform, new Vector3(0.037f, 0.03f, 0.14f) * s, new Vector3(0.008f, 0.025f, 0.42f) * s, M["NeonWhite"], false);
        Transform muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(gun.transform, false);
        muzzle.localPosition = new Vector3(0, 0.05f, 0.7f) * s;

        GunAttach ga = model.transform.parent.gameObject.AddComponent<GunAttach>();
        ga.animator = model.GetComponent<Animator>();
        ga.gun = gun.transform;
        return muzzle;
    }

    static GameObject AddIcon(Transform parent, float size, Material mat, bool tintedByTeam)
    {
        GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        icon.name = "Icon";
        Object.DestroyImmediate(icon.GetComponent<Collider>());
        icon.layer = MinimapLayer;
        icon.transform.SetParent(parent, false);
        Vector3 ls = parent.lossyScale;
        icon.transform.localScale = new Vector3(size / ls.x, 0.1f / ls.y, size / ls.z);
        icon.transform.position = new Vector3(parent.position.x, 60f, parent.position.z);
        Renderer r = icon.GetComponent<Renderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.sharedMaterial = mat != null ? mat : M["IconWhite"];
        return icon;
    }
}
