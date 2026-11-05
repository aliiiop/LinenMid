using UnityEngine;

// Держит винтовку в руках героя. У модели Y Bot нет своего оружия, а анимации Rifle Idle/Run
// держат "невидимую" винтовку обеими руками. Поэтому каждый кадр ставим ствол в правую руку
// и направляем его на левую руку: винтовка сама следует за анимацией.
public class GunAttach : MonoBehaviour
{
    public Animator animator;           // Animator модели (Humanoid)
    public Transform gun;               // объект винтовки (вперёд = ствол)
    public Vector3 gripOffset = new Vector3(0f, 0.02f, 0.05f);   // сдвиг от правой ладони

    Transform rightHand, leftHand;

    void Start()
    {
        if (animator != null && animator.isHuman)
        {
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        }
    }

    // LateUpdate идёт после анимации, поэтому руки уже в нужной позе
    void LateUpdate()
    {
        if (gun == null || rightHand == null || leftHand == null) return;

        Vector3 forward = leftHand.position - rightHand.position;
        if (forward.sqrMagnitude < 0.0001f) return;

        Quaternion rot = Quaternion.LookRotation(forward.normalized, animator.transform.up);
        gun.rotation = rot;
        gun.position = rightHand.position + rot * gripOffset;
    }
}
