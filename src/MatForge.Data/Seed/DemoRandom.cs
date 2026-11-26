namespace MatForge.Data.Seed;

internal static class DemoRandom
{
    public static bool Chance(this Random random, double probability) => random.NextDouble() < probability;

    // Picks an item with a probability proportional to its weight
    public static T Pick<T>(this Random random, IReadOnlyList<(T Item, int Weight)> items)
    {
        var roll = random.Next(items.Sum(i => i.Weight));
        foreach (var (item, weight) in items)
        {
            if (roll < weight)
            {
                return item;
            }

            roll -= weight;
        }

        throw new InvalidOperationException("Weights must be positive.");
    }
}
