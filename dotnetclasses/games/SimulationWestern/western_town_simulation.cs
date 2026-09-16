using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

namespace WesternSimGame;

class WesternTownSimulation
{
	
	public GameState gameState;

	public ItemData FoodData { get; }
    public ItemData PickaxeData { get; }
    public ItemData GunData { get; }
	public ItemData AmmoData {get;}

	public Dictionary<ItemData, int> TurnsToRefillItem { get; } = new();

	public AI.MultiAgentMCTS<GameState.CharacterAction, CharacterInfo> mcts;

	private int maxExplorationDepth = 7;

	private int maxIterations = 200000;


	private bool useMcts = false;
	private int maxRolloutDepth = 10;
	private int maxMCTSEspansionDepth = 30;
	private int maxMCTSIterations = 20000;
	private float mctsCFactor = 1.4f;

	private Dictionary<short, CharacterValueFunction> valueFunctions;


	public WesternTownSimulation()
	{
		mcts = new AI.MultiAgentMCTS<GameState.CharacterAction, CharacterInfo>();

		gameState = new GameState();
		ActionsDefinitions.fillActionList(gameState);

		gameState.OnTurnCompleted += refillCharacters;

		FoodData = new ItemData
        {
            Type = Definitions.ItemType.Food,
			MaxUses = 5,
            Price = 2,
        };

        PickaxeData = new ItemData
        {
            Type = Definitions.ItemType.Pickaxe,
            MaxUses = 10,
            Price = 5
        };

        GunData = new ItemData
        {
            Type = Definitions.ItemType.Gun,
            Price = 5
        };

		AmmoData = new ItemData
		{
			Type = Definitions.ItemType.Ammo,
			MaxUses = 6,
			Price = 1,
		};

		TurnsToRefillItem[PickaxeData] = 10;
        TurnsToRefillItem[GunData] = 20;
        TurnsToRefillItem[FoodData] = 5;
		TurnsToRefillItem[AmmoData] = 15;


        gameState.AddConnection(Definitions.PlaceType.Saloon, Definitions.PlaceType.Thuroughfare);
        gameState.AddConnection(Definitions.PlaceType.Shop, Definitions.PlaceType.Thuroughfare);
        gameState.AddConnection(Definitions.PlaceType.SheriffStation, Definitions.PlaceType.Thuroughfare);
        gameState.AddConnection(Definitions.PlaceType.Thuroughfare, Definitions.PlaceType.Road);
        gameState.AddConnection(Definitions.PlaceType.Road, Definitions.PlaceType.Mine);

        var nameList = new List<string>(Names.NameList);

		var numOfMiners = 1;
        for (int i = 0; i < numOfMiners; i++)
        {
            gameState.AddCharacter(
                PopName(nameList),
                Definitions.CharacterType.Miner)
                .SetGold(7)
				.SetHunger(9)
                .SetPlace(Definitions.PlaceType.Thuroughfare);
        }

        // gameState.AddCharacter(
        //     PopName(nameList),
        //     Definitions.CharacterType.ShopOwner)
        //     .SetGold(10)
        //     .SetPlace(Definitions.PlaceType.Shop)
        //     .AddItemAmount(GunData, 1)
		// 	.AddItemAmount(AmmoData, 10)
        //     .AddItemAmount(PickaxeData, 3);

        gameState.AddCharacter(
            PopName(nameList),
            Definitions.CharacterType.SaloonOwner)
            .SetGold(10)
            .SetPlace(Definitions.PlaceType.Saloon)
			.AddItem(PickaxeData, 3)
            .AddItem(FoodData, 10);

        // gameState.AddCharacter(
        //     PopName(nameList),
        //     Definitions.CharacterType.Sheriff)
        //     .SetGold(10)
        //     .SetPlace(Definitions.PlaceType.SheriffStation)
        //     .AddItem(GunData)
		// 	.AddItem(AmmoData, 1);

		// var numOfBamdits = 1;
        // for (int i = 0; i < numOfBamdits; i++)
        // {
        //     gameState.AddCharacter(
        //         PopName(nameList),
        //         Definitions.CharacterType.Bandit)
        //         .SetGold(10)
		// 		.SetHunger(7)
        //         .SetPlace(Definitions.PlaceType.Road)
        //         .AddItem(GunData)
		// 		.AddItem(AmmoData, 1);
        // }

		valueFunctions = new Dictionary<short, CharacterValueFunction>();
		foreach (var character in gameState.Characters)
		{
			valueFunctions[character.Id] = new CharacterValueFunction(character.Id);
		}
	}

	private static string PopName(List<string> names)
    {
        int index = names.Count - 1;
        string name = names[index];
        names.RemoveAt(index);
        return name;
    }

