using UnityEngine;

// Просто вращает объект (кольца реактора, кристаллы башен, неоновые вывески).
public class Rotator : MonoBehaviour
{
    public Vector3 degreesPerSecond = new Vector3(0f, 45f, 0f);
    public float bobAmplitude = 0f;     // покачивание вверх-вниз
    public float bobSpeed = 1.5f;

    Vector3 startLocalPos;

    void Start()
    {
        startLocalPos = transform.localPosition;
    }

    void Update()
    {
        transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        if (bobAmplitude > 0f)
            transform.localPosition = startLocalPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
    }
}
