using UnityEngine;

public class HealingStation : Interactable
{
    public float cooldown = 30f;
    float nextUseTime;

    protected override void Interact()
    {
        if (Time.time < nextUseTime)
        {
            GameManager.Instance.ShowPrompt("Станция перезаряжается...");
            return;
        }

        Health h = player.GetComponent<Health>();
        h.Heal(h.maxHealth);
        nextUseTime = Time.time + cooldown;
        GameManager.Instance.ShowPrompt("Здоровье восстановлено!");
    }
}
