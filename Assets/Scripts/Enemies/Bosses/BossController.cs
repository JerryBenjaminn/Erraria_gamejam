using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BossController : MonoBehaviour
{
    public enum State { Patrol, Chase, Expose, Recover, Dead }

    [Header("HP")]
    public int maxHP = 3;
    public int currentHp;

    [Header("Chase")]
    public float chaseSpeed = 6f;
    public float chaseStopRange = 0.1f;    // jos ollaan käytännössä “kiinni”, älä tärise
    public bool killPlayerOnTouch = true;  // voi kytkeä pois debugissa

    [Header("Patrol")]
    public Transform leftLimit;
    public Transform rightLimit;
    public float moveSpeed = 3f;
    public float turnMargin = 0.05f;

    [Header("Reuna/Seinä-varmistus")]
    public LayerMask groundMask;
    public LayerMask wallMask;
    public float edgeCheckDistance = 0.6f;
    public float wallCheckDistance = 0.5f;

    [Header("Expose-ikkuna")]
    public Collider2D coreHitbox;     // trigger coreen (child)
    public Collider2D shieldCollider; // ulkokuori (ei-trigger). Voi olla null jos et käytä
    public float exposeDuration = 1.0f;
    public float recoverDuration = 1.0f;
    public float autoExposeCooldown = 6.0f; // ajastettu paljastus

    [Header("Aggro (valinnainen)")]
    public Transform player;
    public float aggroRange = 12f;
    public float verticalTolerance = 2.5f;
    public bool requireLineOfSight = false;
    public LayerMask losBlockMask;

    [Header("Corpse -> Expose")]
    public float corpseExposeDuration = 1.2f;   // kuinka kauan core auki ruumisosumasta
    public float corpseExposeCooldown = 0.4f;
    public float suppressExposeAfterPlayerKill = 0.15f;// spämmisuoja, sekunteina
    float corpseExposeTimer;                    // sisäinen cooldown
    float suppressTimer;


    Rigidbody2D rb;
    Collider2D bodyCol;
    SpriteRenderer srenderer;
    State state;
    int hp;
    bool facingRight = true;
    float exposeCD;    // ajastettu paljastus
    float stateTimer;  // keston mittaus
    public TextMeshProUGUI thankText;
    public TextMeshProUGUI buttonText;
    public Button button;

    void Awake()
    {
        thankText.enabled = false;
        button.enabled = false;
        buttonText.enabled = false;
        
        rb = GetComponent<Rigidbody2D>();
        bodyCol = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        hp = maxHP;
        currentHp = hp;
        SetCoreExposed(false);

        // Korjaa rajojen järjestys, jos menivät ristiin
        if (leftLimit && rightLimit && rightLimit.position.x < leftLimit.position.x)
        {
            var t = leftLimit; leftLimit = rightLimit; rightLimit = t;
        }

        // Etsi player jos ei annettu
        if (!player)
            player = FindFirstObjectByType<PlayerController2D>()?.transform;

        ChangeState(State.Patrol);
    }

    void Update()
    {
        switch (state)
        {
            case State.Patrol: TickPatrol(); break;
            case State.Chase: TickChase(); break;
            case State.Expose: TickExpose(); break;
            case State.Recover: TickRecover(); break;
            case State.Dead: break;
        }
        corpseExposeTimer -= Time.deltaTime;
        suppressTimer -= Time.deltaTime;

    }

    // ---------- States ----------

    void ChangeState(State s)
    {
        state = s;
        stateTimer = 0f;
        switch (s)
        {
            case State.Patrol:
                // liike päälle
                break;
            case State.Expose:
                SetCoreExposed(true);
                break;
            case State.Recover:
                SetCoreExposed(false);
                rb.linearVelocity = Vector2.zero;
                break;
            case State.Dead:
                SetCoreExposed(false);
                rb.linearVelocity = Vector2.zero;
                if (shieldCollider) shieldCollider.enabled = false;
                Destroy(gameObject);
                thankText.enabled = true;
                button.enabled = true;
                buttonText.enabled = true;
                break;
        }
    }

    void TickPatrol()
    {
        stateTimer += Time.deltaTime;
        exposeCD -= Time.deltaTime;

        // Aggro-portti: jos pelaaja olemassa ja liian kaukana/eri korkeudella, vain partioidaan
        bool canAutoExpose = exposeCD <= 0f && PlayerInAggro();

        // Perusjuoksu
        float dir = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, 0f);

        // Käänny limitillä
        if (rightLimit && facingRight && transform.position.x >= rightLimit.position.x - turnMargin) Flip(false);
        if (leftLimit && !facingRight && transform.position.x <= leftLimit.position.x + turnMargin) Flip(true);

        // Fallback: seinä/edge check "nenästä"
        Bounds b = bodyCol.bounds;
        float sign = facingRight ? 1f : -1f;
        Vector2 nose = (Vector2)b.center + Vector2.right * sign * (b.extents.x + 0.05f);
        Vector2 toe = nose + Vector2.down * (b.extents.y - 0.02f);

        if (wallMask.value != 0)
        {
            var wallAhead = Physics2D.Raycast(nose, Vector2.right * sign, Mathf.Max(wallCheckDistance, 0.4f), wallMask);
            if (wallAhead) Flip(!facingRight);
        }

        if (groundMask.value != 0)
        {
            var groundAhead = Physics2D.Raycast(toe, Vector2.down, edgeCheckDistance, groundMask);
            if (!groundAhead) Flip(!facingRight);
        }

        if (PlayerInAggro())
        {
            ChangeState(State.Chase);
        }
        else
        {
            // Jos haluat edelleen auto-exposen ilman chasea, pidä tämä kytkimen takana
            canAutoExpose = /* enableAutoExpose */ false && exposeCD <= 0f;
            if (canAutoExpose)
            {
                exposeCD = autoExposeCooldown;
                ChangeState(State.Expose);
            }
        }
    }

    void TickExpose()
    {
        stateTimer += Time.deltaTime;
        rb.linearVelocity = Vector2.zero; // pysähdy expose-ikkunassa
        if (stateTimer >= exposeDuration)
            ChangeState(State.Recover);
    }

    void TickRecover()
    {
        stateTimer += Time.deltaTime;
        // pieni hengähdys, sitten takaisin patroliin
        if (stateTimer >= recoverDuration)
            ChangeState(State.Patrol);
    }

    // ---------- Public hooks ----------

    // Kutsu PressurePlatelta: bossi paljastaa coren hetkeksi
    public void ExternalExpose(float seconds)
    {
        if (state == State.Dead) return;
        SetCoreExposed(true);
        state = State.Expose;
        stateTimer = 0f;
        exposeDuration = seconds; // override-ikkuna
    }

    // Osuma coreen (Ricochet-luoti tms.)
    public void TakeHit(int dmg = 1)
    {
        if (state == State.Dead) return;
        // sallitaan osumat vain kun core on auki
        if (coreHitbox && !coreHitbox.enabled) return;

        hp -= dmg;
        currentHp = hp;
        if (hp <= 0) { ChangeState(State.Dead); return; }

        // palaa recoveriin osuman jälkeen
        ChangeState(State.Recover);
    }

    // ---------- Helpers ----------

    void SetCoreExposed(bool exposed)
    {
        if (coreHitbox) coreHitbox.enabled = exposed;
        // Haluatko kilven pois käytöstä exposessa? Avaa tämä:
        if (shieldCollider) shieldCollider.enabled = !exposed;

        // Visuaalinen vihje? Vaihda väriä yms. täällä.
        GetComponent<SpriteRenderer>().color = exposed ? Color.red : Color.white;
    }

    void Flip(bool toRight)
    {
        facingRight = toRight;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (toRight ? 1f : -1f);
        transform.localScale = s;
    }

    bool PlayerInAggro()
    {
        if (!player) return true; // jos ei löydy, anna mennä
        float dx = Mathf.Abs(player.position.x - transform.position.x);
        float dy = Mathf.Abs(player.position.y - transform.position.y);
        if (dx > aggroRange || dy > verticalTolerance) return false;

        if (!requireLineOfSight) return true;

        Vector2 from = transform.position;
        Vector2 to = player.position;
        var hit = Physics2D.Raycast(from, (to - from).normalized, Vector2.Distance(from, to), losBlockMask);
        return !hit;
    }
    void TickChase()
    {
        stateTimer += Time.deltaTime;

        if (!PlayerInAggro())
        {
            // Pelaaja poistui kantamasta palaa patroliin
            ChangeState(State.Patrol);
            return;
        }

        // Suunta pelaajaan vaakatasossa
        float dx = player.position.x - transform.position.x;
        float dir = Mathf.Sign(dx);

        // Käännä facing
        Flip(dir >= 0f);

        // Liiku kohti
        // Jos aivan iholla, vältä tärinää
        if (Mathf.Abs(dx) > chaseStopRange)
            rb.linearVelocity = new Vector2(dir * chaseSpeed, 0f);
        else
            rb.linearVelocity = new Vector2(0f, 0f);

        // Älä juokse seinään tai pudottaudu reunalta
        Bounds b = bodyCol.bounds;
        Vector2 nose = (Vector2)b.center + Vector2.right * dir * (b.extents.x + 0.05f);
        Vector2 toe = nose + Vector2.down * (b.extents.y - 0.02f);

        if (wallMask.value != 0)
        {
            var wallAhead = Physics2D.Raycast(nose, Vector2.right * dir, Mathf.Max(wallCheckDistance, 0.4f), wallMask);
            if (wallAhead)
            {
                rb.linearVelocity = Vector2.zero;
                // halutessasi voit Exposeta tässä hetkeksi tai vaihtaa suuntaa
            }
        }

        if (groundMask.value != 0)
        {
            var groundAhead = Physics2D.Raycast(toe, Vector2.down, edgeCheckDistance, groundMask);
            if (!groundAhead)
            {
                rb.linearVelocity = Vector2.zero;
                // reuna edessä älä tipu. Voit valita kääntymisen:
                // Flip(!facingRight);
            }
        }
    }
    void OnCollisionEnter2D(Collision2D c)
    {
        if (!killPlayerOnTouch) return;

        var d = c.collider.GetComponent<DeathAndRespawn>();
        if (d != null)
        {
            d.Die();
            suppressTimer = suppressExposeAfterPlayerKill;
            return;
        }
        TryExposeFromCorpse(c.collider);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        // Jos käytät triggertörmäyksiä, sama logiikka
        var d = other.GetComponent<DeathAndRespawn>();
        if (killPlayerOnTouch && d != null)
        {
            d.Die();
            suppressTimer = suppressExposeAfterPlayerKill;
            return;
        }
        TryExposeFromCorpse(other);
    }

    void TryExposeFromCorpse(Collider2D col)
    {
        if (state == State.Dead) return;
        if (corpseExposeTimer > 0f) return;
        if (suppressTimer > 0f) return;

        // Tunnista ruumis komponentista (parempi kuin pelkkä layer)
        if (col.TryGetComponent<Corpse>(out var corpse))
        {
            corpseExposeTimer = corpseExposeCooldown;

            // Tuhoa ruumis
            var go = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.gameObject;
            Destroy(go);

            // Pysäytä hetkeksi ja paljasta core
            rb.linearVelocity = Vector2.zero;
            ExternalExpose(corpseExposeDuration);
        }
    }



#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Limit-viivat
        if (leftLimit)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftLimit.position + Vector3.up * 2, leftLimit.position + Vector3.down * 2);
        }
        if (rightLimit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rightLimit.position + Vector3.up * 2, rightLimit.position + Vector3.down * 2);
        }
        // Nenä/edge -säteet jos runtime-tietoja saatavilla
        if (bodyCol)
        {
            Bounds b = bodyCol.bounds;
            float sign = facingRight ? 1f : -1f;
            Vector2 nose = (Vector2)b.center + Vector2.right * sign * (b.extents.x + 0.05f);
            Vector2 toe = nose + Vector2.down * (b.extents.y - 0.02f);
            Gizmos.color = Color.magenta; Gizmos.DrawLine(nose, nose + Vector2.right * sign * wallCheckDistance);
            Gizmos.color = Color.cyan; Gizmos.DrawLine(toe, toe + Vector2.down * edgeCheckDistance);
        }
    }
#endif
}
