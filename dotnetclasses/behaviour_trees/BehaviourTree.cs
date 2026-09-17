using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

namespace BT;

public enum Status {Running, Failure, Success}
public interface INode
{
	public Status tick(float delta);
	public void abort()
	{
		
	}

	public String getDebugString()
	{
		return getName();
	}
	public String getName();

	public static String getDecoratorDebugString(INode self, INode child)
	{
		return self.getName() + $"[ul]{child.getDebugString()}[/ul]";
	}

	public static String getSequenceDebugString(INode self, List<INode> children, int idxRunning)
	{
		var debugString = $"{self.getName()}:";
		for (int i = 0; i < children.Count; i++)
		{
			if (i == idxRunning)
			{
				debugString += $"[ul][color=green]{children[i].getDebugString()}[/color][/ul]";
			}
			else
			{
				debugString += $"[ul]{children[i].getName()}[/ul]";
			}
		}
		return debugString;
	}
}

public class Task: INode
{
	Func<float, Status> tickFunc;
	String name;

	public Task(Func<float, Status> tickFunc, String name)
	{
		this.tickFunc = tickFunc;
		this.name = name;
	}

	public Status tick(float delta)
	{
		return tickFunc(delta);
	}

	public String getName()
	{
		return $"Task: {name}";
	}
}

public class Wait: INode
{
	float timeToWait;
	float timePassed;
	Func<float> getTimeToWait;
	String name;

	public Wait(Func<float> getTimeToWait, String name)
	{
		this.timePassed = 0.0f;
		this.getTimeToWait = getTimeToWait;
		this.timeToWait = getTimeToWait();
		this.name = name;
	}

	public static Wait Constant(float time)
	{
		return new Wait(() => time, "Wait");
	}

	public static Wait Random(float minTime, float maxTime)
	{
		return new Wait(() => GenericUtils.RandomUtils.range(System.Random.Shared, minTime, maxTime), "RandomWait");
	}

	public Status tick(float delta)
	{	
		timePassed += delta;
		if (timePassed >= timeToWait)
		{
			timePassed = 0.0f;
			timeToWait = getTimeToWait();
			return Status.Success;
		}
		return Status.Running;
	}
	public void abort()
	{
		timePassed = 0.0f;
	}

	public String getName()
	{
		return $"{name}: {timePassed:0.00}/{timeToWait:0.00}";
	}
}

/// <summary>
/// The Cooldown node executes its child until it either returns SUCCESS or FAILURE,
///  after which it will start an internal timer and return FAILURE until the timer is complete.
///  The cooldown is then able to execute its child again.
/// </summary>
public class Cooldown: INode
{
	INode child; 
	float cooldown;
	bool timerStarted;
	float timePassed;
	
	public Cooldown(INode child, float cooldown)
	{
		this.child = child;
		this.cooldown = cooldown;
	}
	public Status tick(float delta)
	{
		if (timerStarted)
		{
			timePassed += delta;
			if (timePassed >= cooldown)
			{
				timerStarted = false;
				timePassed = 0.0f;
			}
			return Status.Failure;
		}
		var ret = child.tick(delta);
		if (ret == Status.Running)
		{
			return ret;
		}
		
		timerStarted = true;
		return Status.Failure;

	}
	public void abort()
	{
		child.abort();
		timerStarted = false;
		timePassed = 0.0f;
	}
	
	public String getName()
	{
		return $"CoolDown: {timePassed:0.00}/{cooldown:0.00}";
	}

	public String getDebugString()
	{
		return INode.getDecoratorDebugString(this, child);  
	}
}

/// <summary>
/// The RepeatTimesUntilSuccess node executes its child a specified number of times (x). 
/// When the maximum number of ticks is reached, it returns a FAILURE status code.
/// The RepeatTimesUntilSuccess resets its counter after its child returns either SUCCESS or FAILURE.
/// </summary>
public class RepeatTimesUntilSuccess: INode
{
	INode child; 
	float maxTimes;
	int timesRepeated;
	
