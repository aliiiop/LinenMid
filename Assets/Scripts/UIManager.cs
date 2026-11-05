using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Игровой интерфейс (HUD). Раскладка из концепта:
// сверху счёт и ближайшая башня, снизу по центру здоровье, энергия и способности,
// справа внизу мини-карта и патроны, слева внизу кредиты и временные эффекты.
public class UIManager : MonoBehaviour
{
    [Header("Игрок")]
    public Health playerHealth;
    public PlayerController player;
    public PlayerShooting shooting;
    public PlayerAbilities abilities;
    public Respawner playerRespawn;
    public Health redAncient;

    [Header("Низ экрана")]
    public RectTransform healthFill;
    public TMP_Text healthText;
    public RectTransform energyFill;
    public TMP_Text energyText;
    public TMP_Text ammoText;
    public TMP_Text creditsText;
    public TMP_Text buffsText;
    public RectTransform abilityCooldown;   // затемнение поверх иконки Q (растёт сверху вниз)
    public TMP_Text abilityText;
    public RectTransform dashCooldown;
    public TMP_Text dashText;

    [Header("Верх экрана")]
    public TMP_Text scoreText;
    public TMP_Text timeText;
    public TMP_Text nearTowerText;
    public RectTransform nearTowerFill;

    [Header("Центр экрана")]
    public RectTransform[] crosshairLines;  // верх, низ, лево, право
    public Graphic hitmarker;
    public GameObject respawnPanel;
    public TMP_Text respawnText;
    public Image damageVignette;

    float hitmarkerAlpha;
    float damageFlash;
    Color hitmarkerColor = Color.white;

    void Start()
    {
        if (shooting != null) shooting.OnHit += OnPlayerHit;
        if (playerHealth != null) playerHealth.OnDamaged += OnPlayerDamaged;
        if (damageVignette != null)
        {
            damageVignette.sprite = MakeVignetteSprite();
            damageVignette.color = new Color(1f, 0f, 0f, 0f);
        }
        if (hitmarker != null) hitmarker.color = new Color(1f, 1f, 1f, 0f);
    }

    void OnPlayerHit(bool headshot, bool kill)
    {
        hitmarkerAlpha = 1f;
        hitmarkerColor = kill ? new Color(1f, 0.2f, 0.2f) : (headshot ? new Color(1f, 0.85f, 0.2f) : Color.white);
    }

