using System;
using System.Collections.Generic;
using System.Linq;

namespace MathUtils;

public static class Funtions {
	

	public static float Remap(float value, float minIn, float maxIn, float minOut, float maxOut)
	{
		return minOut + (value - minIn) * (maxOut - minOut) / (maxIn - minIn);
	}


public static T SampleWeighted<T>(IEnumerable<T> enumerable, Func<T, float> weightFunc)
{
    var items = enumerable.ToList();

    float totalWeight = 0f;

    foreach (var item in items)
    {
        float weight = weightFunc(item);

        if (weight < 0f)
            throw new ArgumentException("Weights cannot be negative.");

        totalWeight += weight;
    }

    if (totalWeight <= 0f)
        throw new ArgumentException("The total weight must be greater than zero.");

    float random = Random.Shared.NextSingle() * totalWeight;

    foreach (var item in items)
    {
        random -= weightFunc(item);

        if (random <= 0f)
            return item;
    }

    return items[^1];
}
}