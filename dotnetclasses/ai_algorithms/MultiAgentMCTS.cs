using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DiskGame;
using Godot;

namespace AI;

public class MultiAgentMCTS<TAction, TAgent>
{
	private class MCTSNode
	{
		public int Id {get; set;}
		public int Depth { get; }

		public IMultiAgentGameState<TAction, TAgent> State { get; }
		public MCTSNode Parent { get; }
		public TAction Action { get; }

		public List<MCTSNode> Children { get; }

		public int Visits { get; private set; }
		public float TotalValue { get; private set; }

		public float MaxValue { get; private set; }
		public float MinValue { get; private set; }

		private List<TAction> UntriedActions;


		public MCTSNode(
			IMultiAgentGameState<TAction, TAgent> state,
			MCTSNode parent = null,
			TAction action = default,
			int depth = 0)
		{
			MaxValue = float.NegativeInfinity;
			MinValue = float.PositiveInfinity;
			State = state;

			Parent = parent;
			Action = action;

			Depth = depth;

			Children = new List<MCTSNode>();

			UntriedActions = state.GetAvailableActions(state.GetCurrentExecutingAgent()).ToList();
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
				newState.ExecuteAction(otherAgentPolicy(newState, newState.GetCurrentExecutingAgent()));
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

			return Children.MaxBy(child => this.Ucb(child, c));

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
				var moves = newState.GetAvailableActions(newState.GetCurrentExecutingAgent());
				var moveList = moves.ToList();

				newState.ExecuteAction(moveList[random.Next(moveList.Count)]);
				while(!isSameAgent(newState.GetCurrentExecutingAgent(), agent))
				{
					newState.ExecuteAction(otherAgentPolicy(newState, newState.GetCurrentExecutingAgent()));
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

			if (value > MaxValue)
			{
				MaxValue = value;
			}

			if (value < MinValue)
			{
				MinValue = value;
			}

			Parent?.Backpropagate(value);
		}


		public float Ucb(MCTSNode child, float c)
		{
			float exploit = child.TotalValue / child.Visits;

			float explore = c * MathF.Sqrt(MathF.Log(Visits) / child.Visits);
			float ucb = explore + exploit;
			return ucb;
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
		int iterations = 50,
		int maxRollouts = 30,
		int maxExpansionDepth = 10,
		Action<int> OnIterationCompleted=null,
		float cFactor = 1.4f,
		float minScoreValue = -100,
		float maxScoreValue = 100
		)
	{
		
		Debug.Assert(state.IsSameAgent(state.GetCurrentExecutingAgent(), agent));
		var random = new Random();

		var root = new MCTSNode(state.GetDuplicated());

		var currentNodeId = 0;
		root.Id = currentNodeId;

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
			if (!node.IsTerminal() && !node.IsFullyExpanded())
			{
				node = node.Expand(random, agent, otherAgentPolicy, state.IsSameAgent);
			}

			currentNodeId += 1;
			node.Id = currentNodeId;
			
			watch.Stop();
//			GD.Print($"Expand Elapsed: {watch.ElapsedTicks}");

			// Simulation
			watch.Reset();
			watch.Start();
			float value = node.Rollout(maxRollouts, evaluator, random, agent, otherAgentPolicy, state.IsSameAgent);

			watch.Stop();
//			GD.Print($"Rollout Elapsed: {watch.ElapsedTicks}");

			// Backpropagation
			watch.Reset();
			watch.Start();

			var remappedValue = MathUtils.Funtions.Remap(Math.Clamp(value, minScoreValue, maxScoreValue), minScoreValue, maxScoreValue, 0.0f, 1.0f);
			node.Backpropagate(value);
			OnIterationCompleted?.Invoke(i);
			watch.Stop();
//			GD.Print($"Backpropagate Elapsed: {watch.ElapsedTicks}");

		}
		iterationWatch.Stop();
		GD.Print($"Iterations Elapsed {iterationWatch.ElapsedTicks}");

		var plan = new List<TAction>();

		foreach (var child in root.Children)
		{
			GD.Print($"Visits: {child.Visits}, TotalValue: {child.TotalValue}, AvgValue: {child.TotalValue/child.Visits}, MinValue {child.MinValue}, MaxValue {child.MaxValue}, UCB: {child.Ucb(root, cFactor)}");
		}

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