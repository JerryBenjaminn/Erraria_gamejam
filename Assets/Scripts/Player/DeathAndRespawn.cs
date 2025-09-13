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
        if (corpsePrefab)
        {
            var corpse = Instantiate(corpsePrefab, transform.position, Quaternion.identity);
            if (corpse.TryGetComponent<Rigidbody2D>(out var crb) && rb)
                crb.linearVelocity = rb.linearVelocity;

            CorpseManager.Instance?.RegisterCorpse(corpse);
            SoundManager.PlaySFXAt("corpse_drop", corpse.transform.position, 0.02f);
        }
        RespawnAtSpawn();

    }

    public void RespawnAtSpawn()
    {
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
