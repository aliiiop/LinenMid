using UnityEngine;

public enum PickupType { Credits, Health, Energy }

public class Pickup : MonoBehaviour
{
    public PickupType type = PickupType.Credits;
    public int value = 30;
    public float rotateSpeed = 90f;
    public AudioClip pickupSound;       // звук подбора (по желанию)

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (type == PickupType.Credits) GameManager.Instance.AddCredits(value);
        else if (type == PickupType.Health) other.GetComponent<Health>().Heal(value);
        else other.GetComponent<PlayerAbilities>().AddEnergy(value);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }
}