using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WesternSimGame;

public class GameState
{
	private const int MAX_ACTIONS = 20;
	private const int MAX_PARAMETER_VALUES = 10; 
	private const int MAX_REQUESTS = 10;
	private const int MAX_PROBABILITY_EFFECTS = 10;
    public int CurrentTurn { get; set; }
	public short CurrentCharacterToProcess { get; set; }

    public List<CharacterInfo> Characters { get; } = new();


	public int CurrentRequestId { get; set; }

	public RequestInfo[] Requests { get; } = new RequestInfo[MAX_REQUESTS];
	public CharacterAction[] availableActions = new CharacterAction[MAX_ACTIONS];

    public Dictionary<Definitions.PlaceType, List<Definitions.PlaceType>> PlacesGraph { get; set; } = new();

    public enum ActionType
    { 
		Unclassified, DoNothing, AcceptRequest, RefuseRequest, ProcessRequest, Move, UseItemOnSelf, Shoot, Holster, AimAt, MugRequest, Mine, Loot, BuyItem
    }

	public struct ProbabilityActionEffect
	{
		public float Probability;
		public  Action<GameState, CharacterActionExecution> Callback { get; set; }
	}
    public struct CharacterAction
    {
        public string Description { get; set; }
        public ActionType Type { get; set; }
		public Func<GameState, CharacterActionExecution, bool> Precondition {get; set;}
        public Action<GameState, CharacterActionExecution> Callback { get; set; }
		
		public Func<GameState, CharacterActionExecution, IEnumerable<Definitions.PlaceType>> GetPlaceTypePossibleValues;
		public Func<GameState, CharacterActionExecution, IEnumerable<short>> GetOtherCharacterIdPossibleValues;
		public Func<GameState, CharacterActionExecution, IEnumerable<Definitions.ItemType>> GetItemTypePossibleValues;

		public bool IsProbabilityAction;
		public bool IsParametricAction;

		public ProbabilityActionEffect[] ProbabilityEffects = new ProbabilityActionEffect[MAX_PROBABILITY_EFFECTS];

		public CharacterAction()
		{
			GetItemTypePossibleValues = (state, execution) => {return EmptyItemEnumerable();};
			GetPlaceTypePossibleValues = (state, execution) => {return EmptyPlaceEnumerable();};
			GetOtherCharacterIdPossibleValues = (state, execution) => {return EmptyIdEnumerable();};

		}

    }
	static public IEnumerable<Definitions.PlaceType> EmptyPlaceEnumerable()
	{
		yield return Definitions.PlaceType.Unclassified;
	}

	static public IEnumerable<Definitions.ItemType> EmptyItemEnumerable()
	{
		yield return Definitions.ItemType.Unclassified;
	}

	static public IEnumerable<short> EmptyIdEnumerable()
	{
		yield return -1;
	}
	public struct CharacterActionExecution
	{
        public int InitiatorId { get; set; }
		public ActionType ActionType {get; set;}
		public Definitions.ItemType itemType;
		public Definitions.PlaceType placeType;
		public short otherCharacterId = -1;

		public CharacterActionExecution()
		{
			
		}
	}

	public CharacterAction getActionBytype(ActionType type)
	{
		return availableActions[(int) type];
	}


    public double ProbabilityOfMiningGold { get; set; } = 0.1;
    public int AmountMined { get; set; } = 25;

	public delegate void EndTurn(GameState gameState);

	public event EndTurn OnTurnCompleted;

	public delegate void ActionExecuted(CharacterActionExecution action);
	public event ActionExecuted OnActionExecuted;

	public delegate void TurnUpdate(int turn);

	public event TurnUpdate OnTurnUpdate;
	
	public delegate void CharacterExecutingUpdate(CharacterInfo characterInfo);

	public event CharacterExecutingUpdate OnCharacterExecutingUpdate;

	public delegate void CharacterMoved(CharacterInfo character, Definitions.PlaceType oldPlace, Definitions.PlaceType newPlace);

	public event CharacterMoved OnCharacterMoved;

    public GameState()
    {
    }

	public CharacterInfo getCharacterById(int id)
	{
		return Characters[id];
	}

