using UnityEngine;

// Движение героя от третьего лица: бег, прыжок, рывок, поворот камеры мышью.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 6f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;

    [Header("Камера")]
    public Transform cameraPivot;
    public float minPitch = -35f;   // наклон вверх
    public float maxPitch = 60f;    // наклон вниз

    [Header("Рывок")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.15f;
    public int maxDashCharges = 2;
    public float dashRechargeTime = 4f;
    public int dashCharges;
    public AudioClip dashClip;

    public float DashRecharge01 => dashCharges >= maxDashCharges ? 1f : rechargeTimer / dashRechargeTime;

    CharacterController controller;
    Animator animator;
    AudioSource audioSource;
    float verticalVelocity;
    Vector3 launchVelocity;          // толчок от прыжковой платформы
    float pitch;
    float dashTimeLeft;
    Vector3 dashDirection;
    float rechargeTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Герой "Бегун" из меню выбора: быстрее и 3 рывка
        if (GameSettings.selectedHero == 1)
        {
            moveSpeed *= 1.2f;
            maxDashCharges = 3;
            dashRechargeTime = 3f;
        }
        dashCharges = maxDashCharges;
    }

    void OnEnable()
    {
        // После возрождения сбрасываем скорость падения и толчки
        verticalVelocity = 0f;
        launchVelocity = Vector3.zero;
        dashTimeLeft = 0f;
    }

    void Update()
    {
        bool blocked = GameManager.Instance.InputBlocked;

        if (!blocked) Look();
        Move(blocked);
        RechargeDash();
    }

    void Look()
    {
        float sens = GameSettings.Sensitivity;
        float mouseX = Input.GetAxis("Mouse X") * sens;
        float mouseY = Input.GetAxis("Mouse Y") * sens;

        transform.Rotate(0f, mouseX, 0f);                 // влево-вправо: весь герой

        pitch -= mouseY;                                  // вверх-вниз: только камера
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move(bool blocked)
    {
        Vector3 input = Vector3.zero;
        if (!blocked)
        {
            input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);
        }
        Vector3 move = transform.TransformDirection(input);   // из локальных координат в мировые

        // Рывок
        if (!blocked && Input.GetKeyDown(KeyCode.LeftShift) && dashCharges > 0 && dashTimeLeft <= 0f)
        {
            dashCharges--;
            dashTimeLeft = dashDuration;
            dashDirection = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward;
            if (audioSource != null && dashClip != null) audioSource.PlayOneShot(dashClip, 0.5f);
        }

        // Гравитация и прыжок
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            launchVelocity = Vector3.zero;                // приземлились: толчок закончился
        }

        if (!blocked && Input.GetButtonDown("Jump") && controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        // Горизонтальная скорость: обычная или рывок
        Vector3 horizontal;
        float animSpeed;
        if (dashTimeLeft > 0f)
        {
            dashTimeLeft -= Time.deltaTime;
            horizontal = dashDirection * dashSpeed;
            animSpeed = 1f;
        }
        else
        {
            horizontal = move * moveSpeed;
            animSpeed = input.magnitude;
        }

        controller.Move((horizontal + launchVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (animator != null)
            animator.SetFloat("Speed", animSpeed);
    }

    void RechargeDash()
    {
        if (dashCharges >= maxDashCharges)
        {
            rechargeTimer = 0f;
            return;
        }
        rechargeTimer += Time.deltaTime;
        if (rechargeTimer >= dashRechargeTime)
        {
            dashCharges++;
            rechargeTimer = 0f;
        }
    }

    // Отдача оружия: приподнимает камеру (вызывает PlayerShooting)
    public void AddRecoil(float degrees)
    {
        pitch -= degrees;
    }

    // Прыжковая платформа подбрасывает героя (вызывает JumpPad)
    public void Launch(Vector3 velocity)
    {
        verticalVelocity = velocity.y;
        launchVelocity = new Vector3(velocity.x, 0f, velocity.z);
    }
}
