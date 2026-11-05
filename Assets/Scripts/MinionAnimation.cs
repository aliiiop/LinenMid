using UnityEngine;

// Процедурные анимации миньонов и монстров поверх клипов Idle / Zombie Walk:
//  - удар когтями (замах -> удар -> возврат), вызывает UnitAI при атаке;
//  - вздрагивание при попадании;
//  - падение назад при смерти.
// Клипов атаки и смерти для Mutant в проекте нет, поэтому кости поворачиваются кодом.
// Всё делается в LateUpdate: он идёт ПОСЛЕ Animator, и наши повороты добавляются к позе из клипа.
[RequireComponent(typeof(Health))]
public class MinionAnimation : MonoBehaviour
{
    public Animator animator;               // Animator модели (Humanoid)
    public float attackDuration = 0.5f;
    public float deathDuration = 0.7f;

    Transform model;
    Transform rightArm, rightForearm, leftArm, spine, chest, head;
    Health health;

    float attackTime = -1f;     // -1 = анимация не идёт
    float hitTime = -1f;
    float deathTime = -1f;
    Vector3 modelStartPos;

    void Awake()
    {
        health = GetComponent<Health>();
        health.OnDamaged += (amount, attacker) => { if (amount > 0f && !health.IsDead) hitTime = 0f; };
        health.OnDeath += () => deathTime = 0f;
    }

    void Start()
    {
        if (animator == null || !animator.isHuman) return;
        model = animator.transform;
        modelStartPos = model.localPosition;
        rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        head = animator.GetBoneTransform(HumanBodyBones.Head);
    }

    // Вызывает UnitAI в момент удара
    public void PlayAttack()
    {
        attackTime = 0f;
    }

    void LateUpdate()
    {
        if (model == null) return;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        // ----- Смерть: модель падает назад и немного проседает -----
        if (deathTime >= 0f)
        {
            deathTime += Time.deltaTime;
            float t = Mathf.Clamp01(deathTime / deathDuration);
            float fall = t * t;                                 // ускоряется, как при падении
            model.localRotation = Quaternion.Euler(-85f * fall, 0f, 0f);
            model.localPosition = modelStartPos + Vector3.down * 0.25f * fall;
            if (animator.enabled && t >= 1f) animator.enabled = false;   // поза замирает на земле
            return;
        }

        // ----- Удар: замах назад, резкий удар вперёд, возврат -----
        if (attackTime >= 0f)
        {
            attackTime += Time.deltaTime;
            float t = attackTime / attackDuration;
            float swing;            // угол руки: + назад, - вперёд
            if (t < 0.3f) swing = Mathf.Lerp(0f, 40f, t / 0.3f);                    // замах
            else if (t < 0.5f) swing = Mathf.Lerp(40f, -95f, (t - 0.3f) / 0.2f);    // удар
            else swing = Mathf.Lerp(-95f, 0f, (t - 0.5f) / 0.5f);                   // возврат
            float twist = swing * 0.25f;                                            // корпус доворачивается за рукой

            Rotate(rightArm, right, swing);
            Rotate(rightForearm, right, Mathf.Min(0f, swing) * 0.4f);
            Rotate(leftArm, right, -swing * 0.3f);
            Rotate(spine, up, -twist);
            Rotate(chest, right, Mathf.Min(0f, swing) * -0.15f);                    // наклон вперёд при ударе

            if (t >= 1f) attackTime = -1f;
        }

        // ----- Попадание: короткое вздрагивание назад -----
        if (hitTime >= 0f)
        {
            hitTime += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(hitTime / 0.2f) * Mathf.PI);       // 0 -> 1 -> 0
            Rotate(chest, right, -12f * k);
            Rotate(head, right, -10f * k);
            if (hitTime >= 0.2f) hitTime = -1f;
        }
    }

    // Поворот кости вокруг оси в мировых координатах (поверх позы из клипа)
    static void Rotate(Transform bone, Vector3 axis, float degrees)
    {
        if (bone != null) bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;
    }
}