	public string GetActionDescription(CharacterActionExecution action)
	{
		var itemDesc = action.itemType != Definitions.ItemType.Unclassified ? action.itemType.ToString() : "";
		var otherCharacter = action.otherCharacterId >= 0 ? getCharacterById(action.otherCharacterId).getNameAndType() : "";
		var placeDesc = action.placeType != Definitions.PlaceType.Unclassified ? action.placeType.ToString() : "";
		return $"{action.ActionType}({itemDesc}, {otherCharacter}, {placeDesc})";
	}

	public GameState getDuplicated()
	{
		var new_state = new GameState();
		new_state.PlacesGraph = PlacesGraph;
		Characters.ForEach(c => new_state.Characters.Add(c.getDuplicated()));
		new_state.OnTurnCompleted = this.OnTurnCompleted;
		new_state.CurrentCharacterToProcess = this.CurrentCharacterToProcess;
		new_state.CurrentTurn = this.CurrentTurn;
		new_state.CurrentRequestId = this.CurrentRequestId;

		this.Requests.CopyTo(new_state.Requests, 0);
		this.availableActions.CopyTo(new_state.availableActions, 0);
		new_state.PlacesGraph = this.PlacesGraph;

		return new_state;
	}

	public bool Equals(GameState other)
	{
		if (other.CurrentRequestId != CurrentRequestId)
		{
			return false;
		}
		if (other.Characters.Count != Characters.Count)
		{
			return false;
		}
		foreach (var character in Characters)
		{
			if (!other.getCharacterById(character.Id).Equals(character))
			{
				return false;
			}
		}
		return true;
	}

	public int GetHash()
	{
		var hashCode = new HashCode();
		Characters.ForEach((x) => hashCode.Add(x.GetHash()));
		hashCode.Add(CurrentRequestId);
		return hashCode.ToHashCode();
	}

    public CharacterInfo AddCharacter(
        string name,
        Definitions.CharacterType type)
    {
        var character = new CharacterInfo
        {
            Name = name,
            Type = type
        };

        Characters.Add(character);
		character.Id = (short)(Characters.Count - 1);
        return character;
    }

    public void AddConnection(
        Definitions.PlaceType placeA,
        Definitions.PlaceType placeB,
        bool bidirectional = true)
    {
        if (!PlacesGraph.TryGetValue(placeA, out var neighbours))
        {
            PlacesGraph[placeA] = new List<Definitions.PlaceType>();
        }
        PlacesGraph[placeA].Add(placeB);

        if (!bidirectional)
            return;

        AddConnection(placeB, placeA, false);
    }

    public void Loot(CharacterInfo toCharacter, CharacterInfo other)
    {
        toCharacter.IncreaseGold(other.getGold());
        toCharacter.AddItemList(other.GetAllItems());
		foreach (var item in other.GetAllItems())
		{
			other.RemoveItem(other.GetIndexForType(item.ItemData.Type));
		}
    }

    public IEnumerable<CharacterInfo> GetCharactersInPlace(Definitions.PlaceType place)
    {
        foreach (var character in Characters)
        {
            if (character.getCurrentPlace() == place)
                yield return character;
        }
    }

    public List<Definitions.PlaceType> GetPlaceNeighbours(Definitions.PlaceType place)
    {
        if (!PlacesGraph.TryGetValue(place, out var neighbours))
            return new List<Definitions.PlaceType>();

        return neighbours;
    }

	public RequestInfo getRequestInfoById(int id)
	{
		return Requests[id];
	}

	public void setRequestAtIndex(int id, RequestInfo info)
	{
		Requests[id] = info;
	}

	public int getFirstFreeRequestInfoSlotIndex()
	{
		var idx = -1;

		for (int i=0; i < Requests.Length; i++)
		{
			if (Requests[i].Status == RequestInfo.RequestStatus.Unused)
			{
				return i;
			}
		}
		return idx;
	}

	public void SetRequestStatus(int id, RequestInfo.RequestStatus status)
	{
		Requests[id] = Requests[id] with {Status = status};
	}

