namespace AI;
using System;

public class MiniMax<TMove>
{
	private int _targetDepth = 3;
	private IGameStateEvaluator<TMove> _gameStateEvaluator;
	private TMove _bestMove;

	public MiniMax<TMove> SetTargetDepth(int target)
	{
		_targetDepth = target;
		return this;
	}

	public TMove GetBestMove(
		IGameState<TMove> state,
		IGameStateEvaluator<TMove> evaluator)
	{
		_gameStateEvaluator = evaluator;
		_bestMove = default;

		MinimaxRecursive(
			0,
			true,
			state,
			float.NegativeInfinity,
			float.PositiveInfinity
		);

		return _bestMove;
	}

	private float MinimaxRecursive(
		int currentDepth,
		bool maxTurn,
		IGameState<TMove> state,
		float alpha,
		float beta)
	{
		if (state.IsOver())
		{
			return _gameStateEvaluator.GetTerminationValue(state);
		}

		if (currentDepth >= _targetDepth)
		{
			return _gameStateEvaluator.EvaluateState(state);
		}

		return maxTurn
			? MaxValue(currentDepth, state, alpha, beta)
			: MinValue(currentDepth, state, alpha, beta);
	}

	private float MaxValue(
		int currentDepth,
		IGameState<TMove> state,
		float alpha,
		float beta)
	{
		float value = float.NegativeInfinity;

		foreach (var move in state.GetAvailableMoves())
		{
			var newState = state.GetNewStatePerMove(move);

			float nextValue = MinimaxRecursive(
				currentDepth + 1,
				false,
				newState,
				alpha,
				beta
			);

			if (nextValue > value && currentDepth == 0)
			{
				_bestMove = move;
			}

			value = Math.Max(value, nextValue);

			if (value >= beta)
				return value;

			alpha = Math.Max(alpha, value);
		}

		return value;
	}

	private float MinValue(
		int currentDepth,
		IGameState<TMove> state,
		float alpha,
		float beta)
	{
		float value = float.PositiveInfinity;

		foreach (var move in state.GetAvailableMovesOpponent())
		{
			var newState = state.GetNewStatePerMove(move);

			value = Math.Min(
				value,
				MinimaxRecursive(
					currentDepth + 1,
					true,
					newState,
					alpha,
					beta
				)
			);

			if (value <= alpha)
				return value;

			beta = Math.Min(beta, value);
		}

		return value;
	}
}