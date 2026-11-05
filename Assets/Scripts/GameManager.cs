using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Экономика")]
    public int credits = 50;

    [Header("Панели (подключим на этапе 9)")]
    public GameObject pausePanel;
    public GameObject shopPanel;
    public GameObject endPanel;
    public TMP_Text endTitleText;

    [Header("Подсказки и сообщения")]
    public TMP_Text promptText;
    public TMP_Text messageText;

    public float matchTime;
    public bool IsGameOver { get; private set; }
    public bool InputBlocked { get; private set; }      // true, когда открыт магазин, пауза или конец игры

    int blueTowersLost, redTowersLost;
    int blueKills, redKills;
    bool bluePitBuff, redPitBuff;
    float messageHideTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;
        SetCursor(false);
        SetActive(pausePanel, false);
        SetActive(shopPanel, false);
        SetActive(endPanel, false);
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    void Update()
    {
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
    public void AddCredits(int amount) { credits += amount; }

    public bool TrySpend(int cost)
    {
        if (credits < cost) return false;
        credits -= cost;
        return true;
    }

    // ---------- Счёт матча ----------
    public int TowersLost(TeamId t) { return t == TeamId.Blue ? blueTowersLost : redTowersLost; }
    public int HeroKills(TeamId t) { return t == TeamId.Blue ? blueKills : redKills; }

    public void RegisterTowerLost(TeamId owner)
    {
        if (owner == TeamId.Blue) blueTowersLost++; else redTowersLost++;
        ShowMessage(owner == TeamId.Blue ? "Наша башня разрушена!" : "Вражеская башня разрушена!");
    }

    public void RegisterHeroKill(TeamId victimTeam)
    {
        if (victimTeam == TeamId.Blue) redKills++; else blueKills++;
        ShowMessage(victimTeam == TeamId.Blue ? "Наш герой убит" : "Вражеский герой убит!");
    }

    public void GrantPitBuff(TeamId t)
    {
        if (t == TeamId.Blue) bluePitBuff = true;
        else if (t == TeamId.Red) redPitBuff = true;
        ShowMessage(t == TeamId.Blue ? "Pitmonster убит! Бафф нашей команде" : "Враги убили Pitmonster!");
    }

    // Бонус урона от Pitmonster (x1.25)
    public float PitMultiplier(TeamId t)
    {
        bool has = t == TeamId.Blue ? bluePitBuff : (t == TeamId.Red && redPitBuff);
        return has ? 1.25f : 1f;
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

    public void ShowMessage(string text)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageText.gameObject.SetActive(true);
        messageHideTime = Time.unscaledTime + 3f;
    }

    // ---------- Пауза и магазин ----------
    public void TogglePause()
    {
        if (pausePanel == null) return;
        bool pause = !pausePanel.activeSelf;
        pausePanel.SetActive(pause);
        Time.timeScale = pause ? 0f : 1f;
        InputBlocked = pause;
        SetCursor(pause);
    }

    // Магазин НЕ останавливает время: пока ты покупаешь, враги могут атаковать
    public void OpenShop()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(true);
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
        if (loser == TeamId.Red) EndGame("ПОБЕДА");
        else EndGame("ПОРАЖЕНИЕ");
    }

    void EndGame(string title)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        InputBlocked = true;

        if (endTitleText != null) endTitleText.text = title;
        SetActive(endPanel, true);
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