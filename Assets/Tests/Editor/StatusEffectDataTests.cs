using NUnit.Framework;

[TestFixture]
public class StatusEffectDataTests
{
    private CharacterSheet MakeSheet()
    {
        return new CharacterSheet("Test", CharacterSheet.CharacterClass.CLASS_SOLDIER);
    }

    [Test]
    public void Regeneration_HealsTarget()
    {
        var sheet = MakeSheet();
        sheet.ReceiveDamage(20);
        int hpBefore = sheet.currentHealth;

        StatusEffect.ApplyEffectData(StatusEffectCatalog.Regeneration, 10, sheet);

        Assert.AreEqual(hpBefore + 5, sheet.currentHealth);
    }

    [Test]
    public void Poisoned_DealsPureDamage()
    {
        var sheet = MakeSheet();
        int hpBefore = sheet.currentHealth;

        StatusEffect.ApplyEffectData(StatusEffectCatalog.Poisoned, 10, sheet);

        Assert.AreEqual(hpBefore - 5, sheet.currentHealth);
    }

    [Test]
    public void Burning_DealsNormalDamage()
    {
        var sheet = MakeSheet();
        int hpBefore = sheet.currentHealth;

        StatusEffect.ApplyEffectData(StatusEffectCatalog.Burning, 10, sheet);

        Assert.AreEqual(hpBefore - 10, sheet.currentHealth);
    }

    [Test]
    public void Knockdown_SetsAPToZero()
    {
        var sheet = MakeSheet();
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Knockdown, 10, sheet);
        Assert.AreEqual(0, ap);
    }

    [Test]
    public void Petrified_SetsAPToZero()
    {
        var sheet = MakeSheet();
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Petrified, 8, sheet);
        Assert.AreEqual(0, ap);
    }

    [Test]
    public void Frozen_SetsAPToZero()
    {
        var sheet = MakeSheet();
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Frozen, 12, sheet);
        Assert.AreEqual(0, ap);
    }

    [Test]
    public void Slowed_ReducesAPByFour_HighAP()
    {
        var sheet = MakeSheet();
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Slowed, 10, sheet);
        Assert.AreEqual(6, ap);
    }

    [Test]
    public void Slowed_ClampsToFloor_MediumAP()
    {
        var sheet = MakeSheet();
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Slowed, 4, sheet);
        Assert.AreEqual(2, ap);
    }

    [Test]
    public void Slowed_ClampsToFloor_LowAP()
    {
        var sheet = MakeSheet();
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Slowed, 2, sheet);
        Assert.AreEqual(2, ap);
    }

    [Test]
    public void Slowed_SkipsIfAPZero()
    {
        var sheet = MakeSheet();
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Slowed, 0, sheet);
        Assert.AreEqual(0, ap);
    }

    [Test]
    public void Mobility_AddsAP()
    {
        var sheet = MakeSheet();
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Mobility, 8, sheet);
        Assert.AreEqual(10, ap);
    }

    [Test]
    public void Mobility_SkipsIfAPZero()
    {
        var sheet = MakeSheet();
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Mobility, 0, sheet);
        Assert.AreEqual(0, ap);
    }

    [Test]
    public void NoOpEffect_DoesNotChangeAP()
    {
        var sheet = MakeSheet();
        int hpBefore = sheet.currentHealth;
        int ap = StatusEffect.ApplyEffectData(StatusEffectCatalog.Rage, 6, sheet);

        Assert.AreEqual(6, ap);
        Assert.AreEqual(hpBefore, sheet.currentHealth);
    }

    [Test]
    public void CatalogCoversAllEffectTypes()
    {
        foreach (StatusEffect.EffectType et in System.Enum.GetValues(typeof(StatusEffect.EffectType)))
        {
            var data = ContentRegistry.GetEffectData(et);
            Assert.IsNotNull(data, $"Missing catalog entry for {et}");
        }
    }

    [Test]
    public void CustomEffect_CanBeRegistered()
    {
        var custom = new StatusEffectData
        {
            id = "test_custom_effect",
            effectType = StatusEffect.EffectType.RAGE,
            displayName = "Test Rage Override",
            healPerRound = 99,
        };
        ContentRegistry.Register(custom);

        var retrieved = ContentRegistry.GetEffectData(StatusEffect.EffectType.RAGE);
        Assert.AreEqual("Test Rage Override", retrieved.displayName);

        ContentRegistry.Register(StatusEffectCatalog.Rage);
    }
}
