using UnityEngine;

public enum CardRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
}

public enum CardEffectType
{
    Heal = 0,
    MaxHealth = 1,
    MoveSpeed = 2,
    FireRate = 3,
    ProjectileSpeed = 4,
    JumpForce = 5,
    FlatDamage = 6,
    DashCooldown = 7,
    Invulnerability = 8,
    Luck = 9,
    ExtraJumps = 10,
    CritNextShot = 12,
    CryoAmmo = 13,
    BerserkerRage = 14,
    RadiationAura = 15,
    SpreadShot = 16,
    CompanionDrone = 17,
    EquipWeapon = 18,
}

[CreateAssetMenu(fileName = "CardData", menuName = "Cygnus/Cards/Card Data")]
public sealed class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public Sprite cardIcon;
    public CardRarity rarity;
    public bool isUnique;
    public CardEffectType effectType;
    public float effectValue;
    public WeaponData weaponReward;
}