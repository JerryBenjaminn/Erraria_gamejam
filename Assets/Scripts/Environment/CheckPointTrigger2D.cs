// CheckpointTrigger2D.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckpointTrigger2D : MonoBehaviour
{
    [Tooltip("Pieni nosto ettei spawnaa lattian sis‰‰n")]
    public Vector2 spawnOffset = new Vector2(0f, 0.5f);

    void Reset()
    {
        // Muista laittaa collider triggeriksi
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Hae pelaajan DeathAndRespawn
        var d = other.GetComponentInParent<DeathAndRespawn>();
        if (d == null) return;

        // Jos spawnPoint puuttuu, luodaan turvaksi (sun Awake tekee t‰m‰n joka tapauksessa)
        if (d.spawnPoint == null)
        {
            var sp = new GameObject("AutoSpawnPoint");
            d.spawnPoint = sp.transform;
        }

        // Siirr‰ VANHA spawnPoint t‰h‰n trigger-sijaintiin
        Vector3 newPos = transform.position + (Vector3)spawnOffset;
        d.spawnPoint.position = newPos;

        // Halutessa: pieni ‰‰ni tai v‰ri-feedback
        // SoundManager.PlaySFX("checkpoint", 0.02f);
        // GetComponent<SpriteRenderer>()?.color = Color.cyan;
    }
}
