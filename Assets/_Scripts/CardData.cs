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
    Heal,
    MaxHealth,
    MoveSpeed,
    FireRate,
    ProjectileSpeed,
    JumpForce,
    FlatDamage,
    DashCooldown,
    Invulnerability,
    Luck,
    ExtraJumps,
    DashExplosion,
    CritNextShot,
    CryoAmmo,
    BerserkerRage,
    RadiationAura,
    SpreadShot,
}

[CreateAssetMenu(fileName = "CardData", menuName = "Cygnus/Cards/Card Data")]
public sealed class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public Sprite cardIcon;
    public CardRarity rarity;
    public CardEffectType effectType;
    public float effectValue;
}