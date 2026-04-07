using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Cygnus/Combat/Weapon Data")]
public sealed class WeaponData : ScriptableObject
{
    [Header("Weapon Attributes")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private float projectileSpeed = 14f;

    public float Damage => damage;
    public float FireRate => fireRate;
    public float ProjectileSpeed => projectileSpeed;
    public float ShotInterval => fireRate > 0f ? 1f / fireRate : float.PositiveInfinity;

    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        fireRate = Mathf.Max(0.01f, fireRate);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
    }
}