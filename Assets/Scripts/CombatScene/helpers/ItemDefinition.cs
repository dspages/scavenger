using UnityEngine;

[CreateAssetMenu(menuName = "Scavenger/Item Definition", fileName = "ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea]
    public string description;
    public EquippableItem.EquipmentSlot slot = EquippableItem.EquipmentSlot.RightHand;
    public Sprite icon;

    // Optional future hook: items can reference behaviors via code/ids
}



