using UnityEngine;

/// <summary>Central mapping of damage types to colors for combat log, popup text, and UI. Use everywhere for consistency.</summary>
public static class DamageTypeColors
{
    public static Color Get(EquippableHandheld.DamageType type)
    {
        switch (type)
        {
            case EquippableHandheld.DamageType.Piercing:
            case EquippableHandheld.DamageType.Slashing:
            case EquippableHandheld.DamageType.Bludgeoning:
                return new Color(0.9f, 0.9f, 0.92f); // physical - steel grey
            case EquippableHandheld.DamageType.Fire:
                return new Color(0.98f, 0.45f, 0.09f); // fire - orange
            case EquippableHandheld.DamageType.Cold:
                return new Color(0.4f, 0.75f, 1f); // cold - light blue
            case EquippableHandheld.DamageType.Acid:
                return new Color(0.55f, 0.95f, 0.35f); // acid/poison - green
            case EquippableHandheld.DamageType.Lightning:
                return new Color(0.95f, 0.9f, 0.25f); // lightning - yellow
            case EquippableHandheld.DamageType.Holy:
                return new Color(0.95f, 0.92f, 0.7f); // holy - warm white
            case EquippableHandheld.DamageType.Dark:
                return new Color(0.55f, 0.35f, 0.75f); // dark - purple
            default:
                return Color.white;
        }
    }

    public static string GetHex(EquippableHandheld.DamageType type)
    {
        return ColorUtility.ToHtmlStringRGB(Get(type));
    }

    /// <summary>Color for "MISS" or neutral feedback.</summary>
    public static Color MissColor => new Color(0.65f, 0.65f, 0.7f);
}
