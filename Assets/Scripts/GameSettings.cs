using UnityEngine;

// Настройки, которые живут между сценами (меню -> игра).
// Чувствительность и громкость сохраняются в PlayerPrefs, чтобы не сбрасывались после перезапуска.
public static class GameSettings
{
    public static int selectedHero = 0;   // 0 = Стрелок, 1 = Бегун

    public static string HeroName => selectedHero == 0 ? "Стрелок" : "Бегун";

    public static float Sensitivity
    {
        get => PlayerPrefs.GetFloat("sensitivity", 2f);
        set { PlayerPrefs.SetFloat("sensitivity", value); PlayerPrefs.Save(); }
    }

    public static float Volume
    {
        get => PlayerPrefs.GetFloat("volume", 0.8f);
        set { PlayerPrefs.SetFloat("volume", value); PlayerPrefs.Save(); ApplyVolume(); }
    }

    // AudioListener.volume — общая громкость всех звуков в игре
    public static void ApplyVolume()
    {
        AudioListener.volume = Volume;
    }
}
