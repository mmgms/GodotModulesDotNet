using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

namespace AI;

public class SingleAgentPlanner<TAction, TAgent>
{
	public enum NodeType {Maximizer, Chance, ParameterExpansion}
	public class SearchNode
	{
		public NodeType type;
		public TAction action;
		public int depth;
		public SearchNode parent;
		public List<SearchNode> children;
		public AI.IMultiAgentGameState<TAction, TAgent> state;
		public List<float> childrenProbabilities; 
		public float value = float.NegativeInfinity;

		public SearchNode(NodeType type)
		{
			this.type = type;
			children = new List<SearchNode>();
		}

		public void BackPropagate(float value, int depthSearched, Dictionary<AI.IMultiAgentGameState<TAction, TAgent>, SearchInfo> heuristic)
		{
			if (type == NodeType.Maximizer)
			{
				this.value = Math.Max(this.value, value);
				SearchInfo info;
				if (!heuristic.TryGetValue(state, out info))
				{
					heuristic[state] = new SearchInfo();	
				}
				info = heuristic[state];
				if (children.Count > 0)
				{
					info.bestMove = children.MaxBy((x) => x.value).action;
				}
				info.depthSearched = depthSearched;
				info.value = this.value;
			}
			if (type == NodeType.Chance)
			{
				var expectedValue = 0.0f;
				for(int i=0; i < children.Count; i++)
				{
					expectedValue += children[i].value * childrenProbabilities[i];
				}
				this.value = expectedValue;
			}
			if (type == NodeType.ParameterExpansion)
			{
				this.value = Math.Max(this.value, value);
			}
			
			parent?.BackPropagate(this.value, depthSearched, heuristic);
		}
	}

	public class PlanResult
	{
		public List<TAction> plan;
		public float bestScore;
	}

	public class SearchInfo
	{
		public TAction bestMove;
		public int depthSearched;
		public float value;
	}

	public PlanResult GetIterativeDeepeningPlan(
		TAgent agent,
		AI.IMultiAgentGameState<TAction, TAgent> state, 
		AI.IMultiAgentGameStateEvaluator<TAction, TAgent> evaluator,
		Func<AI.IMultiAgentGameState<TAction, TAgent>, TAgent, TAction> otherAgentsPolicy,
		IEqualityComparer<AI.IMultiAgentGameState<TAction, TAgent>> comparer,
		int maxDepth, int maxIterations
		)
	{
		var heuristic = new Dictionary<AI.IMultiAgentGameState<TAction, TAgent>, SearchInfo>(comparer);

		PlanResult plan = null;
		foreach (var i in Enumerable.Range(1, maxDepth))
		{
			plan = GetPlan(agent, state, evaluator, otherAgentsPolicy, i, maxIterations, heuristic);
		}
		return plan;
	}

	// private IEnumerable<SearchNode> expandNodeParametrizedAction(Action<AI.IMultiAgentGameState<TAction, TAgent>> simulateState,
	// 	 Stack<SearchNode> queue, Func<TAction, IEnumerable<TAction>> expandParametricAction, SearchNode current, TAction action)
	// {
	// 	var parameterNode = new SearchNode(NodeType.ParameterExpansion);
	// 	parameterNode.depth = current.depth;
	// 	parameterNode.action = action;
	// 	parameterNode.parent = current;
	// 	parameterNode.state = current.state;
	// 	current.children.Add(parameterNode);

	// 	foreach (var parametrizedAction in expandParametricAction(action))
	// 	{
	// 		var stateAfterParametrizedAction = current.state.GetNewStatePerMove(parametrizedAction);
	// 		var maximizerNode = new SearchNode(NodeType.Maximizer);
	// 		simulateState(stateAfterParametrizedAction);
	// 		maximizerNode.state = stateAfterParametrizedAction;
	// 		maximizerNode.depth = current.depth + 1;
	// 		maximizerNode.action = action;
	// 		maximizerNode.parent = parameterNode; 
	// 		parameterNode.children.Add(maximizerNode);
	// 		yield return maximizerNode;
	// 	}		
	// }

