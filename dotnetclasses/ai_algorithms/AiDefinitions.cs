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