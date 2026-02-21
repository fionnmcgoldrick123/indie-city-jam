using Fusion;
using UnityEngine;

public class PlayerAttack2D : NetworkBehaviour
{
    [Header("Firing")]
    [Networked] public float ReloadSpeed { get; set; } = 0.5f;
    [Networked] public float ReloadTimer { get; set; } = 0f;

    [Header("Bullet")]
    [SerializeField] private NetworkObject bulletPrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float bulletSpeed = 20f;

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority)
            return;

        if (!GetInput<PlayerNetworkInput>(out var input))
            return;

        // StateAuthority does simulation/spawn to avoid duplicates
        if (!HasStateAuthority)
            return;

        if (ReloadTimer > 0f)
            ReloadTimer = Mathf.Max(0f, ReloadTimer - Runner.DeltaTime);

        Vector2 aim = input.atkInput;
        if (aim == Vector2.zero)
            return;

        if (ReloadTimer > 0f)
            return;

        Fire(aim.normalized);
        ReloadTimer = ReloadSpeed;
    }

    private void Fire(Vector2 dir)
    {
        Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position;

        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);

        // Cache shooter colliders for optional physics-ignore
        var shooterColliders = GetComponentsInChildren<Collider2D>();

        Runner.Spawn(
            bulletPrefab,
            spawnPos,
            rot,
            Object.InputAuthority,
            (runner, spawned) =>
            {
                var bullet = spawned.GetComponent<Bullet2D>();
                if (bullet != null)
                    bullet.Init(dir, bulletSpeed, Object); // <-- pass shooter

                // OPTIONAL (recommended): ignore collisions bullet <-> shooter at physics level
                var bulletColliders = spawned.GetComponentsInChildren<Collider2D>();
                for (int i = 0; i < shooterColliders.Length; i++)
                {
                    var sc = shooterColliders[i];
                    if (sc == null) continue;

                    for (int j = 0; j < bulletColliders.Length; j++)
                    {
                        var bc = bulletColliders[j];
                        if (bc == null) continue;

                        Physics2D.IgnoreCollision(sc, bc, true);
                    }
                }
            }
        );
    }
}