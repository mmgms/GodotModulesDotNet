using Godot;
using System;

public class FPSCameraController3D
{
	public Node3D Pitch { get; set; }
	public Node3D Yaw { get; set; }
	public Camera3D Camera { get; set; }

	public float TiltUpperLimit { get; set; } = 89.0f;
	public float TiltLowerLimit { get; set; } = -89.0f;
	public bool InvertPitch { get; set; } = true;

	private Vector3 _rotation = Vector3.Zero;

	private Func<Vector2> _inputCallback;

	private Vector2 _mouseInput = Vector2.Zero;
	private float _mouseSensitivity;

	public FPSCameraController3D(Camera3D camera, Node3D pitch, Node3D yaw)
	{
		Camera = camera;
		Pitch = pitch;
		Yaw = yaw;
	}

	public FPSCameraController3D SetInputCallback(Func<Vector2> callback)
	{
		_inputCallback = callback;
		return this;
	}

	public FPSCameraController3D SetMouseInputCallback(float mouseSensitivity = 0.005f)
	{
		_mouseSensitivity = mouseSensitivity;
		_inputCallback = GetMouseInput;
		return this;
	}

	public void handleInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			_mouseInput.X -= mouseMotion.ScreenRelative.X * _mouseSensitivity;
			_mouseInput.Y -= mouseMotion.ScreenRelative.Y * _mouseSensitivity;
		}
	}

	private Vector2 GetMouseInput()
	{
		return _mouseInput;
	}

	public void updateCamera(float delta)
	{
		if (_inputCallback == null)
			return;

		Vector2 input = _inputCallback();

		float tiltDirection = InvertPitch ? 1.0f : -1.0f;

		_rotation.X += tiltDirection * input.Y;

		_rotation.X = Mathf.Clamp(
			_rotation.X,
			Mathf.DegToRad(TiltLowerLimit),
			Mathf.DegToRad(TiltUpperLimit)
		);

		_rotation.Y += input.X;

		Yaw.Transform = new Transform3D(
			Basis.FromEuler(new Vector3(0.0f, _rotation.Y, 0.0f)),
			Yaw.Position
		);

		Pitch.Transform = new Transform3D(
			Basis.FromEuler(new Vector3(_rotation.X, 0.0f, 0.0f)),
			Pitch.Position
		);

		_mouseInput = Vector2.Zero;
	}
}