	private GameState.CharacterActionExecution getNullAction(CharacterInfo characterInfo)
	{
		var action = new GameState.CharacterActionExecution
		{
			ActionType = GameState.ActionType.DoNothing,
			InitiatorId = characterInfo.Id

		};
		
		return action;
		
	}

	public class BestActionResults
	{
		public GameState.CharacterActionExecution action;
		public List<GameState.CharacterActionExecution> plan;
		public float bestScore;
	}

	public BestActionResults getBestAction(Action<int> OnIterationCompleted = null)
	{
		CharacterInfo characterToExecute = gameState.getCharacterToProcess();
		var res =  new BestActionResults();
		res.plan = new List<GameState.CharacterActionExecution>();


		Func<AI.IMultiAgentGameState<GameState.CharacterActionExecution, short>, short, GameState.CharacterActionExecution> otherAgentPolicy = (gameState, id) =>
			{	
				var state = (WGameState)gameState;
				// var nullAction = getNullAction(state.gameState.getCharacterById(id)); 
				// var action = nullAction;
				// var useItemActions = state.gameState.getAvailableActions(id).Where(x => x.ActionType == GameState.ActionType.UseItemOnSelf).ToList();
				// if (useItemActions.Count > 0)
				// {
				// 	var eatActions = gameState.ExpandParametricAction(useItemActions.First()).ToList();
				// 	if (eatActions.Count > 0)
				// 	{
				// 		action = eatActions.First();
				// 	}
				// }
				var availableActions = state.gameState.getAvailableActions(id);
				var	action = availableActions.OrderByDescending((x) => valueFunctions[id].getValue(state.gameState, x)).First();
				if (state.IsParametricAction(action))
				{
					return state.ExpandParametricAction(action).First();
				}
				return action;

			};

		// if (characterToExecute.Type == Definitions.CharacterType.SaloonOwner)
		// {
		// 	res.action = otherAgentPolicy(new WGameState(gameState), characterToExecute.Id);
		// 	return res;
		// }
		
		if (!useMcts)
		{
			var planner = new AI.SingleAgentPlanner<GameState.CharacterActionExecution, short>();
			var planRes = planner.GetIterativeDeepeningPlan(
				characterToExecute.Id, new WGameState(gameState), new WEvaluator(),
				otherAgentPolicy,
				(state, characterId, execution, depth, value) =>
				{
					valueFunctions[characterId].setValue(((WGameState)state).gameState, execution, depth, value);
				},
				(state, characterId, execution) =>
				{
					return valueFunctions[characterId].getValue(((WGameState)state).gameState, execution);
				},
				maxExplorationDepth,
				maxIterations
				);
			res.plan = planRes.plan;
			res.action = planRes.plan[0];
			res.bestScore = planRes.bestScore;
		}
		else
		{
			var mcts = new AI.MultiAgentMCTS<GameState.CharacterActionExecution, short>();
			var planRes = mcts.GetPlan(characterToExecute.Id, new WGameState(gameState), new WEvaluator(),
				otherAgentPolicy, 
				maxMCTSIterations, 
				maxRolloutDepth,
				maxMCTSEspansionDepth,
				OnIterationCompleted,
				mctsCFactor 
				);
			res.plan = planRes.plan;
			res.action = planRes.plan[0];
		}
		
		if (res.plan.Count == 0)
		{
			res.action = getNullAction(characterToExecute); 
			return res;
		}
		//valueFunctions[characterToExecute.Id].valueDict.Values.ToList().ForEach((x) => GD.Print($"{x.depth}, {x.value}"));
		GD.Print($"Current Value: {new WEvaluator().EvaluateState(new WGameState(gameState), characterToExecute.Id)}");
		GD.Print("Value Function:");
		foreach (var action in gameState.getAvailableActions(characterToExecute.Id))
		{
			if (gameState.getActionBytype(action.ActionType).IsParametricAction)
			{
				foreach (var paramAction in gameState.expandActionParameter(action))
				{
					var paramInfo = valueFunctions[characterToExecute.Id].getInfo(gameState, paramAction);
					if (paramInfo != null)
					{
						GD.Print($"\t{gameState.GetActionDescription(paramAction)} : (value){paramInfo.Value.value}, (depth){paramInfo.Value.depth}, (action){gameState.GetActionDescription(paramInfo.Value.action)}");
					}		
				}
				continue;
			}
			var info = valueFunctions[characterToExecute.Id].getInfo(gameState, action);
			if (info != null)
			{
				GD.Print($"\t{gameState.GetActionDescription(action)} : (value){info.Value.value}, (depth){info.Value.depth}, (action){gameState.GetActionDescription(info.Value.action)}");
			}
		}


		return res;
	}

	
    private const int TurnsToPaySheriff = 5;
    private const int SheriffSalary = 10;

