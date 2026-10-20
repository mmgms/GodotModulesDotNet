using Godot;
using System.Collections.Generic;

namespace AiVision;
public class AiVision2D
{
	public uint StaticLayer { get; set; }

	public uint TargetLayer { get; set; }

	public Node2D RaycastStartNode { get; set; }

	public class Shape
	{
		public virtual bool CheckIfContainsPoint(Vector2 start, Vector2 point)
		{
			return false;
		}

		public virtual float GetMaxRange()
		{
			return 0.0f;
		}
	}

	public class Circle : Shape
	{
		public float Radius { get; set; }

		public Circle(float radius)
		{
			Radius = radius;
		}

		public override bool CheckIfContainsPoint(Vector2 start, Vector2 point)
		{
			return start.DistanceSquaredTo(point) < Radius * Radius;
		}

		public override float GetMaxRange()
		{
			return Radius;
		}
	}

	public class Cone : Shape
	{
		public float Radius { get; set; }
		public float HalfAngle { get; set; }

		private System.Func<Vector2> _directionCallback;

		public Cone(float radius, float halfAngleDeg)
		{
			Radius = radius;
			HalfAngle = Mathf.DegToRad(halfAngleDeg);
		}

		public Cone SetDirectionCallback(System.Func<Vector2> callback)
		{
			_directionCallback = callback;
			return this;
		}

		public override bool CheckIfContainsPoint(Vector2 start, Vector2 point)
		{
			if (start.DistanceSquaredTo(point) >= Radius * Radius)
				return false;

			if (_directionCallback == null)
				return false;

			return MathUtils.Funtions.IsPointInCone2D( point, start, _directionCallback(), HalfAngle);
		}

		public override float GetMaxRange()
		{
			return Radius;
		}
	}

	private readonly List<Shape> _shapes = new();
	private float _currentMaxRange;

	public AiVision2D(uint StaticLayer, uint TargetLayer, Node2D raycastStart)
	{
		this.RaycastStartNode = raycastStart;
		this.StaticLayer = StaticLayer;
		this.TargetLayer = TargetLayer;
	}

	public AiVision2D AddShape(Shape shape)
	{
		_shapes.Add(shape);
		_currentMaxRange = Mathf.Max(shape.GetMaxRange(), _currentMaxRange);

		return this;
	}

	public List<Area2D> CollectVisibleTargets()
	{
		var visibleTargets = new List<Area2D>();

		if (RaycastStartNode == null) 
		{
			return visibleTargets;
		}

		var spaceState = RaycastStartNode.GetWorld2D().DirectSpaceState;

		Vector2 origin = RaycastStartNode.GlobalPosition;

		var areasInRange = PhysicsUtils.Queries.CollectAreasInRadius2D( spaceState, origin, TargetLayer, _currentMaxRange);

		foreach (Area2D area in areasInRange)
		{
			bool insideShape = false;

			foreach (Shape shape in _shapes)
			{
				if (shape.CheckIfContainsPoint(origin, area.GlobalPosition))
				{
					insideShape = true;
					break;
				}
			}

			if (!insideShape)
			{
				continue;
			}

			var hitArea = PhysicsUtils.Queries.CheckAreaRaycastAvoidStatic2D(spaceState, StaticLayer, TargetLayer, origin, area.GlobalPosition);

			if (hitArea == area)
			{
				visibleTargets.Add(area);
			}
		}

		return visibleTargets;
	}

	public bool checkIfTargetAreaVisible(Area2D targetArea)
	{
		Vector2 origin = RaycastStartNode.GlobalPosition;
		var spaceState = RaycastStartNode.GetWorld2D().DirectSpaceState;

		var hitArea = PhysicsUtils.Queries.CheckAreaRaycastAvoidStatic2D(spaceState, StaticLayer, TargetLayer, origin, targetArea.GlobalPosition);

		return targetArea == hitArea;
	}
}