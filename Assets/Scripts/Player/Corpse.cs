using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Corpse : MonoBehaviour
{
    public float freezeDelay = 0.15f;
    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    IEnumerator Start()
    {
        float t = 0f;
        while (t < freezeDelay)
        {
            if (!rb) yield break;
            t += Time.deltaTime;
            yield return null;
        }
        if (!rb) yield break;
        rb.bodyType = RigidbodyType2D.Static;
    }
}
