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
	
}

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

	public static Vector2I GetGridIdxFromPos(Vector2 vec, Vector2 tileSize)
	{
		Vector2 normVec = vec / tileSize;
		return new Vector2I(
			Mathf.FloorToInt(normVec.X),
			Mathf.FloorToInt(normVec.Y)
		);
	}

	public static Vector2 GetCenteredPositionFromGridIdx(
		Vector2I idx,
		Vector2 tileSize)
	{
		return (new Vector2(idx.X, idx.Y) + Vector2.One * 0.5f) * tileSize;
	}

	public static Vector2 GetGridSnappedCenteredPosition(
		Vector2 vec,
		Vector2 tileSize)
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


}