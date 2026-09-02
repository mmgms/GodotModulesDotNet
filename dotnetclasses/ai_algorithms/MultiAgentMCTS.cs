using System;
using System.Collections.Generic;
using System.Linq;

namespace AI;

public class MultiAgentMCTS<TMove, TAgent>
{
	private IMultiAgentGameStateEvaluator<TMove, TAgent> gameStateEvaluator;
	private int maxRollouts;
	private float cFactor;

	private class MCTSNode
	{
		public int Depth { get; }

		public IMultiAgentGameState<TMove, TAgent> State { get; }
		private readonly IMultiAgentGameStateEvaluator<TMove, TAgent> Evaluator;
		public TAgent PlanningAgent;
		public MCTSNode Parent { get; }
		public TMove Action { get; }

		public List<MCTSNode> Children { get; }

		public int Visits { get; private set; }
		public float Wins { get; private set; }

		private List<TMove> UntriedActions;


		public MCTSNode(
			TAgent agent,
			IMultiAgentGameState<TMove, TAgent> state,
			IMultiAgentGameStateEvaluator<TMove, TAgent> evaluator,
			MCTSNode parent = null,
			TMove action = default,
			int depth = 0)
		{
			PlanningAgent = agent;
			State = state;
			Evaluator = evaluator;

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


		public MCTSNode Expand(Random random)
		{
			int index = random.Next(UntriedActions.Count);

			TMove action = UntriedActions[index];
			UntriedActions.RemoveAt(index);

			var newState = State.GetNewStatePerMove(action);

			var child = new MCTSNode(PlanningAgent, newState, Evaluator, this, action, Depth + 1);

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
			Random random)
		{
			var newState = State.GetDuplicated();

			for (int i = 0; i < maxRolloutDepth; i++)
			{
				var moves = newState.GetAvailableMoves(newState.GetCurrentExecutingAgent());
				var moveList = moves.ToList();
				if (newState.IsOver() || moveList.Count == 0)
				{
					return Evaluator.GetTerminationValue(newState, PlanningAgent);
				}



				newState.ExecuteMove(
					moveList[random.Next(moveList.Count)]
				);

			}

			return Evaluator.EvaluateState(newState, PlanningAgent);
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


	public TMove GetBestMove(
		TAgent agent,
		IMultiAgentGameState<TMove, TAgent> state,
		IMultiAgentGameStateEvaluator<TMove, TAgent> evaluator,
		Action<int> OnIterationCompleted,
		int iterations = 50,
		int maxRollouts = 30,
		float c = 1.4f)
	{
		cFactor = c;
		this.maxRollouts = maxRollouts;
		gameStateEvaluator = evaluator;

		return MctsSearch(agent, state, iterations, OnIterationCompleted);
	}


	private TMove MctsSearch(TAgent agent, IMultiAgentGameState<TMove, TAgent> rootState, int iterations, Action<int> OnIterationCompleted=null)
	{
		var random = new Random();

		var root = new MCTSNode(agent, rootState.GetDuplicated(), gameStateEvaluator);

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
				node = node.Expand(random);
			}

			// Simulation
			float value = node.Rollout(maxRollouts, random);

			// Backpropagation
			node.Backpropagate(value);
			OnIterationCompleted?.Invoke(i);
		}

		var best = root.Children.MaxBy(x => x.Visits);

		return best.Action;
	}	
}