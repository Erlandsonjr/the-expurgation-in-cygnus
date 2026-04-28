using UnityEngine;

/// <summary>
/// Data container for a single inter-wave upgrade option.
/// Designer-facing: create via Assets > Create > The Expurgation > Upgrade Item.
/// </summary>
[CreateAssetMenu(fileName = "NewUpgradeItem", menuName = "The Expurgation/Upgrade Item")]
public sealed class UpgradeItem : ScriptableObject
{
    [Header("Display")]
    public string upgradeName;

    [TextArea(2, 4)]
    public string description;

    [Header("Stat Modifier")]
    /// <summary>
    /// Key used at runtime to route the modifier to the correct system.
    /// Accepted values: "speed", "fire_rate", "damage", "projectile_extra", "heal".
    /// </summary>
    public string statModifierID;

    public float value;
}
