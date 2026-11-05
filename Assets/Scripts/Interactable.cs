using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string prompt = "Нажми E";

    protected bool playerInside;
    protected GameObject player;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        player = other.gameObject;
        GameManager.Instance.ShowPrompt(prompt);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        GameManager.Instance.HidePrompt();
    }

    void Update()
    {
        if (playerInside && !GameManager.Instance.InputBlocked && Input.GetKeyDown(KeyCode.E))
            Interact();
    }

    protected abstract void Interact();     // каждый наследник решает сам
}
