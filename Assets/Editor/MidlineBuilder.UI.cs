// Интерфейс: HUD, пауза, магазин, экран конца матча, главное меню. Плюс общие помощники сборщика.
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static partial class MidlineBuilder
{
    // Якоря экрана
    static readonly Vector2 TL = new Vector2(0f, 1f), TC = new Vector2(0.5f, 1f), TR = new Vector2(1f, 1f);
    static readonly Vector2 ML = new Vector2(0f, 0.5f), C = new Vector2(0.5f, 0.5f), MR = new Vector2(1f, 0.5f);
    static readonly Vector2 BL = new Vector2(0f, 0f), BC = new Vector2(0.5f, 0f), BR = new Vector2(1f, 0f);

    // =====================================================================
    //  HUD сцены Game
    // =====================================================================
    static void BuildHUD(GameManager gm, UIManager ui, GameObject player, Health redAncient)
    {
        Transform c = MakeCanvas("HUD").transform;

        // Красная виньетка урона (под всем остальным)
        Image vignette = Img(c, "DamageVignette", new Color(1f, 0f, 0f, 0f), C, C, Vector2.zero, Vector2.zero);
        Stretch(vignette.rectTransform);

        // ----- Прицел -----
        Transform cross = UIObj("Crosshair", c).transform;
        Place(cross.gameObject, C, C, Vector2.zero, new Vector2(80f, 80f));
        RectTransform[] lines = new RectTransform[4];
        Vector2[] sizes = { new Vector2(2.5f, 11f), new Vector2(2.5f, 11f), new Vector2(11f, 2.5f), new Vector2(11f, 2.5f) };
        for (int i = 0; i < 4; i++)
            lines[i] = Img(cross, "Line" + i, new Color(0.85f, 1f, 1f, 0.95f), C, C, Vector2.zero, sizes[i]).rectTransform;
        Img(cross, "Dot", new Color(0.85f, 1f, 1f, 0.9f), C, C, Vector2.zero, new Vector2(3f, 3f));
        TextMeshProUGUI hit = Txt(cross, "Hitmarker", "×", 56, TextAlignmentOptions.Center, C, C, new Vector2(0f, 3f), new Vector2(80f, 80f), Color.white, FontStyles.Bold);

        // ----- Верх: время, счёт, ближайшая башня -----
        Image timeBg = Img(c, "TimeBg", UiBg, TC, TC, new Vector2(0f, -12f), new Vector2(150f, 42f));
        TextMeshProUGUI time = Txt(timeBg.transform, "TimeText", "00:00", 28, TextAlignmentOptions.Center, C, C, Vector2.zero, new Vector2(150f, 42f), Gold, FontStyles.Bold);
        Image scoreBg = Img(c, "ScoreBg", UiBg, TC, TC, new Vector2(0f, -58f), new Vector2(720f, 78f));
        Accent(scoreBg.transform, Cyan, true);
        TextMeshProUGUI score = Txt(scoreBg.transform, "ScoreText", "", 30, TextAlignmentOptions.Center, C, C, Vector2.zero, new Vector2(700f, 78f), UiText, FontStyles.Bold);
        TextMeshProUGUI near = Txt(c, "NearTowerText", "", 22, TextAlignmentOptions.Center, TC, TC, new Vector2(0f, -142f), new Vector2(700f, 30f), UiText, FontStyles.Bold);
        RectTransform nearFill = Bar(c, "NearTowerBar", TC, TC, new Vector2(0f, -174f), new Vector2(420f, 12f), new Color(1f, 0.45f, 0.1f));

        // Лента событий справа сверху
        TextMeshProUGUI feed = Txt(c, "FeedText", "", 24, TextAlignmentOptions.TopRight, TR, TR, new Vector2(-28f, -24f), new Vector2(620f, 220f), UiText);

        // Подсказка "Esc — пауза"
        Txt(c, "KeysHint", "Esc — пауза   E — терминал   Q — взрыв   Shift — рывок", 18, TextAlignmentOptions.TopLeft, TL, TL, new Vector2(24f, -20f), new Vector2(700f, 30f), UiDim);

        // Крупные сообщения и подсказки взаимодействия
        TextMeshProUGUI message = Txt(c, "MessageText", "", 34, TextAlignmentOptions.Center, TC, TC, new Vector2(0f, -220f), new Vector2(1500f, 60f), Color.white, FontStyles.Bold);
        message.outlineWidth = 0.2f; message.outlineColor = new Color32(0, 0, 0, 220);
        TextMeshProUGUI prompt = Txt(c, "PromptText", "", 30, TextAlignmentOptions.Center, C, C, new Vector2(0f, -150f), new Vector2(1000f, 56f), Color.white, FontStyles.Bold);
        prompt.outlineWidth = 0.2f; prompt.outlineColor = new Color32(0, 0, 0, 220);

        // ----- Низ по центру: здоровье, энергия, способности -----
        Image vitals = Img(c, "Vitals", UiBg, BC, BC, new Vector2(0f, 22f), new Vector2(780f, 118f));
        Accent(vitals.transform, Cyan, true);
        Txt(vitals.transform, "HeroName", GameSettingsHeroLabel(), 18, TextAlignmentOptions.Center, TC, TC, new Vector2(0f, -8f), new Vector2(400f, 24f), UiDim, FontStyles.Bold);
        RectTransform hpFill = Bar(vitals.transform, "HealthBar", C, C, new Vector2(0f, 6f), new Vector2(520f, 32f), new Color(0.25f, 0.95f, 0.45f));
        TextMeshProUGUI hpText = Txt(vitals.transform, "HealthText", "", 22, TextAlignmentOptions.Center, C, C, new Vector2(0f, 6f), new Vector2(520f, 32f), Color.white, FontStyles.Bold);
        RectTransform enFill = Bar(vitals.transform, "EnergyBar", C, C, new Vector2(0f, -28f), new Vector2(520f, 16f), new Color(0.25f, 0.55f, 1f));
        TextMeshProUGUI enText = Txt(vitals.transform, "EnergyText", "", 13, TextAlignmentOptions.Center, C, C, new Vector2(0f, -28f), new Vector2(520f, 16f), Color.white, FontStyles.Bold);

        TextMeshProUGUI dashText, abilText;
        RectTransform dashCd = AbilitySlot(vitals.transform, "DashSlot", new Vector2(-330f, 0f), "РЫВОК", "SHIFT", Cyan, out dashText);
        RectTransform abilCd = AbilitySlot(vitals.transform, "AbilitySlot", new Vector2(330f, 0f), "ВЗРЫВ", "Q", new Color(0.4f, 0.7f, 1f), out abilText);

        // ----- Низ слева: кредиты и эффекты -----
        Image creditsBg = Img(c, "CreditsBg", UiBg, BL, BL, new Vector2(24f, 22f), new Vector2(230f, 96f));
        Accent(creditsBg.transform, Gold, false);
        TextMeshProUGUI credits = Txt(creditsBg.transform, "CreditsText", "", 46, TextAlignmentOptions.Left, C, C, new Vector2(10f, 0f), new Vector2(190f, 90f), UiText, FontStyles.Bold);
        TextMeshProUGUI buffs = Txt(c, "BuffsText", "", 22, TextAlignmentOptions.BottomLeft, BL, BL, new Vector2(26f, 130f), new Vector2(620f, 160f), UiText, FontStyles.Bold);

        // ----- Низ справа: мини-карта и патроны -----
        Image mapFrame = Img(c, "MinimapFrame", new Color(0f, 0.9f, 1f, 0.6f), BR, BR, new Vector2(-22f, 20f), new Vector2(176f, 346f));
        GameObject raw = UIObj("Minimap", mapFrame.transform);
        raw.AddComponent<RawImage>().texture = minimapTexture;
        Stretch(raw.GetComponent<RectTransform>(), 3f);
        Txt(c, "MinimapLabel", "КАРТА", 16, TextAlignmentOptions.Center, BR, BR, new Vector2(-22f, 368f), new Vector2(176f, 22f), UiDim, FontStyles.Bold);
        TextMeshProUGUI ammo = Txt(c, "AmmoText", "", 56, TextAlignmentOptions.BottomRight, BR, BR, new Vector2(-214f, 22f), new Vector2(320f, 120f), Color.white, FontStyles.Bold);

        // ----- Экран смерти -----
        Image respawnPanel = Img(c, "RespawnPanel", new Color(0.05f, 0f, 0.02f, 0.55f), C, C, Vector2.zero, Vector2.zero);
        Stretch(respawnPanel.rectTransform);
        TextMeshProUGUI respawn = Txt(respawnPanel.transform, "RespawnText", "", 86, TextAlignmentOptions.Center, C, C, new Vector2(0f, 60f), new Vector2(1200f, 260f), new Color(1f, 0.35f, 0.3f), FontStyles.Bold);
        respawnPanel.gameObject.SetActive(false);

        // ----- Панели -----
        GameObject pause = BuildPausePanel(c, gm);
        GameObject end = BuildEndPanel(c, gm, out TextMeshProUGUI endTitle, out TextMeshProUGUI endStats);
        GameObject shop = BuildShopPanel(c, player);

        // ----- Подключаем ссылки -----
        gm.pausePanel = pause; gm.shopPanel = shop; gm.endPanel = end;
        gm.endTitleText = endTitle; gm.endStatsText = endStats;
        gm.promptText = prompt; gm.messageText = message; gm.feedText = feed;

        ui.playerHealth = player.GetComponent<Health>();
        ui.player = player.GetComponent<PlayerController>();
        ui.shooting = player.GetComponent<PlayerShooting>();
        ui.abilities = player.GetComponent<PlayerAbilities>();
        ui.playerRespawn = player.GetComponent<Respawner>();
        ui.redAncient = redAncient;
        ui.healthFill = hpFill; ui.healthText = hpText; ui.energyFill = enFill; ui.energyText = enText;
        ui.ammoText = ammo; ui.creditsText = credits; ui.buffsText = buffs;
        ui.abilityCooldown = abilCd; ui.abilityText = abilText; ui.dashCooldown = dashCd; ui.dashText = dashText;
        ui.scoreText = score; ui.timeText = time; ui.nearTowerText = near; ui.nearTowerFill = nearFill;
        ui.crosshairLines = lines; ui.hitmarker = hit;
        ui.respawnPanel = respawnPanel.gameObject; ui.respawnText = respawn;
        ui.damageVignette = vignette;

        pause.SetActive(false); end.SetActive(false); shop.SetActive(false);
        prompt.gameObject.SetActive(false); message.gameObject.SetActive(false);
    }

    static string GameSettingsHeroLabel() { return "ЗДОРОВЬЕ  /  ЭНЕРГИЯ"; }

    // Ячейка способности: подпись сверху, клавиша снизу, затемнение-перезарядка поверх
    static RectTransform AbilitySlot(Transform parent, string name, Vector2 pos, string title, string key, Color color, out TextMeshProUGUI centerText)
    {
        Image bg = Img(parent, name, new Color(0.06f, 0.1f, 0.16f, 1f), C, C, pos, new Vector2(96f, 96f));
        Image icon = Img(bg.transform, "Glow", new Color(color.r, color.g, color.b, 0.25f), C, C, Vector2.zero, new Vector2(80f, 80f));
        Img(bg.transform, "Border", color, TC, TC, Vector2.zero, new Vector2(96f, 3f));
        Txt(bg.transform, "Title", title, 15, TextAlignmentOptions.Center, TC, TC, new Vector2(0f, -8f), new Vector2(96f, 20f), color, FontStyles.Bold);
        Txt(bg.transform, "Key", key, 16, TextAlignmentOptions.Center, BC, BC, new Vector2(0f, 6f), new Vector2(96f, 20f), UiText, FontStyles.Bold);
        Image overlay = Img(bg.transform, "Cooldown", new Color(0f, 0f, 0f, 0.7f), C, C, Vector2.zero, Vector2.zero);
        RectTransform ort = overlay.rectTransform;
        ort.anchorMin = new Vector2(0f, 1f); ort.anchorMax = Vector2.one; ort.offsetMin = ort.offsetMax = Vector2.zero;
        centerText = Txt(bg.transform, "Value", "", 34, TextAlignmentOptions.Center, C, C, new Vector2(0f, 2f), new Vector2(96f, 60f), Color.white, FontStyles.Bold);
        return ort;
    }

    static GameObject BuildPausePanel(Transform c, GameManager gm)
    {
        Image dim = Img(c, "PausePanel", new Color(0f, 0f, 0.02f, 0.72f), C, C, Vector2.zero, Vector2.zero);
        Stretch(dim.rectTransform);
        Image box = Img(dim.transform, "Box", new Color(0.03f, 0.05f, 0.09f, 0.96f), C, C, Vector2.zero, new Vector2(1100f, 640f));
        Accent(box.transform, Cyan, true);
        Txt(box.transform, "Title", "ПАУЗА", 76, TextAlignmentOptions.Left, TL, TL, new Vector2(60f, -40f), new Vector2(500f, 100f), Cyan, FontStyles.Bold);

        Button cont = Btn(box.transform, "ContinueButton", "ПРОДОЛЖИТЬ", TL, TL, new Vector2(60f, -180f), new Vector2(400f, 66f));
        Button rest = Btn(box.transform, "RestartButton", "ЗАНОВО", TL, TL, new Vector2(60f, -266f), new Vector2(400f, 66f));
        Button menu = Btn(box.transform, "MenuButton", "В ГЛАВНОЕ МЕНЮ", TL, TL, new Vector2(60f, -352f), new Vector2(400f, 66f));
        UnityEventTools.AddPersistentListener(cont.onClick, new UnityAction(gm.TogglePause));
        UnityEventTools.AddPersistentListener(rest.onClick, new UnityAction(gm.Restart));
        UnityEventTools.AddPersistentListener(menu.onClick, new UnityAction(gm.ToMenu));

        GameObject settings = BuildSettingsGroup(box.transform, new Vector2(540f, -60f));
        Txt(box.transform, "Controls", ControlsText(), 22, TextAlignmentOptions.TopLeft, TL, TL, new Vector2(540f, -300f), new Vector2(520f, 320f), UiText);
        return dim.gameObject;
    }

    static GameObject BuildEndPanel(Transform c, GameManager gm, out TextMeshProUGUI title, out TextMeshProUGUI stats)
    {
        Image dim = Img(c, "EndPanel", new Color(0f, 0.01f, 0.03f, 0.94f), C, C, Vector2.zero, Vector2.zero);
        Stretch(dim.rectTransform);
        title = Txt(dim.transform, "EndTitle", "ПОБЕДА", 160, TextAlignmentOptions.Center, C, C, new Vector2(0f, 230f), new Vector2(1600f, 200f), Cyan, FontStyles.Bold);
        Image statsBg = Img(dim.transform, "StatsBg", UiBg, C, C, new Vector2(0f, -10f), new Vector2(700f, 250f));
        Accent(statsBg.transform, Cyan, true);
        stats = Txt(statsBg.transform, "EndStats", "", 30, TextAlignmentOptions.Center, C, C, Vector2.zero, new Vector2(660f, 240f), UiText);
        Button rest = Btn(dim.transform, "RestartButton", "ЕЩЁ РАЗ", C, C, new Vector2(-220f, -230f), new Vector2(400f, 70f));
        Button menu = Btn(dim.transform, "MenuButton", "В ГЛАВНОЕ МЕНЮ", C, C, new Vector2(220f, -230f), new Vector2(400f, 70f));
        UnityEventTools.AddPersistentListener(rest.onClick, new UnityAction(gm.Restart));
        UnityEventTools.AddPersistentListener(menu.onClick, new UnityAction(gm.ToMenu));
        return dim.gameObject;
    }

    static GameObject BuildShopPanel(Transform c, GameObject player)
    {
        Image panel = Img(c, "ShopPanel", new Color(0.02f, 0.04f, 0.08f, 0.9f), ML, ML, new Vector2(40f, 20f), new Vector2(1160f, 640f));
        Accent(panel.transform, Gold, true);
        Transform p = panel.transform;
        ShopPanel shop = panel.gameObject.AddComponent<ShopPanel>();

        Txt(p, "Title", "МАГАЗИН", 46, TextAlignmentOptions.Left, TL, TL, new Vector2(30f, -20f), new Vector2(400f, 60f), Gold, FontStyles.Bold);
        TextMeshProUGUI credits = Txt(p, "Credits", "", 32, TextAlignmentOptions.Right, TR, TR, new Vector2(-30f, -26f), new Vector2(400f, 50f), UiText, FontStyles.Bold);
        Txt(p, "Hint", "Время в магазине идёт!   E / Esc — закрыть", 20, TextAlignmentOptions.Left, BL, BL, new Vector2(30f, 22f), new Vector2(600f, 30f), UiDim);
        TextMeshProUGUI msg = Txt(p, "Message", "", 24, TextAlignmentOptions.Center, BC, BC, new Vector2(80f, 60f), new Vector2(600f, 34f), Gold, FontStyles.Bold);

        // Категории
        Txt(p, "CatHead", "КАТЕГОРИИ", 18, TextAlignmentOptions.Left, TL, TL, new Vector2(30f, -92f), new Vector2(240f, 24f), UiDim, FontStyles.Bold);
        Button[] cats = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            cats[i] = Btn(p, "Category" + i, ShopPanel.Categories[i], TL, TL, new Vector2(30f, -124f - i * 80f), new Vector2(240f, 66f), 26);
            UnityEventTools.AddIntPersistentListener(cats[i].onClick, new UnityAction<int>(shop.SelectCategory), i);
        }

        // Карточки
        Image[] bgs = new Image[2];
        TextMeshProUGUI[] titles = new TextMeshProUGUI[2], descs = new TextMeshProUGUI[2], costs = new TextMeshProUGUI[2], labels = new TextMeshProUGUI[2];
        Button[] buys = new Button[2];
        for (int s = 0; s < 2; s++)
        {
            bgs[s] = Img(p, "Card" + s, new Color(0.07f, 0.1f, 0.16f, 0.95f), TL, TL, new Vector2(300f + s * 300f, -92f), new Vector2(280f, 440f));
            Transform card = bgs[s].transform;
            Img(card, "Top", Gold, TC, TC, Vector2.zero, new Vector2(280f, 3f));
            titles[s] = Txt(card, "Name", "", 28, TextAlignmentOptions.Center, TC, TC, new Vector2(0f, -30f), new Vector2(250f, 80f), Color.white, FontStyles.Bold);
            descs[s] = Txt(card, "Desc", "", 22, TextAlignmentOptions.Center, C, C, new Vector2(0f, 30f), new Vector2(240f, 120f), UiText);
            costs[s] = Txt(card, "Cost", "", 36, TextAlignmentOptions.Center, C, C, new Vector2(0f, -70f), new Vector2(240f, 50f), Gold, FontStyles.Bold);
            buys[s] = Btn(card, "Buy", "КУПИТЬ", BC, BC, new Vector2(0f, 22f), new Vector2(230f, 60f), 26);
            labels[s] = buys[s].GetComponentInChildren<TextMeshProUGUI>();
            UnityEventTools.AddIntPersistentListener(buys[s].onClick, new UnityAction<int>(shop.BuyCard), s);
        }

        // Сборка
        Img(p, "BuildBg", new Color(0.05f, 0.08f, 0.13f, 0.9f), TL, TL, new Vector2(900f, -92f), new Vector2(230f, 440f));
        Txt(p, "BuildHead", "СБОРКА", 24, TextAlignmentOptions.Left, TL, TL, new Vector2(916f, -104f), new Vector2(200f, 30f), Cyan, FontStyles.Bold);
        TextMeshProUGUI build = Txt(p, "Build", "", 21, TextAlignmentOptions.TopLeft, TL, TL, new Vector2(916f, -146f), new Vector2(204f, 370f), UiText);

        Button close = Btn(p, "CloseButton", "ЗАКРЫТЬ", BR, BR, new Vector2(-30f, 18f), new Vector2(200f, 50f), 24);
        UnityEventTools.AddPersistentListener(close.onClick, new UnityAction(shop.Close));

        shop.shooting = player.GetComponent<PlayerShooting>();
        shop.playerHealth = player.GetComponent<Health>();
        shop.abilities = player.GetComponent<PlayerAbilities>();
        shop.categoryButtons = cats; shop.cardBackgrounds = bgs; shop.cardTitles = titles; shop.cardDescriptions = descs;
        shop.cardCosts = costs; shop.cardButtons = buys; shop.cardButtonLabels = labels;
        shop.buildText = build; shop.creditsText = credits; shop.messageText = msg;
        return panel.gameObject;
    }

    // Слайдеры чувствительности и громкости (общие для меню и паузы)
    static GameObject BuildSettingsGroup(Transform parent, Vector2 pos)
    {
        GameObject g = UIObj("Settings", parent);
        Place(g, TL, TL, pos, new Vector2(520f, 220f));
        Txt(g.transform, "Head", "НАСТРОЙКИ", 28, TextAlignmentOptions.Left, TL, TL, Vector2.zero, new Vector2(400f, 40f), Cyan, FontStyles.Bold);
        Txt(g.transform, "SensLabel", "Чувствительность мыши", 22, TextAlignmentOptions.Left, TL, TL, new Vector2(0f, -56f), new Vector2(360f, 30f), UiText);
        Slider sens = MakeSlider(g.transform, "Sensitivity", TL, TL, new Vector2(0f, -92f), new Vector2(400f, 22f), 0.3f, 6f);
        TextMeshProUGUI sensVal = Txt(g.transform, "SensValue", "", 22, TextAlignmentOptions.Left, TL, TL, new Vector2(420f, -88f), new Vector2(100f, 30f), Gold, FontStyles.Bold);
        Txt(g.transform, "VolLabel", "Громкость", 22, TextAlignmentOptions.Left, TL, TL, new Vector2(0f, -132f), new Vector2(360f, 30f), UiText);
        Slider vol = MakeSlider(g.transform, "Volume", TL, TL, new Vector2(0f, -168f), new Vector2(400f, 22f), 0f, 1f);
        TextMeshProUGUI volVal = Txt(g.transform, "VolValue", "", 22, TextAlignmentOptions.Left, TL, TL, new Vector2(420f, -164f), new Vector2(100f, 30f), Gold, FontStyles.Bold);
        SettingsPanel sp = g.AddComponent<SettingsPanel>();
        sp.sensitivitySlider = sens; sp.volumeSlider = vol; sp.sensitivityValue = sensVal; sp.volumeValue = volVal;
        return g;
    }

    static string ControlsText()
    {
        return "<color=#00E5FF><b>УПРАВЛЕНИЕ</b></color>\n" +
               "WASD — бег        Мышь — прицел\n" +
               "ЛКМ — выстрел     R — перезарядка\n" +
               "Space — прыжок    Shift — рывок\n" +
               "Q — энергетический взрыв\n" +
               "E — магазин / лечение (у терминала)\n" +
               "Esc — пауза";
    }

    // =====================================================================
    //  Сцена MainMenu
    // =====================================================================
    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupLighting(true);
        deco = new GameObject("Decor").transform;

        // Подиум с героем
        Prim(PrimitiveType.Cylinder, "Podium", deco, new Vector3(0f, 0.15f, 0f), new Vector3(5f, 0.15f, 5f), M["WallDark"], true);
        Prim(PrimitiveType.Cylinder, "PodiumRim", deco, new Vector3(0f, 0.2f, 0f), new Vector3(5.12f, 0.05f, 5.12f), M["Blue"], false);
        Prim(PrimitiveType.Cylinder, "Floor", deco, new Vector3(0f, -0.05f, 0f), new Vector3(80f, 0.05f, 80f), M["Ground"], true);
        GameObject hero = new GameObject("Hero");
        hero.transform.position = new Vector3(0f, 0.3f, 0f);
        GameObject model = AddModel(hero.transform, "Assets/Models/Y Bot.fbx", 1.8f, "Assets/Animations/PlayerAnimator.controller");
        foreach (SkinnedMeshRenderer r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            r.sharedMaterials = Fill(r.sharedMaterials.Length, r.name.Contains("Joint") ? M["Blue"] : M["HeroBody"]);
        AddRifle(model);

        // Враги на заднем плане
        GameObject enemy = new GameObject("EnemyHero");
        enemy.transform.SetPositionAndRotation(new Vector3(-5f, 0f, 7f), Quaternion.Euler(0f, 160f, 0f));
        GameObject em = AddModel(enemy.transform, "Assets/Models/Y Bot.fbx", 1.8f, "Assets/Animations/PlayerAnimator.controller");
        foreach (SkinnedMeshRenderer r in em.GetComponentsInChildren<SkinnedMeshRenderer>())
            r.sharedMaterials = Fill(r.sharedMaterials.Length, r.name.Contains("Joint") ? M["Red"] : M["HeroBody"]);
        AddRifle(em);
        for (int i = 0; i < 3; i++)
        {
            GameObject mn = new GameObject("Minion" + i);
            mn.transform.SetPositionAndRotation(new Vector3(4f + i * 1.6f, 0f, 6f + i), Quaternion.Euler(0f, 200f, 0f));
            GameObject mm = AddModel(mn.transform, "Assets/Models/Mutant.fbx", 1.5f, "Assets/Animations/MinionAnimator.controller");
            foreach (Renderer r in mm.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Fill(r.sharedMaterials.Length, M["MinionRed"]);
        }

        // Город вокруг
        Material[] bmats = { BuildingMat("BuildingA", new Vector2(2f, 4f)), BuildingMat("BuildingB", new Vector2(3f, 7f)), BuildingMat("BuildingC", new Vector2(2f, 9f)) };
        Material[] signs = { M["NeonPink"], M["NeonViolet"], M["Blue"], M["NeonGold"], M["Red"] };
        Random.InitState(7);
        for (int i = 0; i < 22; i++)
        {
            float a = i / 22f * Mathf.PI * 2f;
            float rad = Random.Range(22f, 34f);
            Vector3 pos = new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad);
            int fx = Mathf.Abs(pos.x) > Mathf.Abs(pos.z) ? (pos.x > 0 ? -1 : 1) : 0;
            int fz = fx == 0 ? (pos.z > 0 ? -1 : 1) : 0;
            Skyscraper(pos, new Vector3(Random.Range(8f, 13f), Random.Range(18f, 48f), Random.Range(8f, 13f)), bmats[Random.Range(0, 3)], signs, fx, fz);
        }
        PointLight(deco, new Vector3(-3f, 3f, -2f), Cyan, 9f, 4f);
        PointLight(deco, new Vector3(3f, 2.5f, 2f), Orange, 9f, 3f);
        PointLight(deco, new Vector3(0f, 6f, 0f), new Color(0.7f, 0.8f, 1f), 10f, 2f);

        // Камера облетает героя
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 50f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.025f, 0.05f);
        camGo.AddComponent<AudioListener>();
        UniversalAdditionalCameraData data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        MenuOrbit orbit = camGo.AddComponent<MenuOrbit>();
        orbit.target = hero.transform;
        camGo.transform.position = new Vector3(0f, 2f, -6f);
        MakeMusic(0.35f);

        // ----- Интерфейс меню -----
        Transform c = MakeCanvas("MenuCanvas").transform;
        Image shade = Img(c, "LeftShade", new Color(0.01f, 0.015f, 0.03f, 0.75f), ML, ML, Vector2.zero, new Vector2(760f, 0f));
        RectTransform srt = shade.rectTransform; srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(0f, 1f); srt.sizeDelta = new Vector2(760f, 0f);

        Txt(c, "Title", "MIDLINE", 150, TextAlignmentOptions.Left, TL, TL, new Vector2(84f, -70f), new Vector2(900f, 170f), Cyan, FontStyles.Bold);
        Txt(c, "Subtitle", "NEON CITY  MOBA", 38, TextAlignmentOptions.Left, TL, TL, new Vector2(92f, -232f), new Vector2(800f, 50f), Orange, FontStyles.Bold);
        Txt(c, "Tagline", "Шутер от третьего лица с линиями, башнями и магазином. Матч 2 на 2 с ботами.", 22, TextAlignmentOptions.Left, TL, TL, new Vector2(92f, -290f), new Vector2(620f, 70f), UiText);
        Txt(c, "Footer", "Учебный проект  ·  Жаксыбаев Алискер, Вагнер Альберт  ·  Unity 6", 18, TextAlignmentOptions.Left, BL, BL, new Vector2(92f, 26f), new Vector2(900f, 30f), UiDim);

        GameObject mm2 = new GameObject("MenuManager");
        MainMenu menu = mm2.AddComponent<MainMenu>();

        // Главная панель с кнопками
        GameObject main = UIObj("MainPanel", c);
        Stretch(main.GetComponent<RectTransform>());
        string[] labels = { "ИГРАТЬ", "КАК ИГРАТЬ", "НАСТРОЙКИ", "ТИТРЫ", "ВЫХОД" };
        Button[] buttons = new Button[labels.Length];
        for (int i = 0; i < labels.Length; i++)
            buttons[i] = Btn(main.transform, labels[i], labels[i], TL, TL, new Vector2(92f, -400f - i * 84f), new Vector2(440f, 68f), 30);

        GameObject hero2 = BuildHeroPanel(c, menu, main);
        GameObject how = BuildInfoPanel(c, "HowToPanel", "КАК ИГРАТЬ", HowToText(), ControlsText(), menu, main);
        GameObject settings = BuildBox(c, "SettingsPanel", "НАСТРОЙКИ", new Vector2(900f, 420f), menu, main, out Transform sbox);
        BuildSettingsGroup(sbox, new Vector2(60f, -130f)).transform.Find("Head").gameObject.SetActive(false);
        GameObject credits = BuildInfoPanel(c, "CreditsPanel", "ТИТРЫ", CreditsLeft(), CreditsRight(), menu, main);

        menu.mainPanel = main;
        menu.panels = new[] { main, hero2, how, settings, credits };
        UnityEventTools.AddObjectPersistentListener<GameObject>(buttons[0].onClick, new UnityAction<GameObject>(menu.OpenPanel), hero2);
        UnityEventTools.AddObjectPersistentListener<GameObject>(buttons[1].onClick, new UnityAction<GameObject>(menu.OpenPanel), how);
        UnityEventTools.AddObjectPersistentListener<GameObject>(buttons[2].onClick, new UnityAction<GameObject>(menu.OpenPanel), settings);
        UnityEventTools.AddObjectPersistentListener<GameObject>(buttons[3].onClick, new UnityAction<GameObject>(menu.OpenPanel), credits);
        UnityEventTools.AddPersistentListener(buttons[4].onClick, new UnityAction(menu.Quit));

        foreach (GameObject p in menu.panels) if (p != main) p.SetActive(false);
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    static GameObject BuildBox(Transform c, string name, string title, Vector2 size, MainMenu menu, GameObject main, out Transform box)
    {
        Image bg = Img(c, name, new Color(0.02f, 0.035f, 0.07f, 0.94f), MR, MR, new Vector2(-70f, 0f), size);
        Accent(bg.transform, Cyan, true);
        Txt(bg.transform, "Title", title, 54, TextAlignmentOptions.Left, TL, TL, new Vector2(50f, -34f), new Vector2(size.x - 100f, 70f), Cyan, FontStyles.Bold);
        Button back = Btn(bg.transform, "Back", "НАЗАД", BR, BR, new Vector2(-40f, 30f), new Vector2(220f, 56f), 24);
        UnityEventTools.AddObjectPersistentListener<GameObject>(back.onClick, new UnityAction<GameObject>(menu.OpenPanel), main);
        box = bg.transform;
        return bg.gameObject;
    }

    static GameObject BuildInfoPanel(Transform c, string name, string title, string left, string right, MainMenu menu, GameObject main)
    {
        GameObject panel = BuildBox(c, name, title, new Vector2(1020f, 760f), menu, main, out Transform box);
        Txt(box, "Left", left, 21, TextAlignmentOptions.TopLeft, TL, TL, new Vector2(50f, -120f), new Vector2(460f, 560f), UiText);
        Txt(box, "Right", right, 21, TextAlignmentOptions.TopLeft, TL, TL, new Vector2(540f, -120f), new Vector2(450f, 560f), UiText);
        return panel;
    }

    static GameObject BuildHeroPanel(Transform c, MainMenu menu, GameObject main)
    {
        GameObject panel = BuildBox(c, "HeroPanel", "ВЫБЕРИ ГЕРОЯ", new Vector2(1000f, 640f), menu, main, out Transform box);
        string[] names = { "СТРЕЛОК", "БЕГУН" };
        string[] roles = { "Дальний бой", "Мобильность" };
        string[] stats =
        {
            "• Урон оружия +4\n• +6 патронов в обойме\n• 2 заряда рывка\n\nДля тех, кто любит\nметко стрелять",
            "• Скорость бега +20%\n• 3 заряда рывка\n• Рывок восстанавливается быстрее\n\nДля обходов через\nфланги и джунгли",
        };
        Color[] colors = { Cyan, new Color(0.5f, 1f, 0.6f) };
        for (int i = 0; i < 2; i++)
        {
            Image card = Img(box, "Card" + i, new Color(0.06f, 0.09f, 0.15f, 0.95f), TL, TL, new Vector2(50f + i * 460f, -120f), new Vector2(430f, 420f));
            Img(card.transform, "Top", colors[i], TC, TC, Vector2.zero, new Vector2(430f, 4f));
            Txt(card.transform, "Name", names[i], 44, TextAlignmentOptions.Center, TC, TC, new Vector2(0f, -24f), new Vector2(400f, 60f), colors[i], FontStyles.Bold);
            Txt(card.transform, "Role", roles[i], 22, TextAlignmentOptions.Center, TC, TC, new Vector2(0f, -84f), new Vector2(400f, 30f), UiDim, FontStyles.Bold);
            Txt(card.transform, "Stats", stats[i], 23, TextAlignmentOptions.TopLeft, TC, TC, new Vector2(10f, -130f), new Vector2(360f, 200f), UiText);
            Button go = Btn(card.transform, "Play", "В БОЙ", BC, BC, new Vector2(0f, 22f), new Vector2(360f, 64f), 30);
            UnityEventTools.AddIntPersistentListener(go.onClick, new UnityAction<int>(menu.StartGame), i);
        }
        return panel;
    }

    static string HowToText()
    {
        return "<color=#00E5FF><b>ЦЕЛЬ</b></color>\n" +
               "Разрушь вражеский реактор (главное здание красной базы) раньше, чем враги разрушат твой.\n\n" +
               "<color=#00E5FF><b>ПОРЯДОК</b></color>\n" +
               "На каждой линии 2 башни. Внутренняя башня и реактор под щитом, пока жива предыдущая защита. " +
               "Без своих миньонов рядом башня получает на 70% меньше урона.\n\n" +
               "<color=#00E5FF><b>КРЕДИТЫ</b></color>\n" +
               "Кредиты дают только за добивание: последний выстрел по миньону, нейтралу или герою. " +
               "Трать их в магазине на базе или в джунглях. Магазин не ставит игру на паузу.\n\n" +
               "<color=#00E5FF><b>КАРТА</b></color>\n" +
               "Две линии, между ними джунгли с нейтралами и Pitmonster (+25% урона команде на 90 с). " +
               "Жёлтые платформы перебрасывают через стены. Чем больше башен потеряла команда, тем сильнее миньоны врага.";
    }

    static string CreditsLeft()
    {
        return "<color=#00E5FF><b>КОМАНДА</b></color>\n" +
               "Жаксыбаев Алискер\nВагнер Альберт\n" +
               "Программирование, гейм-дизайн, карта, интерфейс\n\n" +
               "<color=#00E5FF><b>СОБСТВЕННЫЕ АССЕТЫ</b></color>\n" +
               "Карта Neon City, башни, реактор, винтовка, терминалы (собраны из примитивов в Unity)\n" +
               "Процедурные текстуры: неоновая сетка, окна небоскрёбов (Assets/Textures, генератор в Assets/Editor)\n" +
               "Прицел (Sources/Crosshair.xcf), эффекты частиц, материалы\n\n" +
               "<color=#00E5FF><b>ДВИЖОК</b></color>\nUnity 6, URP, AI Navigation, TextMesh Pro";
    }

    static string CreditsRight()
    {
        return "<color=#FF6A00><b>МОДЕЛИ И АНИМАЦИИ</b></color>\n" +
               "Y Bot, Mutant, Rifle Idle, Rifle Run, Idle, Zombie Walk — Adobe Mixamo (mixamo.com)\n\n" +
               "<color=#FF6A00><b>ЗВУКИ</b></color> (freesound.org)\n" +
               "Plasma lazer pistol gun shot 1 — erokia\n" +
               "Reload — dacmod1\n" +
               "Coin C 05 — cabled_mess\n" +
               "Cinematic energy impact — rescopicsound\n\n" +
               "<color=#FF6A00><b>МУЗЫКА</b></color>\n" +
               "Cyberpunk Moonlight Sonata — Joth (opengameart.org, CC0)\n\n" +
               "<color=#FF6A00><b>ШРИФТ</b></color>\nLiberation Sans (SIL OFL), TextMesh Pro\n\n" +
               "Полный список с лицензиями: ASSETS.md";
    }

    // =====================================================================
    //  Build Settings и Player Settings
    // =====================================================================
    static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        PlayerSettings.companyName = "Midline Team";
        PlayerSettings.productName = "Midline";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.visibleInBackground = true;
    }

    // =====================================================================
    //  Помощники: UI
    // =====================================================================
    static GameObject MakeCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return go;
    }

    static GameObject UIObj(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    static Image Img(Transform parent, string name, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = UIObj(name, parent);
        Place(go, anchor, pivot, pos, size);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Txt(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject go = UIObj(name, parent);
        Place(go, anchor, pivot, pos, size);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fontSize; t.alignment = align; t.color = color; t.fontStyle = style;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    // Тонкая цветная полоска сверху (top=true) или слева
    static void Accent(Transform parent, Color color, bool top)
    {
        RectTransform rt = Img(parent, "Accent", color, top ? TC : ML, top ? TC : ML, Vector2.zero, Vector2.zero).rectTransform;
        if (top) { rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.sizeDelta = new Vector2(0f, 3f); }
        else { rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f); rt.sizeDelta = new Vector2(4f, 0f); rt.pivot = new Vector2(0f, 0.5f); }
    }

    static Button Btn(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float fontSize = 30f)
    {
        Image img = Img(parent, name, new Color(0.05f, 0.11f, 0.17f, 0.95f), anchor, pivot, pos, size);
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.55f, 1.6f, 1.8f);
        cb.pressedColor = new Color(0.4f, 0.9f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        cb.colorMultiplier = 1.5f;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        // Неоновая полоска слева у кнопки
        RectTransform acc = Img(img.transform, "Edge", Cyan, ML, ML, Vector2.zero, Vector2.zero).rectTransform;
        acc.anchorMin = new Vector2(0f, 0f); acc.anchorMax = new Vector2(0f, 1f); acc.pivot = new Vector2(0f, 0.5f); acc.sizeDelta = new Vector2(4f, 0f);
        TextMeshProUGUI t = Txt(img.transform, "Text", label, fontSize, TextAlignmentOptions.Center, C, C, Vector2.zero, size, Color.white, FontStyles.Bold);
        Stretch(t.rectTransform);
        return b;
    }

    // Полоса (фон + заполнение). Возвращает RectTransform заполнения: его anchorMax.x = доля
    static RectTransform Bar(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color fill)
    {
        Image bg = Img(parent, name, new Color(0.02f, 0.03f, 0.05f, 0.95f), anchor, pivot, pos, size);
        Image f = Img(bg.transform, "Fill", fill, C, C, Vector2.zero, Vector2.zero);
        Stretch(f.rectTransform, 2f);
        return f.rectTransform;
    }

    static Slider MakeSlider(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float min, float max)
    {
        GameObject go = UIObj(name, parent);
        Place(go, anchor, pivot, pos, size);
        Image bg = Img(go.transform, "Background", new Color(0.08f, 0.12f, 0.18f, 1f), C, C, Vector2.zero, Vector2.zero);
        Stretch(bg.rectTransform);

        GameObject fillArea = UIObj("Fill Area", go.transform);
        Stretch(fillArea.GetComponent<RectTransform>());
        Image fill = Img(fillArea.transform, "Fill", Cyan * 0.8f, C, C, Vector2.zero, Vector2.zero);
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.sizeDelta = Vector2.zero;

        GameObject handleArea = UIObj("Handle Slide Area", go.transform);
        RectTransform har = handleArea.GetComponent<RectTransform>();
        Stretch(har); har.offsetMin = new Vector2(10f, 0f); har.offsetMax = new Vector2(-10f, 0f);
        Image handle = Img(handleArea.transform, "Handle", Color.white, C, C, Vector2.zero, new Vector2(20f, 34f));
        handle.raycastTarget = true;
        handle.rectTransform.anchorMin = new Vector2(0f, 0.5f); handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);

        bg.raycastTarget = true;
        Slider s = go.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = min; s.maxValue = max;
        return s;
    }

    // =====================================================================
    //  Помощники: 3D
    // =====================================================================
    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static Light PointLight(Transform parent, Vector3 localPos, Color color, float range, float intensity)
    {
        GameObject go = new GameObject("PointLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        return l;
    }

    static void Text3D(Transform parent, string text, Vector3 localPos, Quaternion localRot, float size, Color color)
    {
        GameObject go = new GameObject("Label_" + text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        TextMeshPro t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = size * 2f;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.rectTransform.sizeDelta = new Vector2(20f, 4f);
    }

    static Material[] Fill(int count, Material m)
    {
        Material[] arr = new Material[Mathf.Max(1, count)];
        for (int i = 0; i < arr.Length; i++) arr[i] = m;
        return arr;
    }

    static void SetLayerRecursive(GameObject go, int layer, int keepLayer)
    {
        if (go.layer != keepLayer) go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer, keepLayer);
    }

    static GameObject SavePrefab(GameObject sceneObject, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sceneObject, path);
        Object.DestroyImmediate(sceneObject);
        return prefab;
    }

    static AudioClip Clip(string path)
    {
        AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (c == null) Debug.LogWarning("Нет звука " + path);
        return c;
    }
}
