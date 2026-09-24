using System;
using System.Collections.Generic;

namespace Planning;

public class Goal
{
	public String name;
	public Dictionary<Literal, bool> desired;
	public Func<int> priorityCallback;
}
