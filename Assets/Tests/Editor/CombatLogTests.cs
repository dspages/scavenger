using NUnit.Framework;

public class CombatLogTests
{
    [SetUp]
    public void SetUp()
    {
        CombatLog.Clear();
    }

    [Test]
    public void Log_AddsEntry()
    {
        CombatLog.Log("Test message");

        Assert.AreEqual(1, CombatLog.Entries.Count);
        Assert.AreEqual("Test message", CombatLog.Entries[0]);
    }

    [Test]
    public void Log_EmptyString_Ignored()
    {
        CombatLog.Log("");

        Assert.AreEqual(0, CombatLog.Entries.Count);
    }

    [Test]
    public void Log_Null_Ignored()
    {
        CombatLog.Log(null);

        Assert.AreEqual(0, CombatLog.Entries.Count);
    }

    [Test]
    public void Log_FiresEvent()
    {
        string received = null;
        void Handler(string msg, EquippableHandheld.DamageType? _) { received = msg; }
        CombatLog.OnEntryAdded += Handler;

        CombatLog.Log("Hello");

        Assert.AreEqual("Hello", received);

        CombatLog.OnEntryAdded -= Handler;
    }

    [Test]
    public void Clear_RemovesAllEntries()
    {
        CombatLog.Log("A");
        CombatLog.Log("B");

        CombatLog.Clear();

        Assert.AreEqual(0, CombatLog.Entries.Count);
    }

    [Test]
    public void Log_MultipleEntries_PreservesOrder()
    {
        CombatLog.Log("First");
        CombatLog.Log("Second");
        CombatLog.Log("Third");

        Assert.AreEqual(3, CombatLog.Entries.Count);
        Assert.AreEqual("First", CombatLog.Entries[0]);
        Assert.AreEqual("Third", CombatLog.Entries[2]);
    }

    [Test]
    public void Log_ExceedsMaxEntries_RemovesOldest()
    {
        for (int i = 0; i < CombatLog.MaxEntries + 10; i++)
        {
            CombatLog.Log($"Entry {i}");
        }

        Assert.AreEqual(CombatLog.MaxEntries, CombatLog.Entries.Count);
        Assert.AreEqual($"Entry 10", CombatLog.Entries[0]);
    }

    [Test]
    public void AttackResolver_Result_CanBeLogged()
    {
        var attacker = new CharacterSheet("Rook", CharacterSheet.CharacterClass.CLASS_ROGUE);
        var defender = new CharacterSheet("Goblin", CharacterSheet.CharacterClass.CLASS_SOLDIER);
        var context = AttackContext.Melee(weapon: null);

        var result = AttackResolver.Resolve(attacker, defender, context);
        CombatLog.Log(result.logMessage);

        Assert.AreEqual(1, CombatLog.Entries.Count);
        Assert.IsTrue(CombatLog.Entries[0].Contains("Rook"));
        Assert.IsTrue(CombatLog.Entries[0].Contains("Goblin"));
    }
}
