using System;
using System.Collections.Generic;


namespace PixelArmies.SimCore;

public sealed class Rng
{
    private readonly Random _r;

    public Rng(int seed) => _r = new Random(seed);

    public int NextInt(int minInclusive, int maxExclusive) => _r.Next(minInclusive, maxExclusive);

    public float NextFloat01() => (float)_r.NextDouble();

    public bool Chance(float p) => NextFloat01() < p;

    public T PickWeighted<T>(IReadOnlyList<(T item, float weight)> items)
    {
        float total = 0f;
        for (int i = 0; i < items.Count; i++) total += Math.Max(0f, items[i].weight);

        if (total <= 0f) return items[0].item;

        float r = NextFloat01() * total;
        for (int i = 0; i < items.Count; i++)
        {
            r -= Math.Max(0f, items[i].weight);
            if (r <= 0f) return items[i].item;
        }
        return items[^1].item;
    }
}