	public void addAction(CharacterAction action)
	{
		availableActions[(int)action.Type] = action;
	}
	public IEnumerable<CharacterActionExecution> getAvailableActions(short characterId)
	{
		var character = getCharacterById(characterId);
		if (character.isDead())
		{
			var execution = new CharacterActionExecution
			{
				InitiatorId = characterId,
				ActionType = ActionType.DoNothing
			};
			yield return execution;
			yield break;
		}

		if (character.GetHasSentRequest())
		{
			var process = new CharacterActionExecution
			{
				InitiatorId = characterId,
				ActionType = ActionType.ProcessRequest
			};
			var processAction = availableActions[(int)ActionType.ProcessRequest];
			if (processAction.Precondition(this, process))
			{	
				yield return process;
				yield break;
			}
		}

		if (character.GetHasReceivedRequest())
		{
			var accept = new CharacterActionExecution
			{
				InitiatorId = characterId,
				ActionType = ActionType.AcceptRequest
			};
			var action = availableActions[(int)ActionType.AcceptRequest];
			if (action.Precondition(this, accept))
			{	
				yield return accept;
			}

			var refuse = new CharacterActionExecution
			{
				InitiatorId = characterId,
				ActionType = ActionType.RefuseRequest
			};

			action = availableActions[(int)ActionType.RefuseRequest];
			if (action.Precondition(this, refuse))
			{	
				yield return refuse;
				yield break;
			}
			
		}

		for (int i=0; i < availableActions.Length; i++)
		{
			var action = availableActions[i];
			if (action.Type == ActionType.Unclassified)
			{
				continue;
			}
			var execution = new CharacterActionExecution
			{
				InitiatorId = characterId,
				ActionType = action.Type
			};
			if(action.Precondition != null && !action.Precondition(this, execution))
			{
				continue;
			}
			yield return execution;
		}
	}

	public IEnumerable<CharacterActionExecution> expandActionParameter(CharacterActionExecution execution)
	{
		var action = availableActions[(int)execution.ActionType];
		foreach(var place in action.GetPlaceTypePossibleValues(this, execution))
		{
			foreach(var otherId in action.GetOtherCharacterIdPossibleValues(this, execution))
			{
				foreach(var itemId in action.GetItemTypePossibleValues(this, execution))
				{
					var actionExecution = new CharacterActionExecution
					{
						ActionType = execution.ActionType,
						otherCharacterId = otherId,
						placeType = place,
						itemType = itemId,
					};
					yield return actionExecution;
				}
			}
		}
	}


	public void executeAction(CharacterActionExecution execution, bool executeProbEffect=true)
	{
		var action = availableActions[(int)execution.ActionType];
		action.Callback?.Invoke(this, execution);
		if (action.IsProbabilityAction)
		{
			if (executeProbEffect)
			{
				var effect = MathUtils.Funtions.SampleWeighted(action.ProbabilityEffects, (effect) => effect.Probability);
				effect.Callback?.Invoke(this, execution);
			}
		}
		OnActionExecuted?.Invoke(execution);
		CurrentCharacterToProcess += 1;

		if (CurrentCharacterToProcess == Characters.Count)
		{
			ProcessTurn();
			CurrentCharacterToProcess = 0;
			
			CurrentTurn += 1;
			OnTurnUpdate?.Invoke(CurrentTurn);
		}
		OnCharacterExecutingUpdate?.Invoke(Characters[CurrentCharacterToProcess]);
	}

	public CharacterInfo getCharacterToProcess()
	{
		return Characters[CurrentCharacterToProcess];
	}

	public int TurnsToIncreaseHUnger = 3;

    public void ProcessTurn()
    {
        foreach (var character in Characters)
        {
            if (character.isDead())
                continue;

            if (character.getHp() <= 0)
            {
				if (character.GetHasSentRequest())
				{
					SetRequestStatus(character.getCurrentRequestId(), RequestInfo.RequestStatus.Unused);
				}
                character.Kill();
                return;
            }

			if (CurrentTurn % TurnsToIncreaseHUnger == 0)
			{
            	character.IncreaseHunger();
			}

            if (character.getHunger() >= character.MaxHungerLevel)
            {
                character.ReduceHp();

                if (character.getHp() <= 0)
                {
                    character.Kill();
                    return;
                }
            }

        }
		OnTurnCompleted?.Invoke(this);
    }




}