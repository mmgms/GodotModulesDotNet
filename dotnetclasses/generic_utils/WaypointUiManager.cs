using System;
using Godot;
public class WaypointUiManager
{
	
	private const float MARGIN = 8;
	private Camera3D camera;
	private Control rootControl;

	private Func<Vector3> getPosition;
	private bool sticky;

	public WaypointUiManager(Camera3D camera, Control rootControl, Func<Vector3> getPosition, bool sticky)
	{
		this.camera = camera;
		this.rootControl = rootControl;
		this.getPosition = getPosition;
		this.sticky = sticky;
	}

	public enum PositionInfo {InBounds, Left, Right, Bottom, Top}
	public PositionInfo process()
	{
		var position = getPosition();
		var camera_transform = camera.GlobalTransform;
		var camera_position = camera_transform.Origin;

		var isBehind = camera_transform.Basis.Z.Dot(position - camera_position) > 0;
		var distance = camera_position.DistanceTo(position);
		var unprojectedPosition = camera.UnprojectPosition(position);

		Vector2 viewportBaseSize = GenericUtils.NodeUtilities.getViewportSize(rootControl);

		if (!sticky)
		{
			
			rootControl.Position = unprojectedPosition;
			rootControl.Visible = !isBehind;
			return PositionInfo.InBounds;
		}

		if (isBehind)
		{
			if (unprojectedPosition.X < viewportBaseSize.X / 2)
			{	
				unprojectedPosition.X = viewportBaseSize.X - MARGIN;
			}
			else
			{	
				unprojectedPosition.X = MARGIN;
			}
		}

		if (isBehind || unprojectedPosition.X < MARGIN ||
			unprojectedPosition.X > viewportBaseSize.X - MARGIN)
		{
			
			var look = camera_transform.LookingAt(position, Vector3.Up);
			var diff = angleDiff(look.Basis.GetEuler().X, camera_transform.Basis.GetEuler().X);
			unprojectedPosition.Y = viewportBaseSize.Y * (0.5f + (diff / Mathf.DegToRad(camera.Fov)));
		}

		rootControl.Position = new Vector2(
				Mathf.Clamp(unprojectedPosition.X, MARGIN, viewportBaseSize.X - MARGIN),
				Mathf.Clamp(unprojectedPosition.Y, MARGIN, viewportBaseSize.Y - MARGIN)
				);

		// float rotation = 0.0f;
		// float overflow = 0.0f;

		if (rootControl.Position.X <= MARGIN)
		{
			// Left overflow.
			// overflow = -Mathf.Tau / 8.0f;
			return PositionInfo.Left;
		}
		else if (rootControl.Position.X >= viewportBaseSize.X - MARGIN)
		{
			// Right overflow.
			// overflow = Mathf.Tau / 8.0f;
			return PositionInfo.Right;
		}

		if (rootControl.Position.Y <= MARGIN)
		{
			return PositionInfo.Top;
		}
		else if (rootControl.Position.Y >= viewportBaseSize.Y - MARGIN)
		{
			return PositionInfo.Bottom;
		}
		return PositionInfo.InBounds;

	}
	private static float angleDiff(float from, float to)
	{
		var diff = to - from % Mathf.Tau;
		return 2.0f * diff % Mathf.Tau - diff;
	}
}
