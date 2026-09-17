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
}

public class Task: INode
{
	Func<float, Status> tickFunc;

	public Task(Func<float, Status> tickFunc)
	{
		this.tickFunc = tickFunc;
	}

	public Status tick(float delta)
	{
		return tickFunc(delta);
	}
}

public class Wait: INode
{
	float timeToWait;
	float timePassed;
	Func<float> getTimeToWait;

	public Wait(Func<float> getTimeToWait)
	{
		this.timePassed = 0.0f;
		this.getTimeToWait = getTimeToWait;
		this.timeToWait = getTimeToWait();
	}

	public static Wait Constant(float time)
	{
		return new Wait(() => time);
	}

	public static Wait Random(float minTime, float maxTime)
	{
		return new Wait(() => GenericUtils.RandomUtils.range(System.Random.Shared, minTime, maxTime));
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
}

public class Decorator: INode
{
	INode child; 
	Func<Status, Status> childStatusProcessor;

	public Decorator(INode child, Func<Status, Status> childStatusProcessor)
	{
		this.child = child;
		this.childStatusProcessor = childStatusProcessor;
	}

	public static Decorator Succeder(INode child)
	{
		return new Decorator(child, (x) => Status.Success);
	}

	public static Decorator Failer(INode child)
	{
		return new Decorator(child, (x) => Status.Failure);
	}
	
	public static Decorator Inverter(INode child)
	{
		return new Decorator(child, (x) => x == Status.Failure ? Status.Success : Status.Failure);
	}

	public static Decorator UntilFail(INode child)
	{
		return new Decorator(child, (x) => x == Status.Failure ? Status.Success : Status.Running);
	}

	public static Decorator UntilSuccess(INode child)
	{
		return new Decorator(child, (x) => x == Status.Failure ? Status.Running : Status.Success);
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
}

public class NonReactive: INode
{
	List<INode> children;
	Func<Status> onOutOfNodes;
	Func<Status> onChildFailure;
	Func<Status> onChildSuccess;

	private int currentChildIdx = -1;

	public NonReactive(List<INode> children, Func<Status> onOutOfNodes, Func<Status> onChildFailure, Func<Status> onChildSuccess)
	{
		Debug.Assert(children.Count > 0);
		this.children = children;
		this.onChildFailure = onChildFailure;
		this.onChildSuccess = onChildSuccess;
		this.onOutOfNodes = onOutOfNodes;
		currentChildIdx = 0;
	}

	public static NonReactive Sequence(List<INode> children)
	{
		return new NonReactive(children, () => Status.Success, () => Status.Failure, () => Status.Running);
	}

	public static NonReactive Selector(List<INode> children)
	{
		return new NonReactive(children, () => Status.Failure, () => Status.Running, () => Status.Success);
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
}


public class Reactive: INode
{
	public List<INode> children;
	private int runningChildIdx = -1;
	Func<Status> onOutOfNodes;
	Func<Status> onChildFailure;
	Func<Status> onChildSuccess;


	public Reactive(List<INode> children, Func<Status> onOutOfNodes, Func<Status> onChildFailure, Func<Status> onChildSuccess)
	{
		Debug.Assert(children.Count > 0);
		this.onChildFailure = onChildFailure;
		this.onChildSuccess = onChildSuccess;
		this.onOutOfNodes = onOutOfNodes;
		this.children = children;
	}

	public static NonReactive Sequence(List<INode> children)
	{
		return new NonReactive(children, () => Status.Success, () => Status.Failure, () => Status.Running);
	}

	public static NonReactive Selector(List<INode> children)
	{
		return new NonReactive(children, () => Status.Failure, () => Status.Running, () => Status.Success);
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

}