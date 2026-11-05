using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Панель настроек (есть и в главном меню, и в паузе): чувствительность мыши и громкость.
public class SettingsPanel : MonoBehaviour
{
    public Slider sensitivitySlider;
    public Slider volumeSlider;
    public TMP_Text sensitivityValue;
    public TMP_Text volumeValue;

    void OnEnable()
    {
        // Сначала ставим сохранённые значения, потом подписываемся, чтобы не сработать зря
        sensitivitySlider.onValueChanged.RemoveListener(OnSensitivity);
        volumeSlider.onValueChanged.RemoveListener(OnVolume);
        sensitivitySlider.value = GameSettings.Sensitivity;
        volumeSlider.value = GameSettings.Volume;
        sensitivitySlider.onValueChanged.AddListener(OnSensitivity);
        volumeSlider.onValueChanged.AddListener(OnVolume);
        UpdateLabels();
    }

    void OnSensitivity(float v)
    {
        GameSettings.Sensitivity = v;
        UpdateLabels();
    }

    void OnVolume(float v)
    {
        GameSettings.Volume = v;
        UpdateLabels();
    }

    void UpdateLabels()
    {
        if (sensitivityValue != null) sensitivityValue.text = GameSettings.Sensitivity.ToString("0.0");
        if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
    }
}
