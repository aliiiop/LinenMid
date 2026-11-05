using UnityEngine;

// Плавно гасит вспышку света у эффектов (взрыв, способность Q).
[RequireComponent(typeof(Light))]
public class LightFade : MonoBehaviour
{
    public float duration = 0.4f;

    Light lightSource;
    float startIntensity;
    float age;

    void Awake()
    {
        lightSource = GetComponent<Light>();
        startIntensity = lightSource.intensity;
    }

    void Update()
    {
        age += Time.deltaTime;
        lightSource.intensity = Mathf.Lerp(startIntensity, 0f, age / duration);
        if (age >= duration) lightSource.enabled = false;
    }
}
