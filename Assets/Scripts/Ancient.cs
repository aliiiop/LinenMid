using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Team))]
public class Ancient : MonoBehaviour
{
    void Start()
    {
        TeamId owner = GetComponent<Team>().id;
        GetComponent<Health>().OnDeath += () => GameManager.Instance.EndMatch(owner);
    }
}
