using System;
using System.Collections.Generic;
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
	public TAction getBestAction(
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

		queue.Enqueue(start, -evaluator.EvaluateState(start.state, agent));

		SearchNode best = start;
		float bestScore = float.NegativeInfinity;

		var iterations = 0;

		while (queue.Count > 0)
		{
			iterations += 1;
			SearchNode current = queue.Dequeue();

			if (current.depth >= maxDepth || iterations > maxIterations)
			{
				var score = evaluator.EvaluateState(current.state, current.state.GetCurrentExecutingAgent());
				Console.WriteLine($"Score: {score}");
				if (score > bestScore)
				{
					best = current;
					bestScore = score;
				}
				if (iterations > maxIterations)
				{
					break;
				}
				continue;
			}

			var availableMoves = current.state.GetAvailableMoves(current.state.GetCurrentExecutingAgent()).ToList();
			Console.WriteLine($"Available Moves: {availableMoves.ToList().Count}");
			Console.WriteLine($"Depth: {current.depth}");
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

				queue.Enqueue(newNode, -evaluator.EvaluateState(newNode.state, newNode.state.GetCurrentExecutingAgent()));
			}

		}

		var currentNode = best;
		while (currentNode.parent != start && currentNode != start)
		{
			currentNode = currentNode.parent;
		}			
		return currentNode.action;
	}
}