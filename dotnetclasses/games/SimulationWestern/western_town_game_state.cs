using System;
using System.Collections.Generic;

namespace WesternSimGame;

public class GameState
{
    public int CurrentTurn { get; set; }
	public int CurrentCharacterToProcess { get; set; }

    public List<CharacterInfo> Characters { get; } = new();

	public int CurrentRequestId { get; set; }

	public Dictionary<int, RequestInfo> Requests { get; } = new();

    public Dictionary<Definitions.PlaceType, List<Definitions.PlaceType>> PlacesGraph { get; set; } = new();

    public enum ActionType
    {
        Unclassified,
		Move,
        Shoot,
		Eat
    }

	public class ProbabilityActionEffect
	{
		public float Probability;
		public  System.Action<GameState> Callback { get; set; }
	}

    public class CharacterAction
    {
		public int Id {get; set;}
        public string Description { get; set; }
        public CharacterInfo Initiator { get; set; }
        public CharacterInfo Recipient { get; set; }
		public Definitions.PlaceType Place {get; set;}
        public ActionType Type { get; set; }
        public System.Action<GameState> Callback { get; set; }


		public bool IsProbabilityAction;

		public List<ProbabilityActionEffect> ProbabilityEffects;

        public CharacterAction(CharacterInfo initiator)
        {
            Initiator = initiator;
        }

    }


    public double ProbabilityOfMiningGold { get; set; } = 0.1;
    public int AmountMined { get; set; } = 25;

	public delegate void EndTurn(GameState gameState);

	public event EndTurn OnTurnCompleted;

	public delegate void ActionExecuted(CharacterAction action);
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

	public GameState getDuplicated()
	{
		var new_state = new GameState();
		new_state.PlacesGraph = PlacesGraph;
		Characters.ForEach(c => new_state.Characters.Add(c.getDuplicated()));
		new_state.OnTurnCompleted = this.OnTurnCompleted;
		new_state.CurrentCharacterToProcess = this.CurrentCharacterToProcess;
		new_state.CurrentTurn = this.CurrentTurn;
		new_state.CurrentRequestId = this.CurrentRequestId;

		foreach(var item in Requests)
		{
			new_state.Requests[item.Key] = item.Value.getDuplicated();
		}
		
		foreach (var item in PlacesGraph){
			new_state.PlacesGraph[item.Key] = item.Value;
		}

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
		character.Id = Characters.Count - 1;
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
    }

