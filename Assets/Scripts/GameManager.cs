using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Единый центр матча: кредиты, счёт, баффы, пауза, магазин, конец игры, сообщения.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Экономика")]
    public int credits = 50;

    [Header("Панели")]
    public GameObject pausePanel;
    public GameObject shopPanel;
    public GameObject endPanel;
    public TMP_Text endTitleText;
    public TMP_Text endStatsText;

    [Header("Подсказки и сообщения")]
    public TMP_Text promptText;      // "Нажми E" у терминалов
    public TMP_Text messageText;     // крупное сообщение по центру сверху
    public TMP_Text feedText;        // лента событий справа сверху

    [Header("Pitmonster")]
    public float pitBuffDuration = 90f;
    public float pitDamageBonus = 0.25f;

    [Header("Звуки событий")]
    public AudioSource announcer;    // 2D-источник для важных звуков
    public AudioClip towerDestroyedClip;
    public AudioClip purchaseClip;

    public float matchTime;
    public bool IsGameOver { get; private set; }
    public bool InputBlocked { get; private set; }      // true, когда открыт магазин, пауза или конец игры
    public bool IsPaused => pausePanel != null && pausePanel.activeSelf;

    // Статистика для экрана победы/поражения
    public int CreditsEarned { get; private set; }
    public int PlayerKills { get; private set; }
    public int PlayerDeaths { get; private set; }
    public int MinionLastHits { get; private set; }

    int blueTowersLost, redTowersLost;
    int blueKills, redKills;
    float bluePitBuffEnd, redPitBuffEnd;
    float messageHideTime;

    readonly List<string> feed = new List<string>();
    readonly List<float> feedTimes = new List<float>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;
        GameSettings.ApplyVolume();
        SetCursor(false);
        SetActive(pausePanel, false);
        SetActive(shopPanel, false);
        SetActive(endPanel, false);
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);
        StartCoroutine(Tutorial());
    }

    // Короткие подсказки в начале матча
    IEnumerator Tutorial()
    {
        string[] hints =
        {
            "Герой: " + GameSettings.HeroName + ". Защити свой Ancient и разрушь вражеский!",
            "Кредиты дают только за ДОБИВАНИЕ миньонов. Стреляй последним!",
            "Башни ломаются по порядку: внешняя -> внутренняя -> Ancient",
            "Без своих миньонов рядом башня получает на 70% меньше урона",
            "Магазин и лечение на базе: подойди и нажми E. Время в магазине не стоит!",
        };
        yield return new WaitForSeconds(1f);
        foreach (string h in hints)
        {
            ShowMessage(h, 4.5f);
            yield return new WaitForSeconds(5f);
        }
    }

    void Update()
    {
        UpdateFeed();
        if (IsGameOver) return;

        matchTime += Time.deltaTime;

        if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime > messageHideTime)
            messageText.gameObject.SetActive(false);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (shopPanel != null && shopPanel.activeSelf) CloseShop();
            else TogglePause();
        }
    }

    // ---------- Кредиты ----------
    public void AddCredits(int amount)
    {
        credits += amount;
        CreditsEarned += amount;
    }

    public bool TrySpend(int cost)
    {
        if (credits < cost) return false;
        credits -= cost;
        PlayClip(purchaseClip);
        return true;
    }

    // ---------- Счёт матча ----------
    public int TowersLost(TeamId t) { return t == TeamId.Blue ? blueTowersLost : redTowersLost; }
    public int HeroKills(TeamId t) { return t == TeamId.Blue ? blueKills : redKills; }

    public void RegisterTowerLost(TeamId owner)
    {
        if (owner == TeamId.Blue) blueTowersLost++; else redTowersLost++;
        PlayClip(towerDestroyedClip);
        if (owner == TeamId.Blue)
        {
            ShowMessage("Наша башня разрушена! Вражеские миньоны усилились");
            AddFeed("<color=#FF6A00>Красные</color> разрушили башню");
        }
        else
        {
            ShowMessage("Вражеская башня разрушена!");
            AddFeed("<color=#00E5FF>Синие</color> разрушили башню");
        }
    }

    // victim — кто умер, killer — кто добил (может быть null)
    public void RegisterHeroKill(Team victim, GameObject killer)
    {
        TeamId victimTeam = victim.id;
        if (victimTeam == TeamId.Blue) redKills++; else blueKills++;

        bool playerDied = victim.CompareTag("Player");
        bool playerKilled = killer != null && killer.CompareTag("Player");
        if (playerDied) PlayerDeaths++;
        if (playerKilled) PlayerKills++;

        string victimName = string.IsNullOrEmpty(victim.displayName) ? "Герой" : victim.displayName;
        string killerName = "башня";
        if (killer == null) killerName = "?";
        if (killer != null)
        {
            Team kt = killer.GetComponent<Team>();
            if (kt != null && !string.IsNullOrEmpty(kt.displayName)) killerName = kt.displayName;
            else if (kt != null && kt.kind == UnitKind.Minion) killerName = "миньон";
            else if (kt != null && kt.kind == UnitKind.Neutral) killerName = "нейтрал";
        }

        AddFeed(Colored(killerName, killer) + "  убил  " + Colored(victimName, victim.gameObject));
        if (playerDied) ShowMessage("Ты погиб");
        else if (victimTeam == TeamId.Red) ShowMessage("Вражеский герой убит!");
    }

    public void RegisterMinionLastHit()
    {
        MinionLastHits++;
    }

    // ---------- Pitmonster ----------
    public void GrantPitBuff(TeamId t)
    {
        float end = Time.time + pitBuffDuration;
        if (t == TeamId.Blue) bluePitBuffEnd = end;
        else if (t == TeamId.Red) redPitBuffEnd = end;
        else return;

        PlayClip(towerDestroyedClip);
        ShowMessage(t == TeamId.Blue ? "Pitmonster убит! +25% урона нашей команде" : "Враги убили Pitmonster!");
        AddFeed((t == TeamId.Blue ? "<color=#00E5FF>Синие</color>" : "<color=#FF6A00>Красные</color>") + " убили <color=#FFD126>Pitmonster</color>");
    }

    public float PitBuffLeft(TeamId t)
    {
        float end = t == TeamId.Blue ? bluePitBuffEnd : (t == TeamId.Red ? redPitBuffEnd : 0f);
        return Mathf.Max(0f, end - Time.time);
    }

    // Бонус урона от Pitmonster, пока бафф активен
    public float PitMultiplier(TeamId t)
    {
        return PitBuffLeft(t) > 0f ? 1f + pitDamageBonus : 1f;
    }

    // Миньоны команды сильнее, чем больше башен потеряла команда-противник
    public float MinionPower(TeamId t)
    {
        return 1f + 0.15f * TowersLost(Team.Opposite(t));
    }

    // ---------- Сообщения ----------
    public void ShowPrompt(string text)
    {
        if (promptText == null) return;
        promptText.text = text;
        promptText.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    public void ShowMessage(string text, float seconds = 3f)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageText.gameObject.SetActive(true);
        messageHideTime = Time.unscaledTime + seconds;
    }

    public void HideMessage()
    {
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    public void AddFeed(string line)
    {
        feed.Add(line);
        feedTimes.Add(Time.unscaledTime);
        if (feed.Count > 5) { feed.RemoveAt(0); feedTimes.RemoveAt(0); }
    }

    void UpdateFeed()
    {
        // Сообщения ленты живут 8 секунд
        while (feedTimes.Count > 0 && Time.unscaledTime - feedTimes[0] > 8f)
        {
            feed.RemoveAt(0);
            feedTimes.RemoveAt(0);
        }
        if (feedText != null) feedText.text = string.Join("\n", feed);
    }

    static string Colored(string name, GameObject go)
    {
        Team t = go != null ? go.GetComponent<Team>() : null;
        if (t == null) return name;
        return "<color=#" + ColorUtility.ToHtmlStringRGB(Team.ColorOf(t.id)) + ">" + name + "</color>";
    }

    void PlayClip(AudioClip clip)
    {
        if (announcer != null && clip != null) announcer.PlayOneShot(clip);
    }

    // ---------- Пауза и магазин ----------
    public void TogglePause()
    {
        if (pausePanel == null || IsGameOver) return;
        bool pause = !pausePanel.activeSelf;
        HideMessage();
        pausePanel.SetActive(pause);
        Time.timeScale = pause ? 0f : 1f;
        InputBlocked = pause;
        SetCursor(pause);
    }

    // Магазин НЕ останавливает время: пока ты покупаешь, враги могут атаковать
    public void OpenShop()
    {
        if (shopPanel == null || IsPaused) return;
        shopPanel.SetActive(true);
        HideMessage();
        InputBlocked = true;
        SetCursor(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        InputBlocked = false;
        SetCursor(false);
    }

    // ---------- Конец игры ----------
    // Вызывается, когда разрушен Ancient команды loser
    public void EndMatch(TeamId loser)
    {
        if (IsGameOver) return;
        if (shopPanel != null) shopPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        bool win = loser == TeamId.Red;
        if (endTitleText != null)
        {
            endTitleText.text = win ? "ПОБЕДА" : "ПОРАЖЕНИЕ";
            endTitleText.color = win ? Team.BlueColor : Team.RedColor;
        }

        if (endStatsText != null)
        {
            int m = (int)(matchTime / 60f), s = (int)(matchTime % 60f);
            endStatsText.text =
                "Время матча: " + m.ToString("00") + ":" + s.ToString("00") + "\n" +
                "Убийства / смерти: " + PlayerKills + " / " + PlayerDeaths + "\n" +
                "Добито миньонов: " + MinionLastHits + "\n" +
                "Заработано кредитов: " + CreditsEarned + "\n" +
                "Башен разрушено: " + TowersLost(TeamId.Red) + " : " + TowersLost(TeamId.Blue);
        }

        IsGameOver = true;
        InputBlocked = true;
        HideMessage();
        HidePrompt();
        SetActive(endPanel, true);
        PlayClip(towerDestroyedClip);
        Time.timeScale = 0f;
        SetCursor(true);
    }

    // ---------- Кнопки меню ----------
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ---------- Вспомогательное ----------
    static void SetActive(GameObject go, bool value)
    {
        if (go != null) go.SetActive(value);
    }

    void SetCursor(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    // Тест из Inspector: правая кнопка по компоненту
    [ContextMenu("Test Win")] void TestWin() { EndMatch(TeamId.Red); }
    [ContextMenu("Test Lose")] void TestLose() { EndMatch(TeamId.Blue); }
}
