using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class Respawner : MonoBehaviour
{
    public Transform respawnPoint;
    public float baseTime = 6f;
    public float extraPerDeath = 4f;            // каждая смерть удлиняет возрождение
    public GameObject[] hideWhileDead;          // модель и иконка, которые прячем

    public float RespawnTimeLeft { get; private set; }
    public int Deaths { get; private set; }

    Health health;
    Collider body;
    PlayerController playerController;
    PlayerShooting playerShooting;
    PlayerAbilities playerAbilities;
    UnitAI unitAI;
    NavMeshAgent agent;

    void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Collider>();        // CharacterController у игрока, CapsuleCollider у ботов
        playerController = GetComponent<PlayerController>();
        playerShooting = GetComponent<PlayerShooting>();
        playerAbilities = GetComponent<PlayerAbilities>();
        unitAI = GetComponent<UnitAI>();
        agent = GetComponent<NavMeshAgent>();
        health.OnDeath += OnDied;
    }

    void OnDied()
    {
        Deaths++;
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        SetAlive(false);
        RespawnTimeLeft = baseTime + extraPerDeath * (Deaths - 1);

        while (RespawnTimeLeft > 0f)
        {
            RespawnTimeLeft -= Time.deltaTime;
            yield return null;
        }
        RespawnTimeLeft = 0f;

        // Сначала телепорт (компоненты ещё выключены), потом включаем всё обратно
        transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
        health.Revive();
        if (unitAI != null) unitAI.ResetState();
        SetAlive(true);
    }

    void SetAlive(bool alive)
    {
        foreach (GameObject g in hideWhileDead)
            if (g != null) g.SetActive(alive);

        if (body != null) body.enabled = alive;
        if (playerController != null) playerController.enabled = alive;
        if (playerShooting != null) playerShooting.enabled = alive;
        if (playerAbilities != null) playerAbilities.enabled = alive;
        if (unitAI != null) unitAI.enabled = alive;
        if (agent != null) agent.enabled = alive;
    }
}
