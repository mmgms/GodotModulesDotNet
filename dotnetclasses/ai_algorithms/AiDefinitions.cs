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

public class StateProbability<TMove, TAgent>
{
	public IMultiAgentGameState<TMove, TAgent> state;
	public float probability;
}

public interface IMultiAgentGameState<TMove, TAgent> 
{
	
	TAgent GetCurrentExecutingAgent();

    IMultiAgentGameState<TMove, TAgent> GetDuplicated();

    IEnumerable<TMove> GetAvailableMoves(TAgent agent);

    IMultiAgentGameState<TMove, TAgent> GetNewStatePerMove(TMove move);

	IEnumerable<StateProbability<TMove, TAgent>> GetProbabilityStatesPerMove(TMove move);
   void ExecuteMove(TMove move);

    bool IsOver();
}

public interface IMultiAgentGameStateEvaluator<TMove, TAgent>
{
    float EvaluateState(IMultiAgentGameState<TMove, TAgent> gameState, TAgent agent);

    float GetTerminationValue(IMultiAgentGameState<TMove, TAgent> gameState, TAgent agent);
}