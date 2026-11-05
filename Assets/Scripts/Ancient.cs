using System.Collections;
using UnityEngine;

// Главный реактор базы. Кто разрушит чужой Ancient, тот победил.
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Team))]
public class Ancient : MonoBehaviour
{
    public GameObject destroyEffect;    // взрыв
    public GameObject shieldVisual;     // сфера щита, видна пока живы внутренние башни

    Health health;

    void Start()
    {
        health = GetComponent<Health>();
        TeamId owner = GetComponent<Team>().id;
        health.OnDeath += () => StartCoroutine(Explode(owner));
    }

    void Update()
    {
        if (shieldVisual != null && shieldVisual.activeSelf != health.invulnerable)
            shieldVisual.SetActive(health.invulnerable);
    }

    // Сначала взрыв, через секунду экран победы/поражения
    IEnumerator Explode(TeamId owner)
    {
        if (destroyEffect != null)
        {
            GameObject fx = Instantiate(destroyEffect, transform.position, Quaternion.identity);
            fx.transform.localScale *= 3f;
        }
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            if (r.gameObject.layer != LayerMask.NameToLayer("Minimap")) r.enabled = false;

        yield return new WaitForSeconds(1.2f);
        GameManager.Instance.EndMatch(owner);
    }
}
