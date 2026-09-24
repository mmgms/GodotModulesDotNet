using System;

namespace Planning;

public class Literal
{
	public String name;
	public Func<bool> getValueCallback;

	public Literal(String name, Func<bool> getValueCallback=null)
	{
		this.name = name;
		this.getValueCallback = getValueCallback;
	}

	public override bool Equals(object? obj)
	{
		return obj is Literal other && name == other.name;
	}
	public override int GetHashCode()
	{
		return name?.GetHashCode() ?? 0;
	}
}
