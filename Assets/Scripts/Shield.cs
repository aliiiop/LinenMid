using UnityEngine;

// Щит: объект неуязвим, пока жив хотя бы один из его "защитников".
// Так внутренняя башня защищена внешней, а Ancient — внутренними башнями.
[RequireComponent(typeof(Health))]
public class Shield : MonoBehaviour
{
    public Health[] guardians;      // кто защищает этот объект

    Health health;

    void Awake()
    {
        health = GetComponent<Health>();
    }

    void Update()
    {
        bool anyAlive = false;
        foreach (Health g in guardians)
        {
            // Уничтоженная башня (Destroy) превращается в null
            if (g != null && !g.IsDead)
            {
                anyAlive = true;
                break;
            }
        }
        health.invulnerable = anyAlive;
    }
}
