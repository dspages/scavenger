/// <summary>
/// Lightweight campaign clock and economy until a fuller save/meta system exists.
/// </summary>
public static class CampaignMeta
{
    public static int CurrentWeek { get; private set; } = 1;
    public static int Gold { get; set; } = 840;

    /// <summary>Call when a mission ends and the player returns to the home base.</summary>
    public static void AdvanceWeekAfterMission()
    {
        CurrentWeek++;
        RecruitPool.RefreshPoolForNewWeek();
    }

    public static void ResetNewCampaign()
    {
        CurrentWeek = 1;
        Gold = 840;
        RecruitPool.ResetForNewCampaign();
    }
}