	public RepeatTimesUntilSuccess(INode child, int times)
	{
		this.child = child;
		this.maxTimes = times;
	}
	public Status tick(float delta)
	{
		var ret = child.tick(delta);
		if (ret == Status.Running)
		{
			return ret;
		}
		if (ret == Status.Success)
		{
			timesRepeated = 0;
			return ret;
		}
		timesRepeated += 1;
		if (timesRepeated >= maxTimes)
		{
			timesRepeated = 0;
			return Status.Failure;
		}
		return Status.Running;
	}
	public void abort()
	{
		child.abort();
		timesRepeated = 0;
	}
	
	public String getName()
	{
		return $"RepeatUntilSuccess: {timesRepeated}/{maxTimes}";
	}

	public String getDebugString()
	{
		return INode.getDecoratorDebugString(this, child);  
	}
}

public class Decorator: INode
{
	INode child;
	Func<Status, Status> childStatusProcessor;
	String name;

	public Decorator(INode child, Func<Status, Status> childStatusProcessor, String name)
	{
		this.child = child;
		this.childStatusProcessor = childStatusProcessor;
		this.name = name;
	}

	public static Decorator Succeder(INode child)
	{
		return new Decorator(child, (x) => Status.Success, "Succeder");
	}

	public static Decorator Failer(INode child)
	{
		return new Decorator(child, (x) => Status.Failure, "Failer");
	}
	
	public static Decorator Inverter(INode child)
	{
		return new Decorator(child, (x) => x == Status.Failure ? Status.Success : Status.Failure, "Inverter");
	}

	public static Decorator UntilFail(INode child)
	{
		return new Decorator(child, (x) => x == Status.Failure ? Status.Success : Status.Running, "UntilFail");
	}

	public static Decorator UntilSuccess(INode child)
	{
		return new Decorator(child, (x) => x == Status.Failure ? Status.Running : Status.Success, "UntilSuccess");
	}

	public Status tick(float delta)
	{
		var ret = child.tick(delta);
		if (ret == Status.Running)
		{
			return ret;
		}

		return childStatusProcessor(ret);
	}
	public void abort()
	{
		child.abort();
	}

	public String getName()
	{
		return $"{name}";
	}

	public String getDebugString()
	{
		return INode.getDecoratorDebugString(this, child);  
	}
}

public class NonReactive: INode
{
	List<INode> children;
	Func<Status> onOutOfNodes;
	Func<Status> onChildFailure;
	Func<Status> onChildSuccess;
	String name;

	private int currentChildIdx = -1;

	public NonReactive(List<INode> children, Func<Status> onOutOfNodes, Func<Status> onChildFailure, Func<Status> onChildSuccess, String name)
	{
		Debug.Assert(children.Count > 0);
		this.children = children;
		this.onChildFailure = onChildFailure;
		this.onChildSuccess = onChildSuccess;
		this.onOutOfNodes = onOutOfNodes;
		this.name = name;
		currentChildIdx = 0;
	}

	public static NonReactive Sequence(List<INode> children)
	{
		return new NonReactive(children, () => Status.Success, () => Status.Failure, () => Status.Running, "Sequence");
	}

	public static NonReactive Selector(List<INode> children)
	{
		return new NonReactive(children, () => Status.Failure, () => Status.Running, () => Status.Success, "Selector");
	}


	public Status tick(float delta)
	{
		if (currentChildIdx > children.Count)
		{
			currentChildIdx = 0;
			return onOutOfNodes();
		}

		var ret = children[currentChildIdx].tick(delta);
		if (ret == Status.Failure)
		{
			var status = onChildFailure();
			if (status == Status.Running)
			{
				currentChildIdx += 1;
			}
			else
			{
				currentChildIdx = 0;
			}
			return ret;
		}

		if(ret == Status.Success)
		{
			var status = onChildSuccess();
			if (status == Status.Running)
			{
				currentChildIdx += 1;
			}
			else
			{
				currentChildIdx = 0;
			}
			return status;
		}

		return Status.Running;
	}

	public void abort()
	{
		children[currentChildIdx].abort();
		currentChildIdx = 0;
	}

	public String getName()
	{
		return $"NonReactive{name}";
	}	

	public String getDebugString()
	{
		return INode.getSequenceDebugString(this, children, currentChildIdx);  
	}
}


