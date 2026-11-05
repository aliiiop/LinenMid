using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BeamEffect : MonoBehaviour
{
    LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.enabled = false;
    }

    public void Show(Vector3 from, Vector3 to)
    {
        StopAllCoroutines();
        StartCoroutine(Flash(from, to));
    }

    IEnumerator Flash(Vector3 from, Vector3 to)
    {
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.enabled = true;
        yield return new WaitForSeconds(0.08f);
        line.enabled = false;
    }
}