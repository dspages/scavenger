using System.Text;
using UnityEngine;

public static class TooltipTextBuilder
{
    public static (string compact, string detailed) ForEquipped(InventoryItem item, EquippableItem.EquipmentSlot slot)
    {
        if (item == null)
            return ($"{slot}: (empty)", null);
        return ForItem(item);
    }

    public static (string compact, string detailed) ForItem(InventoryItem item)
    {
        if (item == null) return (null, null);

        var compact = item.GetDisplayName();
        var sb = new StringBuilder(256);
        sb.AppendLine(item.itemName);
        if (item.rarity != InventoryItem.ItemRarity.Common)
            sb.AppendLine($"Rarity: {item.rarity}");
        if (item.weight != 0)
            sb.AppendLine($"Weight: {item.weight}");
        if (item.MaxStack > 1)
            sb.AppendLine($"Stack: {item.PeekStackSize()}/{item.MaxStack}");

        if (item is EquippableHandheld w)
        {
            sb.AppendLine($"Damage: {w.damage} ({w.damageType})");
            sb.AppendLine($"Weapon: {w.weaponType} / {w.rangeType}");
            if (w.minRange == w.maxRange) sb.AppendLine($"Range: {w.minRange}");
            else sb.AppendLine($"Range: {w.minRange}-{w.maxRange}");
            if (w.splashRadius > 0) sb.AppendLine($"Splash: {w.splashRadius}");
            if (w.actionPointCost > 0) sb.AppendLine($"AP Cost: {w.actionPointCost}");
            if (w.armorBonus != 0) sb.AppendLine($"Armor Bonus: +{w.armorBonus}");
            if (w.dodgeBonus != 0) sb.AppendLine($"Dodge Bonus: +{w.dodgeBonus}");
        }
        else if (item is EquippableItem eq)
        {
            if (eq.armorBonus != 0) sb.AppendLine($"Armor Bonus: +{eq.armorBonus}");
            if (eq.dodgeBonus != 0) sb.AppendLine($"Dodge Bonus: +{eq.dodgeBonus}");
            sb.AppendLine($"Slot: {eq.slot}");
        }

        if (!string.IsNullOrWhiteSpace(item.description))
        {
            sb.AppendLine();
            sb.Append(item.description.Trim());
        }

        return (compact, sb.ToString().TrimEnd());
    }
}

