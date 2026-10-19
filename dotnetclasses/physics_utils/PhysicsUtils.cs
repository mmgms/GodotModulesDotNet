using Godot;
using System.Collections.Generic;

namespace PhysicsUtils;
public static class Queries
{
	public static List<Area2D> CollectAreasInRadius2D(
		PhysicsDirectSpaceState2D spaceState,
		Vector2 position,
		uint areaLayer,
		float radius)
	{
		var areas = new List<Area2D>();

		var shape = new CircleShape2D
		{
			Radius = radius
		};

		var query = new PhysicsShapeQueryParameters2D
		{
			CollideWithAreas = true,
			CollideWithBodies = false,
			CollisionMask = areaLayer,
			Shape = shape,
			Transform = new Transform2D(0.0f, position)
		};

		var results = spaceState.IntersectShape(query);

		foreach (Godot.Collections.Dictionary result in results)
		{
			if (result["collider"].AsGodotObject() is Area2D area)
				areas.Add(area);
		}

		return areas;
	}

	public static Area2D CheckAreaRaycastAvoidStatic2D(
		PhysicsDirectSpaceState2D spaceState,
		uint staticLayer,
		uint areaLayer,
		Vector2 from,
		Vector2 to,
		bool hitFromInside = false)
	{
		var query = PhysicsRayQueryParameters2D.Create(from, to);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;
		query.HitFromInside = hitFromInside;
		query.CollisionMask = staticLayer | areaLayer;

		var result = spaceState.IntersectRay(query);

		if (result.Count == 0)
			return null;

		GodotObject collider = result["collider"].AsGodotObject();

		if (collider is StaticBody2D)
			return null;

		return collider as Area2D;
	}

	public static List<Area3D> CollectAreasInRadius3D(
		PhysicsDirectSpaceState3D spaceState,
		Vector3 position,
		uint areaLayer,
		float radius)
	{
		var areas = new List<Area3D>();

		var shape = new SphereShape3D
		{
			Radius = radius
		};

		var query = new PhysicsShapeQueryParameters3D
		{
			CollideWithAreas = true,
			CollideWithBodies = false,
			CollisionMask = areaLayer,
			Shape = shape,
			Transform = new Transform3D(Basis.Identity, position)
		};

		var results = spaceState.IntersectShape(query);

		foreach (Godot.Collections.Dictionary result in results)
		{
			if (result["collider"].AsGodotObject() is Area3D area)
				areas.Add(area);
		}

		return areas;
	}

	public static Area3D CheckAreaRaycastAvoidStatic3D(
		PhysicsDirectSpaceState3D spaceState,
		uint staticLayer,
		uint areaLayer,
		Vector3 from,
		Vector3 to,
		bool hitFromInside = false)
	{
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;
		query.HitFromInside = hitFromInside;
		query.CollisionMask = staticLayer | areaLayer;

		var result = spaceState.IntersectRay(query);

		if (result.Count == 0)
			return null;

		GodotObject collider = result["collider"].AsGodotObject();

		if (collider is StaticBody3D)
			return null;

		return collider as Area3D;
	}

	public class RaycastCollisionResult3D
	{
		public Vector3 Position { get; set; }
		public Vector3 Normal { get; set; }
	}

	public static RaycastCollisionResult3D CheckStaticRaycastCollision3D(
		PhysicsDirectSpaceState3D spaceState,
		uint staticLayer,
		Vector3 from,
		Vector3 to)
	{
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;
		query.CollisionMask = staticLayer;

		var result = spaceState.IntersectRay(query);

		if (result.Count == 0)
			return null;

		return new RaycastCollisionResult3D
		{
			Position = result["position"].AsVector3(),
			Normal = result["normal"].AsVector3()
		};
	}

	public class RaycastCollisionResult2D
	{
		public Vector2 Position { get; set; }
		public Vector2 Normal { get; set; }
	}

	public static RaycastCollisionResult2D CheckStaticRaycastCollision2D(
		PhysicsDirectSpaceState2D spaceState,
		uint staticLayer,
		Vector2 from,
		Vector2 to)
	{
		var query = PhysicsRayQueryParameters2D.Create(from, to);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;
		query.CollisionMask = staticLayer;

		var result = spaceState.IntersectRay(query);

		if (result.Count == 0)
			return null;

		return new RaycastCollisionResult2D
		{
			Position = result["position"].AsVector2(),
			Normal = result["normal"].AsVector2()
		};
	}

	public static List<PhysicsBody2D> CollectBodiesInRadius2D(
		PhysicsDirectSpaceState2D spaceState,
		Vector2 position,
		uint bodyLayer,
		float radius)
	{
		var bodies = new List<PhysicsBody2D>();

		var shape = new CircleShape2D
		{
			Radius = radius
		};

		var query = new PhysicsShapeQueryParameters2D
		{
			CollideWithAreas = false,
			CollideWithBodies = true,
			CollisionMask = bodyLayer,
			Shape = shape,
			Transform = new Transform2D(0.0f, position)
		};

		var results = spaceState.IntersectShape(query);

		foreach (Godot.Collections.Dictionary result in results)
		{
			if (result["collider"].AsGodotObject() is PhysicsBody2D body)
				bodies.Add(body);
		}

		return bodies;
	}

