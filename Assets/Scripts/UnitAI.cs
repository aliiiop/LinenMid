using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Team))]
public class UnitAI : MonoBehaviour
{
    [Header("Маршрут")]
    public Transform[] waypoints;       // точки линии по порядку
    public bool reverseRoute;           // true для красных: идти от красной базы к синей
    public Transform finalGoal;         // вражеский Ancient

    [Header("Бой")]
    public float aggroRange = 10f;      // на каком расстоянии замечает врага
    public float attackRange = 2f;
    public float damage = 8f;
    public float attackCooldown = 1.2f;
    public BeamEffect beam;             // луч выстрела для дальнобойных (по желанию)

    [Header("Дом, отступление, привязка (для героев и нейтралов)")]
    public Transform home;
    public float retreatBelow = 0f;     // 0.3 = отступать, когда осталось меньше 30% здоровья
    public float leashDistance = 0f;    // для нейтралов: как далеко можно уйти от дома

    Team team;
    Health health;
    NavMeshAgent agent;
    Animator animator;

    int waypointIndex;
    float nextAttackTime;
    float nextScanTime;
    Health target;
    Team targetTeam;
    bool retreating;

    void Awake()
    {
        team = GetComponent<Team>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (reverseRoute && waypoints != null && waypoints.Length > 0)
        {
            waypoints = (Transform[])waypoints.Clone();   // копия, чтобы не портить общий массив
            System.Array.Reverse(waypoints);
        }
    }

    void Update()
    {
        if (health.IsDead) return;

        // Поиск цели не каждый кадр, а 4 раза в секунду (экономит ресурсы)
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + 0.25f;
            ScanForTarget();
        }

        // Нейтралы не уходят далеко от дома
        if (leashDistance > 0f && home != null &&
            Vector3.Distance(transform.position, home.position) > leashDistance)
            target = null;

        UpdateRetreat();

        if (retreating) GoTo(home.position);
        else if (target != null) Fight();
        else Advance();

        if (animator != null)
            animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    // ---------- Поиск цели ----------
    void ScanForTarget()
    {
        Health best = null;
        Team bestTeam = null;
        float bestScore = float.MaxValue;

        foreach (Collider c in Physics.OverlapSphere(transform.position, aggroRange))
        {
            Team t = c.GetComponentInParent<Team>();
            Health h = c.GetComponentInParent<Health>();
            if (t == null || h == null || h.IsDead || !team.IsEnemyOf(t)) continue;

            float score = Vector3.Distance(transform.position, c.transform.position);
            if (t.kind == UnitKind.Tower || t.kind == UnitKind.Ancient)
                score += 50f;       // сначала живые цели, здания потом

            if (score < bestScore)
            {
                bestScore = score;
                best = h;
                bestTeam = t;
            }
        }
        target = best;
        targetTeam = bestTeam;
    }

    // ---------- Бой ----------
    void Fight()
    {
        if (target == null || target.IsDead)
        {
            target = null;
            return;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude - targetTeam.radius;   // считаем до края цели

        if (distance <= attackRange)
        {
            agent.isStopped = true;
            if (toTarget != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(toTarget);

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                Attack();
            }
        }
        else
        {
            GoTo(target.transform.position);
        }
    }

    void Attack()
    {
        float power = GameManager.Instance.PitMultiplier(team.id);
        if (team.kind == UnitKind.Minion)
            power *= GameManager.Instance.MinionPower(team.id);

        target.TakeDamage(damage * power, gameObject);

        if (beam != null)
            beam.Show(transform.position + Vector3.up * 1.4f, target.transform.position + Vector3.up);
    }

    // ---------- Движение по линии ----------
    void Advance()
    {
        if (waypoints != null && waypointIndex < waypoints.Length)
        {
            Vector3 point = waypoints[waypointIndex].position;
            GoTo(point);

            Vector3 d = point - transform.position;
            d.y = 0f;
            if (d.magnitude < 3f) waypointIndex++;
        }
        else if (finalGoal != null) GoTo(finalGoal.position);
        else if (home != null) GoTo(home.position);
    }

    void GoTo(Vector3 position)
    {
        agent.isStopped = false;
        agent.SetDestination(position);
    }

    // ---------- Отступление к базе (для героев) ----------
    void UpdateRetreat()
    {
        if (retreatBelow <= 0f || home == null) return;

        float percent = health.currentHealth / health.maxHealth;
        if (!retreating && percent < retreatBelow) retreating = true;

        if (retreating && Vector3.Distance(transform.position, home.position) < 6f)
        {
            health.Heal(health.maxHealth * 0.25f * Time.deltaTime);   // лечение у дома
            if (percent > 0.9f) retreating = false;
        }
    }

    // Вызывает Respawner после возрождения
    public void ResetState()
    {
        waypointIndex = 0;
        target = null;
        retreating = false;
    }
}