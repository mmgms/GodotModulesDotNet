using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

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
		public float TotalValue { get; private set; }

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

			UntriedActions = state.GetAvailableMoves(state.GetCurrentExecutingAgent()).ToList();
			Visits = 0;
			TotalValue = 0;

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
			Debug.Assert(isSameAgent(State.GetCurrentExecutingAgent(), agent));
			int index = random.Next(UntriedActions.Count);

			TAction action = UntriedActions[index];
			UntriedActions.RemoveAt(index);

			var newState = State.GetNewStatePerMove(action);

			while(!isSameAgent(newState.GetCurrentExecutingAgent(), agent))
			{
				newState.ExecuteMove(otherAgentPolicy(newState, newState.GetCurrentExecutingAgent()));
			}

			Debug.Assert(isSameAgent(newState.GetCurrentExecutingAgent(), agent));
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
			
			Debug.Assert(isSameAgent(State.GetCurrentExecutingAgent(), agent));
			var newState = State.GetDuplicated();

			for (int i = 0; i < maxRolloutDepth; i++)
			{
				var moves = newState.GetAvailableMoves(newState.GetCurrentExecutingAgent());
				var moveList = moves.ToList();

				newState.ExecuteMove(moveList[random.Next(moveList.Count)]);
				while(!isSameAgent(newState.GetCurrentExecutingAgent(), agent))
				{
					newState.ExecuteMove(otherAgentPolicy(newState, newState.GetCurrentExecutingAgent()));
				}

				Debug.Assert(isSameAgent(newState.GetCurrentExecutingAgent(), agent));
				if (newState.IsOver() || moveList.Count == 0)
				{
					return evaluator.GetTerminationValue(newState, newState.GetCurrentExecutingAgent());
				}

			}
			Debug.Assert(isSameAgent(newState.GetCurrentExecutingAgent(), agent));
			return evaluator.EvaluateState(newState, newState.GetCurrentExecutingAgent());
		}


		public void Backpropagate(float value)
		{
			Visits++;
			TotalValue += value;

			Parent?.Backpropagate(value);
		}


		private float Ucb(MCTSNode child, float c)
		{
			float exploit = child.TotalValue / child.Visits;

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
		int maxExpansionDepth = 10,
		Action<int> OnIterationCompleted=null,
		float cFactor = 1.4f
		)
	{
		
		Debug.Assert(isSameAgent(state.GetCurrentExecutingAgent(), agent));
		var random = new Random();

		var root = new MCTSNode(state.GetDuplicated());

		var iterationWatch = new System.Diagnostics.Stopwatch();
		iterationWatch.Start(); 
		for (int i = 0; i < iterations; i++)
		{
			var node = root;
			var watch = new System.Diagnostics.Stopwatch();

			// Selection
			watch.Reset();
			watch.Start();
			while (!node.IsTerminal() && node.IsFullyExpanded())
			{
				node = node.BestChild(cFactor);
			}
			watch.Stop();
//			GD.Print($"Selection Elapsed: {watch.ElapsedTicks}");

			// Expansion
			watch.Reset();
			watch.Start();
			if (node.Depth < maxExpansionDepth && !node.IsTerminal() && !node.IsFullyExpanded())
			{
				node = node.Expand(random, agent, otherAgentPolicy, isSameAgent);
			}
			watch.Stop();
//			GD.Print($"Expand Elapsed: {watch.ElapsedTicks}");

			// Simulation
			watch.Reset();
			watch.Start();
			float value = node.Rollout(maxRollouts, evaluator, random, agent, otherAgentPolicy, isSameAgent);
			watch.Stop();
//			GD.Print($"Rollout Elapsed: {watch.ElapsedTicks}");

			// Backpropagation
			watch.Reset();
			watch.Start();
			node.Backpropagate(value);
			OnIterationCompleted?.Invoke(i);
			watch.Stop();
//			GD.Print($"Backpropagate Elapsed: {watch.ElapsedTicks}");

		}
		iterationWatch.Stop();
		GD.Print($"Iterations Elapsed {iterationWatch.ElapsedTicks}");

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