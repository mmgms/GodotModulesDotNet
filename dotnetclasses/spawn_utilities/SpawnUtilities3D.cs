
using Godot;
using System.Collections.Generic;

public static class SpawnUtilities3D
{
	public class SpawnInfo
	{
		public Vector3 Position;
		public Vector3 LookDirection;
	}

	public static List<SpawnInfo> GetLineSpawnInfo(
		int count,
		Vector3 posStart,
		Vector3 posEnd)
	{
		var infos = new List<SpawnInfo>();

		if (count <= 0)
			return infos;

		Vector3 direction = posStart.DirectionTo(posEnd);

		float step = count == 1
			? 0.0f
			: posStart.DistanceTo(posEnd) / (count - 1);

		for (int i = 0; i < count; i++)
		{
			var info = new SpawnInfo
			{
				Position = posStart + direction * step * i
			};

			infos.Add(info);
		}

		return infos;
	}

	/// <summary>
	/// Calculates the direction required to intercept a moving target.
	/// Spawn velocity is accounted for in the relative target velocity.
	/// Returns null if interception is impossible.
	/// </summary>
	public static SpawnInfo GetReachTargetSpawnInfo(
		Vector3 spawnPos,
		Vector3 spawnVel,
		float projSpeed,
		Vector3 targetPos,
		Vector3 targetVel)
	{
		Vector3 tp = targetPos - spawnPos;
		Vector3 tv = targetVel - spawnVel;
		float bs = projSpeed;

		float a = tv.Dot(tv) - bs * bs;
		float b = 2.0f * tp.Dot(tv);
		float c = tp.Dot(tp);

		float discriminant = b * b - 4.0f * a * c;

		if (discriminant < 0.0f || bs <= 0.0f)
			return null;

		float dt;

		if (Mathf.IsZeroApprox(a))
		{
			if (Mathf.IsZeroApprox(b))
				return null;

			dt = -c / b;
		}
		else
		{
			float sqrtDiscriminant = Mathf.Sqrt(discriminant);

			float dtMinus = (-b - sqrtDiscriminant) / (2.0f * a);
			float dtPlus = (-b + sqrtDiscriminant) / (2.0f * a);

			if (dtMinus >= 0.0f && dtPlus >= 0.0f)
				dt = Mathf.Min(dtMinus, dtPlus);
			else if (dtMinus >= 0.0f)
				dt = dtMinus;
			else
				dt = dtPlus;
		}

		if (dt < 0.0f)
			return null;

		Vector3 direction = (tp + tv * dt) / (bs * dt);

		return new SpawnInfo
		{
			Position = spawnPos,
			LookDirection = direction
		};
	}

	/// <summary>
	/// Accuracy is between 0 and 100.
	/// Shooting direction is -shootFromNode.GlobalBasis.Z.
	/// Spread and accuracy offsets are applied in the shooter's local basis.
	/// </summary>
	public static List<SpawnInfo> GetProjectilePelletSpawnInfo(
		Node3D shootFromNode,
		int pelletCount,
		float accuracy,
		float spreadAngle)
	{
		var infos = new List<SpawnInfo>();

		if (pelletCount <= 0)
			return infos;

		float accuracySpread = (100.0f - accuracy) / 1000.0f;
		Basis basis = shootFromNode.GlobalBasis;
		Vector3 forward = -basis.Z;

		for (int i = 0; i < pelletCount; i++)
		{
			float accuracyX = (float)GD.RandRange(
				-accuracySpread, accuracySpread);

			float accuracyY = (float)GD.RandRange(
				-accuracySpread, accuracySpread);

			Vector3 direction = forward
				+ basis * new Vector3(accuracyX, accuracyY, 0.0f);

			if (pelletCount > 1)
			{
				float spreadX = (float)GD.RandRange(
					-spreadAngle, spreadAngle);

				float spreadY = (float)GD.RandRange(
					-spreadAngle, spreadAngle);

				direction += basis
					* new Vector3(spreadX, spreadY, 0.0f);
			}

			infos.Add(new SpawnInfo
			{
				Position = shootFromNode.GlobalPosition,
				LookDirection = direction
			});
		}

		return infos;
	}
}
