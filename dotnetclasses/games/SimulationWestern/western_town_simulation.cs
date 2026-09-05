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
				.AddItemAmount(FoodData, 1)
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
			.AddItemAmount(PickaxeData, 3)
            .AddItemAmount(FoodData, 10);

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
				.AddItemAmount(AmmoData, 10);
        }
	}

	private static string PopName(List<string> names)
    {
        int index = names.Count - 1;
        string name = names[index];
        names.RemoveAt(index);
        return name;
    }

	private GameState.CharacterAction getNullAction(CharacterInfo characterInfo)
	{
			var action = new GameState.CharacterAction(characterInfo);
			action.Description = "Empty Policy";
			action.Callback = (state) => {};
			return action;
		
	}

	public class BestActionResults
	{
		public GameState.CharacterAction action;
		public List<GameState.CharacterAction> plan;
		public float bestScore;
	}

	public BestActionResults getBestAction(Action<int> OnIterationCompleted = null)
	{
		CharacterInfo characterToExecute = gameState.getCharacterToProcess();
		var res =  new BestActionResults();
		res.plan = new List<GameState.CharacterAction>();
		if (characterToExecute.Type == Definitions.CharacterType.SaloonOwner)
		{
			var availableActions = gameState.GetActionPerCharacter(characterToExecute).Where((x) => x.Type == GameState.ActionType.Eat).ToList();

			if (availableActions.Count > 0)
			{
				res.action = availableActions[0];
				return res;
			}

			res.action = getNullAction(characterToExecute); 
			return res;
		}



		Func<AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo>, CharacterInfo, GameState.CharacterAction> otherAgentPolicy = (gameState, character) =>
			{	

				var availableActions = gameState.GetAvailableMoves(character).Where((x) => x.Type == GameState.ActionType.Eat).ToList();
				
				if (availableActions.Count > 0)
				{
					return availableActions[0];
				}
				return getNullAction(character);
			} ;
		
		Func<CharacterInfo, CharacterInfo, bool> isSameAgent = (cha, chb) => cha.Id == chb.Id; 
		Func<GameState.CharacterAction, GameState.CharacterAction, bool> isSameAction = (a, b) => a.Id == b.Id;

		if (!useMcts)
		{
			
			var planner = new AI.SingleAgentPlanner<GameState.CharacterAction, CharacterInfo>();
			var planRes = planner.GetIterativeDeepeningPlan(characterToExecute, new WGameState(gameState), new WEvaluator(),
				otherAgentPolicy,
				isSameAgent,
				isSameAction,
				(action) => action.IsProbabilityAction,
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
			var mcts = new AI.MultiAgentMCTS<GameState.CharacterAction, CharacterInfo>();
			var planRes = mcts.GetPlan(characterToExecute, new WGameState(gameState), new WEvaluator(), 
				otherAgentPolicy, 
				isSameAgent, 
				maxMCTSIterations, 
				maxRolloutDepth,
				maxMCTSEspansionDepth,
				OnIterationCompleted,
				mctsCFactor 
				);
			res.plan = planRes.plan;
			res.action = planRes.plan[0];
		}

		
		if (res.plan.Count == 0 || res.plan[0] == null)
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
                    character.AddItemAmount(FoodData, 10);
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
					character.AddItemAmount(AmmoData, 10);
                }
            }
		}
	}

	public class WGameStateComparer: IEqualityComparer<AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo>>
	{
		public bool Equals(AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo> x, AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo> y)
		{
			if (ReferenceEquals(x, y))
				return true;

			if (x is null || y is null)
				return false;

			var gameStateX = (WGameState)x;
			var gameStateY = (WGameState)y;

			return gameStateX.gameState.Equals(gameStateY.gameState);
		}

		public int GetHashCode(AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo> obj)
		{
			var state = (WGameState)obj;
			return state.gameState.GetHash();
		}
	}

	public class WGameState: AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo>
	{
		
		public GameState gameState;
		public WGameState(GameState state)
		{
			this.gameState = state;
		}

		public CharacterInfo GetCurrentExecutingAgent()
		{
			return gameState.getCharacterToProcess();
		}

		public AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo> GetDuplicated()
		{
			var new_state = new WGameState(this.gameState.getDuplicated());
			return new_state;
		}

		public IEnumerable<GameState.CharacterAction> GetAvailableMoves(CharacterInfo agent)
		{
			return gameState.GetActionPerCharacter(agent);
		}

		public AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo> GetNewStatePerMove(GameState.CharacterAction move)
		{
			var newState = this.GetDuplicated();
			newState.ExecuteMove(move);

			return newState;
		}

		public IEnumerable<AI.StateProbability<GameState.CharacterAction, CharacterInfo>> GetProbabilityStatesPerMove(GameState.CharacterAction move)
		{
			Debug.Assert(move.IsProbabilityAction);
			var listOfStates = new List<AI.StateProbability<GameState.CharacterAction, CharacterInfo>>();

			foreach (var effect in move.ProbabilityEffects)
			{
				var stateProbability = new AI.StateProbability<GameState.CharacterAction, CharacterInfo>();
				WGameState newState = (WGameState)this.GetDuplicated();
				newState.gameState.executeAction(move, effect);
				stateProbability.state = newState;
				stateProbability.probability = effect.Probability;
				listOfStates.Add(stateProbability);
			}

			return listOfStates;
		}

		public int GetHash()
		{
			return gameState.GetHash(); 
		}

		public void ExecuteMove(GameState.CharacterAction move)
		{
			gameState.executeAction(move);
		}

		public bool IsOver()
		{
			return false;
		}
			
	}

	public class WEvaluator: AI.IMultiAgentGameStateEvaluator<GameState.CharacterAction, CharacterInfo>
	{
		public float EvaluateState(AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo> state, CharacterInfo characterToEvaluate)
		{

			GameState gameState = ((WGameState)state).gameState;

			var character = gameState.getCharacterById(characterToEvaluate.Id);
		
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

    	public float GetTerminationValue(AI.IMultiAgentGameState<GameState.CharacterAction, CharacterInfo> gameState, CharacterInfo agent)
		{
			return 0.0f;
		}
	}

}