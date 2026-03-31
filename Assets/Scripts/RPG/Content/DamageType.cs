/// <summary>
/// Damage / resistance element for weapons, abilities, combat resolution, and UI coloring.
/// Lives in Content so catalog data and inventory items share one type without nesting under <see cref="EquippableHandheld"/>.
/// </summary>
public enum DamageType
{
    Piercing,
    Bludgeoning,
    Slashing,
    Fire,
    Cold,
    Acid,
    Lightning,
    Holy,
    Dark
}
