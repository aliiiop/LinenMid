using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public Slider healthBar;
    public TMP_Text healthText;
    public Slider energyBar;
    public TMP_Text ammoText;
    public TMP_Text creditsText;
    public TMP_Text dashText;
    public TMP_Text abilityText;

    [Header("Верх экрана")]
    public TMP_Text scoreText;
    public TMP_Text nearTowerText;
    public Slider nearTowerBar;

    [Header("Центр экрана")]
    public TMP_Text respawnText;

    void Update()
    {
        GameManager gm = GameManager.Instance;

        healthBar.value = playerHealth.currentHealth / playerHealth.maxHealth;
        healthText.text = Mathf.CeilToInt(playerHealth.currentHealth) + " / " + Mathf.CeilToInt(playerHealth.maxHealth);
        energyBar.value = abilities.energy / abilities.maxEnergy;

        ammoText.text = shooting.IsReloading
            ? "Перезарядка..."
            : shooting.currentAmmo + " / " + shooting.magazineSize;

        creditsText.text = "Кредиты: " + gm.credits;
        dashText.text = "Рывок (Shift): " + player.dashCharges + " / " + player.maxDashCharges;
        abilityText.text = abilities.CooldownLeft > 0f
            ? "Q: " + Mathf.CeilToInt(abilities.CooldownLeft) + " с"
            : "Q: готово";

        int minutes = (int)(gm.matchTime / 60f);
        int seconds = (int)(gm.matchTime % 60f);
        scoreText.text = "Башни " + gm.TowersLost(TeamId.Red) + " : " + gm.TowersLost(TeamId.Blue)
            + "   Убийства " + gm.HeroKills(TeamId.Blue) + " : " + gm.HeroKills(TeamId.Red)
            + "   " + minutes.ToString("00") + ":" + seconds.ToString("00");

        UpdateNearTower();

        bool dead = playerRespawn != null && playerRespawn.RespawnTimeLeft > 0f;
        respawnText.gameObject.SetActive(dead);
        if (dead)
            respawnText.text = "Возрождение через " + Mathf.CeilToInt(playerRespawn.RespawnTimeLeft);
    }

    // Показывает состояние ближайшей вражеской башни (а если все башни разрушены, то Ancient)
    void UpdateNearTower()
    {
        Health nearest = null;
        float best = float.MaxValue;

        foreach (Tower t in Tower.All)
        {
            if (t.TeamInfo.id != TeamId.Red || t.Hp.IsDead) continue;
            float d = Vector3.Distance(player.transform.position, t.transform.position);
            if (d < best)
            {
                best = d;
                nearest = t.Hp;
            }
        }

        bool isAncient = nearest == null;
        if (isAncient) nearest = redAncient;
        if (nearest == null) return;

        nearTowerText.text = isAncient ? "Вражеский Ancient" : "Ближайшая вражеская башня";
        nearTowerBar.value = nearest.currentHealth / nearest.maxHealth;
    }
}
