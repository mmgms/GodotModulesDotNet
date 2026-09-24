using System;
using System.Collections.Generic;
using System.Linq;

namespace Planning.Goap;
public class Action
{
	public String name;
	public Dictionary<Literal, bool> precondition;
	public Dictionary<Literal, bool> effects;

	public Func<int> costCallback;
	public System.Action effectCallback;

	
	public bool satisfiesPreconditions(State state)
	{
		return state.satisfiesLiterals(precondition);
	}

	public void applyAction(State state)
	{
		state.applyEffects(effects);
	}
}

public class Planner
{
	public class Node
	{
		public State state;
		public Action action;

	}

	public static IEnumerable<Action> getPlan(State state, List<Action> actions, Goal goal)
	{
		var plan = GenericUtils.GraphSearchUtils.AStar<Node>(new Node{state=state}, null, 
			(x) => expandNode(x.state, actions), 
			EqualityComparer<Node>.Default, 
			(a, b) => b.action.costCallback(), 
			(a, b) => a.state.getApproxDistanceToGoal(goal),
			(x) => x.state.satisfiesGoal(goal) 
		 );
		
		return plan.Skip(1).Select((x) => x.action);
	}

	private static IEnumerable<Node> expandNode(State state, List<Action> actions)
	{
		foreach (var action in actions)
		{
			if (action.satisfiesPreconditions(state))
			{
				var newState = state.clone();
				action.applyAction(state);
				var node = new Node
				{
					state = newState,
					action = action
				};
				yield return node;
			}
		}
	}
}