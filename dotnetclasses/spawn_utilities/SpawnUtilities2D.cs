using Godot;
using System.Collections.Generic;

public static class SpawnUtilities2D
{
	public class SpawnInfo
	{
		public Vector2 Position;
		public Vector2 LookDirection;
	}

	public static List<SpawnInfo> GetCircleSpawnInfo(
		int count,
		Vector2 centerPos,
		float radius,
		float startAngleDeg)
	{
		var infos = new List<SpawnInfo>();

		if (count <= 0)
			return infos;

		float angleStep = Mathf.Tau / count;
		float startAngle = Mathf.DegToRad(startAngleDeg);

		for (int i = 0; i < count; i++)
		{
			var info = new SpawnInfo();

			info.LookDirection = Vector2.FromAngle(
				startAngle + i * angleStep);

			info.Position = centerPos
				+ info.LookDirection * radius;

			infos.Add(info);
		}

		return infos;
	}

	public static List<SpawnInfo> GetArcSpawnInfo(
		int count,
		Vector2 centerPos,
		float radius,
		Vector2 dir,
		float halfAngleDeg)
	{
		var infos = new List<SpawnInfo>();

		if (count <= 0)
			return infos;

		float halfAngleRad = Mathf.DegToRad(halfAngleDeg);
		float angleStep = count == 1
			? 0.0f
			: (2.0f * halfAngleRad) / (count - 1);

		float startAngle = count == 1
			? dir.Angle()
			: dir.Angle() - halfAngleRad;

		for (int i = 0; i < count; i++)
		{
			var info = new SpawnInfo();

			info.LookDirection = Vector2.FromAngle(
				startAngle + i * angleStep);

			info.Position = centerPos
				+ info.LookDirection * radius;

			infos.Add(info);
		}

		return infos;
	}

	public static List<SpawnInfo> GetLineSpawnInfo(
		int count,
		Vector2 posStart,
		Vector2 posEnd)
	{
		var infos = new List<SpawnInfo>();

		if (count <= 0)
			return infos;

		Vector2 direction = posStart.DirectionTo(posEnd);

		float step = count == 1
			? 0.0f
			: posStart.DistanceTo(posEnd) / (count - 1);

		for (int i = 0; i < count; i++)
		{
			var info = new SpawnInfo();

			info.Position = posStart + direction * step * i;

			infos.Add(info);
		}

		return infos;
	}

	/// <summary>
	/// Calculates the direction needed to intercept a moving target.
	/// Spawn velocity is subtracted from target velocity to calculate
	/// the projectile's relative motion.
	/// Returns null if no valid interception time exists.
	/// </summary>
	public static SpawnInfo GetReachTargetSpawnInfo(
		Vector2 spawnPos,
		Vector2 spawnVel,
		float projSpeed,
		Vector2 targetPos,
		Vector2 targetVel)
	{
		Vector2 tp = targetPos - spawnPos;
		Vector2 tv = targetVel - spawnVel;
		float bs = projSpeed;

		float a = tv.Dot(tv) - bs * bs;
		float b = 2.0f * tp.Dot(tv);
		float c = tp.Dot(tp);

		float discriminant = b * b - 4.0f * a * c;

		if (discriminant < 0.0f)
			return null;

		float sqrtDiscriminant = Mathf.Sqrt(discriminant);

		// Handle the linear case where a is approximately zero.
		float dt;

		if (Mathf.IsZeroApprox(a))
		{
			if (Mathf.IsZeroApprox(b))
				return null;

			dt = -c / b;
		}
		else
		{
			float dtMinus = (-b - sqrtDiscriminant) / (2.0f * a);
			float dtPlus = (-b + sqrtDiscriminant) / (2.0f * a);

			// Choose the earliest non-negative interception time.
			if (dtMinus >= 0.0f && dtPlus >= 0.0f)
				dt = Mathf.Min(dtMinus, dtPlus);
			else if (dtMinus >= 0.0f)
				dt = dtMinus;
			else
				dt = dtPlus;
		}

		if (dt < 0.0f || bs <= 0.0f)
			return null;

		Vector2 dir = (tp + tv * dt) / (bs * dt);

		var info = new SpawnInfo
		{
			Position = spawnPos,
			LookDirection = dir
		};

		return info;
	}
}
