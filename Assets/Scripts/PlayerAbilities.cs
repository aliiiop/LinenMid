using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilities : MonoBehaviour
{
    [Header("Энергия")]
    public float maxEnergy = 100f;
    public float energy;
    public float energyRegen = 5f;

    [Header("Способность Q: энергетический взрыв")]
    public float burstCost = 30f;
    public float burstCooldown = 8f;
    public float burstRadius = 7f;
    public float burstDamage = 60f;
    public float cooldownMultiplier = 1f;       // магазин уменьшает это значение
    public GameObject burstEffectPrefab;        // по желанию

    public float CooldownLeft { get; private set; }

    Team team;

    void Start()
    {
        team = GetComponent<Team>();
        energy = maxEnergy;
    }

    void Update()
    {
        energy = Mathf.Min(maxEnergy, energy + energyRegen * Time.deltaTime);
        if (CooldownLeft > 0f) CooldownLeft -= Time.deltaTime;

        if (GameManager.Instance.InputBlocked) return;

        if (Input.GetKeyDown(KeyCode.Q) && CooldownLeft <= 0f && energy >= burstCost)
            Burst();
    }

    void Burst()
    {
        energy -= burstCost;
        CooldownLeft = burstCooldown * cooldownMultiplier;

        if (burstEffectPrefab != null)
            Destroy(Instantiate(burstEffectPrefab, transform.position, Quaternion.identity), 2f);

        // Один и тот же враг может иметь несколько коллайдеров, поэтому запоминаем, кого уже ударили
        HashSet<Health> alreadyHit = new HashSet<Health>();

        foreach (Collider c in Physics.OverlapSphere(transform.position, burstRadius))
        {
            Health h = c.GetComponentInParent<Health>();
            Team t = c.GetComponentInParent<Team>();
            if (h == null || h.IsDead || alreadyHit.Contains(h)) continue;
            if (t != null && t.id == team.id) continue;      // своих не бьём

            alreadyHit.Add(h);
            h.TakeDamage(burstDamage * GameManager.Instance.PitMultiplier(team.id), gameObject);
        }
    }

    public void AddEnergy(float amount)
    {
        energy = Mathf.Min(maxEnergy, energy + amount);
    }
}