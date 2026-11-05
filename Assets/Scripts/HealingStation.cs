using UnityEngine;

// Фонтан лечения на базе: E полностью восстанавливает здоровье и энергию, потом перезаряжается.
public class HealingStation : Interactable
{
    public float cooldown = 20f;
    public AudioClip healClip;
    public GameObject healEffect;
    public Light glow;                  // гаснет, пока станция перезаряжается

    float nextUseTime;

    void LateUpdate()
    {
        if (glow != null) glow.enabled = Time.time >= nextUseTime;
    }

    protected override void Interact()
    {
        if (Time.time < nextUseTime)
        {
            GameManager.Instance.ShowPrompt("Станция перезаряжается: " + Mathf.CeilToInt(nextUseTime - Time.time) + " с");
            return;
        }

        Health h = player.GetComponent<Health>();
        h.Heal(h.maxHealth);
        PlayerAbilities a = player.GetComponent<PlayerAbilities>();
        if (a != null) a.AddEnergy(a.maxEnergy);

        nextUseTime = Time.time + cooldown;
        if (healClip != null) AudioSource.PlayClipAtPoint(healClip, transform.position);
        if (healEffect != null) Destroy(Instantiate(healEffect, player.transform.position, Quaternion.identity), 2f);
        GameManager.Instance.ShowPrompt("Здоровье и энергия восстановлены!");
    }
}
