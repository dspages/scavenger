using UnityEngine;

/// <summary>Central mapping of damage types to colors for combat log, popup text, and UI. Use everywhere for consistency.</summary>
public static class DamageTypeColors
{
    public static Color Get(DamageType type)
    {
        switch (type)
        {
            case DamageType.Piercing:
            case DamageType.Slashing:
            case DamageType.Bludgeoning:
                return new Color(0.9f, 0.9f, 0.92f); // physical - steel grey
            case DamageType.Fire:
                return new Color(0.98f, 0.45f, 0.09f); // fire - orange
            case DamageType.Cold:
                return new Color(0.4f, 0.75f, 1f); // cold - light blue
            case DamageType.Acid:
                return new Color(0.55f, 0.95f, 0.35f); // acid/poison - green
            case DamageType.Lightning:
                return new Color(0.95f, 0.9f, 0.25f); // lightning - yellow
            case DamageType.Holy:
                return new Color(0.95f, 0.92f, 0.7f); // holy - warm white
            case DamageType.Dark:
                return new Color(0.55f, 0.35f, 0.75f); // dark - purple
            default:
                return Color.white;
        }
    }

    public static string GetHex(DamageType type)
    {
        return ColorUtility.ToHtmlStringRGB(Get(type));
    }

    /// <summary>Color for "MISS" or neutral feedback.</summary>
    public static Color MissColor => new Color(0.65f, 0.65f, 0.7f);
}
