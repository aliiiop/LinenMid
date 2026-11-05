using UnityEngine;

[RequireComponent(typeof(Health))]
public class Bounty : MonoBehaviour
{
    public int credits = 15;
    public GameObject dropPrefab;       // монета, аптечка и т. д. (по желанию)
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
        if (killedByPlayer)
            GameManager.Instance.AddCredits(credits);

        if (team != null && team.kind == UnitKind.Hero)
            GameManager.Instance.RegisterHeroKill(team.id);

        if (grantsPitBuff && killerTeam != TeamId.Neutral)
            GameManager.Instance.GrantPitBuff(killerTeam);

        if (dropPrefab != null)
            Instantiate(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}