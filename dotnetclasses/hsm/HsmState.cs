using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HSM;

public struct EnterInfo
{
	public enum EnterType {Initial, FromTransition}
	public EnterType type;
	public Event? transitionEvent;

}

public enum HandlingResult {Unhandled, Handled}
public interface IState
{
	public String getId();
	public void onEnter(EnterInfo info);
	public void onExit();
	public void onProcess(float delta);
	public HandlingResult handleEvent(Event hsmEvent);
}

public struct Event
{
	public String name;
	public bool Equals(Event obj)
	{
		return name.Equals(obj.name);
	}

}

public class StateCallbacks
{
	Action onEnterAction;
	Action onExitAction;
	Action<Event> onHandleEventAction;
	Action<float> onProcessAction;

	public StateCallbacks(Action onEnter=null, Action onExit=null, Action<float> onProcess=null, Action<Event> onhandleEvent=null)
	{
		this.onEnterAction = onEnter;
		this.onExitAction = onExit;
		this.onProcessAction = onProcess;
		this.onHandleEventAction = onhandleEvent;
	}
	
	public void onEnter()
	{
		if (onEnterAction != null)
		{
			onEnterAction();
		}
	}
	public void onExit()
	{
		if (onExitAction != null)
		{
			onExitAction();
		}
	}
	public void onProcess(float delta)
	{
		if (onProcessAction != null)
		{
			onProcessAction(delta);
		}
	}
	public void onHandleEvent(Event hsmEvent)
	{
		if (onHandleEventAction != null)
		{
			onHandleEventAction(hsmEvent);
		}
	}
}

public class AtomicState: IState
{
	StateCallbacks callbacks;
	String name;
	public AtomicState(String name, StateCallbacks callbacks=null)
	{
		this.name = name;
		this.callbacks = callbacks;
		this.callbacks ??= new StateCallbacks();
	}

	public String getId()
	{
		return name;
	}

	public void onEnter(EnterInfo info)
	{
		callbacks.onEnter();
	}
	public void onExit()
	{
		callbacks.onExit();
	}
	public void onProcess(float delta)
	{
		callbacks.onProcess(delta);
	}
	public HandlingResult handleEvent(Event hsmEvent)
	{
		callbacks.onHandleEvent(hsmEvent);
		return HandlingResult.Unhandled;
	}
}

public class CompoundState: IState
{
	struct Transition
	{
		public int id;
		public String idFrom;
		public String idTo; 
		public Event? hsmEvent;
		public Func<bool> guard;
		public float delay;
		public Action takenCallback;
		public bool cancellable;
	}

	String name;
	Dictionary<String, IState> childrenDict;
	String initialStateId;
	String currentStateId;
	List<Transition> transitions;
	Func<Event, String> eventToInitialState;
	StateCallbacks callbacks;

	int transitionToProcess = -1;
	float timePassedForTransionProcess = 0.0f;
	public CompoundState(String name, List<IState> children, StateCallbacks callbacks=null, Func<Event, String> eventToInitialState=null)
	{
		Debug.Assert(children.Count > 0);
		Debug.Assert(children.GroupBy((x) => x.getId()).Count() == children.Count);
		transitions = new List<Transition>();
		childrenDict = new Dictionary<string, IState>();

		this.name = name;
		children.ForEach((x) => childrenDict[x.getId()] = x);
		this.callbacks = callbacks;
		this.callbacks ??= new StateCallbacks();
		initialStateId = children[0].getId();
		this.eventToInitialState = eventToInitialState;
	}
	public CompoundState addTransition(string stateIdFrom, string stateIdTo, Event? hsmEvent, bool cancellable=true, Func<bool> guard=null, float delay=0.0f, Action takenCallback=null)
	{
		Debug.Assert(childrenDict.ContainsKey(stateIdFrom));
		Debug.Assert(childrenDict.ContainsKey(stateIdTo));
		transitions.Add(new Transition{id = transitions.Count, idFrom=stateIdFrom, idTo=stateIdTo, hsmEvent=hsmEvent, cancellable = cancellable, guard = guard, delay = delay, takenCallback=takenCallback});
		return this;
	}

