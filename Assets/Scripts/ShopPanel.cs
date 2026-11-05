using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ItemEffect { MagazineSize, FireRate, MaxHealth, Armor, AbilityCooldown, MaxEnergy }

[System.Serializable]
public class ShopItem
{
    public string name;
    public string description;
    public int cost;
    public ItemEffect effect;
    public float amount;
    public bool bought;
}

// Магазин из концепта: слева категории, в центре карточки предметов, справа сборка героя.
// Покупка в одно нажатие. Игра при этом НЕ ставится на паузу.
public class ShopPanel : MonoBehaviour
{
    [Header("Игрок")]
    public PlayerShooting shooting;
    public Health playerHealth;
    public PlayerAbilities abilities;

    [Header("Интерфейс")]
    public Button[] categoryButtons;    // 3 кнопки: Оружие, Защита, Способности
    public Image[] cardBackgrounds;     // 2 карточки на категорию
    public TMP_Text[] cardTitles;
    public TMP_Text[] cardDescriptions;
    public TMP_Text[] cardCosts;
    public Button[] cardButtons;
    public TMP_Text[] cardButtonLabels;
    public TMP_Text buildText;          // правая колонка: что уже куплено
    public TMP_Text creditsText;
    public TMP_Text messageText;

    public static readonly string[] Categories = { "ОРУЖИЕ", "ЗАЩИТА", "СПОСОБНОСТИ" };

    // Порядок: по 2 предмета на категорию
    public ShopItem[] items =
    {
        // Оружие
        new ShopItem { name = "Расширенный магазин", description = "+12 патронов в обойме", cost = 60, effect = ItemEffect.MagazineSize, amount = 12 },
        new ShopItem { name = "Разогнанный затвор", description = "Скорострельность +20%", cost = 90, effect = ItemEffect.FireRate, amount = 0.2f },
        // Защита
        new ShopItem { name = "Усиленное сердце", description = "+80 к максимальному здоровью", cost = 70, effect = ItemEffect.MaxHealth, amount = 80 },
        new ShopItem { name = "Кевларовые пластины", description = "Получаемый урон -15%", cost = 90, effect = ItemEffect.Armor, amount = 0.15f },
        // Способности
        new ShopItem { name = "Ускоритель реактора", description = "Перезарядка Q -30%", cost = 70, effect = ItemEffect.AbilityCooldown, amount = 0.3f },
        new ShopItem { name = "Плазменная батарея", description = "+50 энергии и быстрее восстановление", cost = 70, effect = ItemEffect.MaxEnergy, amount = 50 },
    };

    int category;
    int openedFrame;

    void OnEnable()
    {
        openedFrame = Time.frameCount;      // чтобы то же нажатие E, что открыло магазин, его не закрыло
        if (messageText != null) messageText.text = "";
        Refresh();
    }

    void Update()
    {
        // Повторное нажатие E тоже закрывает магазин
        if (Time.frameCount != openedFrame && Input.GetKeyDown(KeyCode.E)) { Close(); return; }
        Refresh();
    }

    public void SelectCategory(int index)
    {
        category = index;
        Refresh();
    }

    // Кнопка "Купить" на карточке slot (0 или 1) текущей категории
    public void BuyCard(int slot)
    {
        Buy(category * 2 + slot);
    }

    public void Buy(int index)
    {
        ShopItem item = items[index];
        if (item.bought) { Show("Уже куплено"); return; }
        if (!GameManager.Instance.TrySpend(item.cost)) { Show("Не хватает кредитов: добивай миньонов!"); return; }

        item.bought = true;
        switch (item.effect)
        {
            case ItemEffect.MagazineSize:
                shooting.magazineSize += (int)item.amount;
                shooting.currentAmmo = shooting.magazineSize;
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
                abilities.energyRegen *= 1.5f;
                abilities.AddEnergy(item.amount);
                break;
        }
        Show("Куплено: " + item.name);
        Refresh();
    }

    public void Close()
    {
        GameManager.Instance.CloseShop();
    }

    void Refresh()
    {
        int credits = GameManager.Instance.credits;
        if (creditsText != null) creditsText.text = "Кредиты: <color=#FFD126>" + credits + "</color>";

        // Подсветка выбранной категории
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            Image img = categoryButtons[i].GetComponent<Image>();
            img.color = i == category ? new Color(0f, 0.55f, 0.65f, 1f) : new Color(0.06f, 0.1f, 0.16f, 0.95f);
        }

        for (int slot = 0; slot < 2; slot++)
        {
            ShopItem item = items[category * 2 + slot];
            cardTitles[slot].text = item.name;
            cardDescriptions[slot].text = item.description;
            cardCosts[slot].text = item.cost + " кр.";

            bool canAfford = credits >= item.cost;
            cardCosts[slot].color = item.bought ? new Color(0.5f, 0.55f, 0.6f) : (canAfford ? new Color(1f, 0.82f, 0.15f) : new Color(1f, 0.35f, 0.3f));
            cardButtonLabels[slot].text = item.bought ? "КУПЛЕНО" : "КУПИТЬ";
            cardButtons[slot].interactable = !item.bought;
            cardBackgrounds[slot].color = item.bought ? new Color(0.05f, 0.18f, 0.14f, 0.95f) : new Color(0.07f, 0.1f, 0.16f, 0.95f);
        }

        if (buildText != null)
        {
            string s = "";
            foreach (ShopItem it in items)
                if (it.bought) s += "<color=#00E5FF>•</color> " + it.name + "\n";
            buildText.text = s == "" ? "<color=#7A8699>Пока ничего не куплено</color>" : s;
        }
    }

    void Show(string text)
    {
        if (messageText != null) messageText.text = text;
    }
}
