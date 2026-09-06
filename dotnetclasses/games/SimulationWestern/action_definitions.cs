using System.Diagnostics;
using System.Linq;

namespace WesternSimGame;

public static class ActionsDefinitions
{
	
	public static void fillActionList(GameState gameState)
	{
		var doNothingAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.DoNothing,
			Callback = (state, execution) => {}
		};
		gameState.addAction(doNothingAction);

		var useItemAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.UseItemOnSelf,
			IsParametricAction = true,
			GetItemTypePossibleValues = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				return character.getAllTypes().Where((x) => x == Definitions.ItemType.Food); 
			},
			Callback = (state, execution) =>
			{
				if (execution.itemType == Definitions.ItemType.Food)
				{
					var character = state.getCharacterById(execution.InitiatorId);
					var index = character.GetIndexForType(execution.itemType); 
					var item = character.GetItemAt(index);

					character.IncreseItemTurnUsed(index);

					if (character.GetItemTurnsUsed(index) > item.ItemData.MaxUses)
					{
						character.RemoveItem(index);
					}
					
					character.ReduceHunger();
				}
			}
		};
		gameState.addAction(useItemAction);

		var moveAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.Move,
			IsParametricAction = true,
			GetPlaceTypePossibleValues = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				var places = state.GetPlaceNeighbours(character.getCurrentPlace());
				return places.AsEnumerable();
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				character.moveToPlace(execution.placeType);
			}
		};
		gameState.addAction(moveAction);

		var mineAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.Mine,
			IsProbabilityAction = true,
			Precondition = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				return character.HasItem(Definitions.ItemType.Pickaxe) && character.getCurrentPlace() == Definitions.PlaceType.Mine;
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				var index = character.GetIndexForType(Definitions.ItemType.Pickaxe);
				character.IncreaseHunger();
				var item = character.GetItemAt(index);

				character.IncreseItemTurnUsed(index);

				if (character.GetItemTurnsUsed(index) > item.ItemData.MaxUses)
				{
					character.RemoveItem(index);
				}
				
			}

		};
		var mineProbailityEffects = new GameState.ProbabilityActionEffect[]
		{
			new GameState.ProbabilityActionEffect()
			{
				Probability = 0.1f,
				Callback = (state, execution) =>
				{
					var character = state.getCharacterById(execution.InitiatorId);
					character.IncreaseGold(gameState.AmountMined);
				}
			},
			new GameState.ProbabilityActionEffect()
			{
				Probability = 0.9f,
				Callback = (state, execution) => {}
			}

		};
		mineAction.ProbabilityEffects = mineProbailityEffects;
		gameState.addAction(mineAction);

		var aimAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.AimAt,
			IsParametricAction = true,
			Precondition = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				return character.HasItem(Definitions.ItemType.Gun);
			},
			GetOtherCharacterIdPossibleValues = (state, execution) =>
			{	
				var character = state.getCharacterById(execution.InitiatorId);
				return state.GetCharactersInPlace(character.getCurrentPlace()).Where((x) => x.Id != character.Id).Select((x) => x.Id);
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				character.IsAimingGun = true;
				character.CharacterAimedId = execution.otherCharacterId;
			}	
		};
		gameState.addAction(aimAction);

		var shootAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.Shoot,
			IsParametricAction = true,
			Precondition = (state, execution) =>
			{	
				var character = state.getCharacterById(execution.InitiatorId);
				return character.IsAimingGun && character.HasItem(Definitions.ItemType.Gun) && character.HasItem(Definitions.ItemType.Ammo);
			},
			GetOtherCharacterIdPossibleValues = (state, execution) =>
			{	
				var character = state.getCharacterById(execution.InitiatorId);
				return state.GetCharactersInPlace(character.getCurrentPlace()).Where((x) => x.Id == character.CharacterAimedId).Select((x) => x.Id);
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				var other = state.getCharacterById(character.CharacterAimedId);
				other.ReduceHp();

				var index = character.GetIndexForType(Definitions.ItemType.Ammo);
				character.IncreseItemTurnUsedAndRemoveOnEmpty(index);
			}	
		};
		gameState.addAction(shootAction);

		var holsterAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.Holster,
			Precondition = (state, execution) =>
			{	
				var character = state.getCharacterById(execution.InitiatorId);
				return character.IsAimingGun;
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				character.IsAimingGun = false;
			}	
		};
		gameState.addAction(holsterAction);

		var lootAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.Loot,
			IsParametricAction = true,
			GetOtherCharacterIdPossibleValues = (state, execution) =>
			{	
				var character = state.getCharacterById(execution.InitiatorId);
				return state.GetCharactersInPlace(character.getCurrentPlace()).Where((x) => x.isDead()).Select((x) => x.Id);
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				var other = state.getCharacterById(execution.otherCharacterId);
				state.Loot(character, other);
			}	
		};
		gameState.addAction(holsterAction);

		var buyItemAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.BuyItem,
			IsParametricAction = true,
			Precondition = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				return character.getCurrentPlace() == Definitions.PlaceType.Saloon || character.getCurrentPlace() == Definitions.PlaceType.Shop;
			},
			GetOtherCharacterIdPossibleValues = (state, execution) =>
			{	
				var character = state.getCharacterById(execution.InitiatorId);
				return state.GetCharactersInPlace(character.getCurrentPlace()).Where((x) => {
						return !x.isDead() && x.Id != character.Id && (x.Type == Definitions.CharacterType.SaloonOwner || x.Type == Definitions.CharacterType.ShopOwner);
					}
				).Select((x) => x.Id);
			},
			GetItemTypePossibleValues = (state, execution) =>
			{
				var otherCharacter = state.getCharacterById(execution.otherCharacterId);
				return otherCharacter.getAllTypes();
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				var other = state.getCharacterById(execution.otherCharacterId);
				var index = other.GetIndexForType(execution.itemType);
				var item = other.GetItemAt(index);

				character.AddItem(item.ItemData);
				other.RemoveItem(index);
				other.IncreaseGold(item.ItemData.Price);
				character.ReduceGold(item.ItemData.Price);
			}	
		};
		gameState.addAction(buyItemAction);

		var mugRequestAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.MugRequest,
			IsParametricAction = true,
			Precondition = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				if (character.Type != Definitions.CharacterType.Bandit)
				{
					return false;
				}
				if (state.getFirstFreeRequestInfoSlotIndex() < 0)
				{
					return false;
				}
				if (character.GetHasSentRequest() || character.GetHasReceivedRequest())
				{
					return false;
				}
				if (!character.IsAimingGun)
				{
					return false;
				}
				var other = state.getCharacterById(character.CharacterAimedId);
				if (other.GetHasSentRequest() || other.GetHasReceivedRequest())
				{
					return false;
				}
				if (other.getCurrentPlace() != character.getCurrentPlace())
				{
					return false;
				}
				return true;	
			},
			Callback = (state, execution) =>
			{
				var request = new RequestInfo
				{
					DescriptionIfAccepted = "You give me all your gold.",
					DescriptionIfRefused = "I shoot you."
				};

				request.CallbackAccepted = (state, execution) =>
				{
					var character = state.getCharacterById(execution.InitiatorId);
					var other = state.getCharacterById(character.CharacterAimedId); 
					character.IncreaseGold(other.getGold());
					other.ReduceGold(other.getGold());
				};

				request.CallbackRefused = (state, execution) =>
				{
					var character = state.getCharacterById(execution.InitiatorId);
					var index = character.GetIndexForType(Definitions.ItemType.Ammo);
					if (character.isDead())
					{
						return;
					}
					if(index < 0)
					{
						return;
					}

					state.getCharacterById(character.CharacterAimedId).ReduceHp();

					character.IncreseItemTurnUsedAndRemoveOnEmpty(index);
				};

				var character = state.getCharacterById(execution.InitiatorId);
				var other = state.getCharacterById(character.CharacterAimedId);
				var idx = state.getFirstFreeRequestInfoSlotIndex();
				Debug.Assert(idx >= 0);
				state.setRequestAtIndex(idx, request);
				character.SendRequest(other, idx);
			}
		};
		gameState.addAction(mugRequestAction);

		var acceptRequestAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.AcceptRequest,
			Precondition = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				return character.GetHasReceivedRequest();
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				var reqId = character.getCurrentRequestId();
				state.SetRequestStatus(reqId, RequestInfo.RequestStatus.Accepted);
				character.AcceptRequest();
			}
		};
		gameState.addAction(acceptRequestAction);

		var refuseRequestAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.RefuseRequest,
			Precondition = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				return character.GetHasReceivedRequest();
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				var reqId = character.getCurrentRequestId();
				state.SetRequestStatus(reqId, RequestInfo.RequestStatus.Refused);
				character.RefuseRequest();
			}
		};
		gameState.addAction(refuseRequestAction);
		
		var processRequestAction = new GameState.CharacterAction
		{
			Type = GameState.ActionType.ProcessRequest,
			Precondition = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				return character.GetHasSentRequest();
			},
			Callback = (state, execution) =>
			{
				var character = state.getCharacterById(execution.InitiatorId);
				var reqId = character.getCurrentRequestId();
				state.SetRequestStatus(reqId, RequestInfo.RequestStatus.Refused);
				character.RefuseRequest();
				var CurrentRequest = state.getRequestInfoById(character.getCurrentRequestId());
				if (CurrentRequest.Status == RequestInfo.RequestStatus.Accepted)
				{
					CurrentRequest.CallbackAccepted?.Invoke(state, execution);
				}
				else if (CurrentRequest.Status == RequestInfo.RequestStatus.Refused)
				{
					CurrentRequest.CallbackRefused?.Invoke(state, execution);
				}

				state.SetRequestStatus(reqId, RequestInfo.RequestStatus.Unused);
				character.ProcessSentRequest();
			}
		};
		gameState.addAction(processRequestAction);
	}

	
}