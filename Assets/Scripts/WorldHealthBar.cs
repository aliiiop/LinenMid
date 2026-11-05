using UnityEngine;
using UnityEngine.UI;

// Полоска здоровья над головой (миньоны, боты, нейтралы, башни).
// Создаётся кодом при старте: маленький Canvas в мире, фон и заполнение.
// Цвет: союзники бирюзовые, враги оранжевые, нейтралы жёлтые.
[RequireComponent(typeof(Health))]
public class WorldHealthBar : MonoBehaviour
{
    public float height = 2.4f;             // на какой высоте над объектом
    public float width = 1.2f;              // ширина в метрах
    public bool onlyWhenDamaged;            // миньонам показываем только после первого урона

    Health health;
    Transform barRoot;
    RectTransform fill;
    RectTransform shieldMark;
    Image fillImage;
    float shownPercent = 1f;

    void Start()
    {
        health = GetComponent<Health>();
        Team team = GetComponent<Team>();
        Build(team != null ? Team.ColorOf(team.id) : Color.white);
    }

    void Build(Color color)
    {
        GameObject root = new GameObject("HealthBar", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.up * height;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 12f);
        // 100 единиц UI = width метров
        root.transform.localScale = Vector3.one * (width / 100f) / Mathf.Max(0.01f, transform.lossyScale.x);
        barRoot = root.transform;

        Image bg = MakeImage("Bg", barRoot, new Color(0f, 0f, 0f, 0.75f));
        Stretch(bg.rectTransform, 0f);

        fillImage = MakeImage("Fill", barRoot, color);
        fill = fillImage.rectTransform;
        Stretch(fill, 1.5f);

        // Голубая рамка, когда объект под щитом
        Image shield = MakeImage("Shield", barRoot, new Color(0.5f, 0.8f, 1f, 0.9f));
        shieldMark = shield.rectTransform;
        Stretch(shieldMark, -2f);
        shieldMark.SetAsFirstSibling();
    }

    void LateUpdate()
    {
        if (barRoot == null) return;

        bool visible = !health.IsDead && (!onlyWhenDamaged || health.currentHealth < health.maxHealth);
        if (barRoot.gameObject.activeSelf != visible) barRoot.gameObject.SetActive(visible);
        if (!visible) return;

        // Плавно догоняем реальное здоровье
        shownPercent = Mathf.MoveTowards(shownPercent, health.Percent, 2f * Time.deltaTime);
        if (health.Percent > shownPercent) shownPercent = health.Percent;
        fill.anchorMax = new Vector2(shownPercent, 1f);

        shieldMark.gameObject.SetActive(health.invulnerable);

        // Полоска всегда смотрит в камеру
        Camera cam = Camera.main;
        if (cam != null) barRoot.rotation = cam.transform.rotation;
    }

    static Image MakeImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