	private void refillCharacters(GameState gameState)
	{
		foreach (var character in gameState.Characters)
		{
			
		    if (character.Type == Definitions.CharacterType.Sheriff)
            {
                if (gameState.CurrentTurn % TurnsToPaySheriff == 0)
                    character.IncreaseGold(SheriffSalary);
            }

            if (character.Type == Definitions.CharacterType.SaloonOwner)
            {
                if (gameState.CurrentTurn % TurnsToRefillItem[FoodData] == 0)
				{
					
                    character.AddItem(FoodData, 10);
				}
				if (gameState.CurrentTurn % TurnsToRefillItem[PickaxeData] == 0)
                {
					character.AddItem(PickaxeData);
				}
				if (gameState.CurrentTurn % TurnsToRefillItem[GunData] == 0)
                {
                    if (!character.HasItem(Definitions.ItemType.Gun))
					{
						character.AddItem(GunData);
					}
					if (!character.HasItem(Definitions.ItemType.Ammo))
					{
						character.AddItem(AmmoData);
					}
                }
            }

            if (character.Type == Definitions.CharacterType.ShopOwner)
            {
                if (gameState.CurrentTurn % TurnsToRefillItem[PickaxeData] == 0){
                    character.AddItem(PickaxeData);
				}

                if (gameState.CurrentTurn % TurnsToRefillItem[GunData] == 0)
                {
                    if (!character.HasItem(Definitions.ItemType.Gun))
                        character.AddItem(GunData);
                }

				if (gameState.CurrentTurn % TurnsToRefillItem[AmmoData] == 0)
                {
					character.AddItem(AmmoData, 1);
                }
            }
		}
	}

	public class CharacterValueFunction
	{
		public struct SearchInfo
		{
			public CharacterInfo characterState;
			public GameState.CharacterActionExecution action;
			public float value;
			public int depth;
		}
		
		private short characterId;
		public Dictionary<int, SearchInfo> valueDict;
		public CharacterValueFunction(short id)
		{
			this.characterId = id;
			valueDict = new Dictionary<int, SearchInfo>();
		}

		private int getActionStateHash(GameState state, GameState.CharacterActionExecution action)
		{
			
			var hash = new HashCode();

			var character = state.getCharacterById(this.characterId);
			var hp = character.getHp();
			hash.Add(hp <= 3);
			hash.Add(hp > 3 && hp < 6);
			hash.Add(hp >= 6);

			var hunger = character.getHunger();
			hash.Add(hunger <= 3);
			hash.Add(hunger > 3 && hunger < 6);
			hash.Add(hunger >= 6);

			var gold = character.getGold();
			hash.Add(gold <= 10);
			hash.Add(gold > 10 && gold < 30);
			hash.Add(gold >= 30);

			hash.Add(character.getCurrentPlace());
			hash.Add(character.HasItem(Definitions.ItemType.Gun));
			hash.Add(character.HasItem(Definitions.ItemType.Food));
			hash.Add(character.HasItem(Definitions.ItemType.Ammo));
			hash.Add(character.HasItem(Definitions.ItemType.Pickaxe));
			hash.Add(character.isDead());
			hash.Add(character.IsAimingGun);
			hash.Add(character.GetHasReceivedRequest());
			hash.Add(character.GetHasSentRequest());
			hash.Add(action.ActionType);
			hash.Add(action.placeType);
			hash.Add(action.itemType);
			hash.Add(action.otherCharacterId);
			return hash.ToHashCode();
		}
		public void setValue(GameState state, GameState.CharacterActionExecution action, int depth, float value)
		{
			var hash = getActionStateHash(state, action);
			var characterState = state.getCharacterById(characterId).getDuplicated();
			if (!valueDict.ContainsKey(hash))
			{
				valueDict[hash] = new SearchInfo{ depth = depth, value = value, characterState = characterState, action = action};
				return;
			}
			var info = valueDict[hash];
			if (depth > info.depth)
			{
				valueDict[hash] = new SearchInfo{ depth = depth, value = value, characterState = characterState, action = action};
			}
		}

		public float getValue(GameState state, GameState.CharacterActionExecution action)
		{
			var hash = getActionStateHash(state, action);
			if (!valueDict.ContainsKey(hash))
			{
				return float.NegativeInfinity;
			}
			return valueDict[hash].value;
		}

		public SearchInfo? getInfo(GameState state, GameState.CharacterActionExecution action)
		{
			var hash = getActionStateHash(state, action);
			if (!valueDict.ContainsKey(hash))
			{
				return null;
			}
			return valueDict[hash];
		}
	}

