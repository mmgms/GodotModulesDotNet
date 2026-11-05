
using Godot;
using System;
using System.Collections.Generic;

public class ProjectileComponent3D
{
	public CharacterBody3D Character3D { get; set; }

	public uint StaticLayer { get; set; }

	public uint HitboxLayer { get; set; }

	public float RadiusCheckHitboxes { get; set; } = 1.0f;

	public bool PerformRaycastBetweenUpdates { get; set; }

	public event Action<Node3D, Vector3, Vector3> HitStatic;
	public event Action<RigidBody3D, Vector3, Vector3> HitRigidBody;
	public event Action<Area3D, Vector3> HitHitbox;
	public event Action<Vector3> LifetimeOver;

	private Vector3 _initialVelocity;
	private float _lifetime = -1.0f;
	private readonly List<Area3D> _areasToExclude = new();

	private Vector3 _gravity;
	private float _timePassed;
	private Vector3 _prevPos;

	private Func<float, Vector3, Vector3> _velocityUpdateCallback;

	public ProjectileComponent3D(
		CharacterBody3D characterBody, uint staticLayer, uint areaLayer, float radiusCheck, bool performRaycast, 
		Vector3 initialVelocity, float lifetime = -1.0f)
	{
		Character3D = characterBody;
		StaticLayer = staticLayer;
		HitboxLayer = areaLayer;
		RadiusCheckHitboxes = radiusCheck;
		PerformRaycastBetweenUpdates = performRaycast;

		_initialVelocity = initialVelocity;
		_lifetime = lifetime;
		_timePassed = 0.0f;

		_prevPos = Character3D.GlobalPosition;
		Character3D.Velocity = _initialVelocity;

		_velocityUpdateCallback = KeepInitialVelocity;

	}

	public ProjectileComponent3D SetLifetimeFromMaxRange(float maxRange)
	{
		_lifetime = maxRange / _initialVelocity.Length();
		return this;
	}

	public ProjectileComponent3D AddAreaToExclude(Area3D area)
	{
		_areasToExclude.Add(area);
		return this;
	}

	public ProjectileComponent3D SetCustomVelocityUpdateCallback(
		Func<float, Vector3, Vector3> callback)
	{
		_velocityUpdateCallback = callback;
		return this;
	}

	public ProjectileComponent3D SetGravityUpdateCallback(Vector3 gravity)
	{
		_gravity = gravity;
		_velocityUpdateCallback = GravityUpdate;
		return this;
	}

	public void SetVelocity(Vector3 velocity)
	{
		Character3D.Velocity = velocity;
	}

	public Vector3 GetVelocity()
	{
		return Character3D.Velocity;
	}

	public void Move(float delta)
	{
		_timePassed += delta;

		if (_lifetime > 0.0f && _timePassed > _lifetime)
		{
			LifetimeOver?.Invoke(Character3D.GlobalPosition);
		}

		_prevPos = Character3D.GlobalPosition;

		Vector3 newVelocity = _velocityUpdateCallback(
			delta, Character3D.Velocity);

		Character3D.Velocity = newVelocity;

		KinematicCollision3D collision =
			Character3D.MoveAndCollide(Character3D.Velocity * delta);

		if (PerformRaycastBetweenUpdates)
			CheckHitboxesRaycast(_prevPos, Character3D.GlobalPosition);
		else
			CheckHitboxes(Character3D.GlobalPosition);

		if (collision == null)
			return;

		GodotObject collider = collision.GetCollider();

		if (collider is StaticBody3D ||
			collider is CsgShape3D ||
			collider is GridMap)
		{
			HitStatic?.Invoke(
				collider as Node3D,
				collision.GetPosition(),
				collision.GetNormal());
		}

		if (collider is RigidBody3D rigidBody)
		{
			HitRigidBody?.Invoke(
				rigidBody,
				collision.GetPosition(),
				collision.GetNormal());
		}
	}

	private void CheckHitboxes(Vector3 position)
	{
		var areas = PhysicsUtils.Queries.CollectAreasInRadius3D(
			Character3D.GetWorld3D().DirectSpaceState,
			position,
			HitboxLayer,
			RadiusCheckHitboxes);

		if (areas == null || areas.Count == 0)
			return;

		HitHitbox?.Invoke(areas[0], position);
	}

	private void CheckHitboxesRaycast(Vector3 from, Vector3 to)
	{
		Area3D area = PhysicsUtils.Queries.CheckAreaRaycastAvoidStatic3D(
			Character3D.GetWorld3D().DirectSpaceState,
			StaticLayer,
			HitboxLayer,
			from,
			to);

		if (area == null || _areasToExclude.Contains(area))
			return;

		HitHitbox?.Invoke(area, from);
	}

	private Vector3 KeepInitialVelocity(
		float delta, Vector3 previousVelocity)
	{
		return _initialVelocity;
	}

	private Vector3 GravityUpdate(
		float delta, Vector3 previousVelocity)
	{
		return previousVelocity + _gravity * delta;
	}
}
