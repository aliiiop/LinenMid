public class ShopTerminal : Interactable
{
    protected override void Interact()
    {
        GameManager.Instance.HidePrompt();
        GameManager.Instance.OpenShop();
    }
}
