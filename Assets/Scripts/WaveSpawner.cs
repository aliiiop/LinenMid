using System.Collections;
using UnityEngine;

// Выпускает волну миньонов каждые waveInterval секунд.
// Чем больше башен потеряла вражеская команда, тем сильнее (больше HP и урона) наши миньоны.
public class WaveSpawner : MonoBehaviour
{
    public TeamId team;
    public GameObject minionPrefab;
    public Transform[] lanePoints;          // точки линии от СИНЕЙ базы к КРАСНОЙ
    public Transform enemyAncient;

    public float firstDelay = 5f;
    public float waveInterval = 25f;
    public int minionsPerWave = 3;
    public float delayBetweenMinions = 1f;

    public int WaveNumber { get; private set; }

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(firstDelay);

        while (true)
        {
            WaveNumber++;
            // Каждая пятая волна на одного миньона больше
            int count = minionsPerWave + WaveNumber / 5;
            for (int i = 0; i < count; i++)
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

        // Усиление от потерянных вражеских башен: +15% здоровья за каждую
        float power = GameManager.Instance.MinionPower(team);
        Health h = minion.GetComponent<Health>();
        h.IncreaseMax(h.maxHealth * (power - 1f));
    }
}
