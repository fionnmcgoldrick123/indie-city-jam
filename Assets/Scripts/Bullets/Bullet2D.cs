using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet2D : NetworkBehaviour
{
    [Header("Bullet")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float maxLifeSeconds = 2f;

    [Networked] private Vector2 Velocity { get; set; }
    [Networked] private float Life { get; set; }

    // The player who fired this bullet
    [Networked] private NetworkObject Shooter { get; set; }

    private Rigidbody2D rb;

    private void CacheRb()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
        CacheRb();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.gravityScale = 0f;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        if (HasStateAuthority)
        {
            Life = maxLifeSeconds;
            rb.linearVelocity = Velocity;
        }
    }

    public void Init(Vector2 dir, float speed, NetworkObject shooter)
    {
        if (!HasStateAuthority) return;

        CacheRb();

        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
        else dir = Vector2.zero;

        Shooter = shooter;
        Velocity = dir * speed;

        rb.linearVelocity = Velocity;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        CacheRb();
        rb.linearVelocity = Velocity;

        Life -= Runner.DeltaTime;
        if (Life <= 0f)
            Runner.Despawn(Object);
    }

    private bool IsShooter(Collider2D col)
    {
        if (Shooter == null) return false;

        var hitNO = col.GetComponentInParent<NetworkObject>();
        return hitNO != null && hitNO == Shooter;
    }

    private void TryDamage(Collider2D col)
    {
        if (!HasStateAuthority) return;

        // Ignore the shooter (no self-hit)
        if (IsShooter(col))
            return;

        // Damage health if present
        var health = col.GetComponentInParent<NetworkHealthBase2D>();
        if (health != null)
            health.ApplyDamage(damage, Object);

        Runner.Despawn(Object);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDamage(other);

    private void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider);
}