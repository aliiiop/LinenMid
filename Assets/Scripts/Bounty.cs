using UnityEngine;

// Что происходит, когда объект умирает: награда, счёт, бафф, выпадающий предмет, эффект.
[RequireComponent(typeof(Health))]
public class Bounty : MonoBehaviour
{
    public int credits = 15;
    public GameObject dropPrefab;       // аптечка, энергия и т. д. (по желанию)
    public GameObject deathEffect;      // вспышка при смерти (по желанию)
    public bool grantsPitBuff;          // включить только у Pitmonster
    public bool destroyOnDeath;         // true у миньонов и нейтралов

    Health health;
    Team team;

    void Awake()
    {
        health = GetComponent<Health>();
        team = GetComponent<Team>();
        health.OnDeath += OnKilled;
    }

    void OnKilled()
    {
        GameObject killer = health.LastAttacker;
        bool killedByPlayer = killer != null && killer.CompareTag("Player");

        TeamId killerTeam = TeamId.Neutral;
        if (killer != null)
        {
            Team kt = killer.GetComponent<Team>();
            if (kt != null) killerTeam = kt.id;
        }

        // Ластхит: кредиты получает только тот, кто нанёс последний удар
        if (killedByPlayer && credits > 0)
        {
            GameManager.Instance.AddCredits(credits);
            DamagePopup.Show(transform.position + Vector3.up * 2.2f, "+" + credits, new Color(1f, 0.85f, 0.2f), 1.3f);
            if (team != null && team.kind == UnitKind.Minion)
                GameManager.Instance.RegisterMinionLastHit();
        }

        if (team != null && team.kind == UnitKind.Hero)
            GameManager.Instance.RegisterHeroKill(team, killer);

        if (grantsPitBuff && killerTeam != TeamId.Neutral)
            GameManager.Instance.GrantPitBuff(killerTeam);

        if (dropPrefab != null)
            Instantiate(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        if (deathEffect != null)
            Destroy(Instantiate(deathEffect, transform.position + Vector3.up, Quaternion.identity), 2f);

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}
