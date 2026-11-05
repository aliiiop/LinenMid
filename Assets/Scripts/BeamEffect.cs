using System.Collections;
using UnityEngine;

// Короткий луч выстрела (LineRenderer) и звук выстрела.
// Используется у игрока (трассер), ботов и башен.
[RequireComponent(typeof(LineRenderer))]
public class BeamEffect : MonoBehaviour
{
    public float duration = 0.08f;
    public AudioClip shotClip;          // звук выстрела (по желанию)
    public float pitch = 1f;
    public float volume = 0.6f;

    LineRenderer line;
    AudioSource source;
    float startWidth;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.enabled = false;
        startWidth = line.widthMultiplier;

        if (shotClip != null)
        {
            // 3D-звук: чем дальше от камеры, тем тише
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 6f;
            source.maxDistance = 60f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.pitch = pitch;
            source.volume = volume;
        }
    }

    public void Show(Vector3 from, Vector3 to)
    {
        if (source != null) source.PlayOneShot(shotClip);
        StopAllCoroutines();
        StartCoroutine(Flash(from, to));
    }

    IEnumerator Flash(Vector3 from, Vector3 to)
    {
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.enabled = true;

        // Луч быстро становится тоньше и гаснет
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            line.widthMultiplier = startWidth * (1f - t / duration);
            yield return null;
        }
        line.enabled = false;
        line.widthMultiplier = startWidth;
    }
}
