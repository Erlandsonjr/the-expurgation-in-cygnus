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