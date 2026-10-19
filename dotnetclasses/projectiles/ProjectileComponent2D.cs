using Godot;
using System;
using System.Collections.Generic;

public class ProjectileComponent2D
{
	/// <summary>
	/// Moves a CharacterBody2D using velocity, supports an optional lifetime,
	/// and detects collisions with static bodies, rigid bodies, and hitboxes.
	/// Call Move() every physics frame.
	/// </summary>

	public CharacterBody2D character2D { get; set; }

	public uint StaticLayer { get; set; }

	public uint HitboxLayer { get; set; }

	public float RadiusCheckHitboxes { get; set; } = 10.0f;

	public bool PerformRaycastBetweenUpdates { get; set; }

	public delegate void HitStatic(Vector2 position, Vector2 normal);

	public delegate void HitRigidBody(RigidBody2D body, Vector2 position, Vector2 normal);

	public delegate void HitHitbox(Area2D body, Vector2 position);

	public delegate void LifetimeOver(Vector2 position);
	public event HitStatic OnHitStatic;
	public event HitRigidBody OnHitRigidBody;
	public event HitHitbox OnHitHitbox;
	public event LifetimeOver OnLifetimeOver;


	private Vector2 initialVelocity;
	private float lifetime = -1.0f;
	private readonly List<Area2D> _areasToExclude = new();

	private Vector2 _gravity;
	private float timePassed;
	private Vector2 prevPos;

	private Func<float, Vector2, Vector2> velocityUpdateCallback;

	public ProjectileComponent2D(CharacterBody2D characterBody2D, Vector2 initialVelocity, float lifetime, uint staticLayer, uint hitboxLayer, float RadiusCheckHitboxes, bool PerformRaycastBetweenUpdates)
	{
		character2D = characterBody2D;
		this.RadiusCheckHitboxes = RadiusCheckHitboxes;
		this.PerformRaycastBetweenUpdates = PerformRaycastBetweenUpdates;
		StaticLayer = staticLayer;
		HitboxLayer = hitboxLayer;

		this.initialVelocity = initialVelocity;
		this.lifetime = lifetime;

		timePassed = 0.0f;
		prevPos = character2D.GlobalPosition;
		character2D.Velocity = this.initialVelocity;

		velocityUpdateCallback = KeepInitialVelocity;

	}

	public ProjectileComponent2D SetLifetimeFromMaxRange(float maxRange)
	{
		lifetime = maxRange / initialVelocity.Length();
		return this;
	}

	public ProjectileComponent2D AddAreaToExclude(Area2D area)
	{
		_areasToExclude.Add(area);
		return this;
	}

	public ProjectileComponent2D SetCustomVelocityUpdateCallback(Func<float, Vector2, Vector2> callback)
	{
		velocityUpdateCallback = callback;
		return this;
	}

	public ProjectileComponent2D SetGravityUpdateCallback(Vector2 gravity)
	{
		_gravity = gravity;

		velocityUpdateCallback = GravityUpdate;

		return this;
	}

	public void SetVelocity(Vector2 velocity)
	{
		character2D.Velocity = velocity;
	}

	public Vector2 GetVelocity()
	{
		return character2D.Velocity;
	}

	public void Move(float delta)
	{
		timePassed += delta;

		if (lifetime > 0.0f && timePassed > lifetime)
		{
			OnLifetimeOver?.Invoke(character2D.Position);
		}

		prevPos = character2D.GlobalPosition;

		Vector2 newVelocity = velocityUpdateCallback(delta, character2D.Velocity);

		character2D.Velocity = newVelocity;

		KinematicCollision2D collision =
			character2D.MoveAndCollide(character2D.Velocity * delta);

		if (PerformRaycastBetweenUpdates)
		{
			CheckHitboxesRaycast(prevPos, character2D.GlobalPosition);
		}
		else
		{
			CheckHitboxes(character2D.GlobalPosition);
		}

		if (collision == null)
			return;

		GodotObject collider = collision.GetCollider();

		if (collider is StaticBody2D || collider is TileMapLayer )
		{
			OnHitStatic?.Invoke(collision.GetPosition(), collision.GetNormal());
		}

		if (collider is RigidBody2D rigidBody)
		{
			OnHitRigidBody?.Invoke(rigidBody, collision.GetPosition(), collision.GetNormal());
		}
	}

	private void CheckHitboxes(Vector2 position)
	{
		var areas = PhysicsUtils.Queries.CollectAreasInRadius2D( character2D.GetWorld2D().DirectSpaceState, position, HitboxLayer, RadiusCheckHitboxes);

		if (areas.Count == 0)
			return;

		OnHitHitbox?.Invoke(areas[0], position);
	}

	private void CheckHitboxesRaycast(Vector2 from, Vector2 to)
	{
		Area2D area = PhysicsUtils.Queries.CheckAreaRaycastAvoidStatic2D(
			character2D.GetWorld2D().DirectSpaceState,
			StaticLayer,
			HitboxLayer,
			from,
			to
		);

		if (area == null)
			return;

		if (_areasToExclude.Contains(area))
			return;

		OnHitHitbox?.Invoke(area, from);
	}

	public Vector2 KeepInitialVelocity(
		float delta,
		Vector2 previousVelocity)
	{
		return initialVelocity;
	}

	public Vector2 GravityUpdate(
		float delta,
		Vector2 previousVelocity)
	{
		return previousVelocity + _gravity * delta;
	}
}

