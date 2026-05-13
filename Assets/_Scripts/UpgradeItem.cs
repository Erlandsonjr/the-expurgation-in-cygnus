using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeItem", menuName = "The Expurgation/Upgrade Item")]
public sealed class UpgradeItem : ScriptableObject
{
    [Header("Display")]
    public string upgradeName;

    [TextArea(2, 4)]
    public string description;

    [Header("Stat Modifier")]
    public string statModifierID;

    public float value;
}
