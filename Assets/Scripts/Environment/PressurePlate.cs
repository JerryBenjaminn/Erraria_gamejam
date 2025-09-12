using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [Header("Kohteet jotka painavat")]
    public LayerMask weightMask;          // esim. Player, Corpse, Crate
    public float requiredWeight = 2f;     // paljonko tarvitaan avatakseen
    public bool latch;                    // true = pysyy �pressed� ensimm�isest� kerrasta

    [Header("Ulostulo")]
    public DoorController door;           // referenssi oveen (valinnainen)
    public UnityEngine.Events.UnityEvent onPressed;
    public UnityEngine.Events.UnityEvent onReleased;

    [Header("Visuaali (valinnainen)")]
    public Transform plateVisual;         // animoidaan alas/yl�s
    public float pressedYOffset = -0.06f;
    public float visualLerp = 12f;

    Collider2D area;
    bool isPressed;
    bool hasLatched;
    Vector3 visualStart;

    readonly Collider2D[] results = new Collider2D[32];

    void Awake()
    {
        area = GetComponent<Collider2D>();
        area.isTrigger = true;
        if (plateVisual) visualStart = plateVisual.localPosition;
    }

    void Update()
    {
        if (latch && hasLatched)
        {
            // pid� vain visuaali alhaalla
            if (plateVisual)
            {
                var target = visualStart + Vector3.up * pressedYOffset;
                plateVisual.localPosition = Vector3.Lerp(plateVisual.localPosition, target, visualLerp * Time.deltaTime);
            }
            return;
        }

        float total = ComputeWeightInside();
        bool pressedNow = total >= requiredWeight;

        if (pressedNow != isPressed)
        {
            isPressed = pressedNow;
            if (isPressed)
            {
                onPressed?.Invoke();
                if (door) door.SetOpen(true);
                if (latch) hasLatched = true;
            }
            else
            {
                onReleased?.Invoke();
                if (door) door.SetOpen(false);
            }
        }

        // Visuaali
        if (plateVisual)
        {
            var target = isPressed ? visualStart + Vector3.up * pressedYOffset : visualStart;
            plateVisual.localPosition = Vector3.Lerp(plateVisual.localPosition, target, visualLerp * Time.deltaTime);
        }
    }

    float ComputeWeightInside()
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = weightMask, useTriggers = true };
        int count = area.Overlap(filter, results);

        float total = 0f;
        for (int i = 0; i < count; i++)
        {
            var c = results[i];
            if (!c) continue;

            // 1) Jos on RB, k�yt� massaa
            var rb = c.attachedRigidbody;
            if (rb != null)
            {
                total += Mathf.Max(0f, rb.mass);
                continue;
            }

            // 2) Jos on Corpse (staattinen), k�yt� sen weighttia
            if (c.TryGetComponent<Corpse>(out var corpse))
            {
                total += Mathf.Max(0f, corpse.weight);
                continue;
            }

            // 3) Fallback
            total += 1f;
        }
        return total;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isPressed ? Color.green : Color.red;
        var b = GetComponent<Collider2D>() as BoxCollider2D;
        if (b) Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
#endif
}
