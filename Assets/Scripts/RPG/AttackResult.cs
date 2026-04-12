public struct AttackResult
{
    public bool hit;
    public bool critical;
    public bool defenderDied;
    public int damageDealt;
    public DamageType damageType;
    public float hitChance;
    public float critChance;
    public string attackerName;
    public string defenderName;
    public string weaponName;
    public string logMessage;
    /// <summary>How many backstab conditions applied (0–2).</summary>
    public int backstabCount;
}
