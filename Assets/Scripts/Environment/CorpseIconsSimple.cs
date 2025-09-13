// CorpsesIconsSimple.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CorpsesIconsSimple : MonoBehaviour
{
    [Header("Icon")]
    public Sprite iconSprite;                    // kallon tms. sprite
    public Vector2 iconSize = new Vector2(24, 24);
    public Color iconColor = Color.white;
    public int defaultMax = 5;                   // jos manageri ei ole vielä pystyssä
    public int hardMax = 20;                     // jarru, ettet räjäytä UI:ta

    Image[] icons;
    int cachedMax;
    int lastRemaining = -1;
    bool built;

    void OnEnable() { StartCoroutine(Init()); }

    IEnumerator Init()
    {
        // odota yhden framen, että Canvas/CorpseManager ovat varmasti olemassa
        yield return null;

        int max = defaultMax;
        if (CorpseManager.Instance != null)
            max = Mathf.Max(1, CorpseManager.Instance.GetMaxCorpses());

        Build(max);
        Subscribe();
        ForceRefresh();
    }

    void OnDisable()
    {
        if (CorpseManager.Instance != null)
            CorpseManager.Instance.OnCorpsesChanged -= OnChanged;
    }

    void Subscribe()
    {
        if (CorpseManager.Instance == null) return;
        CorpseManager.Instance.OnCorpsesChanged -= OnChanged;
        CorpseManager.Instance.OnCorpsesChanged += OnChanged;
    }

    void OnChanged(int remaining, int max)
    {
        if (!built || max != cachedMax) Build(max);
        Show(remaining);
    }

    void ForceRefresh()
    {
        int remaining = cachedMax;
        if (CorpseManager.Instance != null)
            remaining = CorpseManager.Instance.GetRemainingCorpses();
        Show(remaining);
    }

    void Build(int max)
    {
        max = Mathf.Clamp(max, 1, hardMax);

        // siivoa vanhat kerran
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        icons = new Image[max];
        for (int i = 0; i < max; i++)
        {
            var go = new GameObject($"Icon_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            var img = go.GetComponent<Image>();
            img.sprite = iconSprite;
            img.color = iconColor;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = iconSize;

            icons[i] = img;
        }

        cachedMax = max;
        built = true;
    }

    void Show(int remaining)
    {
        if (!built) return;
        remaining = Mathf.Clamp(remaining, 0, cachedMax);
        if (remaining == lastRemaining) return;

        for (int i = 0; i < cachedMax; i++)
            icons[i].enabled = i < remaining;

        lastRemaining = remaining;
    }
}
