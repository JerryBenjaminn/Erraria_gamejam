using UnityEngine;

public class BossCoreHit : MonoBehaviour
{
    public BossController boss;

    void OnTriggerEnter2D(Collider2D other)
    {
        // osui luotiin
        if (other.GetComponent<RicochetBullet2D>() != null)
        {
            boss.TakeHit(1);
            Destroy(other.gameObject);
        }
    }
}
