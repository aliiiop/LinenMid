using UnityEngine;

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
