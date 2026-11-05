using Godot;

public partial class HitBox3D : Area3D
{
	public delegate void HitEvent(HitData data);
	public event HitEvent OnHit;

	public interface HitData
	{
	}

	public void Hit(HitData data)
	{
		OnHit?.Invoke(data);
	}

	public void SetEnabled(bool enabled)
	{
		foreach (Node child in GetChildren())
		{
			if (child is CollisionShape3D collisionShape)
			{
				collisionShape.SetDeferred(
					CollisionShape3D.PropertyName.Disabled,
					!enabled
				);
			}
		}
	}
}
