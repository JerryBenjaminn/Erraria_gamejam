using System.Collections;
using UnityEngine;

public class DeathAndRespawn : MonoBehaviour
{
    [Header("Respawn-asetukset")]
    public Transform spawnPoint;

    [Header("Ruumis")]
    public GameObject corpsePrefab;
    public float corpseFreezeDelay = 0.15f; // pieni viive ennen kuin ruumis j�� staattiseksi

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spawnPoint == null)
        {
            // Jos et ole asettanut erillist� spawnia, k�yt� aloituspaikkaa
            GameObject sp = new GameObject("AutoSpawnPoint");
            sp.transform.position = transform.position;
            spawnPoint = sp.transform;
        }
    }

    public void Die()
    {
        // 1) Putoava ruumis
        if (corpsePrefab != null)
        {
            var corpse = Instantiate(corpsePrefab, transform.position, Quaternion.identity);
            var crb = corpse.GetComponent<Rigidbody2D>();
            if (crb != null)
            {
                crb.linearVelocity = rb != null ? rb.linearVelocity : Vector2.zero;
                StartCoroutine(FreezeCorpseNextFrame(crb));
            }
        }

        // 2) Respawnaa pelaaja
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        transform.position = spawnPoint.position;
    }

    IEnumerator FreezeCorpseNextFrame(Rigidbody2D crb)
    {
        yield return new WaitForSeconds(corpseFreezeDelay);
        crb.bodyType = RigidbodyType2D.Static; // nyt ruumis on tukeva taso
    }
}