    public List<CharacterInfo> GetCharactersInPlace(Definitions.PlaceType place)
    {
        var result = new List<CharacterInfo>();

        foreach (var character in Characters)
        {
            if (character.getCurrentPlace() == place)
                result.Add(character);
        }

        return result;
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

    public List<CharacterAction> GetActionPerCharacter(CharacterInfo character)
    {
        var actions = new List<CharacterAction>();

		var nullAction = new CharacterAction(character);
		nullAction.Callback = (state) => {return;};
		nullAction.Description = "Do nothing";

		actions.Add(nullAction);

		if (character.isDead())
		{
			return actions;
		}

		var characterId = character.Id;

        if (character.GetHasReceivedRequest())
        {
            var refuseRequest = new CharacterAction(character)
            {
                Description =
                    $"Refuse Request: {getRequestInfoById(character.getCurrentRequestId()).GetDescription()}",
                Callback = (state) => {
					var character = state.getCharacterById(characterId);
					state.getRequestInfoById(character.getCurrentRequestId()).Status = RequestInfo.RequestStatus.Refused;
					character.RefuseRequest();
					}
            };

            var acceptRequest = new CharacterAction(character)
            {
                Description =
                    $"Accept Request: {getRequestInfoById(character.getCurrentRequestId()).GetDescription()}",
                Callback = (state) => {
					var character = state.getCharacterById(characterId);
					state.getRequestInfoById(character.getCurrentRequestId()).Status = RequestInfo.RequestStatus.Accepted;
					character.AcceptRequest();
					}
            };

            actions.Add(refuseRequest);
            actions.Add(acceptRequest);

            return actions;
        }

        if (character.GetHasSentRequest())
        {
            var processRequest = new CharacterAction(character)
            {
                Description = $"Processing Request: {getRequestInfoById(character.getCurrentRequestId()).GetDescription()}",
                Callback = (state) => 
					{
						var character = state.getCharacterById(characterId);
						var CurrentRequest = state.getRequestInfoById(character.getCurrentRequestId());
						if (CurrentRequest.Status == RequestInfo.RequestStatus.Accepted)
						{
							CurrentRequest.CallbackAccepted?.Invoke(state);
						}
						else if (CurrentRequest.Status == RequestInfo.RequestStatus.Refused)
						{
							CurrentRequest.CallbackRefused?.Invoke(state);
						}

						character.ProcessSentRequest();
						state.Requests.Remove(character.getCurrentRequestId());
						
					}
            };

            actions.Add(processRequest);

            return actions;
        }

        actions.AddRange(GetUseItemActions(character));
        actions.AddRange(GetMoveActions(character));
        actions.AddRange(GetBuyItemsShopActions(character));
        actions.AddRange(GetMugRequests(character));
        actions.AddRange(GetLootActions(character));

		var currentId = 0;
		actions.ForEach((x) => {x.Id = currentId; currentId += 1;});


        return actions;
    }

    public List<CharacterAction> GetLootActions(CharacterInfo character)
    {
        var actions = new List<CharacterAction>();

		var characterId = character.Id;

        foreach (var other in GetCharactersInPlace(character.getCurrentPlace()))
        {
			if (!other.isDead()){
				continue;
			}
			if (other.getGold() == 0 && other.GetAllItems().Count == 0)
			{
				continue;
			}
			var otherId = other.Id; 
            var action = new CharacterAction(character)
            {
                Description = "Loot",
                Callback = (state) => {

					var character = state.getCharacterById(characterId);
					var other = state.getCharacterById(otherId);
					state.Loot(character, other);
					}
            };

            actions.Add(action);
        }

        return actions;
    }

    public List<CharacterAction> GetUseItemActions(CharacterInfo character)
    {
        var actions = new List<CharacterAction>();

		var all_items = character.GetAllItems();
		var characterId = character.Id;

        for(int i = 0; i < all_items.Count; i++)
        {
			var item = all_items[i];
			
            if (item.ItemData.Type == Definitions.ItemType.Food)
            {
				var itemIndex = i;
                var action = new CharacterAction(character) 
				{
                    Description = "Eat",
					Type = ActionType.Eat,
                    Callback = (state) =>
                    {
						var character = state.getCharacterById(characterId);
						var item = character.GetItemAt(itemIndex); 
						
						item.TurnsUsed += 1;

                        if (item.TurnsUsed > item.ItemData.MaxUses)
                        {
							character.RemoveItem(item);
						}
                        
                        character.ReduceHunger();
                    }
                };

                actions.Add(action);
            }

            if (item.ItemData.Type == Definitions.ItemType.MedKit)
            {
				var itemIndex = i;
                var action = new CharacterAction(character)
                {
                    Description = "Heal",
                    Callback = (state) =>
                    {
						var character = state.getCharacterById(characterId);
						var item = character.GetItemAt(itemIndex);
						
						item.TurnsUsed += 1;

                        if (item.TurnsUsed > item.ItemData.MaxUses)
                        {
							character.RemoveItem(item);
						}

                        character.RemoveItem(item);
                        character.IncreaseHp();
                    }
                };

                actions.Add(action);
            }

            if (item.ItemData.Type == Definitions.ItemType.Pickaxe &&
                character.getCurrentPlace() == Definitions.PlaceType.Mine)
            {
				var itemIndex = i;
                var action = new CharacterAction(character)
                {
                    Description = "Mine",
					IsProbabilityAction = true,
                    Callback = (state) =>
                    {
						var character = state.getCharacterById(characterId);
						var item = character.GetItemAt(itemIndex);
                        character.IncreaseHunger();

                        item.TurnsUsed += 1;

                        if (item.TurnsUsed > item.ItemData.MaxUses)
						{
                            character.RemoveItem(item);
						}

                    },
					ProbabilityEffects = new List<ProbabilityActionEffect>
					{
						new ProbabilityActionEffect()
						{
							Probability = 0.1f,
							Callback = (state) =>
							{
								var character = state.getCharacterById(characterId);
                        		character.IncreaseGold(AmountMined);
							}
						},
						new ProbabilityActionEffect()
						{
							Probability = 0.9f,
							Callback = (state) => {}
						}
					}
                };

                actions.Add(action);
            }

            if (item.ItemData.Type == Definitions.ItemType.Gun)
            {
                actions.AddRange(GetGunActions(character));
            }
        }

        return actions;
    }

    public List<CharacterAction> GetGunActions(CharacterInfo character)
    {
        var actions = new List<CharacterAction>();

		var characterId = character.Id;

        if (character.IsAimingGun)
        {
            if (character.HasItem(Definitions.ItemType.Ammo) && getCharacterById(character.CharacterAimedId).getCurrentPlace() == character.getCurrentPlace())
            {
				var other = getCharacterById(character.CharacterAimedId);
                var action = new CharacterAction(character)
                {
                    Recipient = other,
                    Type = ActionType.Shoot,
                    Description = $"Shoot {other.getNameAndType()}",
                    Callback = (state) =>
                    {
						var character = state.getCharacterById(characterId);
						var other = state.getCharacterById(character.CharacterAimedId);
                        other.ReduceHp();

                        var item = character.GetItemOfType(Definitions.ItemType.Ammo);
						item.TurnsUsed += 1;
						if (item.TurnsUsed > item.ItemData.MaxUses)
						{
							character.RemoveItem(item);
						}
                    }
                };

                actions.Add(action);
            }

            var holsterAction = new CharacterAction(character)
            {
                Description = "Holster Gun",
                Callback = (state) => state.getCharacterById(characterId).HolsterGun()
            };

            actions.Add(holsterAction);
        }

        foreach (var other in GetCharactersInPlace(character.getCurrentPlace()))
        {
            if (other == character)
                continue;

            var action = new CharacterAction(character)
            {
                Description = $"Aim Gun at {other.getNameAndType()}",

                // The original GDScript says character here,
                // but this should presumably be 'other'.
                Callback = (state) => state.getCharacterById(characterId).AimGunAt(state.getCharacterById(other.Id))
            };

            actions.Add(action);
        }

        return actions;
    }

    public List<CharacterAction> GetMoveActions(CharacterInfo character)
    {
        var actions = new List<CharacterAction>();
		var characterId = character.Id;

        foreach (var place in GetPlaceNeighbours(character.getCurrentPlace()))
        {
			var placeType = place;
            var action = new CharacterAction(character)
            {
                Description = $"Move to {place}",
				Place = place,
				Type = ActionType.Move,
                Callback = (state) => {
					var character = state.getCharacterById(characterId);
					var oldPlace = character.getCurrentPlace();
					character.moveToPlace(placeType);
					state.OnCharacterMoved?.Invoke(character, oldPlace, placeType);
					}
            };

            actions.Add(action);
        }

        return actions;
    }

    public List<CharacterAction> GetBuyItemsShopActions(CharacterInfo character)
    {
        var actions = new List<CharacterAction>();

        if (character.getCurrentPlace() != Definitions.PlaceType.Shop &&
            character.getCurrentPlace() != Definitions.PlaceType.Saloon)
        {
            return actions;
        }

        CharacterInfo owner = null;

        foreach (var other in GetCharactersInPlace(character.getCurrentPlace()))
        {
            if (other == character)
                continue;

            if (other.Type == Definitions.CharacterType.ShopOwner ||
                other.Type == Definitions.CharacterType.SaloonOwner)
            {
                owner = other;
                break;
            }
        }

        if (owner == null)
            return actions;

		var characterId = character.Id;
		var ownerId = owner.Id;

        foreach (var data in owner.GetAllItemData())
        {
            if (character.getGold() <= data.Price)
                continue;

			var ItemType = data.Type;
            var action = new CharacterAction(character)
            {
                Description = $"Buy {data.Type.ToString()}",
                Callback = (state) =>
                {
					var character = state.getCharacterById(characterId);
					var owner = state.getCharacterById(ownerId);
					var item = owner.GetItemOfType(ItemType);

                    character.AddItem(item.ItemData);
                    owner.RemoveItem(item);
                    owner.IncreaseGold(item.ItemData.Price);
                    character.ReduceGold(item.ItemData.Price);
                }
            };

            actions.Add(action);
        }

        return actions;
    }

    public List<CharacterAction> GetMugRequests(CharacterInfo character)
    {
        var actions = new List<CharacterAction>();

        if (character.Type != Definitions.CharacterType.Bandit)
            return actions;

        if (!character.IsAimingGun)
            return actions;

        var other = getCharacterById(character.CharacterAimedId);

		if (character.GetHasSentRequest() || character.GetHasReceivedRequest() || other.GetHasReceivedRequest() || other.GetHasSentRequest())
		{
			return actions;
		}

		var characterId = character.Id;
		var otherId = other.Id;

        var request = new RequestInfo
        {
            DescriptionIfAccepted = "You give me all your gold.",
            DescriptionIfRefused = "I shoot you."
        };

        request.CallbackAccepted = (state) =>
        {
			var character = state.getCharacterById(characterId);
			var other = state.getCharacterById(otherId); 
            character.IncreaseGold(other.getGold());
            other.ReduceGold(other.getGold());
        };

        request.CallbackRefused = (state) =>
        {

			var character = state.getCharacterById(characterId);
            var gunActions = state.GetGunActions(character);

            foreach (var gunAction in gunActions)
            {
                if (gunAction.Type == ActionType.Shoot &&
                    gunAction.Recipient.Id == otherId)
                {
                    gunAction.Callback?.Invoke(state);
                    return;
                }
            }
        };

        var action = new CharacterAction(character)
        {
            Description = $"Request: {request.GetDescription()}, to {other.getNameAndType()}",
            Callback = (state) => 
				{
					var character = state.getCharacterById(characterId);
					var other = state.getCharacterById(otherId);
					state.Requests[state.CurrentRequestId] = request;
					character.SendRequest(other, state.CurrentRequestId);
					state.CurrentRequestId += 1;
				}
        };

        actions.Add(action);

        return actions;
    }

	public void executeAction(CharacterAction action, ProbabilityActionEffect forceEffect=null)
	{
		action.Callback?.Invoke(this);
		if (action.IsProbabilityAction)
		{
			if (forceEffect == null)
			{
				var effect = MathUtils.Funtions.SampleWeighted(action.ProbabilityEffects, (effect) => effect.Probability);
				effect.Callback?.Invoke(this);
			}
			else
			{
				forceEffect.Callback?.Invoke(this);
			}
		}
		OnActionExecuted?.Invoke(action);
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