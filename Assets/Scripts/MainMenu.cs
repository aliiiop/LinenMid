using UnityEngine;
using UnityEngine.SceneManagement;

// Главное меню: Играть (выбор героя), Как играть, Настройки, Титры, Выход.
// Одновременно открыта только одна панель.
public class MainMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject[] panels;     // все панели меню, включая mainPanel

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameSettings.ApplyVolume();
        OpenPanel(mainPanel);
    }

    void Update()
    {
        // Esc в любой панели возвращает на главную
        if (Input.GetKeyDown(KeyCode.Escape)) OpenPanel(mainPanel);
    }

    // Вызывается кнопками: в On Click передаём панель, которую нужно открыть
    public void OpenPanel(GameObject panel)
    {
        foreach (GameObject p in panels)
            if (p != null) p.SetActive(p == panel);
    }

    // Кнопки выбора героя: 0 = Стрелок, 1 = Бегун
    public void StartGame(int hero)
    {
        GameSettings.selectedHero = hero;
        SceneManager.LoadScene("Game");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
