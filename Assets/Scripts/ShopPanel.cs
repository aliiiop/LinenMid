using TMPro;
using UnityEngine;

public enum ItemEffect { MagazineSize, FireRate, MaxHealth, Armor, AbilityCooldown, MaxEnergy }

[System.Serializable]
public class ShopItem
{
    public string name;
    public int cost;
    public ItemEffect effect;
    public float amount;
    public bool bought;
}

public class ShopPanel : MonoBehaviour
{
    public PlayerShooting shooting;
    public Health playerHealth;
    public PlayerAbilities abilities;
    public TMP_Text messageText;

    public ShopItem[] items =
    {
        // Оружие
        new ShopItem { name = "Расширенный магазин (+10 патронов)", cost = 60, effect = ItemEffect.MagazineSize, amount = 10 },
        new ShopItem { name = "Скорострельность (-20% к паузе)", cost = 80, effect = ItemEffect.FireRate, amount = 0.2f },
        // Защита
        new ShopItem { name = "Усиленное сердце (+60 HP)", cost = 60, effect = ItemEffect.MaxHealth, amount = 60 },
        new ShopItem { name = "Пластины (-15% урона)", cost = 80, effect = ItemEffect.Armor, amount = 0.15f },
        // Способности
        new ShopItem { name = "Ускоритель (-25% перезарядки Q)", cost = 70, effect = ItemEffect.AbilityCooldown, amount = 0.25f },
        new ShopItem { name = "Батарея (+50 энергии)", cost = 70, effect = ItemEffect.MaxEnergy, amount = 50 },
    };

    // Вызывается кнопками: в On Click выбираешь Buy(int) и вводишь номер предмета 0-5
    public void Buy(int index)
    {
        ShopItem item = items[index];
        if (item.bought) { Show("Уже куплено"); return; }
        if (!GameManager.Instance.TrySpend(item.cost)) { Show("Не хватает кредитов"); return; }

        item.bought = true;
        switch (item.effect)
        {
            case ItemEffect.MagazineSize:
                shooting.magazineSize += (int)item.amount;
                break;
            case ItemEffect.FireRate:
                shooting.fireRate *= 1f - item.amount;
                break;
            case ItemEffect.MaxHealth:
                playerHealth.IncreaseMax(item.amount);
                break;
            case ItemEffect.Armor:
                playerHealth.armor = Mathf.Min(0.6f, playerHealth.armor + item.amount);
                break;
            case ItemEffect.AbilityCooldown:
                abilities.cooldownMultiplier *= 1f - item.amount;
                break;
            case ItemEffect.MaxEnergy:
                abilities.maxEnergy += item.amount;
                break;
        }
        Show("Куплено: " + item.name);
    }

    public void Close()
    {
        GameManager.Instance.CloseShop();
    }

    void Show(string text)
    {
        if (messageText != null) messageText.text = text;
    }
}
