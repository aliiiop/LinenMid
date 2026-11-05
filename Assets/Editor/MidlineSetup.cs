// Автоматизирует ручные шаги этапов 7-12 из инструкции (расстановка объектов, префабы, интерфейс, меню).
// Использование: открой сцену Game -> меню Midline -> по порядку пункты 1, 2, 3. После каждого пункта сохрани сцену.
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class MidlineSetup
{
    const int MinimapLayer = 7;

    // =====================================================================
    //  Пункт 1: этапы 7-8 (башни, Ancient, боты, нейтралы, пикапы)
    // =====================================================================
    [MenuItem("Midline/1. Башни, Ancient, боты, нейтралы (этапы 7-8)")]
    static void BuildWorld()
    {
        EnsureMinimapLayer();

        Material blue = Mat("TeamBlue", new Color(0.04f, 0.06f, 0.08f), new Color(0f, 0.9f, 1f), 2f);
        Material red = Mat("TeamRed", new Color(0.04f, 0.06f, 0.08f), new Color(1f, 0.42f, 0f), 2f);
        Material beamMat = UnlitMat("Beam", new Color(1f, 0.23f, 0f));
        Material gold = Mat("Gold", new Color(0.1f, 0.08f, 0f), new Color(1f, 0.8f, 0.1f), 2f);
        Material green = Mat("Green", new Color(0f, 0.1f, 0f), new Color(0.2f, 1f, 0.3f), 1.5f);
        Material energyMat = Mat("EnergyBlue", new Color(0f, 0f, 0.1f), new Color(0.2f, 0.5f, 1f), 2f);
        Material iconWhite = Mat("IconWhite", Color.white, Color.white, 1f);

        // ----- Пикапы -----
        GameObject coin = MakePickup("CreditCoin", PrimitiveType.Cylinder, PickupType.Credits, 30, gold, new Vector3(0.5f, 0.05f, 0.5f), "Assets/Audio/350868__cabled_mess__coin_c_05.wav");
        GameObject health = MakePickup("HealthPack", PrimitiveType.Cube, PickupType.Health, 60, green, new Vector3(0.5f, 0.5f, 0.5f), "Assets/Audio/350868__cabled_mess__coin_c_05.wav");
        GameObject energy = MakePickup("EnergyCell", PrimitiveType.Sphere, PickupType.Energy, 40, energyMat, new Vector3(0.5f, 0.5f, 0.5f), "Assets/Audio/350868__cabled_mess__coin_c_05.wav");

        // ----- Префаб башни -----
        GameObject towerPrefab = MakeTowerPrefab(beamMat, blue, red);

        // ----- Минион: TeamTint и иконка -----
        PatchMinionPrefab(blue, red, coin);

        // ----- Башни и Ancient внутри Environment -----
        GameObject env = GameObject.Find("Environment");
        if (env == null) { Debug.LogError("Нет объекта Environment в сцене. Открой сцену Game."); return; }

        Health bl1 = PlaceTower(towerPrefab, env, "BlueTowerL1", new Vector3(-27, 0, -15), TeamId.Blue, 600, null);
        Health bl2 = PlaceTower(towerPrefab, env, "BlueTowerL2", new Vector3(-27, 0, -48), TeamId.Blue, 800, bl1);
        Health br1 = PlaceTower(towerPrefab, env, "BlueTowerR1", new Vector3(27, 0, -15), TeamId.Blue, 600, null);
        Health br2 = PlaceTower(towerPrefab, env, "BlueTowerR2", new Vector3(27, 0, -48), TeamId.Blue, 800, br1);
        Health rl1 = PlaceTower(towerPrefab, env, "RedTowerL1", new Vector3(-27, 0, 15), TeamId.Red, 600, null);
        Health rl2 = PlaceTower(towerPrefab, env, "RedTowerL2", new Vector3(-27, 0, 48), TeamId.Red, 800, rl1);
        Health rr1 = PlaceTower(towerPrefab, env, "RedTowerR1", new Vector3(27, 0, 15), TeamId.Red, 600, null);
        Health rr2 = PlaceTower(towerPrefab, env, "RedTowerR2", new Vector3(27, 0, 48), TeamId.Red, 800, rr1);

        GameObject blueAncient = PlaceAncient(env, "BlueAncient", new Vector3(0, 3, -92), TeamId.Blue, blue, red, new[] { bl2, br2 });
        GameObject redAncient = PlaceAncient(env, "RedAncient", new Vector3(0, 3, 92), TeamId.Red, blue, red, new[] { rl2, rr2 });

        // ----- Магазины и фонтан (внутри Environment, чтобы NavMesh обошёл) -----
        MakeInteractable(env, "ShopBase", PrimitiveType.Cube, new Vector3(10, 1, -80), new Vector3(1.5f, 2, 1), blue, typeof(ShopTerminal), "Нажми E: магазин");
        MakeInteractable(env, "ShopMid", PrimitiveType.Cube, new Vector3(0, 1, -20), new Vector3(1.5f, 2, 1), blue, typeof(ShopTerminal), "Нажми E: магазин");
        MakeInteractable(env, "Fountain", PrimitiveType.Cylinder, new Vector3(-10, 0.5f, -80), new Vector3(1.5f, 0.5f, 1.5f), green, typeof(HealingStation), "Нажми E: лечение");

        // ----- Спавнеры волн -----
        WireSpawner("SpawnBlueL", redAncient);
        WireSpawner("SpawnBlueR", redAncient);
        WireSpawner("SpawnRedL", blueAncient);
        WireSpawner("SpawnRedR", blueAncient);

        // ----- Точки кемпов -----
        GameObject points = GameObject.Find("Points");
        Transform campBlue = EnsurePoint(points, "CampBlue", new Vector3(0, 0, -42));
        Transform campRed = EnsurePoint(points, "CampRed", new Vector3(0, 0, 42));
        Transform pitHome = EnsurePoint(points, "PitHome", Vector3.zero);

        // ----- Запекаем навигацию -----
        NavMeshSurface surface = env.GetComponent<NavMeshSurface>();
        if (surface != null) surface.BuildNavMesh();
        else Debug.LogWarning("На Environment нет NavMesh Surface: запеки навигацию вручную.");

        // ----- Боты и нейтралы -----
        GameObject botPrefab = MakeBotPrefab(beamMat, blue, red, iconWhite);
        GameObject neutralPrefab = MakeNeutralPrefab("NeutralMonster", false, health, gold);
        GameObject pitPrefab = MakeNeutralPrefab("Pitmonster", true, energy, gold);

        Transform blueRespawn = Find("BlueRespawn"), redRespawn = Find("RedRespawn");
        PlaceBot(botPrefab, "AllyBot", new Vector3(-4, 0, -76), TeamId.Blue, "LaneL_", false, redAncient.transform, blueRespawn);
        PlaceBot(botPrefab, "EnemyBotL", new Vector3(-4, 0, 76), TeamId.Red, "LaneL_", true, blueAncient.transform, redRespawn);
        PlaceBot(botPrefab, "EnemyBotR", new Vector3(4, 0, 76), TeamId.Red, "LaneR_", true, blueAncient.transform, redRespawn);

        PlaceNeutral(neutralPrefab, "NeutralBlue1", new Vector3(-6, 0, -42), campBlue);
        PlaceNeutral(neutralPrefab, "NeutralBlue2", new Vector3(6, 0, -42), campBlue);
        PlaceNeutral(neutralPrefab, "NeutralRed1", new Vector3(-6, 0, 42), campRed);
        PlaceNeutral(neutralPrefab, "NeutralRed2", new Vector3(6, 0, 42), campRed);
        PlaceNeutral(pitPrefab, "Pitmonster", Vector3.zero, pitHome);

        // ----- Игрок: Bounty, Respawner, иконка -----
        SetupPlayer(iconWhite);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Midline: этапы 7-8 готовы. Сохрани сцену (Ctrl+S) и запусти Play.");
    }

    // =====================================================================
    //  Пункт 2: этапы 9-10 и 12 (HUD, мини-карта, паузы, магазин, звук)
    // =====================================================================
    [MenuItem("Midline/2. HUD, мини-карта, магазин, звук (этапы 9, 10, 12)")]
    static void BuildUI()
    {
        EnsureMinimapLayer();

        GameObject player = GameObject.Find("Player");
        GameObject managers = GameObject.Find("Managers");
        if (player == null) { Debug.LogError("В сцене нет объекта Player."); return; }
        if (managers == null) managers = new GameObject("Managers");
        GameManager gm = GetOrAdd<GameManager>(managers);
        UIManager ui = GetOrAdd<UIManager>(managers);

        // Старый Canvas пересоздаём, чтобы пункт можно было запускать повторно
        GameObject oldCanvas = GameObject.Find("HUDCanvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

        GameObject canvasGo = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        ConfigureCanvas(canvasGo);
        Transform c = canvasGo.transform;
        EnsureEventSystem();

        // ----- HUD -----
        MakeCrosshair(c);
        TMP_Text score = Text(c, "ScoreText", "", A.TopC, new Vector2(0, -30), new Vector2(1000, 50), 32, TextAlignmentOptions.Center);
        TMP_Text nearText = Text(c, "NearTowerText", "", A.TopC, new Vector2(0, -80), new Vector2(700, 40), 26, TextAlignmentOptions.Center);
        Slider nearBar = SliderUI(c, "NearTowerBar", A.TopC, new Vector2(0, -115), new Vector2(500, 16), new Color(0.9f, 0.2f, 0.1f));
        Slider hpBar = SliderUI(c, "HealthBar", A.BotC, new Vector2(0, 70), new Vector2(500, 28), new Color(0.2f, 0.85f, 0.3f));
        TMP_Text hpText = Text(c, "HealthText", "", A.BotC, new Vector2(0, 70), new Vector2(500, 28), 24, TextAlignmentOptions.Center);
        Slider enBar = SliderUI(c, "EnergyBar", A.BotC, new Vector2(0, 38), new Vector2(500, 14), new Color(0.2f, 0.5f, 1f));
        TMP_Text dash = Text(c, "DashText", "", A.BotC, new Vector2(-380, 60), new Vector2(300, 40), 26, TextAlignmentOptions.Center);
        TMP_Text abil = Text(c, "AbilityText", "", A.BotC, new Vector2(380, 60), new Vector2(300, 40), 26, TextAlignmentOptions.Center);
        TMP_Text credits = Text(c, "CreditsText", "", A.BotL, new Vector2(60, 60), new Vector2(400, 50), 30, TextAlignmentOptions.Left);
        TMP_Text ammo = Text(c, "AmmoText", "", A.BotR, new Vector2(-40, 330), new Vector2(300, 50), 32, TextAlignmentOptions.Right);
        TMP_Text prompt = Text(c, "PromptText", "Нажми E", A.Center, new Vector2(0, -150), new Vector2(600, 60), 32, TextAlignmentOptions.Center);
        TMP_Text message = Text(c, "MessageText", "", A.TopC, new Vector2(0, -170), new Vector2(900, 50), 30, TextAlignmentOptions.Center);
        TMP_Text respawn = Text(c, "RespawnText", "", A.Center, new Vector2(0, 60), new Vector2(900, 100), 64, TextAlignmentOptions.Center);

        // ----- Мини-карта -----
        BuildMinimap(c);

        // ----- Пауза -----
        GameObject pausePanel = Panel(c, "PausePanel", new Color(0, 0, 0, 0.7f));
        Text(pausePanel.transform, "PauseTitle", "ПАУЗА", A.Center, new Vector2(0, 160), new Vector2(600, 100), 72, TextAlignmentOptions.Center);
        Button bCont = ButtonUI(pausePanel.transform, "ContinueButton", "Продолжить", A.Center, new Vector2(0, 40), new Vector2(400, 60));
        Button bRest = ButtonUI(pausePanel.transform, "RestartButton", "Заново", A.Center, new Vector2(0, -40), new Vector2(400, 60));
        Button bMenu = ButtonUI(pausePanel.transform, "MenuButton", "В меню", A.Center, new Vector2(0, -120), new Vector2(400, 60));
        UnityEventTools.AddPersistentListener(bCont.onClick, new UnityAction(gm.TogglePause));
        UnityEventTools.AddPersistentListener(bRest.onClick, new UnityAction(gm.Restart));
        UnityEventTools.AddPersistentListener(bMenu.onClick, new UnityAction(gm.ToMenu));

        // ----- Конец игры -----
        GameObject endPanel = Panel(c, "EndPanel", new Color(0, 0, 0, 0.75f));
        TMP_Text endTitle = Text(endPanel.transform, "EndTitle", "", A.Center, new Vector2(0, 120), new Vector2(1000, 160), 120, TextAlignmentOptions.Center);
        Button eRest = ButtonUI(endPanel.transform, "RestartButton", "Заново", A.Center, new Vector2(0, -40), new Vector2(400, 60));
        Button eMenu = ButtonUI(endPanel.transform, "MenuButton", "В меню", A.Center, new Vector2(0, -120), new Vector2(400, 60));
        UnityEventTools.AddPersistentListener(eRest.onClick, new UnityAction(gm.Restart));
        UnityEventTools.AddPersistentListener(eMenu.onClick, new UnityAction(gm.ToMenu));

        // ----- Магазин -----
        GameObject shopGo = Panel(c, "ShopPanel", new Color(0.04f, 0.06f, 0.1f, 0.8f));
        RectTransform shopRt = shopGo.GetComponent<RectTransform>();
        shopRt.anchorMin = shopRt.anchorMax = new Vector2(0, 0.5f);
        shopRt.pivot = new Vector2(0, 0.5f);
        shopRt.anchoredPosition = new Vector2(30, 0);
        shopRt.sizeDelta = new Vector2(520, 760);
        Text(shopGo.transform, "ShopTitle", "МАГАЗИН", A.Top, new Vector2(0, -20), new Vector2(480, 60), 48, TextAlignmentOptions.Center);
        string[] names = { "Магазин +10 патронов (60)", "Скорострельность (80)", "+60 HP (60)", "Пластины -15% урона (80)", "Ускоритель Q (70)", "Батарея +50 энергии (70)" };
        string[] heads = { "Оружие", "Защита", "Способности" };
        ShopPanel shop = shopGo.AddComponent<ShopPanel>();
        float y = -90;
        for (int g = 0; g < 3; g++)
        {
            Text(shopGo.transform, "Head" + g, heads[g], A.Top, new Vector2(0, y), new Vector2(480, 36), 28, TextAlignmentOptions.Center);
            y -= 40;
            for (int k = 0; k < 2; k++)
            {
                int index = g * 2 + k;
                Button b = ButtonUI(shopGo.transform, "Item" + index, names[index], A.Top, new Vector2(0, y - 24), new Vector2(460, 48));
                UnityEventTools.AddIntPersistentListener(b.onClick, new UnityAction<int>(shop.Buy), index);
                y -= 56;
            }
            y -= 8;
        }
        TMP_Text shopMsg = Text(shopGo.transform, "ShopMessage", "", A.Top, new Vector2(0, y - 20), new Vector2(480, 40), 24, TextAlignmentOptions.Center);
        Button close = ButtonUI(shopGo.transform, "CloseButton", "Закрыть", A.Bot, new Vector2(0, 40), new Vector2(300, 50));
        UnityEventTools.AddPersistentListener(close.onClick, new UnityAction(shop.Close));

        shop.shooting = player.GetComponent<PlayerShooting>();
        shop.playerHealth = player.GetComponent<Health>();
        shop.abilities = player.GetComponent<PlayerAbilities>();
        shop.messageText = shopMsg;

        // ----- Подключаем GameManager и UIManager -----
        gm.pausePanel = pausePanel;
        gm.shopPanel = shopGo;
        gm.endPanel = endPanel;
        gm.endTitleText = endTitle;
        gm.promptText = prompt;
        gm.messageText = message;

        ui.playerHealth = player.GetComponent<Health>();
        ui.player = player.GetComponent<PlayerController>();
        ui.shooting = player.GetComponent<PlayerShooting>();
        ui.abilities = player.GetComponent<PlayerAbilities>();
        ui.playerRespawn = player.GetComponent<Respawner>();
        GameObject ra = GameObject.Find("RedAncient");
        if (ra != null) ui.redAncient = ra.GetComponent<Health>();
        ui.healthBar = hpBar; ui.healthText = hpText; ui.energyBar = enBar;
        ui.ammoText = ammo; ui.creditsText = credits; ui.dashText = dash; ui.abilityText = abil;
        ui.scoreText = score; ui.nearTowerText = nearText; ui.nearTowerBar = nearBar;
        ui.respawnText = respawn;

        pausePanel.SetActive(false);
        endPanel.SetActive(false);
        shopGo.SetActive(false);

        // ----- Звук (этап 12) -----
        WireAudio(player);

        EditorUtility.SetDirty(gm);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Midline: интерфейс, мини-карта, магазин и звук готовы. Сохрани сцену (Ctrl+S).");
    }

    // =====================================================================
    //  Пункт 3: этап 11 (сцена MainMenu и Build Settings)
    // =====================================================================
    [MenuItem("Midline/3. Сцена MainMenu и Build Settings (этап 11)")]
    static void BuildMainMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
        }
        Light light = Object.FindFirstObjectByType<Light>();
        if (light != null) Object.DestroyImmediate(light.gameObject);

        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        ConfigureCanvas(canvasGo);
        Transform c = canvasGo.transform;
        EnsureEventSystem();

        GameObject bg = Panel(c, "Background", new Color(0.04f, 0.06f, 0.1f, 1f));

        TMP_Text title = Text(c, "Title", "MIDLINE", A.Top, new Vector2(0, -120), new Vector2(1200, 180), 120, TextAlignmentOptions.Center);
        title.color = new Color(0f, 0.9f, 1f);
        Text(c, "Subtitle", "Neon City MOBA", A.Top, new Vector2(0, -290), new Vector2(800, 60), 40, TextAlignmentOptions.Center);

        Button play = ButtonUI(c, "PlayButton", "Играть", A.Center, new Vector2(0, 40), new Vector2(420, 70));
        Button credits = ButtonUI(c, "CreditsButton", "Титры", A.Center, new Vector2(0, -50), new Vector2(420, 70));
        Button quit = ButtonUI(c, "QuitButton", "Выход", A.Center, new Vector2(0, -140), new Vector2(420, 70));

        // Выбор героя
        GameObject heroPanel = Panel(c, "HeroPanel", new Color(0.03f, 0.05f, 0.09f, 1f));
        Text(heroPanel.transform, "HeroTitle", "ВЫБЕРИ ГЕРОЯ", A.Top, new Vector2(0, -100), new Vector2(1000, 100), 72, TextAlignmentOptions.Center);
        Button gunner = ButtonUI(heroPanel.transform, "GunnerButton", "Стрелок", A.Center, new Vector2(-300, 40), new Vector2(460, 90));
        Text(heroPanel.transform, "GunnerDesc", "Урон оружия +5", A.Center, new Vector2(-300, -40), new Vector2(460, 50), 30, TextAlignmentOptions.Center);
        Button runner = ButtonUI(heroPanel.transform, "RunnerButton", "Бегун", A.Center, new Vector2(300, 40), new Vector2(460, 90));
        Text(heroPanel.transform, "RunnerDesc", "Скорость +20%, 3 заряда рывка", A.Center, new Vector2(300, -40), new Vector2(560, 50), 30, TextAlignmentOptions.Center);
        Button heroBack = ButtonUI(heroPanel.transform, "HeroBackButton", "Назад", A.Center, new Vector2(0, -200), new Vector2(360, 70));

        // Титры (подставь свои данные, текст должен совпадать с ASSETS.md)
        GameObject creditsPanel = Panel(c, "CreditsPanel", new Color(0.03f, 0.05f, 0.09f, 1f));
        TMP_Text creditsText = Text(creditsPanel.transform, "CreditsText", CreditsString(), A.Center, new Vector2(0, 40), new Vector2(1500, 800), 30, TextAlignmentOptions.Center);
        Button back = ButtonUI(creditsPanel.transform, "BackButton", "Назад", A.Bot, new Vector2(0, 60), new Vector2(360, 70));

        GameObject mm = new GameObject("MenuManager");
        MainMenu menu = mm.AddComponent<MainMenu>();
        menu.creditsPanel = creditsPanel;
        menu.heroPanel = heroPanel;

        UnityEventTools.AddBoolPersistentListener(play.onClick, new UnityAction<bool>(menu.ShowHeroSelect), true);
        UnityEventTools.AddBoolPersistentListener(heroBack.onClick, new UnityAction<bool>(menu.ShowHeroSelect), false);
        UnityEventTools.AddIntPersistentListener(gunner.onClick, new UnityAction<int>(menu.StartGame), 0);
        UnityEventTools.AddIntPersistentListener(runner.onClick, new UnityAction<int>(menu.StartGame), 1);
        UnityEventTools.AddBoolPersistentListener(credits.onClick, new UnityAction<bool>(menu.ShowCredits), true);
        UnityEventTools.AddBoolPersistentListener(back.onClick, new UnityAction<bool>(menu.ShowCredits), false);
        UnityEventTools.AddPersistentListener(quit.onClick, new UnityAction(menu.Quit));

        // Музыка меню
        AudioClip music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cyberpunk Moonlight Sonata.mp3");
        GameObject musicGo = new GameObject("Music");
        AudioSource src = musicGo.AddComponent<AudioSource>();
        src.clip = music; src.loop = true; src.playOnAwake = true; src.volume = 0.3f; src.spatialBlend = 0f;

        heroPanel.SetActive(false);
        creditsPanel.SetActive(false);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true),
        };
        Debug.Log("Midline: сцена MainMenu создана и добавлена в Build Settings (0 - MainMenu, 1 - Game).");
    }

    static string CreditsString()
    {
        return "MIDLINE\n\n" +
               "Автор, программирование, гейм-дизайн:\n<Имя Фамилия>, группа <...>\n\n" +
               "Модели и анимации: Adobe Mixamo (mixamo.com)\n\n" +
               "Звуки (freesound.org):\n" +
               "plasma laser pistol shot — erokia (#427396)\n" +
               "reload — dacmod1 (#688866)\n" +
               "coin — cabled_mess (#350868)\n" +
               "cinematic energy impact — rescopicsound (#749788)\n" +
               "(лицензии: <проверь на странице каждого звука>)\n\n" +
               "Музыка: Cyberpunk Moonlight Sonata — <автор, лицензия, ссылка>\n\n" +
               "Прицел: собственная работа\nДвижок: Unity\n" +
               "Учебный проект, <университет>, <год>";
    }

    // =====================================================================
    //  Игровые объекты
    // =====================================================================
    static GameObject MakeTowerPrefab(Material beamMat, Material blue, Material red)
    {
        GameObject root = new GameObject("Tower");
        Health hp = root.AddComponent<Health>(); hp.maxHealth = 600f;
        Team team = root.AddComponent<Team>(); team.kind = UnitKind.Tower; team.radius = 2f;
        Tower tower = root.AddComponent<Tower>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0, 4, 0);
        visual.transform.localScale = new Vector3(3, 4, 3);

        GameObject beamGo = new GameObject("Beam");
        beamGo.transform.SetParent(root.transform, false);
        beamGo.transform.localPosition = new Vector3(0, 7.5f, 0);
        BeamEffect be = beamGo.AddComponent<BeamEffect>();
        LineRenderer lr = beamGo.GetComponent<LineRenderer>();
        lr.widthMultiplier = 0.15f; lr.useWorldSpace = true; lr.sharedMaterial = beamMat;
        tower.beam = be; tower.muzzle = beamGo.transform;
        tower.range = 18f; tower.damage = 25f; tower.fireInterval = 1.5f;

        AddIcon(root.transform, 8f, null);

        TeamTint tint = root.AddComponent<TeamTint>();
        tint.blueMaterial = blue; tint.redMaterial = red;

        return SavePrefab(root, "Assets/Prefabs/Tower.prefab");
    }

    static Health PlaceTower(GameObject prefab, GameObject env, string name, Vector3 pos, TeamId id, float hp, Health guard)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, env.transform);
        go.name = name;
        go.transform.position = pos;
        go.GetComponent<Team>().id = id;
        Health h = go.GetComponent<Health>();
        h.maxHealth = hp;
        if (guard != null)
        {
            Shield s = go.AddComponent<Shield>();
            s.guardians = new[] { guard };
        }
        return h;
    }

    static GameObject PlaceAncient(GameObject env, string name, Vector3 pos, TeamId id, Material blue, Material red, Health[] guards)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(env.transform, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(6, 6, 6);

        Team t = go.AddComponent<Team>(); t.id = id; t.kind = UnitKind.Ancient; t.radius = 4f;
        Health h = go.AddComponent<Health>(); h.maxHealth = 1500f;
        go.AddComponent<Ancient>();
        Shield s = go.AddComponent<Shield>(); s.guardians = guards;
        TeamTint tint = go.AddComponent<TeamTint>(); tint.blueMaterial = blue; tint.redMaterial = red;
        AddIcon(go.transform, 14f, null);
        return go;
    }

    static void MakeInteractable(GameObject env, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, System.Type script, string prompt)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Object.DestroyImmediate(old);
        GameObject oldZone = GameObject.Find(name + "Zone");
        if (oldZone != null) Object.DestroyImmediate(oldZone);

        GameObject vis = GameObject.CreatePrimitive(type);
        vis.name = name;
        vis.transform.SetParent(env.transform, false);
        vis.transform.position = pos;
        vis.transform.localScale = scale;
        vis.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject zone = new GameObject(name + "Zone");
        zone.transform.position = new Vector3(pos.x, 1f, pos.z);
        BoxCollider bc = zone.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(5, 2, 5);
        Interactable it = (Interactable)zone.AddComponent(script);
        it.prompt = prompt;
    }

    static void WireSpawner(string name, GameObject enemyAncient)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) { Debug.LogWarning("Не найден спавнер " + name + " (его делают на этапе 6)."); return; }
        WaveSpawner ws = go.GetComponent<WaveSpawner>();
        if (ws == null) { Debug.LogWarning(name + ": нет компонента WaveSpawner."); return; }
        ws.enemyAncient = enemyAncient.transform;
        ws.teamMaterial = null;     // красит TeamTint
        EditorUtility.SetDirty(ws);
    }

    static Transform EnsurePoint(GameObject points, string name, Vector3 pos)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            if (points != null) go.transform.SetParent(points.transform, false);
        }
        go.transform.position = pos;
        return go.transform;
    }

    static void PatchMinionPrefab(Material blue, Material red, GameObject coin)
    {
        string path = "Assets/Prefabs/Minion.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogWarning("Нет префаба Minion."); return; }
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        TeamTint tint = GetOrAdd<TeamTint>(root);
        tint.blueMaterial = blue; tint.redMaterial = red;
        if (root.transform.Find("Icon") == null) AddIcon(root.transform, 3f, null);
        Bounty b = root.GetComponent<Bounty>();
        if (b != null && b.dropPrefab == null && coin != null) b.dropPrefab = coin;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static GameObject MakeBotPrefab(Material beamMat, Material blue, Material red, Material iconWhite)
    {
        GameObject root = new GameObject("BotHero");
        root.AddComponent<Health>();
        root.AddComponent<Team>();
        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed = 4.5f; agent.stoppingDistance = 1f; agent.radius = 0.4f; agent.height = 1.8f;
        CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0, 0.9f, 0); cc.radius = 0.4f; cc.height = 1.8f;

        Health hp = root.GetComponent<Health>(); hp.maxHealth = 300f; hp.regenPerSecond = 2f;
        Team team = root.GetComponent<Team>(); team.kind = UnitKind.Hero; team.radius = 0.5f;

        GameObject beamGo = new GameObject("Beam");
        beamGo.transform.SetParent(root.transform, false);
        BeamEffect be = beamGo.AddComponent<BeamEffect>();
        LineRenderer lr = beamGo.GetComponent<LineRenderer>();
        lr.widthMultiplier = 0.1f; lr.useWorldSpace = true; lr.sharedMaterial = beamMat;

        UnitAI ai = root.AddComponent<UnitAI>();
        ai.aggroRange = 18f; ai.attackRange = 14f; ai.damage = 12f; ai.attackCooldown = 0.6f; ai.retreatBelow = 0.3f; ai.beam = be;

        Bounty bounty = root.AddComponent<Bounty>(); bounty.credits = 80; bounty.destroyOnDeath = false;
        Respawner rs = root.AddComponent<Respawner>(); rs.baseTime = 8f; rs.extraPerDeath = 4f;
        TeamTint tint = root.AddComponent<TeamTint>(); tint.blueMaterial = blue; tint.redMaterial = red;

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Y Bot.fbx");
        GameObject model;
        if (modelAsset != null) model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        else { model = GameObject.CreatePrimitive(PrimitiveType.Capsule); Object.DestroyImmediate(model.GetComponent<Collider>()); }
        model.name = "Model";
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = new Vector3(0, modelAsset != null ? 0 : 0.9f, 0);
        Animator anim = model.GetComponent<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            RuntimeAnimatorController ctrl = FindController("PlayerAnimator");
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
            else Debug.LogWarning("PlayerAnimator не найден: у ботов не будет анимации (назначь его на Model в префабе BotHero).");
        }

        GameObject icon = AddIcon(root.transform, 5f, iconWhite);
        rs.hideWhileDead = new[] { model, icon };

        return SavePrefab(root, "Assets/Prefabs/BotHero.prefab");
    }

    static void PlaceBot(GameObject prefab, string name, Vector3 pos, TeamId id, string lanePrefix, bool reverse, Transform goal, Transform respawn)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Object.DestroyImmediate(old);

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        go.GetComponent<Team>().id = id;

        UnitAI ai = go.GetComponent<UnitAI>();
        ai.waypoints = LanePoints(lanePrefix);
        ai.reverseRoute = reverse;
        ai.finalGoal = goal;
        ai.home = respawn;
        go.GetComponent<Respawner>().respawnPoint = respawn;
    }

    static Transform[] LanePoints(string prefix)
    {
        Transform[] pts = new Transform[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject g = GameObject.Find(prefix + (i + 1));
            if (g == null) Debug.LogWarning("Нет точки " + prefix + (i + 1));
            else pts[i] = g.transform;
        }
        return pts;
    }

    static GameObject MakeNeutralPrefab(string name, bool pit, GameObject drop, Material gold)
    {
        GameObject minionAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minion.prefab");
        if (minionAsset == null) { Debug.LogError("Нужен префаб Minion (этап 6)."); return null; }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(minionAsset);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = name;

        TeamTint tint = go.GetComponent<TeamTint>();
        if (tint != null) Object.DestroyImmediate(tint);

        Team t = go.GetComponent<Team>();
        t.id = TeamId.Neutral; t.kind = UnitKind.Neutral; t.radius = pit ? 2.5f : 0.6f;

        Health h = go.GetComponent<Health>();
        h.maxHealth = pit ? 1500f : 250f; h.regenPerSecond = 8f;

        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        agent.speed = 3f;
        if (pit) { agent.radius = 1.2f; agent.height = 4f; }

        CapsuleCollider cc = go.GetComponent<CapsuleCollider>();
        if (cc != null && pit) { cc.center = new Vector3(0, 2, 0); cc.radius = 1.2f; cc.height = 4f; }

        UnitAI ai = go.GetComponent<UnitAI>();
        ai.waypoints = new Transform[0]; ai.reverseRoute = false; ai.finalGoal = null;
        ai.aggroRange = pit ? 9f : 7f;
        ai.attackRange = pit ? 3.5f : 2f;
        ai.damage = pit ? 35f : 12f;
        ai.attackCooldown = pit ? 1.8f : 1.2f;
        ai.leashDistance = pit ? 10f : 12f;
        ai.retreatBelow = 0f;

        Bounty b = go.GetComponent<Bounty>();
        b.credits = pit ? 150 : 40; b.destroyOnDeath = true; b.grantsPitBuff = pit; b.dropPrefab = drop;

        // Масштаб модели
        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim != null) anim.transform.localScale = Vector3.one * (pit ? 2.5f : 1.3f);

        // Золотая иконка
        Transform icon = go.transform.Find("Icon");
        if (icon != null)
        {
            float size = pit ? 8f : 4f;
            icon.localScale = new Vector3(size, 0.1f, size);
            Renderer r = icon.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = gold;
        }
        else
        {
            GameObject ic = AddIcon(go.transform, pit ? 8f : 4f, gold);
        }

        return SavePrefab(go, "Assets/Prefabs/" + name + ".prefab");
    }

    static void PlaceNeutral(GameObject prefab, string name, Vector3 pos, Transform home)
    {
        if (prefab == null) return;
        GameObject old = GameObject.Find(name);
        if (old != null) Object.DestroyImmediate(old);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        go.GetComponent<UnitAI>().home = home;
    }

    static void SetupPlayer(Material iconWhite)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogWarning("В сцене нет Player: пропускаю Bounty и Respawner."); return; }

        GetOrAdd<Health>(player);
        Bounty b = GetOrAdd<Bounty>(player);
        b.credits = 0; b.destroyOnDeath = false;
        Team t = GetOrAdd<Team>(player);
        t.id = TeamId.Blue; t.kind = UnitKind.Hero;

        Respawner rs = GetOrAdd<Respawner>(player);
        rs.baseTime = 6f; rs.extraPerDeath = 4f;
        Transform br = Find("BlueRespawn");
        rs.respawnPoint = br;

        Transform model = player.transform.Find("Model");
        Transform existingIcon = player.transform.Find("Icon");
        GameObject icon = existingIcon != null ? existingIcon.gameObject : AddIcon(player.transform, 5f, iconWhite);
        rs.hideWhileDead = model != null ? new[] { model.gameObject, icon } : new[] { icon };
        EditorUtility.SetDirty(player);
    }

    static void WireAudio(GameObject player)
    {
        AudioSource src = GetOrAdd<AudioSource>(player);
        src.playOnAwake = false; src.spatialBlend = 0f;

        PlayerShooting ps = player.GetComponent<PlayerShooting>();
        if (ps != null)
        {
            ps.audioSource = src;
            ps.shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/427396__erokia__plasma-lazer-pistol-gun-shot-1.wav");
            ps.reloadClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/688866__dacmod1__reload.wav");
            ps.hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/749788__rescopicsound__cinematic-energy-impact-grainy-voltage.mp3");
            EditorUtility.SetDirty(ps);
        }

        GameObject music = GameObject.Find("Music");
        if (music == null) music = new GameObject("Music");
        AudioSource ms = GetOrAdd<AudioSource>(music);
        ms.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cyberpunk Moonlight Sonata.mp3");
        ms.loop = true; ms.playOnAwake = true; ms.volume = 0.3f; ms.spatialBlend = 0f;
    }

    // =====================================================================
    //  Мини-карта
    // =====================================================================
    static void BuildMinimap(Transform canvas)
    {
        string rtPath = "Assets/UI/MinimapTexture.renderTexture";
        System.IO.Directory.CreateDirectory("Assets/UI");
        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (rt == null)
        {
            rt = new RenderTexture(512, 512, 16);
            AssetDatabase.CreateAsset(rt, rtPath);
        }

        GameObject camGo = GameObject.Find("MinimapCamera");
        if (camGo == null) camGo = new GameObject("MinimapCamera", typeof(Camera));
        camGo.transform.position = new Vector3(0, 120, 0);
        camGo.transform.rotation = Quaternion.Euler(90, 0, 0);
        Camera cam = camGo.GetComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 108f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
        cam.targetTexture = rt;
        cam.cullingMask = 1 << MinimapLayer;        // только иконки
        AudioListener al = camGo.GetComponent<AudioListener>();
        if (al != null) Object.DestroyImmediate(al);
        UniversalAdditionalCameraData data = camGo.GetComponent<UniversalAdditionalCameraData>();
        if (data == null) data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        data.antialiasing = AntialiasingMode.None;

        Camera main = Camera.main;
        if (main != null) main.cullingMask &= ~(1 << MinimapLayer);

        GameObject raw = new GameObject("Minimap", typeof(RectTransform), typeof(RawImage));
        raw.transform.SetParent(canvas, false);
        RectTransform rtr = raw.GetComponent<RectTransform>();
        rtr.anchorMin = rtr.anchorMax = new Vector2(1, 0);
        rtr.pivot = new Vector2(1, 0);
        rtr.anchoredPosition = new Vector2(-30, 30);
        rtr.sizeDelta = new Vector2(280, 280);
        raw.GetComponent<RawImage>().texture = rt;
    }

    static GameObject AddIcon(Transform parent, float size, Material mat)
    {
        GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        icon.name = "Icon";
        Object.DestroyImmediate(icon.GetComponent<Collider>());
        icon.layer = MinimapLayer;
        icon.transform.SetParent(parent, false);

        Vector3 ls = parent.lossyScale;
        icon.transform.localScale = new Vector3(size / ls.x, 0.1f / ls.y, size / ls.z);
        icon.transform.position = new Vector3(parent.position.x, 60f, parent.position.z);
        if (mat != null) icon.GetComponent<Renderer>().sharedMaterial = mat;
        return icon;
    }

    static void EnsureMinimapLayer()
    {
        SerializedObject tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tm.FindProperty("layers");
        SerializedProperty slot = layers.GetArrayElementAtIndex(MinimapLayer);
        if (string.IsNullOrEmpty(slot.stringValue))
        {
            slot.stringValue = "Minimap";
            tm.ApplyModifiedProperties();
        }
    }

    // =====================================================================
    //  Помощники интерфейса
    // =====================================================================
    struct A
    {
        public static readonly Vector2 TopC = new Vector2(0.5f, 1f);
        public static readonly Vector2 Top = new Vector2(0.5f, 1f);
        public static readonly Vector2 BotC = new Vector2(0.5f, 0f);
        public static readonly Vector2 Bot = new Vector2(0.5f, 0f);
        public static readonly Vector2 BotL = new Vector2(0f, 0f);
        public static readonly Vector2 BotR = new Vector2(1f, 0f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    }

    static void ConfigureCanvas(GameObject go)
    {
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static RectTransform Rect(GameObject go, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x, anchor.y == 0.5f ? 0.5f : anchor.y);
        if (anchor.x == 0.5f) rt.pivot = new Vector2(0.5f, rt.pivot.y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static TMP_Text Text(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Rect(go, parent, anchor, pos, size);
        // Текст якорится по центру своего прямоугольника, а позиция считается от якоря
        RectTransform rt = go.GetComponent<RectTransform>();
        if (anchor.y == 1f) rt.pivot = new Vector2(rt.pivot.x, 1f);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fontSize; t.alignment = align;
        t.color = new Color(0.9f, 0.95f, 1f);
        t.raycastTarget = false;
        return t;
    }

    static Slider SliderUI(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fill)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        Rect(go, parent, anchor, pos, size);

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.85f);

        GameObject area = new GameObject("Fill Area", typeof(RectTransform));
        area.transform.SetParent(go.transform, false);
        Stretch(area.GetComponent<RectTransform>());

        GameObject f = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        f.transform.SetParent(area.transform, false);
        RectTransform fr = f.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        f.GetComponent<Image>().color = fill;

        Slider s = go.GetComponent<Slider>();
        s.fillRect = fr;
        s.targetGraphic = f.GetComponent<Image>();
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0; s.maxValue = 1; s.value = 1;
        s.interactable = false;
        s.transition = Selectable.Transition.None;
        return s;
    }

    static Button ButtonUI(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Rect(go, parent, anchor, pos, size);
        // Кнопки в таблице координат задаются от центра: центрируем pivot
        go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        if (anchor.y == 1f) go.GetComponent<RectTransform>().anchoredPosition = pos;
        Image img = go.GetComponent<Image>();
        img.color = new Color(0.08f, 0.2f, 0.28f, 0.95f);
        Button b = go.GetComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(0.3f, 0.9f, 1f);
        cb.pressedColor = new Color(0.1f, 0.5f, 0.6f);
        b.colors = cb;

        GameObject tgo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        tgo.transform.SetParent(go.transform, false);
        Stretch(tgo.GetComponent<RectTransform>());
        TextMeshProUGUI t = tgo.GetComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 28; t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white; t.raycastTarget = false;
        return b;
    }

    static GameObject Panel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = color;
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void MakeCrosshair(Transform canvas)
    {
        GameObject go = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
        Rect(go, canvas, A.Center, Vector2.zero, new Vector2(40, 40));
        go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;

        string[] guids = AssetDatabase.FindAssets("Crosshair t:Sprite");
        if (guids.Length > 0)
        {
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            img.color = Color.white;
        }
        else
        {
            // PNG прицела ещё нет в проекте: рисуем плюс из двух полосок (потом замени на свой PNG из Sources/Crosshair.xcf)
            img.color = new Color(0, 0, 0, 0);
            MakeBar(go.transform, new Vector2(40, 3));
            MakeBar(go.transform, new Vector2(3, 40));
        }
    }

    static void MakeBar(Transform parent, Vector2 size)
    {
        GameObject bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        Image im = bar.GetComponent<Image>();
        im.color = new Color(0.9f, 1f, 1f, 0.9f); im.raycastTarget = false;
    }

    // =====================================================================
    //  Общие помощники
    // =====================================================================
    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static Transform Find(string name)
    {
        GameObject g = GameObject.Find(name);
        if (g == null) Debug.LogWarning("Не найден объект " + name);
        return g != null ? g.transform : null;
    }

    static GameObject SavePrefab(GameObject sceneObject, string path)
    {
        System.IO.Directory.CreateDirectory("Assets/Prefabs");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sceneObject, path);
        Object.DestroyImmediate(sceneObject);
        return prefab;
    }

    static RuntimeAnimatorController FindController(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:AnimatorController"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(p);
        }
        return null;
    }

    static GameObject MakePickup(string name, PrimitiveType shape, PickupType type, int value, Material mat, Vector3 scale, string clipPath)
    {
        string path = "Assets/Prefabs/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = name;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Collider col = go.GetComponent<Collider>();
        Object.DestroyImmediate(col);
        SphereCollider sc = go.AddComponent<SphereCollider>();
        sc.isTrigger = true; sc.radius = 1.5f / Mathf.Max(scale.x, 0.01f);
        Pickup p = go.AddComponent<Pickup>();
        p.type = type; p.value = value;
        p.pickupSound = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        return SavePrefab(go, path);
    }

    static Material Mat(string name, Color baseColor, Color emission, float intensity)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        System.IO.Directory.CreateDirectory("Assets/Materials");
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", baseColor);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission * intensity);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Material UnlitMat(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        System.IO.Directory.CreateDirectory("Assets/Materials");
        m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
