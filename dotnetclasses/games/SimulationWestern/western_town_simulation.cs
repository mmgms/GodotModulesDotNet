using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

	private int maxIterations = 100000;


	private bool useMcts = false;
	private int maxRolloutDepth = 10;
	private int maxMCTSEspansionDepth = 30;
	private int maxMCTSIterations = 20000;
	private float mctsCFactor = 1.4f;


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
            Price = 3
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
                .SetGold(10)
				.SetHunger(7)
				.AddItem(FoodData, 1)
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
		// 	.AddItemAmount(AmmoData, 10);

		var numOfBamdits = 1;
        for (int i = 0; i < numOfBamdits; i++)
        {
            gameState.AddCharacter(
                PopName(nameList),
                Definitions.CharacterType.Bandit)
                .SetGold(10)
				.SetHunger(7)
                .SetPlace(Definitions.PlaceType.Road)
                .AddItem(GunData)
				.AddItem(AmmoData, 10);
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


		Func<WGameState, short, GameState.CharacterActionExecution> otherAgentPolicy = (gameState, id) =>
			{	
				var action = getNullAction(characterToExecute); 
				var useItemActions = gameState.gameState.getAvailableActions(id).Where(x => x.ActionType == GameState.ActionType.UseItemOnSelf).ToList();
				if (useItemActions.Count > 0)
				{
					var eatActions = gameState.ExpandParametricAction(useItemActions.First()).ToList();
					if (eatActions.Count > 0)
					{
						action = eatActions.First();
					}
				}
				
				return action;

			};

		if (characterToExecute.Type == Definitions.CharacterType.SaloonOwner)
		{
			res.action = otherAgentPolicy(new WGameState(gameState), characterToExecute.Id);
			return res;
		}
		
		Func<short, short, bool> isSameAgent = (a, b) => a == b; 
		Func<GameState.CharacterAction, GameState.CharacterAction, bool> isSameAction = (a, b) => a.Type == b.Type;

		if (!useMcts)
		{
			var planner = new AI.SingleAgentPlanner<GameState.CharacterActionExecution, short>();
			var planRes = planner.GetIterativeDeepeningPlan(characterToExecute.Id, new WGameState(gameState), new WEvaluator(),
				(Func<AI.IMultiAgentGameState<GameState.CharacterActionExecution, short>, short, GameState.CharacterActionExecution>)otherAgentPolicy,
				new WGameStateComparer(),
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
				(Func<AI.IMultiAgentGameState<GameState.CharacterActionExecution, short>, short, GameState.CharacterActionExecution>)otherAgentPolicy, 
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
                    character.AddItem(FoodData, 10);
            }

            if (character.Type == Definitions.CharacterType.ShopOwner)
            {
                if (gameState.CurrentTurn % TurnsToRefillItem[PickaxeData] == 0)
                    character.AddItem(PickaxeData);

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
				probabilityEffect.Callback = (Action<AI.IMultiAgentGameState<GameState.CharacterActionExecution, short>, GameState.CharacterActionExecution>)effect.Callback;
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
				return character.getGold() + 10 * character.getHp() - character.getHunger();

			if (character.Type == Definitions.CharacterType.Bandit)
				return character.getGold();

			if (character.Type == Definitions.CharacterType.ShopOwner)
				return character.getGold();

			if (character.Type == Definitions.CharacterType.SaloonOwner)
				return character.getGold();

			if (character.Type == Definitions.CharacterType.Sheriff)
			{
				int score = 0;

				foreach (var other in gameState.Characters)
				{
					if (other != character)
						score += other.getHp();
				}

				return score;
			}

			return 0;
		}

    	public float GetTerminationValue(AI.IMultiAgentGameState<GameState.CharacterActionExecution, short> gameState, short agent)
		{
			return 0.0f;
		}
	}

}