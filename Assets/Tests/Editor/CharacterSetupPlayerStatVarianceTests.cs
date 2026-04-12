using NUnit.Framework;

public class CharacterSetupPlayerStatVarianceTests
{
    static int SumBaseStats(CharacterSheet s)
    {
        return s.strength + s.agility + s.speed + s.intellect + s.endurance + s.perception + s.willpower;
    }

    [Test]
    public void ApplyStartingPlayerStatVariance_PreservesTotalOfBaseStats()
    {
        for (int i = 0; i < 200; i++)
        {
            var sheet = new CharacterSheet("t", CharacterSheet.CharacterClass.CLASS_SOLDIER, assignDefaults: false);
            Assert.AreEqual(28, SumBaseStats(sheet));
            CharacterSetup.ApplyStartingPlayerStatVariance(sheet);
            Assert.AreEqual(28, SumBaseStats(sheet));
            Assert.AreEqual(sheet.MaxHealth(), sheet.currentHealth);
            Assert.AreEqual(sheet.MaxMana(), sheet.currentMana);
            Assert.AreEqual(sheet.MaxSanity(), sheet.currentSanity);
        }
    }
}
