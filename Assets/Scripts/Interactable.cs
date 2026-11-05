using UnityEngine;

// Базовый класс объекта взаимодействия: игрок входит в зону, видит подсказку, нажимает E.
// Наследники (ShopTerminal, HealingStation) решают, что произойдёт при нажатии.
public abstract class Interactable : MonoBehaviour
{
    public string prompt = "Нажми E";

    protected bool playerInside;
    protected GameObject player;
    Collider playerCollider;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        player = other.gameObject;
        playerCollider = other;
        GameManager.Instance.ShowPrompt(prompt);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Leave();
    }

    void Update()
    {
        // Игрок умер прямо в зоне: его коллайдер выключен, выходим вручную
        if (playerInside && (playerCollider == null || !playerCollider.enabled))
        {
            Leave();
            return;
        }

        if (playerInside && !GameManager.Instance.InputBlocked && Input.GetKeyDown(KeyCode.E))
            Interact();
    }

    void Leave()
    {
        playerInside = false;
        GameManager.Instance.HidePrompt();
    }

    protected abstract void Interact();     // каждый наследник решает сам
}
