using Godot;
using System;
using System.Collections.Generic;

public static class TileMapUtils
{
	public static float GetTileRotationDeg(TileMapLayer tileMapLayer, Vector2I coord)
	{
		bool isTranspose = tileMapLayer.IsCellTransposed(coord);
		bool isFlipH = tileMapLayer.IsCellFlippedH(coord);
		bool isFlipV = tileMapLayer.IsCellFlippedV(coord);

		if (isTranspose && isFlipH)
			return 90.0f;

		if (isFlipH && isFlipV)
			return 180.0f;

		if (isTranspose && isFlipV)
			return 270.0f;

		return 0.0f;
	}

	public static void SetTileRotationDeg(
		TileMapLayer tileMapLayer,
		Vector2I coord,
		int sourceId,
		Vector2I atlasCoords,
		float rotation)
	{
		long transform = (int)rotation switch
		{
			0   => 0,
			90  => TileSetAtlasSource.TransformTranspose |
				TileSetAtlasSource.TransformFlipH,
			180 => TileSetAtlasSource.TransformFlipH |
				TileSetAtlasSource.TransformFlipV,
			270 => TileSetAtlasSource.TransformTranspose |
				TileSetAtlasSource.TransformFlipV,
			_ => throw new ArgumentException(
				$"Unsupported tile rotation: {rotation}")
		};

		tileMapLayer.SetCell(coord, sourceId, atlasCoords, (int)transform);
	}
}