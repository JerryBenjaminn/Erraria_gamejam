using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class RicochetBullet2D : MonoBehaviour
{
    public float speed = 28f;
    public int maxBounces = 6;
    public float lifeTime = 6f;
    public LayerMask hitMask;          // seinät, lattiat, esteet, PLAYER myös jos haluat osuman
    public float spawnIgnoreTime = 0.06f; // suoja: ei osu ampujaan heti suulla
    public LayerMask ricochetMask; // Ground/Default/Seinät + Player, EI Bullet


    Rigidbody2D rb;
    Collider2D col;
    int bounces;
    bool canHitShooter;
    bool hasRicocheted;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Fire(Vector2 dir, GameObject shooter)
    {
        // Aseta lähtösuunta
        rb.linearVelocity = dir.normalized * speed;
        // Ignoraa ampujan colliderit aluksi
        if (shooter && shooter.TryGetComponent<Collider2D>(out var shooterCol))
            Physics2D.IgnoreCollision(col, shooterCol, true);
        StartCoroutine(ReenableShooterCollisionNextFrame(shooter));
        Destroy(gameObject, lifeTime);
    }

    IEnumerator ReenableShooterCollisionNextFrame(GameObject shooter)
    {
        yield return new WaitForSeconds(spawnIgnoreTime);
        if (shooter && shooter.TryGetComponent<Collider2D>(out var shooterCol))
            Physics2D.IgnoreCollision(col, shooterCol, false);
        canHitShooter = true;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        // Tapa, jos kohteessa on DeathAndRespawn (pelaaja tai vihollinen)
        var d = c.collider.GetComponent<DeathAndRespawn>();
        if (d != null)
        {
            d.Die();
            Destroy(gameObject);
            return;
        }

        Vector2 v = rb.linearVelocity;
        Vector2 n = c.GetContact(0).normal;

        if (!hasRicocheted)
        {            
            rb.linearVelocity = Vector2.Reflect(v, n);
            hasRicocheted = true;

            // Valinnainen: kimmon jälkeen kulje vain pelaajaan päin, ei enää seiniin
            // Esim. vaihda layer niin, ettei törmää seiniin:
            // gameObject.layer = LayerMask.NameToLayer("BulletPost");
        }
        else
        {
            // Toinen törmäys mihin tahansa -> poistutaan
            Destroy(gameObject);
        }

        bounces++;
        if (bounces > maxBounces) Destroy(gameObject);
    }
    void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;
        float dist = v.magnitude * Time.fixedDeltaTime;
        if (dist <= 0f) return;

        var hits = new RaycastHit2D[1];
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = ricochetMask, useTriggers = true };
        int n = rb.Cast(v.normalized, filter, hits, dist);
        if (n > 0)
        {
            var h = hits[0];
            // siirrä luoti osumapisteeseen pienen skinin päähän
            transform.position = h.point + h.normal * 0.01f;
            rb.linearVelocity = Vector2.Reflect(v, h.normal);

            bounces++;
            if (bounces > maxBounces) Destroy(gameObject);
        }
    }

}
