using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public bool invulnerable;                   // неуязвимость (щит башен и Ancient)
    [Range(0f, 0.6f)] public float armor;       // 0.2 = получает на 20% меньше урона
    public float regenPerSecond;                // восстановление здоровья в секунду

    public GameObject LastAttacker { get; private set; }   // кто нанёс последний урон
    public event Action OnDeath;                           // событие смерти

    public bool IsDead => currentHealth <= 0f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (!IsDead && regenPerSecond > 0f)
            Heal(regenPerSecond * Time.deltaTime);
    }

    public void TakeDamage(float amount, GameObject attacker = null)
    {
        if (IsDead || invulnerable) return;
        if (attacker != null) LastAttacker = attacker;

        currentHealth = Mathf.Max(0f, currentHealth - amount * (1f - armor));

        if (IsDead)
            OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void IncreaseMax(float amount)
    {
        maxHealth += amount;
        currentHealth += amount;
    }

    public void Revive()
    {
        currentHealth = maxHealth;
        LastAttacker = null;
    }
}