	public class WGameStateComparer: IEqualityComparer<AI.IMultiAgentGameState<GameState.CharacterActionExecution, short>>
	{
		public bool Equals(AI.IMultiAgentGameState<GameState.CharacterActionExecution, short> x, AI.IMultiAgentGameState<GameState.CharacterActionExecution, short> y)
		{
			if (ReferenceEquals(x, y))
				return true;

			if (x is null || y is null)
				return false;

			var gameStateX = (WGameState)x;
			var gameStateY = (WGameState)y;

			return gameStateX.gameState.Equals(gameStateY.gameState);
		}

		public int GetHashCode(AI.IMultiAgentGameState<GameState.CharacterActionExecution, short> obj)
		{
			var state = (WGameState)obj;
			return state.gameState.GetHash();
		}
	}

	public class WGameState: AI.IMultiAgentGameState<GameState.CharacterActionExecution, short>
	{
		public GameState gameState;
		public WGameState(GameState state)
		{
			this.gameState = state;
		}

		public short GetCurrentExecutingAgent()
		{
			return gameState.CurrentCharacterToProcess;
		}

		public AI.IMultiAgentGameState<GameState.CharacterActionExecution, short> GetDuplicated()
		{
			var new_state = new WGameState(this.gameState.getDuplicated());
			return new_state;
		}

		public IEnumerable<GameState.CharacterActionExecution> GetAvailableActions(short id)
		{
			return gameState.getAvailableActions(id);
		}

		public AI.IMultiAgentGameState<GameState.CharacterActionExecution, short> GetNewStatePerMove(GameState.CharacterActionExecution action)
		{
			var newState = this.GetDuplicated();
			newState.ExecuteAction(action);

			return newState;
		}

		public IEnumerable<AI.ProabilityEffect<GameState.CharacterActionExecution, short>> GetProbabilityEffectsPerAction(GameState.CharacterActionExecution action)
		{
			Debug.Assert(IsProbabilityAction(action));
			
			foreach (var effect in gameState.getActionBytype(action.ActionType).ProbabilityEffects)
			{
				var probabilityEffect = new AI.ProabilityEffect<GameState.CharacterActionExecution, short>();
				
				probabilityEffect.probability = effect.Probability;
				probabilityEffect.Callback = (state, action) => effect.Callback(((WGameState)state).gameState, action);
				yield return probabilityEffect;
			}
		}

		public void ExecuteAction(GameState.CharacterActionExecution action)
		{
			gameState.executeAction(action, false);
		}

		public bool IsOver()
		{
			return false;
		}

		public IEnumerable<GameState.CharacterActionExecution> ExpandParametricAction(GameState.CharacterActionExecution action)
		{
			return gameState.expandActionParameter(action);
		}

		public bool IsProbabilityAction(GameState.CharacterActionExecution action)
		{
			return gameState.getActionBytype(action.ActionType).IsProbabilityAction;
		}

		
		public bool IsParametricAction(GameState.CharacterActionExecution action)
		{
			return gameState.getActionBytype(action.ActionType).IsParametricAction;
		}

		public bool IsSameAction(GameState.CharacterActionExecution a, GameState.CharacterActionExecution b)
		{
			return a.ActionType == b.ActionType;
		}

		public bool IsSameAgent(short ida, short idb)
		{
			return ida == idb;
		}
			
	}

	public class WEvaluator: AI.IMultiAgentGameStateEvaluator<GameState.CharacterActionExecution, short>
	{
		public float EvaluateState(AI.IMultiAgentGameState<GameState.CharacterActionExecution, short> state, short id)
		{

			GameState gameState = ((WGameState)state).gameState;

			var character = gameState.getCharacterById(id);
		
			if (character.isDead())
				return -1000;//float.NegativeInfinity;

			if (character.Type == Definitions.CharacterType.Miner)
				return getStatsScore(character);

			if (character.Type == Definitions.CharacterType.Bandit)
				return getStatsScore(character);

			if (character.Type == Definitions.CharacterType.ShopOwner)
				return getStatsScore(character);

			if (character.Type == Definitions.CharacterType.SaloonOwner)
				return getStatsScore(character);

			if (character.Type == Definitions.CharacterType.Sheriff)
			{
				int score = getStatsScore(character);

				foreach (var other in gameState.Characters)
				{
					if (other != character && other.Type != Definitions.CharacterType.Bandit)
					{
						score += other.getHp();
					}
				}

				return score;
			}

			return 0;
		}

		private int getStatsScore(CharacterInfo character)
		{
			return character.getGold() + 10 * character.getHp() - character.getHunger();
		}

    	public float GetTerminationValue(AI.IMultiAgentGameState<GameState.CharacterActionExecution, short> gameState, short agent)
		{
			return 0.0f;
		}
	}

}