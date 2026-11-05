using UnityEngine;

// Камера главного меню медленно облетает героя на подиуме.
public class MenuOrbit : MonoBehaviour
{
    public Transform target;
    public float distance = 5.5f;
    public float height = 1.6f;
    public float speed = 8f;            // градусов в секунду
    public Vector3 lookOffset = new Vector3(0f, 1.1f, 0f);

    float angle = 200f;

    void LateUpdate()
    {
        if (target == null) return;
        angle += speed * Time.deltaTime;
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, -distance);
        transform.position = target.position + offset + Vector3.up * height;
        transform.LookAt(target.position + lookOffset);
    }
}
