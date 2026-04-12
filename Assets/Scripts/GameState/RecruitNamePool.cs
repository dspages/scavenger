using UnityEngine;

/// <summary>
/// Shared first-name pool for weekly recruit generation (<see cref="RecruitPool"/>) and
/// default starting party members (<see cref="PlayerParty.Reset"/>).
/// </summary>
public static class RecruitNamePool
{
    public static readonly string[] FirstNames =
    {
        "Asha", "Bren", "Cole", "Dara", "Eli", "Fen", "Greta", "Hal", "Iris", "Jace",
        "Kira", "Lorne", "Mira", "Nox", "Orin", "Pike", "Quinn", "Rhea", "Soren", "Tess",
    };

    public static string PickRandomFirstName()
    {
        return FirstNames[Globals.rng.Next(FirstNames.Length)];
    }

    /// <summary>
    /// Picks <paramref name="count"/> distinct random names via partial Fisher–Yates shuffle.
    /// If <paramref name="count"/> exceeds the pool size, returns one name per pool entry.
    /// </summary>
    public static string[] PickDistinctFirstNames(int count)
    {
        if (count <= 0)
            return System.Array.Empty<string>();

        int pool = FirstNames.Length;
        if (count > pool)
            count = pool;

        var indices = new int[pool];
        for (int i = 0; i < pool; i++)
            indices[i] = i;

        for (int i = 0; i < count; i++)
        {
            int j = Globals.rng.Next(i, pool);
            int tmp = indices[i];
            indices[i] = indices[j];
            indices[j] = tmp;
        }

        var result = new string[count];
        for (int i = 0; i < count; i++)
            result[i] = FirstNames[indices[i]];
        return result;
    }
}
