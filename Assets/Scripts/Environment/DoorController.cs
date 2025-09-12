using UnityEngine;

public class DoorController : MonoBehaviour
{
    public bool startsClosed = true;
    public Collider2D solidCollider; // jos j�t�t tyhj�ksi, haetaan automaattisesti
    public SpriteRenderer visual;    // valinnainen

    void Awake()
    {
        if (!solidCollider) solidCollider = GetComponent<Collider2D>();
        SetOpen(!startsClosed, instant: true);
    }

    public void SetOpen(bool open, bool instant = false)
    {
        if (solidCollider) solidCollider.enabled = !open;
        if (visual) visual.enabled = !open; // jos haluat piilottaa oven n�kyvyyden
    }
}
