public class StatusEffectData
{
    public string id;
    public StatusEffect.EffectType effectType;
    public string displayName;

    public int healPerRound;
    public int damagePerRound;
    public bool isPureDamage;

    public enum APEffect { None, Zero, Modify }
    public APEffect apEffect = APEffect.None;
    public int apModifier;
    public int apFloor;

    public string popupText;
}
