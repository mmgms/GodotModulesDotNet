using Godot;

public partial class HitBox2D : Area2D
{
	public delegate void HitEvent(HitData data);

	public event HitEvent OnHit;

	public class HitData
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
			if (child is CollisionShape2D collisionShape)
			{
				collisionShape.SetDeferred(
					CollisionShape2D.PropertyName.Disabled,
					!enabled
				);
			}
		}
	}
}
