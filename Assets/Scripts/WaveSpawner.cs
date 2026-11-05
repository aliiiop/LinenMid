using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public TeamId team;
    public GameObject minionPrefab;
    public Material teamMaterial;           // цвет команды (TeamBlue или TeamRed)
    public Transform[] lanePoints;          // точки линии от СИНЕЙ базы к КРАСНОЙ
    public Transform enemyAncient;

    public float firstDelay = 5f;
    public float waveInterval = 25f;
    public int minionsPerWave = 3;
    public float delayBetweenMinions = 1f;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(firstDelay);

        while (true)
        {
            for (int i = 0; i < minionsPerWave; i++)
            {
                SpawnOne();
                yield return new WaitForSeconds(delayBetweenMinions);
            }
            yield return new WaitForSeconds(waveInterval);
        }
    }

    void SpawnOne()
    {
        GameObject minion = Instantiate(minionPrefab, transform.position, transform.rotation);
        minion.GetComponent<Team>().id = team;

        UnitAI ai = minion.GetComponent<UnitAI>();
        ai.waypoints = lanePoints;
        ai.reverseRoute = (team == TeamId.Red);
        ai.finalGoal = enemyAncient;

        // Красим модель в цвет команды
        if (teamMaterial != null)
            foreach (Renderer r in minion.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = teamMaterial;
    }
}