	public static List<PhysicsBody3D> CollectBodiesInRadius3D(
		PhysicsDirectSpaceState3D spaceState,
		Vector3 position,
		uint bodyLayer,
		float radius)
	{
		var bodies = new List<PhysicsBody3D>();

		var shape = new SphereShape3D
		{
			Radius = radius
		};

		var query = new PhysicsShapeQueryParameters3D
		{
			CollideWithAreas = false,
			CollideWithBodies = true,
			CollisionMask = bodyLayer,
			Shape = shape,
			Transform = new Transform3D(Basis.Identity, position)
		};

		var results = spaceState.IntersectShape(query);

		foreach (Godot.Collections.Dictionary result in results)
		{
			if (result["collider"].AsGodotObject() is PhysicsBody3D body)
				bodies.Add(body);
		}

		return bodies;
	}

	public static DampedSpringJoint2D CreateDampedSpringJoint(
		PhysicsBody2D bodyA,
		PhysicsBody2D bodyB,
		float restLength,
		float stiffness = 100.0f,
		float damping = 1.0f,
		float bias = 1.0f)
	{
		var joint = new DampedSpringJoint2D();

		bodyA.AddChild(joint);

		joint.Stiffness = stiffness;
		joint.RestLength = restLength;
		joint.Damping = damping;
		joint.Bias = bias;

		joint.LookAt(bodyB.GlobalPosition);
		joint.Rotation -= Mathf.DegToRad(90.0f);
		joint.Length = bodyA.GlobalPosition.DistanceTo(bodyB.GlobalPosition);

		joint.NodeA = bodyA.GetPath();
		joint.NodeB = bodyB.GetPath();

		return joint;
	}

	public class ShapeCastCollisionResult3D
	{
		public Vector3 FinalSafePosition { get; set; }
	}

	public static ShapeCastCollisionResult3D CheckStaticShapeCastCollision3D(
		PhysicsDirectSpaceState3D spaceState,
		uint staticLayer,
		Vector3 from,
		Vector3 to,
		float radius)
	{
		var shape = new SphereShape3D
		{
			Radius = radius
		};

		var query = new PhysicsShapeQueryParameters3D
		{
			CollideWithAreas = false,
			CollideWithBodies = true,
			CollisionMask = staticLayer,
			Shape = shape,
			Transform = new Transform3D(Basis.Identity, from),
			Motion = to - from
		};

		var result = spaceState.CastMotion(query);
		float safeProportion = result[0];

		return new ShapeCastCollisionResult3D
		{
			FinalSafePosition = from + safeProportion * (to - from)
		};
	}

	public class RaycastAreasCollisionResult2D
	{
		public Area2D Area { get; set; }
		public Vector2 Position { get; set; }
		public Vector2 Normal { get; set; }
	}

	public static RaycastAreasCollisionResult2D CheckAreaRaycastCollectPosCollision(
		PhysicsDirectSpaceState2D spaceState,
		uint staticLayer,
		uint areaLayer,
		Vector2 from,
		Vector2 to,
		bool hitFromInside = false)
	{
		var query = PhysicsRayQueryParameters2D.Create(from, to);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;
		query.HitFromInside = hitFromInside;
		query.CollisionMask = staticLayer | areaLayer;

		var result = spaceState.IntersectRay(query);

		var collision = new RaycastAreasCollisionResult2D();

		if (result.Count == 0)
			return collision;

		GodotObject collider = result["collider"].AsGodotObject();

		if (collider is not StaticBody2D)
			collision.Area = collider as Area2D;

		collision.Position = result["position"].AsVector2();
		collision.Normal = result["normal"].AsVector2();

		return collision;
	}

	public static List<PhysicsBody2D> CollectBodiesInRectangle2D(
		PhysicsDirectSpaceState2D spaceState,
		uint bodyLayer,
		Vector2 from,
		Vector2 to,
		float size)
	{
		var bodies = new List<PhysicsBody2D>();

		var shape = new RectangleShape2D
		{
			Size = new Vector2(from.DistanceTo(to), size)
		};

		var query = new PhysicsShapeQueryParameters2D
		{
			CollideWithAreas = false,
			CollideWithBodies = true,
			CollisionMask = bodyLayer,
			Shape = shape,
			Transform = new Transform2D(
				(to - from).Angle(),
				(from + to) / 2.0f
			)
		};

		var results = spaceState.IntersectShape(query);

		foreach (Godot.Collections.Dictionary result in results)
		{
			if (result["collider"].AsGodotObject() is PhysicsBody2D body)
				bodies.Add(body);
		}

		return bodies;
	}
}
