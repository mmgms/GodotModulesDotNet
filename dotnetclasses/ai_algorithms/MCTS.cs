using System;
using System.Collections.Generic;
using System.Linq;

namespace AI;

public class MonteCarloTreeSearch<TMove>
{
	private IGameStateEvaluator<TMove> _gameStateEvaluator;
	private int _maxRollouts;
	private float _c;

	private class MCTSNode
	{
		public int Depth { get; }
		public IGameState<TMove> State { get; }
		public MCTSNode Parent { get; }
		public TMove Action { get; }

		public List<MCTSNode> Children { get; }

		public int Visits { get; private set; }
		public float Wins { get; private set; }

		private List<TMove> _untriedActions;

		public bool IsOpponent { get; }

		private readonly IGameStateEvaluator<TMove> _evaluator;

		public MCTSNode(
			IGameState<TMove> state,
			IGameStateEvaluator<TMove> evaluator,
			MCTSNode parent = null,
			TMove action = default,
			int depth = 0,
			bool isOpponent = false)
		{
			State = state;
			_evaluator = evaluator;

			Parent = parent;
			Action = action;

			Depth = depth;
			IsOpponent = isOpponent;

			Children = new List<MCTSNode>();

			Visits = 0;
			Wins = 0;

			_untriedActions = isOpponent
				? state.GetAvailableMovesOpponent().ToList()
				: state.GetAvailableMoves().ToList();
		}


		public bool IsTerminal()
		{
			return State.IsOver();
		}


		public bool IsFullyExpanded()
		{
			return _untriedActions.Count == 0;
		}


		public MCTSNode Expand(Random random)
		{
			int index = random.Next(_untriedActions.Count);

			TMove action = _untriedActions[index];
			_untriedActions.RemoveAt(index);

			var newState = State.GetNewStatePerMove(action);

			var child = new MCTSNode(
				newState,
				_evaluator,
				this,
				action,
				Depth + 1,
				!IsOpponent
			);

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

			bool opponentTurn = IsOpponent;

			for (int i = 0; i < maxRolloutDepth; i++)
			{
				if (newState.IsOver())
				{
					return _evaluator.GetTerminationValue(newState);
				}

				var moves = opponentTurn
					? newState.GetAvailableMovesOpponent()
					: newState.GetAvailableMoves();

				var moveList = moves.ToList();

				newState.ExecuteMove(
					moveList[random.Next(moveList.Count)]
				);

				opponentTurn = !opponentTurn;
			}

			return _evaluator.EvaluateState(newState);
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
		IGameState<TMove> state,
		IGameStateEvaluator<TMove> evaluator,
		int iterations = 50,
		int maxRollouts = 30,
		float c = 1.4f)
	{
		_c = c;
		_maxRollouts = maxRollouts;
		_gameStateEvaluator = evaluator;

		return MctsSearch(state, iterations);
	}


	private TMove MctsSearch(
		IGameState<TMove> rootState,
		int iterations)
	{
		var random = new Random();

		var root = new MCTSNode(
			rootState.GetDuplicated(),
			_gameStateEvaluator
		);


		for (int i = 0; i < iterations; i++)
		{
			var node = root;


			// Selection
			while (!node.IsTerminal() &&
					node.IsFullyExpanded())
			{
				node = node.BestChild(_c);
			}


			// Expansion
			if (!node.IsTerminal() &&
				!node.IsFullyExpanded())
			{
				node = node.Expand(random);
			}


			// Simulation
			float value = node.Rollout(
				_maxRollouts,
				random
			);


			// Backpropagation
			node.Backpropagate(value);
		}

		var best = root.Children.MaxBy(x => x.Visits);

		Console.WriteLine("Values:");

		foreach (var child in root.Children)
		{
			Console.WriteLine(
				$"visits: {child.Visits}, wins: {child.Wins}"
			);
		}


		return best.Action;
	}
}