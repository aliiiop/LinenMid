using UnityEngine;

public enum TeamId { Blue, Red, Neutral }
public enum UnitKind { Minion, Hero, Tower, Ancient, Neutral }

public class Team : MonoBehaviour
{
    public TeamId id;
    public UnitKind kind;
    public float radius = 0.5f;     // "размер" объекта, нужен для дальности атаки

    // Враг ли other для этого объекта
    public bool IsEnemyOf(Team other)
    {
        if (other == null || other.id == id) return false;
        if (id == TeamId.Neutral) return true;          // нейтралы нападают на всех
        if (other.id == TeamId.Neutral) return false;   // синие и красные сами не лезут к нейтралам
        return true;                                    // синие и красные враги друг другу
    }

    public static TeamId Opposite(TeamId t)
    {
        return t == TeamId.Blue ? TeamId.Red : TeamId.Blue;
    }
}