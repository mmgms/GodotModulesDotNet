using System;
using System.Collections.Generic;
using System.Linq;
namespace Planning.HTN;

public class HLA
{
	public String name;
	public List<Refinement> refinements;
	public Dictionary<Literal, bool> effects;
	public bool isPrimitive;
	public Dictionary<Literal, bool> primitivePrecondition;

}

public class Refinement
{
	public Dictionary<Literal, bool> precondition;
	public List<HLA> actions;
}


public class Planner
{
	public class Node
	{
		public State state;
		public Action action;
	}

	public static IEnumerable<HLA> decomposeHla(State state, HLA hla, Stack<Node> nodes)
	{
		if (hla.isPrimitive)
		{
			if (!state.satisfiesLiterals(hla.primitivePrecondition))
			{
				yield break;
			}
			state.applyEffects(hla.effects);
			yield return hla;
			yield break;
		}

		foreach (var refinement in hla.refinements)
		{
			if (state.satisfiesLiterals(refinement.precondition))
			{
				var newState = state.clone();
				nodes.Push(new Node{state = newState});
				var decomposed = decomposeRefinement(refinement, nodes);
				var hasElements = false;
				foreach (var elem in decomposed)
				{
					hasElements = true;
					yield return elem;
				}
				if (!hasElements)
				{
					nodes.Pop();
					continue;
				}
			}
		}

	}

	public static IEnumerable<HLA> decomposeRefinement(Refinement refinement, Stack<Node> nodes)
	{
		var state = nodes.Peek().state;
		foreach (var action in refinement.actions)
		{
			if (state.satisfiesLiterals(refinement.precondition))
			{
				var decomposed = decomposeHla(state, action, nodes);
				var hasElements = false;
				foreach (var elem in decomposed)
				{
					hasElements = true;
					yield return elem;
				}
				if (!hasElements)
				{
					yield break;
				}
			}
		}
	}
}