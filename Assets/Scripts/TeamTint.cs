using UnityEngine;

public class TeamTint : MonoBehaviour
{
    public Material blueMaterial;
    public Material redMaterial;

    void Start()
    {
        Team team = GetComponent<Team>();
        Material m = null;
        if (team.id == TeamId.Blue) m = blueMaterial;
        else if (team.id == TeamId.Red) m = redMaterial;
        if (m == null) return;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is LineRenderer) continue;        // лучи не красим
            Material[] slots = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < slots.Length; i++) slots[i] = m;
            r.sharedMaterials = slots;
        }
    }
}
