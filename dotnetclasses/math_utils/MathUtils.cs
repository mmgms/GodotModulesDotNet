using System;
using System.Collections.Generic;
using System.Linq;
using Godot;


namespace MathUtils;

public static class Definitions{
	public static readonly float[] Rotations =
	{
		0.0f,
		-Mathf.Pi / 2.0f,
		Mathf.Pi,
		Mathf.Pi / 2.0f
	};

	public static readonly Vector2I[] FourDirections =
    {
        Vector2I.Down,
        Vector2I.Left,
        Vector2I.Right,
        Vector2I.Up
    };

    public static readonly Vector2I[] EightDirections =
    {
        Vector2I.Down,
        Vector2I.Left,
        Vector2I.Right,
        Vector2I.Up,
        Vector2I.Down + Vector2I.Left,
        Vector2I.Down + Vector2I.Right,
        Vector2I.Up + Vector2I.Left,
        Vector2I.Up + Vector2I.Right
    };

	public static readonly Vector3I[] SixDirections =
    {
        Vector3I.Down,
        Vector3I.Left,
        Vector3I.Right,
        Vector3I.Up,
		Vector3I.Forward,
		Vector3I.Back
    };

	
}

public static class Funtions {

	public static IEnumerable<Vector3I> getNeighbours4XZ(Vector3I point)
	{
		foreach (var i in Enumerable.Range(0, 4))
		{
			yield return point + new Vector3I(Definitions.FourDirections[i].X, 0, Definitions.FourDirections[i].Y);
		};
	}

	public static IEnumerable<Vector3I> getNeighbours8XZ(Vector3I point)
	{
		foreach (var i in Enumerable.Range(0, 8))
		{
			yield return point + new Vector3I(Definitions.EightDirections[i].X, 0, Definitions.EightDirections[i].Y);
		};
	}

	public static IEnumerable<Vector3I> getNeighbours6(Vector3I point)
	{
		foreach (var i in Enumerable.Range(0, 6))
		{
			yield return point + Definitions.SixDirections[i];
		};
	}

	public static IEnumerable<Vector3I> getNeighbours8XZAvoidCorners(Vector3I start, Func<Vector3I, bool> isSolid)
	{
		return getNeighbours8XZ(start)
			.Where((end) => !isSolid(end))
			.Where((end) =>
			{
				if(end.X == start.X || end.Z == start.Z)
				{
					return true;
				}
				var vecTo = end - start;
				if(isSolid(start + vecTo * new Vector3I(1, 0, 0)))
				{
					return false;
				}
				if(isSolid(start + vecTo * new Vector3I(0, 0, 1)))
				{
					return false;
				}
				return true;
			});
	}

	public static bool IsPointInCone2D( Vector2 point, Vector2 conePos, Vector2 coneDir, float halfAngle)
	{
		float angleToTarget = coneDir.AngleTo(conePos.DirectionTo(point));

		return Mathf.Abs(angleToTarget) < halfAngle;
	}

	public static bool IsPointInCone3D( Vector3 point, Vector3 conePos, Vector3 coneDir, float halfAngle)
	{
		float angleToTarget = coneDir.AngleTo(conePos.DirectionTo(point));

		return Mathf.Abs(angleToTarget) < halfAngle;
	}
	
	public static IEnumerable<Vector2I> getBorders(Vector2I size)
	{
		foreach (var i in Enumerable.Range(0, size.X))
		{
			yield return new Vector2I(i, 0);
			yield return new Vector2I(i, size.Y-1);
		}
		foreach (var i in Enumerable.Range(0, size.Y))
		{
			yield return new Vector2I(0, i);
			yield return new Vector2I(size.X-1, i);
		}
	}
	
	public static int getRotationIdx(float rotationRad)
	{
		foreach(var i in Enumerable.Range(0, 4))
		{
			if (Mathf.IsEqualApprox(Definitions.Rotations[i], rotationRad))
			{
				return i;
			}
		}
		return 0;
	}
	public static Vector2I getVecIFromRot(int rotation)
	{
		return toVec2I(Vector2.Right.Rotated(Definitions.Rotations[rotation]));
	}

	public static Vector2I toVec2I(Vector2 vec)
	{
		return new Vector2I((int)Math.Round(vec.X), (int)Math.Round(vec.Y));
	}

	public static Vector2 toVec2(Vector2I vec)
	{
		return new Vector2(vec.X, vec.Y);
	}

	public static Vector3I toVec3I(Vector3 vec)
	{
		return new Vector3I((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z));
	}

