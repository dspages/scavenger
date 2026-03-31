/// <summary>Combat tuning values as integer percents (5 = 5%) for readability. Merge layer converts to 0–1 for RNG.</summary>
public static class RpgCombatBalance
{
    public const int BaseRangedHitChancePercent = 75;
    public const int RangedHitPenaltyPerTileOverVisionRangePercent = 20;
    public const int RangedHitBonusPerTileUnderVisionRangePercent = 5;
    /// <summary>Applied per point of defender agility + total gear dodge bonus.</summary>
    public const int RangedHitPenaltyPerDefenderEvasionPointPercent = 5;

    public const int BlindedRangedHitPenaltyPercent = 40;

    public const int BaseCritChancePercent = 2;
    public const int CritChancePerAgilityPointPercent = 2;
    public const int EmpowerCritBonusPercent = 10;

    public const int MinHitChancePercent = 5;
    public const int MaxHitChancePercent = 95;
    public const int MaxCritChancePercent = 50;
}
