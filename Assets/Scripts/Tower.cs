using System.Collections.Generic;
using UnityEngine;

// Защитная башня: стреляет лучом по врагам рядом (миньоны в приоритете).
// Правило из концепта: башню нельзя сломать "как попало".
//   1) внутренняя башня под щитом, пока жива внешняя (это делает скрипт Shield);
//   2) если рядом нет вражеских миньонов, башня получает на 70% меньше урона.
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Team))]
public class Tower : MonoBehaviour
{
    public static readonly List<Tower> All = new List<Tower>();   // все башни (для интерфейса)

    public float range = 18f;
    public float damage = 25f;
    public float fireInterval = 1.5f;
    [Range(0f, 0.9f)] public float noMinionReduction = 0.7f;
    public Transform muzzle;            // откуда вылетает луч (можно пустым)
    public BeamEffect beam;             // визуальный луч (можно пустым)
    public GameObject shieldVisual;     // полупрозрачная сфера, видна пока башня под щитом
    public GameObject destroyEffect;    // взрыв при разрушении

    public Health Hp { get; private set; }
    public Team TeamInfo { get; private set; }
    public bool HasMinionSupport { get; private set; }   // есть ли рядом вражеские миньоны

    float nextShotTime;
    float nextScanTime;
    Health target;

    void Awake()
    {
        Hp = GetComponent<Health>();
        TeamInfo = GetComponent<Team>();
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        Hp.OnDeath += OnDestroyed;
    }

    void OnDestroyed()
    {
        if (destroyEffect != null)
            Destroy(Instantiate(destroyEffect, transform.position + Vector3.up * 3f, Quaternion.identity), 3f);
        GameManager.Instance.RegisterTowerLost(TeamInfo.id);
        Destroy(gameObject);
    }

    void Update()
    {
        if (Hp.IsDead) return;

        if (shieldVisual != null && shieldVisual.activeSelf != Hp.invulnerable)
            shieldVisual.SetActive(Hp.invulnerable);

        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + 0.3f;
            target = FindTarget();
            Hp.extraReduction = HasMinionSupport ? 0f : noMinionReduction;
        }

        if (target == null || target.IsDead || !target.gameObject.activeInHierarchy) return;
        if (Time.time < nextShotTime) return;

        nextShotTime = Time.time + fireInterval;
        target.TakeDamage(damage, gameObject);

        if (beam != null)
        {
            Vector3 from = muzzle != null ? muzzle.position : transform.position + Vector3.up * 3f;
            beam.Show(from, target.transform.position + Vector3.up);
        }
    }

    Health FindTarget()
    {
        Health best = null;
        float bestScore = float.MaxValue;
        HasMinionSupport = false;

        foreach (Collider c in Physics.OverlapSphere(transform.position, range))
        {
            Team t = c.GetComponentInParent<Team>();
            Health h = c.GetComponentInParent<Health>();
            if (t == null || h == null || h.IsDead || !TeamInfo.IsEnemyOf(t)) continue;

            float score = Vector3.Distance(transform.position, c.transform.position);
            if (t.kind == UnitKind.Minion)
            {
                score -= 100f;                  // миньоны в приоритете
                HasMinionSupport = true;
            }
            else if (t.kind != UnitKind.Hero) continue;            // здания и нейтралы не цели

            if (score < bestScore)
            {
                bestScore = score;
                best = h;
            }
        }
        return best;
    }
}
