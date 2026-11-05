using System.Collections;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Оружие")]
    public Camera cam;
    public float damage = 20f;
    public float range = 120f;
    public float fireRate = 0.15f;          // секунд между выстрелами
    public int magazineSize = 30;
    public float reloadTime = 1.5f;
    public float headshotMultiplier = 2f;

    [Header("Разброс и отдача")]
    public float baseSpread = 0.004f;       // разброс в долях экрана
    public float spreadPerShot = 0.004f;    // на сколько растёт после выстрела
    public float maxSpread = 0.03f;
    public float spreadRecovery = 0.06f;    // как быстро возвращается в норму (в секунду)
    public float recoilKick = 0.7f;         // подъём камеры за выстрел (градусы)

    [Header("Эффекты (можно оставить пустыми)")]
    public AudioSource audioSource;
    public AudioClip shootClip;
    public AudioClip reloadClip;
    public AudioClip hitClip;
    public GameObject hitEffectPrefab;

    public int currentAmmo;
    public bool IsReloading { get; private set; }

    float nextFireTime;
    float currentSpread;
    Team myTeam;
    PlayerController player;

    void Start()
    {
        currentAmmo = magazineSize;
        currentSpread = baseSpread;
        myTeam = GetComponent<Team>();
        player = GetComponent<PlayerController>();

        if (GameSettings.selectedHero == 0)
            damage += 5f;                   // герой "Стрелок" бьёт сильнее
    }

    void Update()
    {
        // Разброс постепенно сходит на нет, пока не стреляешь
        currentSpread = Mathf.Max(baseSpread, currentSpread - spreadRecovery * Time.deltaTime);

        if (GameManager.Instance.InputBlocked) return;
        if (IsReloading) return;

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineSize)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (currentAmmo <= 0)
            {
                StartCoroutine(Reload());
                return;
            }
            Shoot();
        }
    }

    void Shoot()
    {
        nextFireTime = Time.time + fireRate;
        currentAmmo--;
        PlaySound(shootClip);

        // Луч из центра экрана со случайным отклонением (разброс)
        Vector2 offset = Random.insideUnitCircle * currentSpread;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f + offset.x, 0.5f + offset.y, 0f));

        currentSpread = Mathf.Min(maxSpread, currentSpread + spreadPerShot);
        player.AddRecoil(recoilKick);

        // Игнорируем слои Player и Minimap, чтобы не попасть в себя и в иконки
        int mask = ~LayerMask.GetMask("Player", "Minimap");

        if (!Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
            return;

        Health target = hit.collider.GetComponentInParent<Health>();
        Team targetTeam = hit.collider.GetComponentInParent<Team>();
        bool friendly = targetTeam != null && targetTeam.id == myTeam.id;

        if (target != null && !friendly && !target.IsDead)
        {
            float dmg = damage * GameManager.Instance.PitMultiplier(TeamId.Blue);

            // Хедшот: попали в верхнюю часть коллайдера живого существа
            bool isUnit = targetTeam != null && targetTeam.kind != UnitKind.Tower && targetTeam.kind != UnitKind.Ancient;
            if (isUnit && hit.point.y > hit.collider.bounds.max.y - 0.3f)
                dmg *= headshotMultiplier;

            target.TakeDamage(dmg, gameObject);
            PlaySound(hitClip);
        }

        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(fx, 1f);
        }
    }

    IEnumerator Reload()
    {
        IsReloading = true;
        PlaySound(reloadClip);
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = magazineSize;
        IsReloading = false;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}