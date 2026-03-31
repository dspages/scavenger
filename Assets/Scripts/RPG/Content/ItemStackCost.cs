/// <summary>
/// Additional inventory costs for abilities (e.g. rare reagents). Resolved via <see cref="ContentRegistry"/> registry ids.
/// </summary>
[System.Serializable]
public struct ItemStackCost
{
    public string registryId;
    public int amount;
}
