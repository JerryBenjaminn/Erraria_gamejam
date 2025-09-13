// CorpseManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class CorpseManager : MonoBehaviour
{
    public static CorpseManager Instance;

    [Header("Elämät per taso")]
    public int maxCorpses = 5;
    int remainingCorpses;

    public event Action<int, int> OnCorpsesChanged; // (remaining, max)

    List<GameObject> activeCorpses = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        remainingCorpses = maxCorpses;
        OnCorpsesChanged?.Invoke(remainingCorpses, maxCorpses);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            ResetLevel();
    }

    public void RegisterCorpse(GameObject corpse)
    {
        activeCorpses.Add(corpse);
        remainingCorpses = Mathf.Max(remainingCorpses - 1, 0);
        OnCorpsesChanged?.Invoke(remainingCorpses, maxCorpses);

        if (remainingCorpses <= 0)
            ResetLevel();
    }

    bool isResetting;
    public void ResetLevel()
    {
        if (isResetting) return;
        isResetting = true;

        // siivoa ruumiit
        foreach (var c in activeCorpses) if (c) Destroy(c);
        activeCorpses.Clear();

        remainingCorpses = maxCorpses;
        OnCorpsesChanged?.Invoke(remainingCorpses, maxCorpses);

        var player = FindFirstObjectByType<DeathAndRespawn>();
        if (player != null)
        {
            player.RespawnAtSpawn();
        }

        // pieni viive ennen kuin sallitaan uudet resetit
        StartCoroutine(ResetLatch());
    }
    System.Collections.IEnumerator ResetLatch()
    {
        yield return null;           // 1 frame
        yield return new WaitForSeconds(0.1f);
        isResetting = false;
    }

    public int GetRemainingCorpses() => remainingCorpses;
    public int GetMaxCorpses() => maxCorpses;
}
