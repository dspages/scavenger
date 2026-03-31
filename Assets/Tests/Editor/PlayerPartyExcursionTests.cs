using NUnit.Framework;

public class PlayerPartyExcursionTests
{
    [TearDown]
    public void TearDown()
    {
        PlayerParty.Reset();
    }

    [Test]
    public void TryAssignExcursionSlot_DisplacesPreviousOccupant()
    {
        PlayerParty.Reset();
        Assert.AreEqual(0, PlayerParty.GetExcursionSlotPartyIndex(0));
        Assert.IsTrue(PlayerParty.TryAssignExcursionSlot(0, 2));
        Assert.AreEqual(2, PlayerParty.GetExcursionSlotPartyIndex(0));
        Assert.AreEqual(-1, PlayerParty.GetExcursionSlotPartyIndex(2));
        Assert.IsFalse(PlayerParty.IsOnExcursionSquad(0));
    }

    [Test]
    public void IsEligibleForExcursionDropdown_OnlyWhenNotOnSquadAndNoBaseJob()
    {
        PlayerParty.Reset();
        PlayerParty.TryClearExcursionSlot(0);
        Assert.IsTrue(PlayerParty.IsEligibleForExcursionDropdown(0));
        Assert.IsFalse(PlayerParty.IsEligibleForExcursionDropdown(1));
    }

    [Test]
    public void TryClearExcursionSlot_LeavesSlotEmpty()
    {
        PlayerParty.Reset();
        PlayerParty.TryClearExcursionSlot(1);
        Assert.AreEqual(-1, PlayerParty.GetExcursionSlotPartyIndex(1));
        Assert.IsTrue(PlayerParty.IsEligibleForExcursionDropdown(1));
    }
}