	public PlanResult GetPlan(
		TAgent agent,
		AI.IMultiAgentGameState<TAction, TAgent> state, 
		AI.IMultiAgentGameStateEvaluator<TAction, TAgent> evaluator,
		Func<AI.IMultiAgentGameState<TAction, TAgent>, TAgent, TAction> otherAgentsPolicy,
		int maxDepth, int maxIterations,
		Dictionary<AI.IMultiAgentGameState<TAction, TAgent>, SearchInfo> heuristic
		)
	{
		var queue = new Stack<SearchNode>();

		var start = new SearchNode(NodeType.Maximizer);
		start.state = state.GetDuplicated();

		queue.Push(start);

		var iterations = 0;

		while (queue.Count > 0)
		{
			iterations += 1;
			SearchNode current = queue.Pop();

			if (current.depth >= maxDepth || iterations > maxIterations)
			{
				current.value = evaluator.EvaluateState(current.state, agent);
				current.BackPropagate(current.value, current.depth, heuristic);
				continue;
			}
			if (iterations > maxIterations)
			{
				continue;
			}

			var availableMoves = current.state.GetAvailableActions(current.state.GetCurrentExecutingAgent()).ToList();

			SearchInfo info = null;
			if (heuristic.ContainsKey(current.state))
			{
				info = heuristic[current.state];
			}  

			// if (info != null && info.depthSearched >= current.depth)
			// {
			// 	current.value = info.value;
			// 	current.BackPropagate(current.value, current.depth, heuristic);
			// 	continue;
			// }

			// if (info != null && info.bestMove != null)
			// {
			// 	availableMoves = availableMoves
			// 		.OrderByDescending(x => isSameAction(x, info.bestMove))
			// 		.ToList();				
			// }

			foreach (var action in availableMoves)
			{
				if (current.state.IsParametricAction(action))
				{
					var parameterNode = new SearchNode(NodeType.ParameterExpansion);
					parameterNode.depth = current.depth;
					parameterNode.action = action;
					parameterNode.parent = current;
					parameterNode.state = current.state;
					current.children.Add(parameterNode);

					foreach (var parametrizedAction in current.state.ExpandParametricAction(action))
					{
						var stateAfterParametrizedAction = current.state.GetNewStatePerMove(parametrizedAction);
						var maximizerNode = new SearchNode(NodeType.Maximizer);
						SimulateStateUntilAgent(agent, stateAfterParametrizedAction, otherAgentsPolicy, current.state.IsSameAgent);
						maximizerNode.state = stateAfterParametrizedAction;
						maximizerNode.depth = current.depth + 1;
						maximizerNode.action = action;
						maximizerNode.parent = parameterNode; 
						parameterNode.children.Add(maximizerNode);
						queue.Push(maximizerNode);		
					}
					continue;	
				}

				if (current.state.IsProbabilityAction(action))
				{	

					var effects =  current.state.GetProbabilityEffectsPerAction(action).ToList();
					var newStateProb = current.state.GetNewStatePerMove(action);
					var chanceNode = new SearchNode(NodeType.Chance);
					chanceNode.childrenProbabilities = effects.Select((x) => x.probability).ToList();
					chanceNode.depth = current.depth;
					chanceNode.action = action;
					chanceNode.parent = current;
					chanceNode.state = newStateProb;
					current.children.Add(chanceNode);

					foreach (var effect in effects)
					{
						var maximizerNode = new SearchNode(NodeType.Maximizer);
						var newStateAfterEffect = newStateProb.GetDuplicated();
						effect.Callback(newStateAfterEffect, action);
						SimulateStateUntilAgent(agent, newStateAfterEffect, otherAgentsPolicy, current.state.IsSameAgent);
						maximizerNode.depth = current.depth + 1;
						maximizerNode.action = action;
						maximizerNode.parent = chanceNode; 
						maximizerNode.state = newStateAfterEffect;
						chanceNode.children.Add(maximizerNode);
						queue.Push(maximizerNode);		
					}
					continue;
				}
				// expand with other policy until its our turn again
				var newState = current.state.GetNewStatePerMove(action);
				SimulateStateUntilAgent(agent, newState, otherAgentsPolicy, current.state.IsSameAgent);
				var newNode = new SearchNode(NodeType.Maximizer);
				newNode.state = newState;
				newNode.depth = current.depth + 1;
				newNode.action = action;
				newNode.parent = current;
				current.children.Add(newNode);

				queue.Push(newNode);
			}

		}

		var plan = new List<TAction>();
		var res = new PlanResult();

		var temp = start;
		while (temp.children.Count > 0)
		{
			var best = temp.children.MaxBy((x) => x.value);
			plan.Add(best.action);
			temp = best;
		}
		res.plan = plan;
		res.bestScore = start.value;
		return res;
	}

	private void SimulateStateUntilAgent(TAgent agent,
		AI.IMultiAgentGameState<TAction, TAgent> state, 
		Func<AI.IMultiAgentGameState<TAction, TAgent>, TAgent, TAction> otherAgentsPolicy,
		Func<TAgent, TAgent, bool> isSameAgent)
	{
		while (!isSameAgent(state.GetCurrentExecutingAgent(), agent))
		{
			state.ExecuteAction(otherAgentsPolicy(state, state.GetCurrentExecutingAgent()));
		}
	}
}