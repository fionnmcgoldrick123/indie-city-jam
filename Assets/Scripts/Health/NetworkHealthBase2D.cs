using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class NetworkHealthBase2D : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Networked] public float MaxHealth { get; private set; }
    [Networked] public float Health { get; private set; }

    [Header("UI")]
    [SerializeField] private Slider healthSlider;

    private float _lastHealth = float.NaN;
    private float _lastMaxHealth = float.NaN;

    public bool IsDead => Health <= 0f;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
        }

        UpdateHealthUI(force: true);
    }

    /// <summary>Only StateAuthority modifies networked health.</summary>
    public virtual void ApplyDamage(float amount, NetworkObject source = null)
    {
        if (!HasStateAuthority) return;
        if (IsDead) return;
        if (amount <= 0f) return;

        Health = Mathf.Max(0f, Health - amount);
        UpdateHealthUI(true);
        if (Health <= 0f)
            OnDied(source);
    }

    public virtual void Heal(float amount)
    {
        if (!HasStateAuthority) return;
        if (IsDead) return;
        if (amount <= 0f) return;

        Health = Mathf.Min(MaxHealth, Health + amount);
    }

    protected virtual void OnDied(NetworkObject killer)
    {
        if (HasStateAuthority)
            Runner.Despawn(Object);
    }

    /// <summary>
    /// Render() runs on proxies too. We use it to update UI only when values changed.
    /// </summary>
    public override void Render()
    {
        UpdateHealthUI(force: false);
    }

    private void UpdateHealthUI(bool force)
    {
        if (healthSlider == null) return;

        if (!force && Mathf.Approximately(_lastHealth, Health) && Mathf.Approximately(_lastMaxHealth, MaxHealth))
            return;

        _lastHealth = Health;
        _lastMaxHealth = MaxHealth;

        float max = (MaxHealth > 0f) ? MaxHealth : Mathf.Max(1f, maxHealth);
        healthSlider.value = Mathf.Clamp01(Health / max);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHealth < 1f) maxHealth = 1f;
    }
#endif
}