	public static Vector3 toVec3(Vector3I vec)
	{
		return new Vector3(vec.X, vec.Y, vec.Z);
	}

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

	public static Vector2I GetGridIdxFromPos(Vector2 vec, Vector2 tileSize)
	{
		Vector2 normVec = vec / tileSize;
		return new Vector2I(
			Mathf.FloorToInt(normVec.X),
			Mathf.FloorToInt(normVec.Y)
		);
	}

	public static Vector2 GetCenteredPositionFromGridIdx( Vector2I idx, Vector2 tileSize)
	{
		return (new Vector2(idx.X, idx.Y) + Vector2.One * 0.5f) * tileSize;
	}

	public static Vector2 GetGridSnappedCenteredPosition(Vector2 vec,Vector2 tileSize)
	{
		return GetCenteredPositionFromGridIdx(
			GetGridIdxFromPos(vec, tileSize),
			tileSize
		);
	}

	
	public static Vector3I GetGridIdxFromPos(Vector3 vec, Vector3 tileSize)
	{
		Vector3 normVec = vec / tileSize;
		return new Vector3I(
			Mathf.FloorToInt(normVec.X),
			Mathf.FloorToInt(normVec.Y),
			Mathf.FloorToInt(normVec.Z)
		);
	}

	public static Vector3 GetCenteredPositionFromGridIdx( Vector3I idx, Vector3 tileSize)
	{
		return (new Vector3(idx.X, idx.Y, idx.Z) + Vector3.One * 0.5f) * tileSize;
	}

	public static Vector3 GetGridSnappedCenteredPosition(Vector3 vec,Vector3 tileSize)
	{
		return GetCenteredPositionFromGridIdx(
			GetGridIdxFromPos(vec, tileSize),
			tileSize
		);
	}


	public static Vector2 GetRotationSnappedDirection(
		Vector2 vec,
		float snapSizeRad)
	{
		float angle = vec.Angle();
		float angleSnapped = Mathf.Snapped(angle, snapSizeRad);

		return Vector2.Right.Rotated(angleSnapped) * vec.Length();
	}

	public static Rect2 RectBetweenPoints(Vector2 a, Vector2 b)
	{
		var rect = new Rect2();

		rect.Position = new Vector2(
			Mathf.Min(a.X, b.X),
			Mathf.Min(a.Y, b.Y)
		);

		rect.Size = new Vector2(
			Mathf.Abs(a.X - b.X),
			Mathf.Abs(a.Y - b.Y)
		);

		return rect;
	}

	// Get center that places an item with extents and rotation in a grid
	// with tileSize. The center is "normalized" with respect to the grid.
	public static Vector2 GetSnappedCenterFromExtentsRotation(Vector2 pos, Vector2I extents, int rotation = 0, Vector2? tileSize = null)
	{
		Vector2 actualTileSize = tileSize ?? Vector2.One;

		Vector2 relPosNorm = pos / actualTileSize;

		Vector2I flippedExtents = new Vector2I(
			extents.Y,
			extents.X
		);

		if (rotation % 2 == 1)
			extents = flippedExtents;

		Vector2 extentsCenter = new Vector2(
			extents.X,
			extents.Y
		) / 2.0f;

		Vector2 gridOffset = new Vector2(
			Mathf.PosMod(extentsCenter.X, 1.0f),
			Mathf.PosMod(extentsCenter.Y, 1.0f)
		);

		Vector2 center = (relPosNorm - gridOffset)
			.Snapped(Vector2.One)
			+ gridOffset;

		return center;
	}

	public static Vector2I GetGridIdxFromCenterAndRotation(Vector2I extents, Vector2I relGridIdx, Vector2 positionCenter, int rotation)
	{
		var relCenter = new Vector2(extents.X, extents.Y)/2.0f;
		var vecToRelGridIdx = new Vector2(relGridIdx.X, relGridIdx.Y) + Vector2.One/2.0f - relCenter;
		var rotatedVecToRelGridIdx = vecToRelGridIdx.Rotated(Definitions.Rotations[rotation]);
		var temp = (positionCenter + rotatedVecToRelGridIdx).Floor();
		return new Vector2I((int)temp.X, (int)temp.Y);
		
	}

	public static Vector3 RandomDirectionXZ()
	{
		double angle = GD.Randf() * Mathf.Tau;
		return new Vector3(Mathf.Cos((float)angle), 0, Mathf.Sin((float)angle));
	}

	public static Vector2 RandomDirection2D()
	{
		double angle = GD.Randf() * Mathf.Tau;
		return new Vector2(Mathf.Cos((float)angle), Mathf.Sin((float)angle));
	}


}