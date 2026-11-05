using UnityEngine;

public enum TeamId { Blue, Red, Neutral }
public enum UnitKind { Minion, Hero, Tower, Ancient, Neutral }

// Команда и тип объекта. Есть у всех, кто участвует в бою.
public class Team : MonoBehaviour
{
    public TeamId id;
    public UnitKind kind;
    public float radius = 0.5f;     // "размер" объекта, нужен для дальности атаки
    public string displayName;      // имя для сообщений ("Вражеский бот" и т. п.)

    // Цвета команд из концепта: синие светятся бирюзовым, красные оранжевым
    public static readonly Color BlueColor = new Color(0f, 0.9f, 1f);
    public static readonly Color RedColor = new Color(1f, 0.42f, 0f);
    public static readonly Color NeutralColor = new Color(1f, 0.82f, 0.15f);

    // Враг ли other для этого объекта
    public bool IsEnemyOf(Team other)
    {
        if (other == null || other.id == id) return false;
        if (id == TeamId.Neutral) return true;          // нейтралы нападают на всех
        if (other.id == TeamId.Neutral) return false;   // синие и красные сами не лезут к нейтралам
        return true;                                    // синие и красные враги друг другу
    }

    public bool IsBuilding => kind == UnitKind.Tower || kind == UnitKind.Ancient;

    public static TeamId Opposite(TeamId t)
    {
        return t == TeamId.Blue ? TeamId.Red : TeamId.Blue;
    }

    public static Color ColorOf(TeamId t)
    {
        if (t == TeamId.Blue) return BlueColor;
        if (t == TeamId.Red) return RedColor;
        return NeutralColor;
    }
}