    void OnPlayerDamaged(float amount, GameObject attacker)
    {
        if (amount > 0f) damageFlash = Mathf.Min(1f, damageFlash + 0.35f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;

        // ----- Здоровье и энергия -----
        SetFill(healthFill, playerHealth.Percent);
        healthText.text = Mathf.CeilToInt(playerHealth.currentHealth) + " / " + Mathf.CeilToInt(playerHealth.maxHealth);
        SetFill(energyFill, abilities.energy / abilities.maxEnergy);
        energyText.text = Mathf.FloorToInt(abilities.energy) + " / " + Mathf.CeilToInt(abilities.maxEnergy);

        // ----- Патроны -----
        ammoText.text = shooting.IsReloading
            ? "<size=60%>ПЕРЕЗАРЯДКА</size>\n" + Mathf.RoundToInt(shooting.ReloadProgress * 100f) + "%"
            : "<size=60%>ПАТРОНЫ</size>\n" + shooting.currentAmmo + "<size=60%> / " + shooting.magazineSize + "</size>";
        ammoText.color = shooting.currentAmmo <= shooting.magazineSize / 5 && !shooting.IsReloading ? new Color(1f, 0.4f, 0.3f) : Color.white;

        // ----- Кредиты и эффекты -----
        creditsText.text = "<size=60%><color=#7A8699>КРЕДИТЫ</color></size>\n<color=#FFD126>" + gm.credits + "</color>";
        string buffs = "";
        float pit = gm.PitBuffLeft(TeamId.Blue);
        if (pit > 0f) buffs += "<color=#FFD126>Бафф Pitmonster +25% урона: " + Mathf.CeilToInt(pit) + " с</color>\n";
        float enemyPit = gm.PitBuffLeft(TeamId.Red);
        if (enemyPit > 0f) buffs += "<color=#FF6A00>У врагов бафф Pitmonster: " + Mathf.CeilToInt(enemyPit) + " с</color>\n";
        int ourMinionBonus = gm.TowersLost(TeamId.Red) * 15;
        if (ourMinionBonus > 0) buffs += "<color=#00E5FF>Наши миньоны +" + ourMinionBonus + "%</color>\n";
        int enemyMinionBonus = gm.TowersLost(TeamId.Blue) * 15;
        if (enemyMinionBonus > 0) buffs += "<color=#FF6A00>Вражеские миньоны +" + enemyMinionBonus + "%</color>\n";
        buffsText.text = buffs;

        // ----- Способности -----
        float qCd = abilities.CooldownTotal > 0f ? Mathf.Clamp01(abilities.CooldownLeft / abilities.CooldownTotal) : 0f;
        SetHeight(abilityCooldown, abilities.energy < abilities.burstCost ? 1f : qCd);
        abilityText.text = abilities.CooldownLeft > 0f ? Mathf.CeilToInt(abilities.CooldownLeft).ToString()
            : (abilities.energy < abilities.burstCost ? "<size=60%>нет\nэнергии</size>" : "");

        SetHeight(dashCooldown, player.dashCharges > 0 ? 0f : 1f - player.DashRecharge01);
        dashText.text = player.dashCharges.ToString();

        // ----- Счёт и время -----
        scoreText.text = "<color=#00E5FF>СИНИЕ  " + gm.HeroKills(TeamId.Blue) + "</color>"
            + "   <color=#7A8699>убийства</color>   "
            + "<color=#FF6A00>" + gm.HeroKills(TeamId.Red) + "  КРАСНЫЕ</color>\n"
            + "<size=70%><color=#7A8699>башен разрушено</color>  <color=#00E5FF>" + gm.TowersLost(TeamId.Red) + "</color> : <color=#FF6A00>" + gm.TowersLost(TeamId.Blue) + "</color></size>";
        int minutes = (int)(gm.matchTime / 60f);
        int seconds = (int)(gm.matchTime % 60f);
        timeText.text = minutes.ToString("00") + ":" + seconds.ToString("00");

        UpdateNearTower();
        UpdateCrosshair();

        // ----- Смерть и возрождение -----
        bool dead = playerRespawn != null && playerRespawn.RespawnTimeLeft > 0f && !gm.IsGameOver;
        if (respawnPanel.activeSelf != dead) respawnPanel.SetActive(dead);
        if (dead)
            respawnText.text = "ТЫ ПОГИБ\n<size=50%>Возрождение через " + Mathf.CeilToInt(playerRespawn.RespawnTimeLeft) + " с</size>";

        // ----- Красная виньетка: при уроне и при малом здоровье -----
        damageFlash = Mathf.MoveTowards(damageFlash, 0f, 2f * Time.deltaTime);
        float lowHp = dead ? 0f : Mathf.Clamp01((0.35f - playerHealth.Percent) / 0.35f) * 0.6f;
        if (damageVignette != null)
            damageVignette.color = new Color(1f, 0.05f, 0.05f, Mathf.Max(damageFlash * 0.7f, lowHp));
    }

    // Состояние ближайшей вражеской башни (а если все башни разрушены, то Ancient)
    void UpdateNearTower()
    {
        Tower nearestTower = null;
        float best = float.MaxValue;

        foreach (Tower t in Tower.All)
        {
            if (t.TeamInfo.id != TeamId.Red || t.Hp.IsDead) continue;
            float d = Vector3.Distance(player.transform.position, t.transform.position);
            if (d < best)
            {
                best = d;
                nearestTower = t;
            }
        }

        Health target = nearestTower != null ? nearestTower.Hp : redAncient;
        if (target == null) return;

        string name = nearestTower != null ? "ВРАЖЕСКАЯ БАШНЯ" : "ВРАЖЕСКИЙ РЕАКТОР";
        string state = "";
        if (target.invulnerable) state = "  <color=#7FC8FF>[щит]</color>";
        else if (nearestTower != null && !nearestTower.HasMinionSupport) state = "  <color=#7A8699>[нужны миньоны]</color>";
        nearTowerText.text = name + state;
        SetFill(nearTowerFill, target.Percent);
    }

    // Прицел расходится вместе с разбросом, хитмаркер вспыхивает при попадании
    void UpdateCrosshair()
    {
        if (crosshairLines != null && crosshairLines.Length == 4)
        {
            float gap = 6f + shooting.CurrentSpread * 1080f;     // разброс в долях экрана -> пиксели
            crosshairLines[0].anchoredPosition = new Vector2(0f, gap);
            crosshairLines[1].anchoredPosition = new Vector2(0f, -gap);
            crosshairLines[2].anchoredPosition = new Vector2(-gap, 0f);
            crosshairLines[3].anchoredPosition = new Vector2(gap, 0f);
        }

        if (hitmarker != null)
        {
            hitmarkerAlpha = Mathf.MoveTowards(hitmarkerAlpha, 0f, 5f * Time.deltaTime);
            Color c = hitmarkerColor;
            c.a = hitmarkerAlpha;
            hitmarker.color = c;
        }
    }

    // Полоса заполнения: меняем правый край (anchorMax.x) от 0 до 1
    static void SetFill(RectTransform fill, float value)
    {
        if (fill == null) return;
        fill.anchorMax = new Vector2(Mathf.Clamp01(value), fill.anchorMax.y);
    }

    // Затемнение способности: высота от 0 (готово) до 1 (на перезарядке)
    static void SetHeight(RectTransform overlay, float value)
    {
        if (overlay == null) return;
        overlay.anchorMin = new Vector2(0f, 1f - Mathf.Clamp01(value));
    }

    // Рисуем текстуру виньетки кодом: прозрачный центр, плотные края
    static Sprite MakeVignetteSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x / (size - 1f)) * 2f - 1f;
                float dy = (y / (size - 1f)) * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