	public String getId()
	{
		return name;
	}

	public void enterInitial()
	{
		this.onEnter(new EnterInfo{type = EnterInfo.EnterType.Initial});
	}

	public void onEnter(EnterInfo info)
	{
		callbacks.onEnter();
		if (info.type == EnterInfo.EnterType.Initial)
		{
			currentStateId = initialStateId;
		}
		else
		{
			if (eventToInitialState != null && info.transitionEvent != null)
			{
				currentStateId = eventToInitialState(info.transitionEvent.Value);
				Debug.Assert(childrenDict.ContainsKey(currentStateId));
			}
			else
			{
				currentStateId = initialStateId;
			}
		}
		childrenDict[currentStateId].onEnter(info);
	}
	public void onExit()
	{
		transitionToProcess = -1;
		timePassedForTransionProcess = 0.0f;
		childrenDict[currentStateId].onExit();
		callbacks.onExit();
	}
	public void onProcess(float delta)
	{	
		callbacks.onProcess(delta);
		childrenDict[currentStateId].onProcess(delta);

		if (transitionToProcess != -1)
		{
			timePassedForTransionProcess += delta;
			if (timePassedForTransionProcess >= transitions[transitionToProcess].delay)
			{
				var queuedTransition = transitionToProcess;
				timePassedForTransionProcess = 0.0f;
				transitionToProcess = -1;
				processTransition(queuedTransition);
			}
		}
	}
	public HandlingResult handleEvent(Event hsmEvent)
	{
		callbacks.onHandleEvent(hsmEvent);
		var res = childrenDict[currentStateId].handleEvent(hsmEvent);
		if (res == HandlingResult.Handled)
		{
			return res;
		}

		if (transitionToProcess != -1 && !transitions[transitionToProcess].cancellable)
		{
			return HandlingResult.Unhandled;
		}

		foreach (var transition in transitions)
		{
			if (transition.hsmEvent != null && !transition.hsmEvent.Value.Equals(hsmEvent))
			{
				continue;
			}
			if (!transition.idFrom.Equals(currentStateId))
			{
				continue;
			}
			if(transition.guard != null && !transition.guard())
			{
				continue;
			}
			transitionToProcess = transition.id;
			timePassedForTransionProcess = 0.0f;
			return HandlingResult.Handled;
		}
	
		return HandlingResult.Unhandled;
	}

	private void processTransition(int id)
	{
		var transition = transitions[id];
		childrenDict[currentStateId].onExit();
		currentStateId = transition.idTo;
		childrenDict[currentStateId].onEnter(new EnterInfo{type = EnterInfo.EnterType.FromTransition, transitionEvent = transition.hsmEvent});
		if (transition.takenCallback != null)
		{
			transition.takenCallback();
		}
	}
}

public class ParallelState: IState
{
	String name;
	Dictionary<String, IState> childrenDict;
	StateCallbacks callbacks;
	public ParallelState(String name, List<IState> children, StateCallbacks callbacks=null)
	{
		Debug.Assert(children.Count > 0);
		Debug.Assert(children.GroupBy((x) => x.getId()).Count() == children.Count);
		
		childrenDict = new Dictionary<string, IState>();

		this.name = name;
		children.ForEach((x) => childrenDict[x.getId()] = x);
		this.callbacks = callbacks;
		this.callbacks ??= new StateCallbacks();
		
	}
	public String getId()
	{
		return name;
	}
	public void onEnter(EnterInfo info)
	{
		callbacks.onEnter();
		foreach (var child in childrenDict.Values)
		{
			child.onEnter(info);
		}

	}
	public void onExit()
	{
		callbacks.onExit();
		foreach (var child in childrenDict.Values)
		{
			child.onExit();
		}
	}
	public void onProcess(float delta)
	{
		callbacks.onProcess(delta);
		foreach (var child in childrenDict.Values)
		{
			child.onProcess(delta);
		}
	}
	public HandlingResult handleEvent(Event hsmEvent)
	{
		callbacks.onHandleEvent(hsmEvent);
		foreach (var child in childrenDict.Values)
		{
			child.handleEvent(hsmEvent);
		}
		return HandlingResult.Unhandled;
	}
}
