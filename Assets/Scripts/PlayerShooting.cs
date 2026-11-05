using System;
using System.Collections;
using UnityEngine;

// Стрельба игрока: луч из центра экрана, магазин, перезарядка, разброс, отдача, хедшоты.
public class PlayerShooting : MonoBehaviour
{
    [Header("Оружие")]
    public Camera cam;
    public float damage = 14f;
    public float range = 120f;
    public float fireRate = 0.13f;          // секунд между выстрелами
    public int magazineSize = 30;
    public float reloadTime = 1.5f;
    public float headshotMultiplier = 2f;

    [Header("Разброс и отдача")]
    public float baseSpread = 0.004f;       // разброс в долях экрана
    public float spreadPerShot = 0.004f;    // на сколько растёт после выстрела
    public float maxSpread = 0.03f;
    public float spreadRecovery = 0.06f;    // как быстро возвращается в норму (в секунду)
    public float recoilKick = 0.6f;         // подъём камеры за выстрел (градусы)

    [Header("Эффекты ")]
    public Transform muzzle;                // кончик ствола отсюда летит трассер
    public BeamEffect tracer;
    public Light muzzleFlash;
    public AudioSource audioSource;
    public AudioClip shootClip;
    public AudioClip reloadClip;
    public AudioClip hitClip;
    public GameObject hitEffectPrefab;

    public int currentAmmo;
    public bool IsReloading { get; private set; }
    public float ReloadProgress { get; private set; }
    public float CurrentSpread => currentSpread;

    // Событие для интерфейса (хитмаркер): (хедшот?, убил?)
    public event Action<bool, bool> OnHit;

    float nextFireTime;
    float currentSpread;
    float nextShieldHint;
    Team myTeam;
    PlayerController player;

    void Start()
    {
        myTeam = GetComponent<Team>();
        player = GetComponent<PlayerController>();

        if (GameSettings.selectedHero == 0)
        {
            damage += 4f;                   // герой "Стрелок" бьёт сильнее
            magazineSize += 6;              // и у него больше патронов
        }
        currentAmmo = magazineSize;
        currentSpread = baseSpread;
        if (muzzleFlash != null) muzzleFlash.enabled = false;
    }

    void OnDisable()
    {
        IsReloading = false;
        if (muzzleFlash != null) muzzleFlash.enabled = false;
    }

    void Update()
    {
        
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
        if (muzzleFlash != null) StartCoroutine(Flash());

        // Луч из центра экрана со случайным отклонением (разброс)
        Vector2 offset = UnityEngine.Random.insideUnitCircle * currentSpread;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f + offset.x, 0.5f + offset.y, 0f));

        currentSpread = Mathf.Min(maxSpread, currentSpread + spreadPerShot);
        player.AddRecoil(recoilKick);

        // Игнорируем слои Player и Minimap, чтобы не попасть в себя и в иконки
        int mask = ~LayerMask.GetMask("Player", "Minimap");

        Vector3 endPoint = ray.origin + ray.direction * range;
        if (Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            HandleHit(hit);
        }

        if (tracer != null)
            tracer.Show(muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.4f, endPoint);
    }

    void HandleHit(RaycastHit hit)
    {
        Health target = hit.collider.GetComponentInParent<Health>();
        Team targetTeam = hit.collider.GetComponentInParent<Team>();
        bool friendly = targetTeam != null && targetTeam.id == myTeam.id;

        if (target != null && !friendly && !target.IsDead)
        {
            float dmg = damage * GameManager.Instance.PitMultiplier(myTeam.id);

            // Хедшот: попали в верхнюю часть коллайдера живого существа
            bool isUnit = targetTeam != null && !targetTeam.IsBuilding;
            bool headshot = isUnit && hit.point.y > hit.collider.bounds.max.y - 0.35f;
            if (headshot) dmg *= headshotMultiplier;

            float dealt = target.TakeDamage(dmg, gameObject);

            if (target.invulnerable)
            {
                // Попали в здание под щитом: подсказываем, почему нет урона
                if (Time.time > nextShieldHint)
                {
                    GameManager.Instance.ShowMessage("Щит! Сначала разрушь предыдущую башню", 2f);
                    nextShieldHint = Time.time + 3f;
                }
                DamagePopup.Show(hit.point, "щит", new Color(0.6f, 0.8f, 1f), 0.8f);
            }
            else
            {
                // Башня без поддержки миньонов почти не получает урона: подсказываем игроку
                if (target.extraReduction > 0f && Time.time > nextShieldHint)
                {
                    GameManager.Instance.ShowMessage("Башня защищена: приведи к ней своих миньонов", 2f);
                    nextShieldHint = Time.time + 4f;
                }
                Color c = headshot ? new Color(1f, 0.3f, 0.2f) : Color.white;
                DamagePopup.Show(hit.point, Mathf.RoundToInt(dealt).ToString(), c, headshot ? 1.3f : 1f);
                PlaySound(hitClip, 0.35f);
                OnHit?.Invoke(headshot, target.IsDead);
            }
        }

        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(fx, 1f);
        }
    }

    IEnumerator Flash()
    {
        muzzleFlash.enabled = true;
        yield return new WaitForSeconds(0.04f);
        muzzleFlash.enabled = false;
    }

    IEnumerator Reload()
    {
        IsReloading = true;
        PlaySound(reloadClip);
        float t = 0f;
        while (t < reloadTime)
        {
            t += Time.deltaTime;
            ReloadProgress = t / reloadTime;
            yield return null;
        }
        currentAmmo = magazineSize;
        IsReloading = false;
    }

    void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
