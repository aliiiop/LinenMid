// Сцена Game: карта Neon City, башни, Ancient, база, джунгли, юниты, игрок, мини-карта.
// Схема карты (вид сверху, ось Z вверх):
//   z=+106  КРАСНАЯ БАЗА: Ancient (0, 92), возрождение (0, 78)
//   z=+66   ---------------------------------------------
//           ЛЕВАЯ ЛИНИЯ x=-32 | ДЖУНГЛИ: кемп (0, 42)     | ПРАВАЯ ЛИНИЯ x=+32
//           башни z=±15, ±48  | PITMONSTER (0, 0)        | башни z=±15, ±48
//                             | кемп (0, -42)            |
//   z=-66   ---------------------------------------------
//   z=-106  СИНЯЯ БАЗА: Ancient (0, -92), магазин, фонтан, старт игрока (0, -78)
// Проходы между линиями и джунглями: z = -30 и z = +30, прыжковые платформы z = ±45 и ±15.
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class MidlineBuilder
{
    static Transform env;           // всё, что входит в навигацию
    static Transform deco;          // декор без коллайдеров (здания, вывески, фонари)
    static Transform points;        // пустые точки маршрутов
    static Transform minimapRoot;   // схема карты для мини-карты

    static void BuildGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        env = new GameObject("Environment").transform;
        deco = new GameObject("Decor").transform;
        points = new GameObject("Points").transform;
        minimapRoot = new GameObject("MinimapMap").transform;

        SetupLighting(false);
        BuildArena();
        BuildCity();

        // ----- Точки маршрутов -----
        Transform[] laneL = new Transform[5], laneR = new Transform[5];
        float[] laneZ = { -60f, -30f, 0f, 30f, 60f };
        for (int i = 0; i < 5; i++)
        {
            laneL[i] = Point("LaneL_" + (i + 1), new Vector3(-32f, 0f, laneZ[i]));
            laneR[i] = Point("LaneR_" + (i + 1), new Vector3(32f, 0f, laneZ[i]));
        }
        Transform blueRespawn = Point("BlueRespawn", new Vector3(0f, 0.1f, -78f));
        Transform redRespawn = Point("RedRespawn", new Vector3(0f, 0.1f, 78f));
        redRespawn.rotation = Quaternion.Euler(0f, 180f, 0f);
        Transform campBlue = Point("CampBlue", new Vector3(0f, 0f, -42f));
        Transform campRed = Point("CampRed", new Vector3(0f, 0f, 42f));
        Transform pitHome = Point("PitHome", Vector3.zero);

        // ----- Башни и Ancient -----
        Health bl1 = MakeTower("BlueTowerL1", new Vector3(-27f, 0f, -15f), TeamId.Blue, 1000f, null);
        Health bl2 = MakeTower("BlueTowerL2", new Vector3(-27f, 0f, -48f), TeamId.Blue, 1400f, bl1);
        Health br1 = MakeTower("BlueTowerR1", new Vector3(27f, 0f, -15f), TeamId.Blue, 1000f, null);
        Health br2 = MakeTower("BlueTowerR2", new Vector3(27f, 0f, -48f), TeamId.Blue, 1400f, br1);
        Health rl1 = MakeTower("RedTowerL1", new Vector3(-27f, 0f, 15f), TeamId.Red, 1000f, null);
        Health rl2 = MakeTower("RedTowerL2", new Vector3(-27f, 0f, 48f), TeamId.Red, 1400f, rl1);
        Health rr1 = MakeTower("RedTowerR1", new Vector3(27f, 0f, 15f), TeamId.Red, 1000f, null);
        Health rr2 = MakeTower("RedTowerR2", new Vector3(27f, 0f, 48f), TeamId.Red, 1400f, rr1);

        GameObject blueAncient = MakeAncient("BlueAncient", new Vector3(0f, 0f, -92f), TeamId.Blue, new[] { bl2, br2 });
        GameObject redAncient = MakeAncient("RedAncient", new Vector3(0f, 0f, 92f), TeamId.Red, new[] { rl2, rr2 });

        // ----- Объекты взаимодействия -----
        MakeShop("ShopBase", new Vector3(12f, 0f, -80f), 180f);
        MakeShop("ShopJungle", new Vector3(0f, 0f, -60f), 0f);
        MakeFountain("Fountain", new Vector3(-12f, 0f, -80f), true);
        MakeFountain("FountainRed", new Vector3(-12f, 0f, 80f), false);
        MakeJumpPad("JumpPadL1", new Vector3(-24.5f, 0f, -45f), 90f);
        MakeJumpPad("JumpPadR1", new Vector3(24.5f, 0f, -45f), -90f);
        MakeJumpPad("JumpPadL2", new Vector3(-24.5f, 0f, 45f), 90f);
        MakeJumpPad("JumpPadR2", new Vector3(24.5f, 0f, 45f), -90f);
        MakeJumpPad("JumpPadJL", new Vector3(-15.5f, 0f, 15f), -90f);
        MakeJumpPad("JumpPadJR", new Vector3(15.5f, 0f, 15f), 90f);
        MakeJumpPad("JumpPadJL2", new Vector3(-15.5f, 0f, -15f), -90f);
        MakeJumpPad("JumpPadJR2", new Vector3(15.5f, 0f, -15f), 90f);

        // ----- Собираемые предметы (появляются снова через 30 с) -----
        PlacePickup(P["HealthPack"], new Vector3(-12f, 0f, -22f));
        PlacePickup(P["HealthPack"], new Vector3(12f, 0f, 22f));
        PlacePickup(P["HealthPack"], new Vector3(12f, 0f, -22f));
        PlacePickup(P["HealthPack"], new Vector3(-12f, 0f, 22f));
        PlacePickup(P["EnergyCell"], new Vector3(-38f, 0f, 0f));
        PlacePickup(P["EnergyCell"], new Vector3(38f, 0f, 0f));
        PlacePickup(P["EnergyCell"], new Vector3(0f, 0f, -30f));
        PlacePickup(P["EnergyCell"], new Vector3(0f, 0f, 30f));

        // ----- Навигация (запекаем до того, как появятся юниты) -----
        NavMeshSurface surface = env.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = 1 << 0;
        surface.BuildNavMesh();
        string navPath = "Assets/Scenes/Game/NavMesh-Environment.asset";
        Directory.CreateDirectory("Assets/Scenes/Game");
        AssetDatabase.DeleteAsset(navPath);
        AssetDatabase.CreateAsset(surface.navMeshData, navPath);

        // ----- Спавнеры волн -----
        MakeSpawner("SpawnBlueL", new Vector3(-32f, 0f, -64f), TeamId.Blue, laneL, redAncient.transform);
        MakeSpawner("SpawnBlueR", new Vector3(32f, 0f, -64f), TeamId.Blue, laneR, redAncient.transform);
        MakeSpawner("SpawnRedL", new Vector3(-32f, 0f, 64f), TeamId.Red, laneL, blueAncient.transform);
        MakeSpawner("SpawnRedR", new Vector3(32f, 0f, 64f), TeamId.Red, laneR, blueAncient.transform);

        // ----- Боты-герои -----
        PlaceBot("AllyBot", "Союзник", new Vector3(-4f, 0f, -76f), TeamId.Blue, laneL, false, redAncient.transform, blueRespawn);
        PlaceBot("EnemyBotL", "Враг Вектор", new Vector3(-4f, 0f, 76f), TeamId.Red, laneL, true, blueAncient.transform, redRespawn);
        PlaceBot("EnemyBotR", "Враг Глитч", new Vector3(4f, 0f, 76f), TeamId.Red, laneR, true, blueAncient.transform, redRespawn);

        // ----- Нейтралы -----
        PlaceNeutral(P["Neutral"], "NeutralBlue1", new Vector3(-5f, 0f, -42f), campBlue);
        PlaceNeutral(P["Neutral"], "NeutralBlue2", new Vector3(5f, 0f, -42f), campBlue);
        PlaceNeutral(P["Neutral"], "NeutralRed1", new Vector3(-5f, 0f, 42f), campRed);
        PlaceNeutral(P["Neutral"], "NeutralRed2", new Vector3(5f, 0f, 42f), campRed);
        PlaceNeutral(P["Pitmonster"], "Pitmonster", Vector3.zero, pitHome);

        // ----- Игрок, менеджеры, интерфейс -----
        GameObject player = MakePlayer(blueRespawn);
        GameManager gm = new GameObject("Managers").AddComponent<GameManager>();
        UIManager ui = gm.gameObject.AddComponent<UIManager>();
        AudioSource announcer = gm.gameObject.AddComponent<AudioSource>();
        announcer.playOnAwake = false; announcer.spatialBlend = 0f; announcer.volume = 0.6f;
        gm.announcer = announcer;
        gm.towerDestroyedClip = Clip(ImpactClip);
        gm.purchaseClip = Clip(CoinClip);
        MakeMusic(0.22f);

        BuildMinimapCamera();
        BuildHUD(gm, ui, player, redAncient.GetComponent<Health>());

        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    // =====================================================================
    //  Свет и атмосфера
    // =====================================================================
    static void SetupLighting(bool menu)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = menu ? new Color(0.08f, 0.09f, 0.14f) : new Color(0.1f, 0.12f, 0.18f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.085f);
        RenderSettings.fogDensity = menu ? 0.035f : 0.0085f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.reflectionIntensity = 0.3f;

        Light sun = new GameObject("Moonlight").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.55f, 0.65f, 1f);
        sun.intensity = menu ? 0.25f : 0.4f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.7f;
        sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);

        // Постобработка: свечение неона (Bloom), виньетка, тонмаппинг
        string path = menu ? "Assets/Settings/MenuVolume.asset" : "Assets/Settings/GameVolume.asset";
        AssetDatabase.DeleteAsset(path);
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(menu ? 1.6f : 1.2f);
        bloom.scatter.Override(0.72f);
        Tonemapping tm = profile.Add<Tonemapping>(true);
        tm.mode.Override(TonemappingMode.ACES);
        Vignette vig = profile.Add<Vignette>(true);
        vig.intensity.Override(menu ? 0.4f : 0.28f);
        vig.smoothness.Override(0.45f);
        ColorAdjustments ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(menu ? 0.3f : 0.45f);
        ca.contrast.Override(14f);
        ca.saturation.Override(12f);
        foreach (VolumeComponent c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);

        Volume vol = new GameObject("Global Volume").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
    }

    // =====================================================================
    //  Арена: пол, стены, линии, джунгли, базы
    // =====================================================================
    static void BuildArena()
    {
        Box("Ground", env, new Vector3(0f, -0.5f, 0f), new Vector3(90f, 1f, 214f), M["Ground"], true);

        // Покрытие линий (декор) и неоновые бортики: синяя половина бирюзовая, красная оранжевая
        foreach (float x in new[] { -32f, 32f })
        {
            Box("LaneFloor", deco, new Vector3(x, 0.005f, 0f), new Vector3(22f, 0.01f, 132f), M["Lane"], false);
            foreach (float edge in new[] { x - 11.2f, x + 11.2f })
            {
                Box("LaneEdge", deco, new Vector3(edge, 0.03f, -33f), new Vector3(0.25f, 0.06f, 66f), M["Blue"], false);
                Box("LaneEdge", deco, new Vector3(edge, 0.03f, 33f), new Vector3(0.25f, 0.06f, 66f), M["Red"], false);
            }
            // Центральная разметка пунктиром
            for (float z = -62f; z <= 62f; z += 8f)
                Box("LaneDash", deco, new Vector3(x, 0.02f, z), new Vector3(0.18f, 0.03f, 3.5f), z < 0 ? M["Blue"] : M["Red"], false);
            MinimapRect(new Vector3(x, 0f, 0f), new Vector2(22f, 132f), M["IconLane"]);
        }

        // Внешние стены и задние стены баз
        WallWithTrim("OuterWallL", new Vector3(-44f, 6f, 0f), new Vector3(2f, 12f, 214f), null);
        WallWithTrim("OuterWallR", new Vector3(44f, 6f, 0f), new Vector3(2f, 12f, 214f), null);
        WallWithTrim("BackWallBlue", new Vector3(0f, 6f, -107f), new Vector3(90f, 12f, 2f), M["Blue"]);
        WallWithTrim("BackWallRed", new Vector3(0f, 6f, 107f), new Vector3(90f, 12f, 2f), M["Red"]);

        // Стены джунглей (низкие, через них можно перепрыгнуть с платформы). Щели — проходы-фланги.
        float h = 3.6f;
        foreach (float x in new[] { -20f, 20f })
        {
            WallWithTrim("JungleWall", new Vector3(x, h / 2f, -49.5f), new Vector3(2f, h, 31f), M["Blue"]);
            WallWithTrim("JungleWall", new Vector3(x, h / 2f, 0f), new Vector3(2f, h, 52f), null);
            WallWithTrim("JungleWall", new Vector3(x, h / 2f, 49.5f), new Vector3(2f, h, 31f), M["Red"]);
        }
        WallWithTrim("JungleCapBlue", new Vector3(0f, h / 2f, -66f), new Vector3(42f, h, 2f), M["Blue"]);
        WallWithTrim("JungleCapRed", new Vector3(0f, h / 2f, 66f), new Vector3(42f, h, 2f), M["Red"]);

        // Средняя стена получает и синюю, и красную подсветку по половинам
        foreach (float x in new[] { -20f, 20f })
        {
            Box("Trim", deco, new Vector3(x, h + 0.04f, -13f), new Vector3(2.05f, 0.08f, 26f), M["Blue"], false);
            Box("Trim", deco, new Vector3(x, h + 0.04f, 13f), new Vector3(2.05f, 0.08f, 26f), M["Red"], false);
        }

        // ----- Базы -----
        foreach (int side in new[] { -1, 1 })
        {
            Material team = side < 0 ? M["Blue"] : M["Red"];
            float zc = 86f * side;
            Box("BasePlate", deco, new Vector3(0f, 0.01f, zc), new Vector3(40f, 0.02f, 34f), M["Lane"], false);
            Box("BaseRim", deco, new Vector3(0f, 0.03f, zc - 17f), new Vector3(40f, 0.06f, 0.3f), team, false);
            Box("BaseRim", deco, new Vector3(0f, 0.03f, zc + 17f), new Vector3(40f, 0.06f, 0.3f), team, false);
            Box("BaseRim", deco, new Vector3(-20f, 0.03f, zc), new Vector3(0.3f, 0.06f, 34f), team, false);
            Box("BaseRim", deco, new Vector3(20f, 0.03f, zc), new Vector3(0.3f, 0.06f, 34f), team, false);

            // Укрытия у выхода с базы
            Cover(new Vector3(-24f, 0f, 72f * side), new Vector3(4f, 1.6f, 1.2f));
            Cover(new Vector3(24f, 0f, 72f * side), new Vector3(4f, 1.6f, 1.2f));
            Cover(new Vector3(-36f, 0f, 96f * side), new Vector3(3f, 3f, 3f));
            Cover(new Vector3(36f, 0f, 96f * side), new Vector3(3f, 3f, 3f));

            PointLight(deco, new Vector3(-16f, 6f, zc), side < 0 ? Cyan : Orange, 22f, 3f);
            PointLight(deco, new Vector3(16f, 6f, zc), side < 0 ? Cyan : Orange, 22f, 3f);
        }

        // ----- Джунгли -----
        // Арена Pitmonster: кольцо из светящихся блоков
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 10f, 0.15f, Mathf.Sin(a) * 10f);
            GameObject b = Box("PitRing", deco, p, new Vector3(1.2f, 0.2f, 0.35f), M["NeonViolet"], false);
            b.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
        }
        PointLight(deco, new Vector3(0f, 7f, 0f), Violet, 20f, 3f);
        Text3D(deco, "PITMONSTER", new Vector3(0f, 8f, -10.5f), Quaternion.identity, 4f, Violet * 2f);

        // Укрытия в джунглях
        Cover(new Vector3(-9f, 0f, -12f), new Vector3(2.5f, 1.8f, 2.5f));
        Cover(new Vector3(9f, 0f, 12f), new Vector3(2.5f, 1.8f, 2.5f));
        Cover(new Vector3(-9f, 0f, 12f), new Vector3(2.5f, 1.8f, 2.5f));
        Cover(new Vector3(9f, 0f, -12f), new Vector3(2.5f, 1.8f, 2.5f));
        Cover(new Vector3(-12f, 0f, -34f), new Vector3(1.5f, 2.6f, 4f));
        Cover(new Vector3(12f, 0f, 34f), new Vector3(1.5f, 2.6f, 4f));
        Cover(new Vector3(12f, 0f, -34f), new Vector3(1.5f, 2.6f, 4f));
        Cover(new Vector3(-12f, 0f, 34f), new Vector3(1.5f, 2.6f, 4f));
        PointLight(deco, new Vector3(0f, 5f, -42f), Gold, 14f, 1.8f);
        PointLight(deco, new Vector3(0f, 5f, 42f), Gold, 14f, 1.8f);

        // Укрытия на линиях (не перекрывают проход миньонов)
        foreach (float x in new[] { -32f, 32f })
        {
            Cover(new Vector3(x - 7f, 0f, -32f), new Vector3(2f, 1.4f, 3f));
            Cover(new Vector3(x + 7f, 0f, 32f), new Vector3(2f, 1.4f, 3f));
            Cover(new Vector3(x + 7f, 0f, -4f), new Vector3(2f, 1.4f, 3f));
            Cover(new Vector3(x - 7f, 0f, 4f), new Vector3(2f, 1.4f, 3f));
        }

        // Фонари вдоль линий
        for (float z = -55f; z <= 55f; z += 22f)
        {
            Color c = z < 0 ? Cyan : Orange;
            foreach (float x in new[] { -42.6f, -21.4f, 21.4f, 42.6f })
                StreetLamp(new Vector3(x, 0f, z), c, x < -30f || (x > 0f && x < 30f) ? 1 : -1);
        }
    }

    static void WallWithTrim(string name, Vector3 pos, Vector3 scale, Material trim)
    {
        Box(name, env, pos, scale, M["Wall"], true);
        if (trim != null)
            Box(name + "Trim", deco, new Vector3(pos.x, pos.y + scale.y / 2f + 0.04f, pos.z), new Vector3(scale.x + 0.05f, 0.08f, scale.z + 0.05f), trim, false);
        MinimapRect(pos, new Vector2(scale.x, scale.z), M["IconWall"]);
    }

    static void Cover(Vector3 groundPos, Vector3 size)
    {
        Box("Cover", env, groundPos + Vector3.up * size.y / 2f, size, M["Crate"], true);
        Box("CoverTrim", deco, groundPos + Vector3.up * (size.y + 0.03f), new Vector3(size.x + 0.04f, 0.06f, size.z + 0.04f),
            groundPos.z < 0 ? M["Blue"] : M["Red"], false);
        MinimapRect(groundPos, new Vector2(size.x, size.z), M["IconWall"]);
    }

    // side: в какую сторону по X выступает плафон (к центру линии)
    static void StreetLamp(Vector3 pos, Color color, int side)
    {
        Box("LampPole", deco, pos + Vector3.up * 3.5f, new Vector3(0.18f, 7f, 0.18f), M["WallDark"], false);
        Box("LampArm", deco, pos + new Vector3(side * 0.8f, 7f, 0f), new Vector3(1.7f, 0.14f, 0.14f), M["WallDark"], false);
        Box("LampBar", deco, pos + new Vector3(side * 1.4f, 6.88f, 0f), new Vector3(0.9f, 0.08f, 0.3f), color.r > 0.5f ? M["Red"] : M["Blue"], false);
        PointLight(deco, pos + new Vector3(side * 1.4f, 6.4f, 0f), color, 15f, 2.2f);
    }

    // =====================================================================
    //  Город вокруг арены (только декор)
    // =====================================================================
    static void BuildCity()
    {
        Material[] bmats =
        {
            BuildingMat("BuildingA", new Vector2(2f, 4f)),
            BuildingMat("BuildingB", new Vector2(3f, 7f)),
            BuildingMat("BuildingC", new Vector2(2f, 9f)),
        };
        Material[] signs = { M["NeonPink"], M["NeonViolet"], M["Blue"], M["NeonGold"], M["Red"] };
        Random.State old = Random.state;
        Random.InitState(1337);

        // Две стены небоскрёбов по бокам и по рядам за базами
        foreach (int side in new[] { -1, 1 })
        {
            for (float z = -120f; z <= 120f; z += 17f)
            {
                float w = Random.Range(10f, 16f), d = Random.Range(12f, 18f), hgt = Random.Range(22f, 62f);
                float x = side * (47f + w / 2f + Random.Range(0f, 4f));
                Skyscraper(new Vector3(x, 0f, z + Random.Range(-3f, 3f)), new Vector3(w, hgt, d), bmats[Random.Range(0, 3)], signs, -side);
                // второй, дальний ряд повыше
                float h2 = Random.Range(40f, 90f);
                Skyscraper(new Vector3(side * Random.Range(80f, 95f), 0f, z + 8f), new Vector3(16f, h2, 16f), bmats[Random.Range(0, 3)], signs, -side);
            }
        }
        foreach (int side in new[] { -1, 1 })
        {
            for (float x = -40f; x <= 40f; x += 20f)
            {
                float hgt = Random.Range(25f, 55f);
                Skyscraper(new Vector3(x, 0f, side * (116f + Random.Range(0f, 6f))), new Vector3(16f, hgt, 14f), bmats[Random.Range(0, 3)], signs, 0, -side);
            }
        }
        Random.state = old;
    }

    static Material BuildingMat(string name, Vector2 tiling)
    {
        Material m = Lit(name, new Color(0.04f, 0.045f, 0.06f), Color.white, 1.4f, 0.6f, 0.4f,
            AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/CityWindows.png"), tiling);
        return m;
    }

    // faceX/faceZ: в какую сторону смотрит фасад с вывеской
    static void Skyscraper(Vector3 basePos, Vector3 size, Material mat, Material[] signs, int faceX, int faceZ = 0)
    {
        Box("Building", deco, basePos + Vector3.up * size.y / 2f, size, mat, false);
        // Крыша-карниз
        Box("Roof", deco, basePos + Vector3.up * (size.y + 0.3f), new Vector3(size.x * 0.7f, 0.6f, size.z * 0.7f), M["WallDark"], false);
        if (Random.value < 0.6f)
            Box("Antenna", deco, basePos + Vector3.up * (size.y + 4f), new Vector3(0.2f, 8f, 0.2f), M["Red"], false);

        // Неоновая вывеска на фасаде, обращённом к арене
        if (Random.value < 0.75f)
        {
            Material sign = signs[Random.Range(0, signs.Length)];
            float hy = Random.Range(12f, Mathf.Max(13f, size.y - 6f));
            Vector3 front = basePos + new Vector3(faceX * (size.x / 2f + 0.15f), hy, faceZ * (size.z / 2f + 0.15f));
            bool vertical = Random.value < 0.5f;
            Vector3 s = faceX != 0
                ? (vertical ? new Vector3(0.2f, 9f, 1.6f) : new Vector3(0.2f, 1.4f, size.z * 0.7f))
                : (vertical ? new Vector3(1.6f, 9f, 0.2f) : new Vector3(size.x * 0.7f, 1.4f, 0.2f));
            Box("Sign", deco, front, s, sign, false);
        }
        // Горизонтальные светящиеся полосы этажей
        if (Random.value < 0.4f)
            Box("Band", deco, basePos + Vector3.up * (size.y * 0.5f), new Vector3(size.x + 0.1f, 0.25f, size.z + 0.1f), signs[Random.Range(0, signs.Length)], false);
    }

    // =====================================================================
    //  Башни, Ancient
    // =====================================================================
    static Health MakeTower(string name, Vector3 pos, TeamId id, float hp, Health guard)
    {
        bool blue = id == TeamId.Blue;
        Material team = blue ? M["Blue"] : M["Red"];
        Color col = blue ? Cyan : Orange;

        GameObject root = new GameObject(name);
        root.transform.SetParent(env, false);
        root.transform.position = pos;
        Team t = root.AddComponent<Team>(); t.id = id; t.kind = UnitKind.Tower; t.radius = 1.6f;
        t.displayName = blue ? "Синяя башня" : "Красная башня";
        Health h = root.AddComponent<Health>(); h.maxHealth = hp;
        CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, 4.5f, 0f); cc.radius = 1.4f; cc.height = 9f;

        Prim(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, 0.3f, 0f), new Vector3(3.8f, 0.3f, 3.8f), M["WallDark"], false);
        Prim(PrimitiveType.Cylinder, "BaseRing", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(3.95f, 0.06f, 3.95f), team, false);
        Prim(PrimitiveType.Cylinder, "Pillar", root.transform, new Vector3(0f, 3.9f, 0f), new Vector3(1.7f, 3.3f, 1.7f), M["Wall"], false);
        for (int i = 0; i < 3; i++)
            Prim(PrimitiveType.Cylinder, "Band", root.transform, new Vector3(0f, 2f + i * 2f, 0f), new Vector3(1.8f, 0.07f, 1.8f), team, false);
        Prim(PrimitiveType.Cylinder, "Head", root.transform, new Vector3(0f, 7.4f, 0f), new Vector3(2.6f, 0.25f, 2.6f), M["WallDark"], false);
        GameObject crystal = Prim(PrimitiveType.Cube, "Crystal", root.transform, new Vector3(0f, 8.8f, 0f), new Vector3(1.1f, 1.1f, 1.1f), team, false);
        crystal.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        Rotator rot = crystal.AddComponent<Rotator>(); rot.degreesPerSecond = new Vector3(0f, 70f, 0f); rot.bobAmplitude = 0.25f;
        PointLight(root.transform, new Vector3(0f, 8.8f, 0f), col, 14f, 3f);

        GameObject beamGo = new GameObject("Beam");
        beamGo.transform.SetParent(root.transform, false);
        beamGo.transform.localPosition = new Vector3(0f, 8.8f, 0f);
        LineRenderer lr = beamGo.AddComponent<LineRenderer>();
        lr.widthMultiplier = 0.22f; lr.sharedMaterial = blue ? M["BeamBlue"] : M["BeamRed"];
        lr.shadowCastingMode = ShadowCastingMode.Off;
        BeamEffect be = beamGo.AddComponent<BeamEffect>();
        be.duration = 0.15f; be.shotClip = Clip(ShotClip); be.pitch = 0.55f; be.volume = 0.8f;

        GameObject shield = Prim(PrimitiveType.Sphere, "ShieldBubble", root.transform, new Vector3(0f, 4.5f, 0f), new Vector3(7f, 10f, 7f), M["Shield"], false);
        shield.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        shield.SetActive(guard != null);

        Tower tw = root.AddComponent<Tower>();
        tw.range = 18f; tw.damage = 30f; tw.fireInterval = 1.4f;
        tw.beam = be; tw.muzzle = beamGo.transform; tw.shieldVisual = shield; tw.destroyEffect = P["Explosion"];

        if (guard != null) root.AddComponent<Shield>().guardians = new[] { guard };
        WorldHealthBar bar = root.AddComponent<WorldHealthBar>(); bar.height = 10.3f; bar.width = 3.2f;

        AddIcon(root.transform, 6f, blue ? M["IconBlue"] : M["IconRed"], false);
        // Площадка с радиусом стрельбы (видно, где башня бьёт)
        GameObject range = Prim(PrimitiveType.Cylinder, "RangeRing", root.transform, new Vector3(0f, 0.015f, 0f), new Vector3(36f, 0.005f, 36f), blue ? M["IconBlue"] : M["IconRed"], false);
        range.GetComponent<Renderer>().sharedMaterial = RangeMat(blue);
        range.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        return h;
    }

    static Material RangeMat(bool blue)
    {
        string key = blue ? "RangeBlue" : "RangeRed";
        if (!M.ContainsKey(key))
            M[key] = Unlit(key, blue ? new Color(0f, 0.9f, 1f, 0.05f) : new Color(1f, 0.42f, 0f, 0.05f), true);
        return M[key];
    }

    static GameObject MakeAncient(string name, Vector3 pos, TeamId id, Health[] guards)
    {
        bool blue = id == TeamId.Blue;
        Material team = blue ? M["Blue"] : M["Red"];
        Color col = blue ? Cyan : Orange;

        GameObject root = new GameObject(name);
        root.transform.SetParent(env, false);
        root.transform.position = pos;
        Team t = root.AddComponent<Team>(); t.id = id; t.kind = UnitKind.Ancient; t.radius = 3.5f;
        t.displayName = blue ? "Синий реактор" : "Красный реактор";
        Health h = root.AddComponent<Health>(); h.maxHealth = 2500f;
        CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, 4f, 0f); cc.radius = 3.2f; cc.height = 8f;

        Prim(PrimitiveType.Cylinder, "Platform", root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(10f, 0.25f, 10f), M["WallDark"], false);
        Prim(PrimitiveType.Cylinder, "PlatformRim", root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(10.2f, 0.06f, 10.2f), team, false);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a) * 4f, 3.5f, Mathf.Sin(a) * 4f);
            Prim(PrimitiveType.Cube, "Pylon", root.transform, p, new Vector3(0.8f, 6f, 0.8f), M["Wall"], false);
            Prim(PrimitiveType.Cube, "PylonStrip", root.transform, p, new Vector3(0.18f, 5f, 0.85f), team, false);
        }
        GameObject core = Prim(PrimitiveType.Sphere, "Core", root.transform, new Vector3(0f, 4.5f, 0f), new Vector3(2.4f, 2.4f, 2.4f), blue ? M["CoreBlue"] : M["CoreRed"], false);
        core.AddComponent<Rotator>().bobAmplitude = 0.3f;
        GameObject orbit = new GameObject("Orbit");
        orbit.transform.SetParent(root.transform, false);
        orbit.transform.localPosition = new Vector3(0f, 4.5f, 0f);
        orbit.AddComponent<Rotator>().degreesPerSecond = new Vector3(15f, 60f, 0f);
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Cube, "Shard", orbit.transform, new Vector3(Mathf.Cos(a) * 2.6f, 0f, Mathf.Sin(a) * 2.6f), Vector3.one * 0.5f, team, false);
        }
        Material shaft = Unlit(blue ? "ShaftBlue" : "ShaftRed", new Color(col.r, col.g, col.b, 0.12f), true);
        GameObject beam = Prim(PrimitiveType.Cylinder, "LightShaft", root.transform, new Vector3(0f, 30f, 0f), new Vector3(1.4f, 26f, 1.4f), shaft, false);
        beam.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        PointLight(root.transform, new Vector3(0f, 5f, 0f), col, 22f, 3f);

        GameObject shield = Prim(PrimitiveType.Sphere, "ShieldBubble", root.transform, new Vector3(0f, 4f, 0f), new Vector3(11f, 11f, 11f), M["Shield"], false);
        shield.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        Ancient an = root.AddComponent<Ancient>();
        an.destroyEffect = P["Explosion"]; an.shieldVisual = shield;
        root.AddComponent<Shield>().guardians = guards;
        WorldHealthBar bar = root.AddComponent<WorldHealthBar>(); bar.height = 9.5f; bar.width = 5f;
        Text3D(root.transform, "РЕАКТОР", new Vector3(0f, 11.5f, 0f), Quaternion.Euler(0f, blue ? 0f : 180f, 0f), 5f, col * 1.5f);
        AddIcon(root.transform, 10f, blue ? M["IconBlue"] : M["IconRed"], false);
        return root;
    }

    // =====================================================================
    //  Магазин, фонтан, прыжковые платформы, пикапы
    // =====================================================================
    static void MakeShop(string name, Vector3 pos, float yaw)
    {
        GameObject kiosk = new GameObject(name);
        kiosk.transform.SetParent(env, false);
        kiosk.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        Prim(PrimitiveType.Cube, "Body", kiosk.transform, new Vector3(0f, 1.3f, 0f), new Vector3(1.8f, 2.6f, 0.9f), M["WallDark"], true);
        Prim(PrimitiveType.Cube, "Screen", kiosk.transform, new Vector3(0f, 1.6f, 0.46f), new Vector3(1.4f, 1.1f, 0.02f), M["Blue"], false);
        Prim(PrimitiveType.Cube, "TopLight", kiosk.transform, new Vector3(0f, 2.65f, 0f), new Vector3(1.9f, 0.1f, 1f), M["NeonGold"], false);
        PointLight(kiosk.transform, new Vector3(0f, 2.2f, 1.2f), Gold, 7f, 2.5f);
        Text3D(kiosk.transform, "МАГАЗИН", new Vector3(0f, 3.3f, 0f), Quaternion.identity, 2.4f, Gold * 1.8f);
        Text3D(kiosk.transform, "МАГАЗИН", new Vector3(0f, 3.3f, 0f), Quaternion.Euler(0f, 180f, 0f), 2.4f, Gold * 1.8f);
        MinimapRect(pos, new Vector2(3f, 3f), M["IconGold"]);

        GameObject zone = new GameObject(name + "Zone");
        zone.transform.position = pos + Vector3.up;
        BoxCollider bc = zone.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(6f, 3f, 6f);
        zone.AddComponent<ShopTerminal>().prompt = "<color=#FFD126>E</color> — открыть магазин";
    }

    static void MakeFountain(string name, Vector3 pos, bool usable)
    {
        GameObject f = new GameObject(name);
        f.transform.SetParent(env, false);
        f.transform.position = pos;
        Prim(PrimitiveType.Cylinder, "Basin", f.transform, new Vector3(0f, 0.35f, 0f), new Vector3(3.4f, 0.35f, 3.4f), M["WallDark"], true);
        Prim(PrimitiveType.Cylinder, "Rim", f.transform, new Vector3(0f, 0.55f, 0f), new Vector3(3.5f, 0.06f, 3.5f), M["NeonGreen"], false);
        GameObject core = Prim(PrimitiveType.Cylinder, "Core", f.transform, new Vector3(0f, 1.6f, 0f), new Vector3(0.7f, 0.9f, 0.7f), M["NeonGreen"], false);
        core.AddComponent<Rotator>().bobAmplitude = 0.15f;
        Light glow = PointLight(f.transform, new Vector3(0f, 2.2f, 0f), new Color(0.2f, 1f, 0.4f), 9f, 3f);
        Text3D(f.transform, "ЛЕЧЕНИЕ", new Vector3(0f, 3.4f, 0f), Quaternion.Euler(0f, pos.z < 0 ? 0f : 180f, 0f), 2.2f, new Color(0.3f, 1f, 0.5f) * 1.8f);
        if (!usable) return;

        GameObject zone = new GameObject(name + "Zone");
        zone.transform.position = pos + Vector3.up;
        BoxCollider bc = zone.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(6f, 3f, 6f);
        HealingStation hs = zone.AddComponent<HealingStation>();
        hs.prompt = "<color=#4DFF73>E</color> — восстановить здоровье и энергию";
        hs.healClip = Clip(CoinClip); hs.healEffect = P["HealFx"]; hs.glow = glow;
    }

    static void MakeJumpPad(string name, Vector3 pos, float yaw)
    {
        GameObject pad = new GameObject(name);
        pad.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        Prim(PrimitiveType.Cylinder, "Plate", pad.transform, new Vector3(0f, 0.05f, 0f), new Vector3(2.6f, 0.05f, 2.6f), M["Hazard"], false);
        GameObject arrow = Prim(PrimitiveType.Cube, "Arrow", pad.transform, new Vector3(0f, 0.12f, 0.3f), new Vector3(0.35f, 0.04f, 1.6f), M["NeonGold"], false);
        GameObject tipL = Prim(PrimitiveType.Cube, "ArrowL", pad.transform, new Vector3(-0.35f, 0.12f, 0.85f), new Vector3(0.25f, 0.04f, 0.9f), M["NeonGold"], false);
        tipL.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
        GameObject tipR = Prim(PrimitiveType.Cube, "ArrowR", pad.transform, new Vector3(0.35f, 0.12f, 0.85f), new Vector3(0.25f, 0.04f, 0.9f), M["NeonGold"], false);
        tipR.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
        PointLight(pad.transform, new Vector3(0f, 1f, 0f), Gold, 5f, 2f);
        BoxCollider bc = pad.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(2.4f, 1.2f, 2.4f); bc.center = new Vector3(0f, 0.6f, 0f);
        JumpPad jp = pad.AddComponent<JumpPad>();
        jp.upSpeed = 15f; jp.forwardSpeed = 7.5f;
        MinimapRect(pos, new Vector2(2.5f, 2.5f), M["IconGold"]);
    }

    static void PlacePickup(GameObject prefab, Vector3 pos)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        go.GetComponent<Pickup>().respawnTime = 30f;
    }

    // =====================================================================
    //  Юниты
    // =====================================================================
    static void MakeSpawner(string name, Vector3 pos, TeamId id, Transform[] lane, Transform enemyAncient)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, id == TeamId.Blue ? 0f : 180f, 0f);
        WaveSpawner ws = go.AddComponent<WaveSpawner>();
        ws.team = id; ws.minionPrefab = P["Minion"]; ws.lanePoints = lane; ws.enemyAncient = enemyAncient;
        ws.firstDelay = 5f; ws.waveInterval = 25f; ws.minionsPerWave = 3; ws.delayBetweenMinions = 1f;
    }

    static void PlaceBot(string name, string display, Vector3 pos, TeamId id, Transform[] lane, bool reverse, Transform goal, Transform respawn)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(P["BotHero"]);
        go.name = name;
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, id == TeamId.Blue ? 0f : 180f, 0f));
        Team t = go.GetComponent<Team>(); t.id = id; t.displayName = display;
        UnitAI ai = go.GetComponent<UnitAI>();
        ai.waypoints = lane; ai.reverseRoute = reverse; ai.finalGoal = goal; ai.home = respawn;
        go.GetComponent<Respawner>().respawnPoint = respawn;
        go.GetComponentInChildren<LineRenderer>().sharedMaterial = id == TeamId.Blue ? M["BeamBlue"] : M["BeamRed"];
    }

    static void PlaceNeutral(GameObject prefab, string name, Vector3 pos, Transform home)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        go.GetComponent<UnitAI>().home = home;
    }

    static GameObject MakePlayer(Transform spawn)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.9f, 0f); cc.radius = 0.4f; cc.height = 1.8f;
        AudioSource audio = player.AddComponent<AudioSource>();
        audio.playOnAwake = false; audio.spatialBlend = 0f;

        Team team = player.AddComponent<Team>(); team.id = TeamId.Blue; team.kind = UnitKind.Hero; team.displayName = "Ты";
        Health hp = player.AddComponent<Health>(); hp.maxHealth = 220f; hp.regenPerSecond = 2f;

        GameObject model = AddModel(player.transform, "Assets/Models/Y Bot.fbx", 1.8f, "Assets/Animations/PlayerAnimator.controller");
        foreach (SkinnedMeshRenderer r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            r.sharedMaterials = Fill(r.sharedMaterials.Length, r.name.Contains("Joint") ? M["Blue"] : M["HeroBody"]);
        Transform muzzle = AddRifle(model);
        Transform rifle = muzzle.parent;

        // Камера над правым плечом
        GameObject pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(pivot.transform, false);
        camGo.transform.localPosition = new Vector3(0.75f, 0.35f, -3.4f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 62f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 450f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.025f, 0.05f);
        cam.cullingMask = ~(1 << MinimapLayer);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraCollision>();
        UniversalAdditionalCameraData data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.cameraPivot = pivot.transform;

        // Трассер и вспышка выстрела
        GameObject tracerGo = new GameObject("Tracer");
        tracerGo.transform.SetParent(player.transform, false);
        LineRenderer lr = tracerGo.AddComponent<LineRenderer>();
        lr.widthMultiplier = 0.045f; lr.sharedMaterial = M["Tracer"]; lr.shadowCastingMode = ShadowCastingMode.Off;
        BeamEffect tracer = tracerGo.AddComponent<BeamEffect>();
        tracer.duration = 0.05f;
        Light flash = PointLight(muzzle, Vector3.zero, new Color(0.6f, 1f, 1f), 6f, 4f);

        PlayerShooting ps = player.AddComponent<PlayerShooting>();
        ps.cam = cam; ps.muzzle = muzzle; ps.tracer = tracer; ps.muzzleFlash = flash;
        ps.audioSource = audio;
        ps.shootClip = Clip(ShotClip); ps.reloadClip = Clip(ReloadClip); ps.hitClip = Clip(ImpactClip);
        ps.hitEffectPrefab = P["HitSpark"];

        PlayerAbilities ab = player.AddComponent<PlayerAbilities>();
        ab.burstEffectPrefab = P["BurstRing"]; ab.burstClip = Clip(ImpactClip);

        Bounty b = player.AddComponent<Bounty>(); b.credits = 0; b.destroyOnDeath = false; b.deathEffect = P["DeathPuff"];

        // Иконка игрока на мини-карте: белый круг и "нос" по направлению взгляда
        GameObject icon = AddIcon(player.transform, 5f, M["IconWhite"], false);
        GameObject nose = Prim(PrimitiveType.Cube, "IconNose", player.transform, Vector3.zero, new Vector3(1.6f, 0.1f, 5f), M["IconWhite"], false);
        nose.transform.position = new Vector3(player.transform.position.x, 60.5f, player.transform.position.z) + player.transform.forward * 3f;
        nose.layer = MinimapLayer;

        Respawner rs = player.AddComponent<Respawner>();
        rs.respawnPoint = spawn; rs.baseTime = 6f; rs.extraPerDeath = 4f;
        rs.hideWhileDead = new[] { model, icon, nose, rifle.gameObject };

        SetLayerRecursive(player, PlayerLayer, MinimapLayer);
        camGo.layer = 0;
        return player;
    }

    static void MakeMusic(float volume)
    {
        AudioSource ms = new GameObject("Music").AddComponent<AudioSource>();
        ms.clip = Clip(MusicClip); ms.loop = true; ms.playOnAwake = true; ms.volume = volume; ms.spatialBlend = 0f;
    }

    // =====================================================================
    //  Мини-карта: камера сверху рендерит слой Minimap в текстуру
    // =====================================================================
    static RenderTexture minimapTexture;

    static void BuildMinimapCamera()
    {
        Directory.CreateDirectory("Assets/UI");
        string rtPath = "Assets/UI/MinimapTexture.renderTexture";
        AssetDatabase.DeleteAsset(rtPath);
        minimapTexture = new RenderTexture(256, 512, 16) { name = "MinimapTexture", antiAliasing = 2 };
        AssetDatabase.CreateAsset(minimapTexture, rtPath);

        GameObject camGo = new GameObject("MinimapCamera");
        camGo.transform.SetPositionAndRotation(new Vector3(0f, 120f, 0f), Quaternion.Euler(90f, 0f, 0f));
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 108f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
        cam.targetTexture = minimapTexture;
        cam.cullingMask = 1 << MinimapLayer;
        cam.nearClipPlane = 1f; cam.farClipPlane = 200f;
        UniversalAdditionalCameraData data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        data.renderShadows = false;
        data.antialiasing = AntialiasingMode.None;
    }

    // Плоский прямоугольник схемы карты на слое Minimap
    static void MinimapRect(Vector3 pos, Vector2 size, Material mat)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Cube);
        q.name = "MapRect";
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.layer = MinimapLayer;
        q.transform.SetParent(minimapRoot, false);
        q.transform.position = new Vector3(pos.x, mat == M["IconLane"] ? 40f : 45f, pos.z);
        q.transform.localScale = new Vector3(size.x, 0.1f, size.y);
        Renderer r = q.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static Transform Point(string name, Vector3 pos)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(points, false);
        t.position = pos;
        return t;
    }
}
