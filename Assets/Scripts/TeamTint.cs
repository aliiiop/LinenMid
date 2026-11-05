using UnityEngine;

// Красит модель и иконку мини-карты в цвет команды при появлении.
// Если у модели два материала (как у Y Bot: тело и суставы), тело делаем тёмным,
// а суставы светящимися в цвет команды.
public class TeamTint : MonoBehaviour
{
    public Material blueMaterial;
    public Material redMaterial;
    public Material bodyMaterial;           // тёмный материал для тела (по желанию)
    public Material blueIcon;               // материалы иконки мини-карты
    public Material redIcon;
    public Renderer[] onlyThese;            // если заполнено, красим только эти рендереры модели

    void Start()
    {
        Team team = GetComponent<Team>();
        if (team.id == TeamId.Neutral) return;
        bool blue = team.id == TeamId.Blue;
        Material accent = blue ? blueMaterial : redMaterial;
        Material icon = blue ? blueIcon : redIcon;
        int minimapLayer = LayerMask.NameToLayer("Minimap");

        // Иконки на слое Minimap
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r.gameObject.layer == minimapLayer && icon != null) r.sharedMaterial = icon;

        if (accent == null) return;
        Renderer[] list = onlyThese != null && onlyThese.Length > 0 ? onlyThese : GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in list)
        {
            if (r is LineRenderer || r.gameObject.layer == minimapLayer) continue;
            Material[] slots = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = IsBodyPart(r, i, slots.Length) ? bodyMaterial : accent;
            r.sharedMaterials = slots;
        }
    }

    // У Y Bot два меша: Alpha_Surface (тело) и Alpha_Joints (суставы). Светятся только суставы.
    bool IsBodyPart(Renderer r, int slot, int slotCount)
    {
        if (bodyMaterial == null) return false;
        if (slotCount > 1) return slot == 0;
        return !r.name.Contains("Joint");
    }
}
