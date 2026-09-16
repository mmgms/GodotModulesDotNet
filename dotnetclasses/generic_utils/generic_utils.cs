using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace GenericUtils;

public static class EnumerableUtils
{
	
	public static IEnumerable<(T First, T Second)> ConsecutivePairs<T>(
		IEnumerable<T> source)
	{
		using var enumerator = source.GetEnumerator();

		if (!enumerator.MoveNext())
			yield break;

		T previous = enumerator.Current;

		while (enumerator.MoveNext())
		{
			yield return (previous, enumerator.Current);
			previous = enumerator.Current;
		}
	}
}

public static class ListUtils
{

	private static Random rng = new Random();

	public static void Shuffle<T>(this IList<T> list)
	{
		int n = list.Count;
		while (n > 1) {
			n--;
			int k = rng.Next(n + 1);
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
	}

}

public static class Colors
{
	public static Color GetRandomColor()
	{
		float r = GD.Randf();
		float g = GD.Randf();
		float b = GD.Randf();

		return new Color(r, g, b);
	}
	public static Color GetRandomColor(float saturation, float value, float alpha=1.0f)
	{
		float hue = GD.Randf();
		var color = Color.FromHsv(hue, saturation, value);
		color.A = alpha;
		return color;
	}
}

public static class Drawing
{
	public static void drawArrow(Node2D node, Vector2 from, Vector2 to, Color color, float width, float arrowLen, float arrowDeg)
	{
		node.DrawLine(from, to, color, width);
		node.DrawLine(to, to + to.DirectionTo(from).Rotated(Mathf.DegToRad(-arrowDeg)) * arrowLen, color, width );
		node.DrawLine(to, to + to.DirectionTo(from).Rotated(Mathf.DegToRad(arrowDeg))* arrowLen, color, width );
	}

	public static void drawRectAtIdx(CanvasItem item, Vector2 tileSize, Vector2I idx, Color color, bool filled=true)
	{
		var pos = MathUtils.Funtions.GetCenteredPositionFromGridIdx(idx, tileSize);
		var rect = MathUtils.Funtions.RectBetweenPoints(pos - tileSize /2, pos + tileSize /2);  
		item.DrawRect(rect, color, filled);
	}
}

public static class GraphSearchUtils<T>
{
	public static List<T> FloodFill(T start, Func<T, IEnumerable<T>> getNeighboursCb)
    {
        var connected = new List<T>();
        var visited = new HashSet<T>();
        var queue = new Queue<T>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var elem = queue.Dequeue();
            connected.Add(elem);

            foreach (var neigh in getNeighboursCb(elem))
            {
                if (visited.Add(neigh))
                    queue.Enqueue(neigh);
            }
        }

        return connected;
    }
}

public class DisjointSet<T>
{
    public List<T> Elements { get; set; } = new();

    public static List<DisjointSet<T>> FindDisjointSets(IEnumerable<T> arr, Func<T, IEnumerable<T>> getNeighboursCb)
    {
        var sets = new List<DisjointSet<T>>();
        var labeledElements = new Dictionary<T, int>();

        int currentSetTag = 0;

        foreach (var elem in arr)
        {
            if (labeledElements.ContainsKey(elem))
                continue;

            var connected = GraphSearchUtils<T>.FloodFill(elem, getNeighboursCb);

            foreach (var x in connected) labeledElements[x] = currentSetTag;

            currentSetTag++;
        }

        for (int i = 0; i < currentSetTag; i++)
        {
            sets.Add(new DisjointSet<T>
            {
                Elements = labeledElements
                    .Where(x => x.Value == i)
                    .Select(x => x.Key)
                    .ToList()
            });
        }

        return sets;
    }

    
}

public static class EnumUtil {
    public static IEnumerable<T> GetValues<T>() {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }
}
