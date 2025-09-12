using UnityEngine;

public class Gun2D : MonoBehaviour
{
    public RicochetBullet2D bulletPrefab;
    public Transform muzzle;              // empty hieman pelaajan edessä
    public float fireCooldown = 0.2f;
    public KeyCode fireKey = KeyCode.Mouse0;
    public Camera cam;                    // jätä tyhjäksi-> haetaan Camera.main
    public float minAimDistance = 0.05f;  // jos hiiri liian lähellä piippua

    float cd;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        cd -= Time.deltaTime;

        // Aiming: hiiren maailma-sijainti
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f; // 2D-taso

        Vector2 aimDir = (mouseWorld - muzzle.position);
        if (aimDir.sqrMagnitude < minAimDistance * minAimDistance)
            aimDir = new Vector2(Mathf.Sign(transform.localScale.x), 0f); // fallback suoraan eteen

        aimDir.Normalize();

        // Facing flip hiiren mukaan
        if (Mathf.Abs(aimDir.x) > 0.01f)
        {
            var s = transform.localScale;
            s.x = Mathf.Sign(aimDir.x) * Mathf.Abs(s.x);
            transform.localScale = s;
        }

        if (Input.GetKeyDown(fireKey) && cd <= 0f)
        {
            Fire(aimDir);
            cd = fireCooldown;
        }
    }

    void Fire(Vector2 dir)
    {
        if (!bulletPrefab || !muzzle) return;

        var bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
        bullet.Fire(dir, gameObject);
    }
}
