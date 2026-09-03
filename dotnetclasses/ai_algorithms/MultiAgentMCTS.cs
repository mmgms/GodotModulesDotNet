using System;
using System.Collections.Generic;
using System.Linq;

namespace AI;

public class MultiAgentMCTS<TAction, TAgent>
{
	private class MCTSNode
	{
		public int Depth { get; }

		public IMultiAgentGameState<TAction, TAgent> State { get; }
		public MCTSNode Parent { get; }
		public TAction Action { get; }

		public List<MCTSNode> Children { get; }

		public int Visits { get; private set; }
		public float Wins { get; private set; }

		private List<TAction> UntriedActions;


		public MCTSNode(
			IMultiAgentGameState<TAction, TAgent> state,
			MCTSNode parent = null,
			TAction action = default,
			int depth = 0)
		{
			State = state;

			Parent = parent;
			Action = action;

			Depth = depth;

			Children = new List<MCTSNode>();

			Visits = 0;
			Wins = 0;

			UntriedActions = state.GetAvailableMoves(state.GetCurrentExecutingAgent()).ToList();
		}


		public bool IsTerminal()
		{
			return State.IsOver();
		}


		public bool IsFullyExpanded()
		{
			return UntriedActions.Count == 0;
		}


		public MCTSNode Expand(Random random, 
			TAgent agent,
			Func<IMultiAgentGameState<TAction, TAgent>, TAgent, TAction> otherAgentPolicy,
			Func<TAgent, TAgent, bool> isSameAgent)
		{
			int index = random.Next(UntriedActions.Count);

			TAction action = UntriedActions[index];
			UntriedActions.RemoveAt(index);

			var newState = State.GetNewStatePerMove(action);

			while(!isSameAgent(newState.GetCurrentExecutingAgent(), agent))
			{
				newState.ExecuteMove(otherAgentPolicy(newState, newState.GetCurrentExecutingAgent()));
			}

			var child = new MCTSNode(newState, this, action, Depth + 1);

			Children.Add(child);

			return child;
		}

		public MCTSNode BestChild(float c)
		{
			foreach (var child in Children)
			{
				if (child.Visits == 0)
					return child;
			}

			return Children.MaxBy(
				child => child.Ucb(this, c)
			);

		}

		public float Rollout(
			int maxRolloutDepth,
			IMultiAgentGameStateEvaluator<TAction, TAgent> evaluator,
			Random random,
			TAgent agent,
			Func<IMultiAgentGameState<TAction, TAgent>, TAgent, TAction> otherAgentPolicy,
			Func<TAgent, TAgent, bool> isSameAgent)
		{
			var newState = State.GetDuplicated();

			for (int i = 0; i < maxRolloutDepth; i++)
			{
				var moves = newState.GetAvailableMoves(newState.GetCurrentExecutingAgent());
				var moveList = moves.ToList();

				if (newState.IsOver() || moveList.Count == 0)
				{
					return evaluator.GetTerminationValue(newState, agent);
				}

				newState.ExecuteMove(moveList[random.Next(moveList.Count)]);
				while(!isSameAgent(newState.GetCurrentExecutingAgent(), agent))
				{
					newState.ExecuteMove(otherAgentPolicy(newState, newState.GetCurrentExecutingAgent()));
				}

			}

			return evaluator.EvaluateState(newState, agent);
		}


		public void Backpropagate(float value)
		{
			Visits++;
			Wins += value;

			Parent?.Backpropagate(value);
		}


		private float Ucb(MCTSNode child, float c)
		{
			float exploit = child.Wins / child.Visits;

			float explore =
				c * MathF.Sqrt(
					MathF.Log(Visits) / child.Visits
				);

			return exploit + explore;
		}
	}
	public class PlanResult
	{
		public List<TAction> plan;

	}


	public PlanResult GetPlan(
		TAgent agent,
		IMultiAgentGameState<TAction, TAgent> state,
		IMultiAgentGameStateEvaluator<TAction, TAgent> evaluator,
		Func<IMultiAgentGameState<TAction, TAgent>, TAgent, TAction> otherAgentPolicy,
		Func<TAgent, TAgent, bool> isSameAgent,
		int iterations = 50,
		int maxRollouts = 30,
		Action<int> OnIterationCompleted=null,
		float cFactor = 1.4f)
	{
	
		var random = new Random();

		var root = new MCTSNode(state.GetDuplicated());

		for (int i = 0; i < iterations; i++)
		{
			var node = root;

			// Selection
			while (!node.IsTerminal() && node.IsFullyExpanded())
			{
				node = node.BestChild(cFactor);
			}

			// Expansion
			if (!node.IsTerminal() && !node.IsFullyExpanded())
			{
				node = node.Expand(random, agent, otherAgentPolicy, isSameAgent);
			}

			// Simulation
			float value = node.Rollout(maxRollouts, evaluator, random, agent, otherAgentPolicy, isSameAgent);

			// Backpropagation
			node.Backpropagate(value);
			OnIterationCompleted?.Invoke(i);
		}

		var plan = new List<TAction>();

		var current = root;
		while (current.Children.Count > 0)
		{
			var bestChild = current.Children.MaxBy(x => x.Visits);
			plan.Add(bestChild.Action);
			current = bestChild;
		}

		var res = new PlanResult();
		res.plan = plan;

		return res;
	}	
}