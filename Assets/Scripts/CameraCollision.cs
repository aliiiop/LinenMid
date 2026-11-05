using UnityEngine;

// Не даёт камере залезать в стены: если между героем и камерой есть препятствие,
// камера придвигается ближе. Скрипт вешается на Main Camera (она лежит внутри CameraPivot).
public class CameraCollision : MonoBehaviour
{
    public float padding = 0.25f;     // отступ от стены
    public float smooth = 12f;        // скорость возврата камеры назад

    Vector3 defaultLocalPos;
    float currentDistance;

    void Start()
    {
        defaultLocalPos = transform.localPosition;
        currentDistance = defaultLocalPos.magnitude;
    }

    void LateUpdate()
    {
        Transform pivot = transform.parent;
        Vector3 dir = pivot.TransformDirection(defaultLocalPos.normalized);
        float maxDistance = defaultLocalPos.magnitude;
        int mask = LayerMask.GetMask("Default");      // только окружение (юниты на слое Units не мешают)

        float target = maxDistance;
        if (Physics.SphereCast(pivot.position, 0.2f, dir, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            target = Mathf.Max(0.4f, hit.distance - padding);

        // К стене прыгаем сразу, назад отъезжаем плавно
        currentDistance = target < currentDistance ? target : Mathf.Lerp(currentDistance, target, smooth * Time.deltaTime);
        transform.localPosition = defaultLocalPos.normalized * currentDistance;
    }
}
