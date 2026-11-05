using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Инструмент разработчика: автоматический прогон игры для проверки.
// Работает ТОЛЬКО если билд запущен с параметром -autotest (обычный игрок его никогда не увидит).
// Пример: Midline.exe -autotest -autotestdir C:\shots -screen-width 1600 -screen-height 900 -screen-fullscreen 0
// Делает скриншоты меню и матча, ускоряет время, пишет в лог события и ошибки, потом закрывает игру.
public class AutoTest : MonoBehaviour
{
    string dir;
    int errors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!System.Environment.CommandLine.Contains("-autotest")) return;
        GameObject go = new GameObject("AutoTest");
        DontDestroyOnLoad(go);
        go.AddComponent<AutoTest>();
    }

    void Start()
    {
        dir = Arg("-autotestdir", Path.Combine(Application.persistentDataPath, "autotest"));
        Directory.CreateDirectory(dir);
        Application.logMessageReceived += (msg, stack, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error) errors++;
        };
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // ---------- Главное меню ----------
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            MainMenu menu = FindAnyObjectByType<MainMenu>();
            yield return Shot("01_menu", 1.5f);
            foreach (GameObject p in menu.panels)
            {
                if (p == menu.mainPanel) continue;
                menu.OpenPanel(p);
                yield return Shot("02_menu_" + p.name, 0.5f);
            }
            menu.StartGame(0);
            yield return null;
            yield return new WaitForSeconds(1f);
        }

        // ---------- Матч ----------
        GameManager gm = GameManager.Instance;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Health playerHp = player.GetComponent<Health>();

        yield return Shot("10_game_start", 2f);

        // Магазин
        gm.credits += 200;
        gm.OpenShop();
        yield return Shot("11_shop", 0.5f);
        ShopPanel shop = gm.shopPanel.GetComponent<ShopPanel>();
        shop.Buy(0); shop.Buy(2);
        shop.SelectCategory(1);
        yield return Shot("12_shop_bought", 0.3f);
        gm.CloseShop();

        // Пауза
        gm.TogglePause();
        yield return ShotRealtime("13_pause", 0.5f);
        gm.TogglePause();

        // Смотрим на линию: ставим игрока у внешней башни и ускоряем время
        playerHp.invulnerable = true;
        Teleport(player, new Vector3(30f, 0.1f, -14f), 0f);
        Time.timeScale = 3f;
        yield return Shot("14_lane_wave", 20f);

        Teleport(player, new Vector3(0f, 0.1f, -10f), 0f);
        yield return Shot("15_jungle_pit", 2f);

        Teleport(player, new Vector3(-6f, 0.1f, -92f), 60f);
        yield return Shot("16_base", 1f);

        // Даём боту и миньонам поиграть несколько минут игрового времени
        Teleport(player, new Vector3(0f, 0.1f, -80f), 0f);
        for (int i = 0; i < 6; i++)
        {
            yield return new WaitForSeconds(20f);
            Log("t=" + Mathf.RoundToInt(gm.matchTime) + " towers lost B/R=" + gm.TowersLost(TeamId.Blue) + "/" + gm.TowersLost(TeamId.Red)
                + " kills B/R=" + gm.HeroKills(TeamId.Blue) + "/" + gm.HeroKills(TeamId.Red)
                + " units=" + FindObjectsByType<UnitAI>().Length + " errors=" + errors);
        }
        yield return Shot("17_after_minutes", 0.5f);

        // Смерть игрока и экран возрождения
        Time.timeScale = 1f;
        playerHp.invulnerable = false;
        playerHp.TakeDamage(99999f, null);
        yield return Shot("18_respawn", 1f);

        // Проверка правил: внутренняя башня под щитом, пока жива внешняя; потом по порядку до Ancient
        yield return new WaitForSeconds(7f);
        Health inner = GameObject.Find("RedTowerR2").GetComponent<Health>();
        float dealt = inner.TakeDamage(500f, player);
        Log("inner tower with outer alive: dealt=" + dealt + " (ожидается 0)");
        string[] order = { "RedTowerR1", "RedTowerR2", "RedTowerL1", "RedTowerL2" };
        foreach (string n in order)
        {
            Health h = GameObject.Find(n).GetComponent<Health>();
            yield return null;                  // Shield обновляется в Update
            h.extraReduction = 0f;
            float d = h.TakeDamage(99999f, player);
            Log(n + " dealt=" + d + " dead=" + h.IsDead);
            yield return new WaitForSeconds(0.3f);
        }
        Health ancient = GameObject.Find("RedAncient").GetComponent<Health>();
        yield return null;
        Log("ancient invulnerable after towers=" + ancient.invulnerable);
        ancient.TakeDamage(99999f, player);
        yield return ShotRealtime("19_victory", 3f);
        Log("game over=" + gm.IsGameOver);

        Log("DONE errors=" + errors);
        Application.Quit();
    }

    void Teleport(GameObject player, Vector3 pos, float yaw)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        cc.enabled = true;
    }

    IEnumerator Shot(string name, float wait)
    {
        yield return new WaitForSeconds(wait);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
        Log("shot " + name);
        yield return null;
    }

    IEnumerator ShotRealtime(string name, float wait)
    {
        yield return new WaitForSecondsRealtime(wait);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
        Log("shot " + name);
        yield return null;
    }

    static void Log(string s)
    {
        Debug.Log("[AutoTest] " + s);
    }

    static string Arg(string name, string fallback)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return fallback;
    }
}
