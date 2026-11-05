using System.Collections.Generic;
using UnityEngine;

// Способность Q "Энергетический взрыв": урон всем врагам вокруг за энергию.
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
    public AudioClip burstClip;

    public float CooldownLeft { get; private set; }
    public float CooldownTotal => burstCooldown * cooldownMultiplier;
    public bool CanUse => CooldownLeft <= 0f && energy >= burstCost;

    Team team;
    AudioSource audioSource;

    void Start()
    {
        team = GetComponent<Team>();
        audioSource = GetComponent<AudioSource>();
        energy = maxEnergy;
    }

    void Update()
    {
        energy = Mathf.Min(maxEnergy, energy + energyRegen * Time.deltaTime);
        if (CooldownLeft > 0f) CooldownLeft -= Time.deltaTime;

        if (GameManager.Instance.InputBlocked) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (CanUse) Burst();
            else if (energy < burstCost) GameManager.Instance.ShowMessage("Не хватает энергии", 1.5f);
        }
    }

    void Burst()
    {
        energy -= burstCost;
        CooldownLeft = CooldownTotal;

        if (burstEffectPrefab != null)
            Destroy(Instantiate(burstEffectPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity), 2f);
        if (audioSource != null && burstClip != null)
            audioSource.PlayOneShot(burstClip, 0.8f);

        // Один и тот же враг может иметь несколько коллайдеров, поэтому запоминаем, кого уже ударили
        HashSet<Health> alreadyHit = new HashSet<Health>();

        foreach (Collider c in Physics.OverlapSphere(transform.position, burstRadius))
        {
            Health h = c.GetComponentInParent<Health>();
            Team t = c.GetComponentInParent<Team>();
            if (h == null || h.IsDead || alreadyHit.Contains(h)) continue;
            if (t != null && t.id == team.id) continue;      // своих не бьём

            alreadyHit.Add(h);
            float dealt = h.TakeDamage(burstDamage * GameManager.Instance.PitMultiplier(team.id), gameObject);
            if (dealt > 0f)
                DamagePopup.Show(h.transform.position + Vector3.up * 2f, Mathf.RoundToInt(dealt).ToString(), new Color(0.4f, 0.8f, 1f), 1.2f);
        }
    }

    public void AddEnergy(float amount)
    {
        energy = Mathf.Min(maxEnergy, energy + amount);
    }
}
