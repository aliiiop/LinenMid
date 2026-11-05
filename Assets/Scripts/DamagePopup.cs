using TMPro;
using UnityEngine;

// Всплывающее число над целью (урон, "+15" кредитов). Само поднимается вверх и исчезает.
public class DamagePopup : MonoBehaviour
{
    const float LifeTime = 0.8f;

    TextMeshPro text;
    float age;
    Color baseColor;
    Vector3 drift;

    // Создаёт надпись в мире. Вызывается из любого скрипта: DamagePopup.Show(...)
    public static void Show(Vector3 position, string value, Color color, float size = 1f)
    {
        GameObject go = new GameObject("Popup");
        go.transform.position = position + Random.insideUnitSphere * 0.3f;

        TextMeshPro t = go.AddComponent<TextMeshPro>();
        t.text = value;
        t.fontSize = 6f * size;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(0, 0, 0, 200);

        DamagePopup p = go.AddComponent<DamagePopup>();
        p.text = t;
        p.baseColor = color;
        p.drift = new Vector3(Random.Range(-0.4f, 0.4f), 1.6f, 0f);
    }

    void LateUpdate()
    {
        age += Time.deltaTime;
        transform.position += drift * Time.deltaTime;

        // Повернуть к камере, чтобы текст всегда читался
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;

        Color c = baseColor;
        c.a = 1f - age / LifeTime;
        text.color = c;

        if (age >= LifeTime) Destroy(gameObject);
    }
}
