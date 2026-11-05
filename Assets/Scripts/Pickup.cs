using System.Collections;
using UnityEngine;

public enum PickupType { Credits, Health, Energy }

// Собираемый предмет: аптечка, энергоячейка или кредиты.
// Если respawnTime > 0, предмет не удаляется, а появляется снова через это время.
public class Pickup : MonoBehaviour
{
    public PickupType type = PickupType.Health;
    public int value = 30;
    public float rotateSpeed = 90f;
    public float respawnTime = 0f;      // 0 = одноразовый (выпал из монстра)
    public AudioClip pickupSound;       // звук подбора (по желанию)

    Vector3 startPos;
    bool active = true;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Вращается и покачивается, чтобы было видно издалека
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.15f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!active || !other.CompareTag("Player")) return;

        string text;
        Color color;
        if (type == PickupType.Credits)
        {
            GameManager.Instance.AddCredits(value);
            text = "+" + value + " кредитов"; color = new Color(1f, 0.85f, 0.2f);
        }
        else if (type == PickupType.Health)
        {
            other.GetComponent<Health>().Heal(value);
            text = "+" + value + " HP"; color = new Color(0.3f, 1f, 0.4f);
        }
        else
        {
            other.GetComponent<PlayerAbilities>().AddEnergy(value);
            text = "+" + value + " энергии"; color = new Color(0.3f, 0.6f, 1f);
        }
        DamagePopup.Show(transform.position + Vector3.up, text, color, 1f);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        if (respawnTime > 0f) StartCoroutine(Respawn());
        else Destroy(gameObject);
    }

    IEnumerator Respawn()
    {
        SetVisible(false);
        yield return new WaitForSeconds(respawnTime);
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        active = visible;
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = visible;
        foreach (Light l in GetComponentsInChildren<Light>()) l.enabled = visible;
    }
}
