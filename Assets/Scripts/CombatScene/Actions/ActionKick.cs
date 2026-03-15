public class ActionKick : ActionMeleeAttack
{
    public override void ConfigureAction()
    {
        base.ConfigureAction();
        actionDisplayName = "Kick";
        BASE_ACTION_COST = 10;
    }
}
