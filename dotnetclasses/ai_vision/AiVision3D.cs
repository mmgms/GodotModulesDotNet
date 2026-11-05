
using Godot;
using System;
using System.Collections.Generic;

public class AiVision3D
{
	public uint StaticLayer { get; set; }

	public uint TargetLayer { get; set; }

	public World3D world3D;

	public Func<Vector3> getRaycastStart { get; set; }

	private Func<Area3D, Vector3> _areaGetEndRaycastPosition;

	public AiVision3D SetAreaGetEndRaycastPosition(
		Func<Area3D, Vector3> callback)
	{
		_areaGetEndRaycastPosition = callback;
		return this;
	}

	public class AiVision3DShape
	{
		public virtual bool CheckIfContainsPoint(
			Vector3 start, Vector3 point)
		{
			return false;
		}

		public virtual float GetMaxRange()
		{
			return 0.0f;
		}
	}

	public class AiVision3DShapeSphere : AiVision3DShape
	{
		public float Radius { get; set; }

		public AiVision3DShapeSphere(float radius)
		{
			Radius = radius;
		}

		public override bool CheckIfContainsPoint(
			Vector3 start, Vector3 point)
		{
			return start.DistanceSquaredTo(point)
				< Radius * Radius;
		}

		public override float GetMaxRange()
		{
			return Radius;
		}
	}

	public class AiVision3DShapeCone : AiVision3DShape
	{
		public float Radius { get; set; }
		public float HalfAngle { get; set; }

		private Func<Vector3> _directionCallback;

		public AiVision3DShapeCone(
			float radius, float halfAngleDegrees)
		{
			Radius = radius;
			HalfAngle = Mathf.DegToRad(halfAngleDegrees);
		}

		public AiVision3DShapeCone SetDirectionCallback(
			Func<Vector3> callback)
		{
			_directionCallback = callback;
			return this;
		}

		public override bool CheckIfContainsPoint(
			Vector3 start, Vector3 point)
		{
			if (start.DistanceSquaredTo(point)
				>= Radius * Radius)
			{
				return false;
			}

			if (_directionCallback == null)
				return false;

			return MathUtils.Funtions.IsPointInCone3D(
				point,
				start,
				_directionCallback(),
				HalfAngle);
		}

		public override float GetMaxRange()
		{
			return Radius;
		}
	}

	private readonly List<AiVision3DShape> _shapes = new();
	private float _currentMaxRange;

	public AiVision3D(World3D world3D, uint StaticLayer, uint TargetLayer, Func<Vector3> raycastStart)
	{
		this.world3D = world3D;
		this.getRaycastStart = raycastStart;
		this.StaticLayer = StaticLayer;
		this.TargetLayer = TargetLayer;
	}


	public AiVision3D AddShape(AiVision3DShape shape)
	{
		_shapes.Add(shape);
		_currentMaxRange = Mathf.Max(
			shape.GetMaxRange(), _currentMaxRange);

		return this;
	}

	public List<Area3D> CollectVisibleTargets()
	{
		var visibleTargets = new List<Area3D>();

		Vector3 start = getRaycastStart();

		PhysicsDirectSpaceState3D spaceState = world3D.DirectSpaceState;

		var areas = PhysicsUtils.Queries.CollectAreasInRadius3D(
			spaceState,
			start,
			TargetLayer,
			_currentMaxRange);

		if (areas == null)
			return visibleTargets;

		foreach (Area3D area in areas)
		{
			if (!GodotObject.IsInstanceValid(area))
				continue;

			Vector3 endPosition = GetEndRaycastPosition(area);

			bool insideShape = false;

			foreach (AiVision3DShape shape in _shapes)
			{
				if (shape.CheckIfContainsPoint(start, endPosition))
				{
					insideShape = true;
					break;
				}
			}

			if (!insideShape)
				continue;

			Area3D hitArea =
				PhysicsUtils.Queries.CheckAreaRaycastAvoidStatic3D(
					spaceState,
					StaticLayer,
					TargetLayer,
					start,
					endPosition);

			if (hitArea == area)
				visibleTargets.Add(area);
		}

		return visibleTargets;
	}

	private Vector3 GetEndRaycastPosition(Area3D area)
	{
		if (_areaGetEndRaycastPosition != null)
			return _areaGetEndRaycastPosition(area);

		return area.GlobalPosition;
	}
}
