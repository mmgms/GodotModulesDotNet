using System;
using Godot;
namespace Steering;

public class VectorOperations<T>
{
    public Func<T, T, T> Add { get; }
	public Func<T, T, T> Subtract { get; }
    public Func<T, T, float> Distance { get; }
	public Func<T, T> Normalize { get; }
	public Func<T, float, T> Multiply {get;}

    public VectorOperations(Func<T, T, T> add, Func<T, T, T> subtract, Func<T, T, float> distance, Func<T, T> normalize, Func<T, float, T> multiply)
    {
        Add = add;
		Subtract = subtract;
        Distance = distance;
		Normalize = normalize;
		Multiply = multiply;
    }

	public static VectorOperations<Vector2> getVec2Operations()
	{
		return new VectorOperations<Vector2>
		(
			add: (a, b) => a + b, subtract: (a, b) => a - b, distance: (a, b) => a.DistanceTo(b), normalize: (a) => a.Normalized(), (a, b) => a * b
		);
	}

	
	public static VectorOperations<Vector3> getVec3Operations()
	{
		return new VectorOperations<Vector3>
		(
			add: (a, b) => a + b, subtract: (a, b) => a - b, distance: (a, b) => a.DistanceTo(b), normalize: (a) => a.Normalized(), (a, b) => a * b
		);
	}
}

public class Parameters
{
	public float linearSpeedMax;
	public float linearAccelerationMax;
	public float angularSpeedMax;
	public float angularAccelerationMax;
}

public class Agent<T>
{
	public T position;
	public T velocity;
	public float orientation;
}



public class TargetAcceleration<T>
{
	public T linear;
	public float angular;
}

public interface IBehaviour<T>
{
	public void calculate(Agent<T> agent, Parameters parameters, TargetAcceleration<T> target);
}

public class Seek<T> : IBehaviour<T>
{
	private Func<T> getTargetPosition;
	private VectorOperations<T> operations;
	public Seek(Func<T> getTargetPosition, VectorOperations<T> operations)
	{
		this.getTargetPosition = getTargetPosition;
		this.operations = operations;
	}

	public void calculate(Agent<T> agent, Parameters parameters, TargetAcceleration<T> target)
	{
		var diff = operations.Subtract(getTargetPosition(), agent.position);	
		target.linear = operations.Multiply(operations.Normalize(diff), parameters.linearAccelerationMax);
		target.angular = 0;
	}
}