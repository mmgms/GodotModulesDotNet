using System;
using System.Collections.Generic;

namespace AI;

public interface IGameState<TMove>
{
    IGameState<TMove> GetDuplicated();

    IEnumerable<TMove> GetAvailableMoves();

    IEnumerable<TMove> GetAvailableMovesOpponent();

    IGameState<TMove> GetNewStatePerMove(TMove move);

    void ExecuteMove(TMove move);

    bool IsOver();
}

public interface IGameStateEvaluator<TMove>
{
    float EvaluateState(IGameState<TMove> gameState);

    float GetTerminationValue(IGameState<TMove> gameState);
}

public class ProabilityEffect<TAction, TAgent>
{
	public Action<IMultiAgentGameState<TAction, TAgent>, TAction> Callback;
	public float probability;
}

public interface IMultiAgentGameState<TAction, TAgent> 
{

	TAgent GetCurrentExecutingAgent();

    IMultiAgentGameState<TAction, TAgent> GetDuplicated();

    IEnumerable<TAction> GetAvailableActions(TAgent agent);

    IMultiAgentGameState<TAction, TAgent> GetNewStatePerMove(TAction move);

	IEnumerable<ProabilityEffect<TAction, TAgent>> GetProbabilityEffectsPerAction(TAction move);
   	void ExecuteAction(TAction move);
	bool IsSameAction(TAction a, TAction b);
	bool IsSameAgent(TAgent a, TAgent b);
	IEnumerable<TAction> ExpandParametricAction(TAction action);

	bool IsProbabilityAction(TAction action);
	bool IsParametricAction(TAction action);
    bool IsOver();
}

public interface IMultiAgentGameStateEvaluator<TMove, TAgent>
{
    float EvaluateState(IMultiAgentGameState<TMove, TAgent> gameState, TAgent agent);

    float GetTerminationValue(IMultiAgentGameState<TMove, TAgent> gameState, TAgent agent);
}