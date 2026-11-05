using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject creditsPanel;
    public GameObject heroPanel;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        creditsPanel.SetActive(false);
        heroPanel.SetActive(false);
    }

    public void ShowHeroSelect(bool show)
    {
        heroPanel.SetActive(show);
    }

    // Кнопки выбора героя: 0 = Стрелок, 1 = Бегун
    public void StartGame(int hero)
    {
        GameSettings.selectedHero = hero;
        SceneManager.LoadScene("Game");
    }

    public void ShowCredits(bool show)
    {
        creditsPanel.SetActive(show);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
