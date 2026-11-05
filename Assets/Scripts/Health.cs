using System;
using UnityEngine;

// Здоровье для всех: игрок, боты, миньоны, нейтралы, башни, Ancient.
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public bool invulnerable;                   // неуязвимость (щит башен и Ancient)
    [Range(0f, 0.6f)] public float armor;       // 0.2 = получает на 20% меньше урона (предметы магазина)
    public float extraReduction;                // доп. снижение урона, которое ставят скрипты (башня без миньонов)
    public float regenPerSecond;                // восстановление здоровья в секунду

    public GameObject LastAttacker { get; private set; }   // кто нанёс последний урон
    public event Action OnDeath;                           // событие смерти
    public event Action<float, GameObject> OnDamaged;      // сколько урона прошло и от кого

    public bool IsDead => currentHealth <= 0f;
    public float Percent => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (!IsDead && regenPerSecond > 0f)
            Heal(regenPerSecond * Time.deltaTime);
    }

    // Возвращает, сколько урона реально прошло (0, если цель под щитом)
    public float TakeDamage(float amount, GameObject attacker = null)
    {
        if (IsDead || invulnerable) return 0f;
        if (attacker != null) LastAttacker = attacker;

        float reduction = Mathf.Clamp01(armor + extraReduction);
        float dealt = Mathf.Min(currentHealth, amount * (1f - reduction));
        currentHealth -= dealt;

        OnDamaged?.Invoke(dealt, attacker);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDeath?.Invoke();
        }
        return dealt;
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
