using UnityEngine;

public class Gun2D : MonoBehaviour
{
    public RicochetBullet2D bulletPrefab;
    public Transform muzzle;      // empty child hieman pelaajan edessä
    public float fireCooldown = 0.2f;
    public KeyCode fireKey = KeyCode.Mouse0;

    float cd;

    void Update()
    {
        cd -= Time.deltaTime;
        if (Input.GetKeyDown(fireKey) && cd <= 0f)
        {
            Fire();
            cd = fireCooldown;
        }
    }

    void Fire()
    {
        if (!bulletPrefab || !muzzle) return;

        // Suunta: pelaajan facing
        float dirX = Mathf.Sign(transform.localScale.x);
        Vector2 dir = new Vector2(dirX, 0f);

        var bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
        bullet.Fire(dir, gameObject);
    }
}