public class Reactive: INode
{
	public List<INode> children;
	private int runningChildIdx = -1;
	Func<Status> onOutOfNodes;
	Func<Status> onChildFailure;
	Func<Status> onChildSuccess;
	String name;


	public Reactive(List<INode> children, Func<Status> onOutOfNodes, Func<Status> onChildFailure, Func<Status> onChildSuccess, String name)
	{
		Debug.Assert(children.Count > 0);
		this.onChildFailure = onChildFailure;
		this.onChildSuccess = onChildSuccess;
		this.onOutOfNodes = onOutOfNodes;
		this.children = children;
		this.name = name;
	}

	public static NonReactive Sequence(List<INode> children)
	{
		return new NonReactive(children, () => Status.Success, () => Status.Failure, () => Status.Running, "Sequence");
	}

	public static NonReactive Selector(List<INode> children)
	{
		return new NonReactive(children, () => Status.Failure, () => Status.Running, () => Status.Success, "Selector");
	}

	public Status tick(float delta)
	{
		var newRunningIdx = -1;

		for (var i=0; i < children.Count; i++)
		{
			var res = children[i].tick(delta);
			switch (res)
			{
				case Status.Running:
					newRunningIdx = i;  
					break;
				case Status.Success:
					var retF = onChildFailure();
					if (retF == Status.Running)
					{
						continue;
					}
					else
					{
						abortLower(i);
						return retF;
					}
				case Status.Failure:
					var retS = onChildSuccess();
					if (retS == Status.Running)
					{
						continue;
					}
					else
					{
						abortLower(i);
						return retS;
					}
			}
			if (newRunningIdx != -1)
			{
				break;
			}
		}

		if (newRunningIdx != -1)
		{
			if (runningChildIdx != -1 && runningChildIdx != newRunningIdx)
			{
				children[runningChildIdx].abort();

			}
			runningChildIdx = newRunningIdx;
			return Status.Running;
		}
		
		runningChildIdx = -1;
		return onOutOfNodes();
	}

	private void abortLower(int fromIdx)
	{
		foreach (var i in Enumerable.Range(fromIdx+1, children.Count - 1))
		{
			children[i].abort();
		}
	}

	public void abort()
	{
		if (runningChildIdx != -1)
		{
			children[runningChildIdx].abort();
		}
		runningChildIdx = -1;
	}

	public String getName()
	{
		return $"Reactive{name}";
	}
	
	public String getDebugString()
	{
		return INode.getSequenceDebugString(this, children, runningChildIdx);  
	}
}

public class Parallel: INode
{
	public enum Policy {RequireAll, RequireOne}
	private Policy successPolicy;
	private Policy failurePolicy;

	List<INode> children;

	public Parallel(List<INode> children, Policy successPolicy, Policy failurePolicy)
	{
		this.children = children;
		this.successPolicy = successPolicy;
		this.failurePolicy = failurePolicy;
	}

	public Status tick(float delta)
	{
		var successCount = 0;
		var failureCount = 0;

		foreach (var child in children)
		{
			var res = child.tick(delta);

			switch (res)
			{
				case Status.Success:
					successCount += 1;
					break;
				case Status.Failure:
					failureCount += 1;
					break;
				case Status.Running:
					continue;
			}
		}
		if (successPolicy == Policy.RequireAll && successCount == children.Count)
		{
			return Status.Success;
		}

		if (successPolicy == Policy.RequireOne && successCount > 0)
		{
			abort();
			return Status.Success;
		}

		if (failurePolicy == Policy.RequireAll && failureCount == children.Count)
		{
			return Status.Failure;
		}

		if (failurePolicy == Policy.RequireOne && failureCount > 0)
		{
			abort();
			return Status.Failure;
		}

		return Status.Running;

	}

	public void abort()
	{
		abortAll();
	}

	private void abortAll()
	{
		foreach(var child in children)
		{
			child.abort();
		}
	}

	public String getName()
	{
		return $"Parallel[SuccessPolicy: {successPolicy}, FailurePolicy: {failurePolicy}]";
	}
	
	public String getDebugString()
	{
		var debugString = $"{getName()}:";
		for (int i = 0; i < children.Count; i++)
		{
			debugString += $"[ul]{children[i].getDebugString()}[/ul]";
		}
		return debugString;
	}

}