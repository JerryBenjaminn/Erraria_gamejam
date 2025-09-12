using UnityEngine;

public class KillOnContact : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var d = collision.collider.GetComponent<DeathAndRespawn>();
        if (d != null) d.Die();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var d = other.GetComponent<DeathAndRespawn>();
        if (d != null) d.Die();
    }
}
