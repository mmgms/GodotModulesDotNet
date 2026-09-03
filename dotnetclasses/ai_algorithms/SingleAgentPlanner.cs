using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AI;

public class SingleAgentPlanner<TAction, TAgent>
{
	public class SearchNode
	{
		public AI.IMultiAgentGameState<TAction, TAgent> state;
		public SearchNode parent;
		public TAction action;
		public int depth;
	}
	public class PlanResult
	{
		public List<TAction> plan;
		public float bestScore;
	}
	public PlanResult GetPlan(
		TAgent agent,
		AI.IMultiAgentGameState<TAction, TAgent> state, 
		AI.IMultiAgentGameStateEvaluator<TAction, TAgent> evaluator,
		Func<AI.IMultiAgentGameState<TAction, TAgent>, TAgent, TAction> otherAgentsPolicy,
		Func<TAgent, TAgent, bool> isSameAgent,
		int maxDepth, int maxIterations)
	{
		var queue = new PriorityQueue<SearchNode, float>();

		var start = new SearchNode();
		start.state = state.GetDuplicated();

		queue.Enqueue(start, 0);

		SearchNode best = start;
		float bestScore = float.NegativeInfinity;

		var iterations = 0;

		while (queue.Count > 0)
		{
			iterations += 1;
			SearchNode current = queue.Dequeue();

			if (current.depth >= maxDepth)
			{
				Debug.Assert(isSameAgent(current.state.GetCurrentExecutingAgent(), agent));
				var score = evaluator.EvaluateState(current.state, current.state.GetCurrentExecutingAgent());

				if (score > bestScore)
				{
					best = current;
					bestScore = score;
				}
				continue;
			}
			if (iterations > maxIterations)
			{
				continue;
			}

			var availableMoves = current.state.GetAvailableMoves(current.state.GetCurrentExecutingAgent()).ToList();

			foreach (var action in availableMoves)
			{
				// expand with other policy until its our turn again
				var newState = current.state.GetNewStatePerMove(action);
				while (!isSameAgent(newState.GetCurrentExecutingAgent(), agent))
				{
					newState.ExecuteMove(otherAgentsPolicy(newState, newState.GetCurrentExecutingAgent()));
				}
				var newNode = new SearchNode();
				newNode.state = newState;
				newNode.depth = current.depth + 1;
				newNode.action = action;
				newNode.parent = current;

				queue.Enqueue(newNode, 0);
			}

		}

		var plan = new List<TAction>();

		var currentNode = best;
		while (currentNode != start)
		{
			plan.Add(currentNode.action);
			currentNode = currentNode.parent;
		}			
		plan.Reverse();
		var res = new PlanResult();
		res.plan = plan;
		res.bestScore = bestScore;
		return res;
	}
}