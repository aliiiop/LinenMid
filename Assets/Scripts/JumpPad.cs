using UnityEngine;

// Прыжковая платформа: подбрасывает героя через стену джунглей (быстрый обход с фланга).
// Направление полёта — ось "вперёд" (синяя стрелка) объекта платформы.
public class JumpPad : MonoBehaviour
{
    public float upSpeed = 13f;         // скорость вверх
    public float forwardSpeed = 9f;     // скорость вперёд
    public AudioClip launchClip;

    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        Vector3 v = transform.forward * forwardSpeed + Vector3.up * upSpeed;
        pc.Launch(v);
        if (launchClip != null) AudioSource.PlayClipAtPoint(launchClip, transform.position, 0.7f);
    }
}
