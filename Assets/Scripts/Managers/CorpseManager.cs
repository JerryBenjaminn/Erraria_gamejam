using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CorpseManager : MonoBehaviour
{
    public static CorpseManager Instance;

    [Header("Elämät per taso")]
    public int maxCorpses = 5;
    int remainingCorpses;

    List<GameObject> activeCorpses = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        remainingCorpses = maxCorpses;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetLevel();
        }
    }

    public void RegisterCorpse(GameObject corpse)
    {
        activeCorpses.Add(corpse);
        remainingCorpses--;

        if (remainingCorpses <= 0)
        {
            ResetLevel();
        }
    }

    public void ResetLevel()
    {
        // Poista kaikki ruumiit
        foreach (var c in activeCorpses)
        {
            if (c != null) Destroy(c);
        }
        activeCorpses.Clear();

        // Resetoi counter
        remainingCorpses = maxCorpses;

        // Resetoi pelaaja spawnille
        var player = FindFirstObjectByType<DeathAndRespawn>();
        if (player != null)
        {
            player.RespawnAtSpawn();
        }

        // Jos haluat täydellisen resetin (glitchitkin nollautuvat),
        // voit vaihtoehtoisesti ladata scenen uudelleen:
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public int GetRemainingCorpses() => remainingCorpses;
}
