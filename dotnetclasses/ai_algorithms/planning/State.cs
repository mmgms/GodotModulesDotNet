using System.Collections.Generic;

namespace Planning;

public class State
{
	public List<Literal> literals;
	public Dictionary<Literal, bool> states;
	public State()
	{
		states = [];
	}

	public State clone()
	{
		var newState = new State();
		foreach (var pair in states)
		{
			newState.states[pair.Key] = pair.Value;
		}
		return newState;
	}

	public void buildStatesFromLiterals()
	{
		states.Clear();
		foreach (var literal in literals)
		{
			if (literal.getValueCallback())
			{
				states[literal] = true;
			}
		}
	}

	public bool satisfiesGoal(Goal goal)
	{
		return satisfiesLiterals(goal.desired);
	}

	public bool satisfiesLiterals(Dictionary<Literal, bool> literals)
	{
		foreach (var (literal, value) in literals)
		{
			if (states.GetValueOrDefault(literal, false) != value)
			{
				return false;
			}
		}
		return true;
	}

	public int getApproxDistanceToGoal(Goal goal)
	{
		var dist = 0;
		foreach (var state in goal.desired)
		{
			if (states.GetValueOrDefault(state.Key, false) != state.Value)
			{
				dist += 1;
			}
		}
		return dist;
	}

	public void removeLiteral(Literal literal)
	{
		states.Remove(literal);
	}

	public void addLiteral(Literal literal)
	{
		states[literal] = true;
	}

	public void applyEffects(Dictionary<Literal, bool> effects)
	{
		foreach (var (literal, value) in effects)
		{
			if (!value)
			{
				removeLiteral(literal);
			}
			else
			{
				addLiteral(literal);
			}
		}
	}
}
