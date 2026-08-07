using System.Collections.Generic;
using DataStructures;
using Godot;
using System.Linq;

namespace TableGames;

public class Chess
{
	public enum GameOutcome {Running, Win, Draw}
	public enum Player {White, Black}
	public enum TileColor {White, Black}
	public enum PieceType {Pawn, Horse, Rock, Bishop, Queen, King}

	static public Player GetOpponent(Player player)
	{
		return player == Player.White 
			? Player.Black 
			: Player.White;
	}

	public struct GameOutcomeResult
	{
		public GameOutcome gameOutcone;
		public Player winner;
	}

	public struct TileInfo
	{
		public TileColor tileColor;
		public bool isOccupied;
		public Player player;
		public PieceType pieceType;
	}

	public class State
	{	

		public Grid2D<TileInfo> grid;
		public Dictionary<Player, PlayerState> playerStates = new Dictionary<Player, PlayerState>();
		public struct PlayerState
		{
			public bool HasKingBeenMoved;
			public bool HasLeftTowerBeenMoved;
			public bool HasRightTowerBeenMoved;
			public bool HasKingCastled;	
		}

		public State()
		{	
			playerStates[Player.Black] = new PlayerState();
			playerStates[Player.White] = new PlayerState();

			grid = new Grid2D<TileInfo>(8, 8);
			foreach (var data in grid)
			{
				TileInfo tileInfo = new TileInfo();

				if ((data.Point.X + data.Point.Y) % 2 == 0)
					tileInfo.tileColor = TileColor.White;
				else
					tileInfo.tileColor = TileColor.Black;

				grid[data.Point] = tileInfo;
			}


			foreach (var data in grid)
			{
				if (data.Point.Y == 1 || data.Point.Y == 6)
				{
					TileInfo tileInfo = data.Data;
					tileInfo.pieceType = PieceType.Pawn;
					tileInfo.isOccupied = true;
					tileInfo.player =
						data.Point.Y == 6 ? Player.White : Player.Black;
					
					grid[data.Point] = tileInfo;
				}
			}


			PieceType[] pieceOrder =
			[
				PieceType.Rock,PieceType.Horse,PieceType.Bishop,PieceType.Queen,PieceType.King,PieceType.Bishop,PieceType.Horse,PieceType.Rock
			];


			for (int i = 0; i < pieceOrder.Length; i++)
			{
				PieceType pieceType = pieceOrder[i];

				TileInfo blackTile = grid[new Vector2I(i, 0)];

				blackTile.pieceType = pieceType;
				blackTile.isOccupied = true;
				blackTile.player = Player.Black;

				grid[new Vector2I(i, 0)] = blackTile;


				TileInfo whiteTile = grid[new Vector2I(i, 7)];

				whiteTile.pieceType = pieceType;
				whiteTile.isOccupied = true;
				whiteTile.player = Player.White;

				grid[new Vector2I(i, 7)] = whiteTile;
			}
		}
		public State Clone()
		{
			State newState = new State();

			newState.grid = grid.Clone();

			newState.playerStates[Player.Black] = playerStates[Player.Black];
			newState.playerStates[Player.White] = playerStates[Player.White];

			return newState;
		}


		public State GetStatePerMove(Move move)
		{
			State newState = Clone();
			newState.ExecuteMove(move);
			return newState;
		}
		
		public int EvaluateStateForPlayer(Player player)
		{
			int playerScore = GetScorePerPlayer(player);
			int opponentScore = GetScorePerPlayer(GetOpponent(player));

			return playerScore - opponentScore;
		}

		public float EvaluateStateForPlayerNormalized(Player player)
		{
			int playerScore = GetScorePerPlayer(player);
			int opponentScore = GetScorePerPlayer(GetOpponent(player));
			if (playerScore < opponentScore)
				return 0;

			return 1;
		}

		public int GetScorePerPlayer(Player player)
		{
			var tilesPlayer = GetTilesPerPlayer(player);

			int pawns = tilesPlayer.Count(x => grid[x].pieceType == PieceType.Pawn);

			int rooks = tilesPlayer.Count(x => grid[x].pieceType == PieceType.Rock);

			int horses = tilesPlayer.Count(x => grid[x].pieceType == PieceType.Horse);

			int bishops = tilesPlayer.Count(x => grid[x].pieceType == PieceType.Bishop);

			int queens = tilesPlayer.Count(x => grid[x].pieceType == PieceType.Queen);


			return pawns + horses * 3 + bishops * 3 + rooks * 5 + queens * 9;
		}

		public List<Vector2I> GetTilesPerPlayer(Player player)
		{
			List<Vector2I> tiles = new();

			foreach (var data in grid)
			{
				if (data.Data.isOccupied &&
					data.Data.player == player)
				{
					tiles.Add(data.Point);
				}
			}

			return tiles;
		}

		private bool IsPromotionIdx(Vector2I idx)
		{
			return idx.Y == 0 || idx.Y == 7;
		}

		private void AddPromotionMovesFromMove(
    		Move lastMove,
    		List<Move> allMoves)
		{
			PieceType[] promotionPieces =
			{
				PieceType.Horse,
				PieceType.Queen,
				PieceType.Bishop,
				PieceType.Rock
			};


			foreach (PieceType pieceType in promotionPieces)
			{
				Move move = new Move
				{
					StartIdx = lastMove.StartIdx,
					EndIdx = lastMove.EndIdx,

					PieceCaptured = lastMove.PieceCaptured,
					PieceCapturedIdx = lastMove.PieceCapturedIdx,

					PiecePromoted = true,
					PieceTypePromoted = pieceType
				};

				allMoves.Add(move);
			}
		}

		private bool MovePutsKingInThreat(
			Move move,
			Player player,
			int direction)
		{
			State newState = GetStatePerMove(move);

			var opponentTiles =
				newState.GetTilesPerPlayer(
					GetOpponent(player));


			var controlledTiles =
				newState.GetTilesControlled(
					GetOpponent(player),
					opponentTiles,
					direction * -1);


			return controlledTiles.Any(
				x => newState.grid[x].pieceType == PieceType.King);
		}

		private void AddCastlingMove(
			Player player,
			List<Move> moves,
			List<Vector2I> playerTiles,
			List<Vector2I> opponentControlledTiles,
			bool right)
		{
			PlayerState state = playerStates[player];


			if (right && state.HasRightTowerBeenMoved)
				return;

			if (!right && state.HasLeftTowerBeenMoved)
				return;


			Vector2I kingIndex =
				playerTiles.First(
					x => grid[x].pieceType == PieceType.King);


			List<Vector2I> castlingTiles;

			if (right)
			{
				castlingTiles = new()
				{
					kingIndex + Vector2I.Right,
					kingIndex + Vector2I.Right * 2
				};
			}
			else
			{
				castlingTiles = new()
				{
					kingIndex + Vector2I.Left,
					kingIndex + Vector2I.Left * 2,
					kingIndex + Vector2I.Left * 3
				};
			}


			if (castlingTiles.Any(
				x => grid[x].isOccupied))
				return;


			if (castlingTiles.Any(
				x => opponentControlledTiles.Contains(x)))
				return;


			int towerX = right ? 7 : 0;


			var matchingTower =
				playerTiles.Where(
					x =>
						grid[x].pieceType == PieceType.Rock &&
						x.X == towerX)
				.ToList();


			if (matchingTower.Count == 0)
			{
				if (right)
					state.HasRightTowerBeenMoved = true;
				else
					state.HasLeftTowerBeenMoved = true;

				return;
			}


			Vector2I towerIndex = matchingTower[0];


			Move castleMove = new()
			{
				StartIdx = kingIndex,
				IsCastling = true,

				TowerPieceStart = towerIndex
			};


			castleMove.EndIdx =
				right
				? kingIndex + Vector2I.Right * 2
				: kingIndex + Vector2I.Left * 2;


			castleMove.TowerPieceEnd =
				right
				? towerIndex + Vector2I.Left * 2
				: towerIndex + Vector2I.Right * 3;


			moves.Add(castleMove);
		}

		public List<Move> GetAvailableMoves(Player player)
		{
			List<Move> allMoves = new();


			int direction =
				player == Player.White ? -1 : 1;


			List<Vector2I> opponentTiles =
				GetTilesPerPlayer(GetOpponent(player));


			List<Vector2I> playerTiles =
				GetTilesPerPlayer(player);


			List<Vector2I> opponentControlled =
				GetTilesControlled(
					GetOpponent(player),
					opponentTiles,
					direction * -1);


			bool kingThreatened =
				opponentControlled.Any(
					x => grid[x].pieceType == PieceType.King);



			foreach (Vector2I tile in playerTiles)
			{
				PieceType pieceType = grid[tile].pieceType;

				foreach (Vector2I destination in
						GetMoveTilesForPiece(tile, direction))
				{
					Move move = new()
					{
						StartIdx = tile,
						EndIdx = destination
					};


					if (MovePutsKingInThreat(
						move, player, direction))
						continue;


					if (IsPromotionIdx(destination) &&
						pieceType == PieceType.Pawn)
					{
						AddPromotionMovesFromMove(
							move,
							allMoves);
					}
					else
					{
						allMoves.Add(move);
					}
				}



				foreach (Vector2I destination in
						GetCaptureTilesForPiece(
							tile,
							direction,
							player))
				{
					Move move = new()
					{
						StartIdx = tile,
						EndIdx = destination,

						PieceCaptured = true,
						PieceCapturedIdx = destination
					};


					if (MovePutsKingInThreat(
						move, player, direction))
						continue;


					if (IsPromotionIdx(destination) &&
						pieceType == PieceType.Pawn)
					{
						AddPromotionMovesFromMove(
							move,
							allMoves);
					}
					else
					{
						allMoves.Add(move);
					}
				}
			}


			PlayerState state = playerStates[player];


			if (!state.HasKingCastled &&
				!state.HasKingBeenMoved)
			{
				AddCastlingMove(player,allMoves,playerTiles,opponentControlled,true);

				AddCastlingMove(player,allMoves,playerTiles,opponentControlled,false);
			}


			if (kingThreatened)
			{
				allMoves = allMoves
					.Where(move =>
					{
						State newState =
							GetStatePerMove(move);

						var newOpponentTiles =
							newState.GetTilesPerPlayer(GetOpponent(player));


						var controlled =
							newState.GetTilesControlled(GetOpponent(player),newOpponentTiles,direction * -1);


						return !controlled.Any(
							x => newState.grid[x].pieceType == PieceType.King);
					})
					.ToList();
			}


			return allMoves;
		}

		public List<Vector2I> GetTilesControlled(
			Player player,
			List<Vector2I> startingTiles,
			int direction)
		{
			List<Vector2I> controlledTiles = new();

			foreach (Vector2I tile in startingTiles)
			{
				controlledTiles.AddRange(
					GetCaptureTilesForPiece(tile, direction, player));
			}

			return controlledTiles;
		}

		public List<Vector2I> GetStraightDirections()
		{
			return new()
			{
				Vector2I.Up,
				Vector2I.Down,
				Vector2I.Right,
				Vector2I.Left
			};
		}


		public List<Vector2I> GetDiagonalDirections()
		{
			return new()
			{
				Vector2I.Up + Vector2I.Left,
				Vector2I.Up + Vector2I.Right,
				Vector2I.Down + Vector2I.Left,
				Vector2I.Down + Vector2I.Right
			};
		}

		private void AppendIfNotNull(
			List<Vector2I> list,
			Vector2I? value)
		{
			if (value.HasValue)
				list.Add(value.Value);
		}

		public List<Vector2I> GetCaptureTilesForPiece(
			Vector2I idx,
			int direction,
			Player player)
		{
			List<Vector2I> tiles = new();

			PieceType pieceType = grid[idx].pieceType;


			switch (pieceType)
			{
				case PieceType.Pawn:

					foreach (int lr in new[] { -1, 1 })
					{
						AppendIfNotNull(
							tiles,
							GetCaptureTileAlongDirection(new Vector2I(lr, direction),idx,player,1));
					}

					break;



				case PieceType.Rock:

					foreach (Vector2I dir in GetStraightDirections())
					{
						AppendIfNotNull(
							tiles,
							GetCaptureTileAlongDirection(dir,idx,player));
					}

					break;



				case PieceType.Bishop:

					foreach (Vector2I dir in GetDiagonalDirections())
					{
						AppendIfNotNull(
							tiles,
							GetCaptureTileAlongDirection(dir,idx,player));
					}

					break;



				case PieceType.Horse:

					foreach (int signX in new[] { -1, 1 })
					{
						foreach (int signY in new[] { -1, 1 })
						{
							foreach (Vector2I dir in new[]
							{
								new Vector2I(1,2),
								new Vector2I(2,1)
							})
							{
								Vector2I finalDir =
									new Vector2I(dir.X * signX,dir.Y * signY);


								AppendIfNotNull(
									tiles,
									GetCaptureTileAlongDirection(finalDir,idx,player,1));
							}
						}
					}

					break;



				case PieceType.Queen:

					foreach (Vector2I dir in GetDiagonalDirections())
					{
						AppendIfNotNull(
							tiles,
							GetCaptureTileAlongDirection(dir,idx,player));
					}


					foreach (Vector2I dir in GetStraightDirections())
					{
						AppendIfNotNull(
							tiles,
							GetCaptureTileAlongDirection(dir,idx,player));
					}

					break;



				case PieceType.King:

					foreach (Vector2I dir in GetDiagonalDirections())
					{
						AppendIfNotNull(
							tiles,
							GetCaptureTileAlongDirection(dir,idx,player,1));
					}


					foreach (Vector2I dir in GetStraightDirections())
					{
						AppendIfNotNull(
							tiles,
							GetCaptureTileAlongDirection(dir,idx,player,1));
					}

					break;
			}


			return tiles;
		}

		public List<Vector2I> GetMoveTilesForPiece(Vector2I idx, int direction)
		{
			List<Vector2I> tiles = new();

			PieceType pieceType = grid[idx].pieceType;


			switch(pieceType)
			{
				case PieceType.Pawn:

					tiles.AddRange(GetMoveTilesAlongDirection(new Vector2I(0, direction), idx, 1));


					if (idx.Y == 1 || idx.Y == 6)
					{
						tiles.AddRange(
							GetMoveTilesAlongDirection(new Vector2I(0, direction * 2),idx,1));
					}

					break;



				case PieceType.Rock:

					foreach(var dir in GetStraightDirections())
					{
						tiles.AddRange(
							GetMoveTilesAlongDirection(dir,idx));
					}

					break;



				case PieceType.Bishop:

					foreach(var dir in GetDiagonalDirections())
					{
						tiles.AddRange(
							GetMoveTilesAlongDirection(dir,idx));
					}

					break;



				case PieceType.Horse:

					foreach(int signX in new[] {-1,1})
					{
						foreach(int signY in new[] {-1,1})
						{
							foreach(var dir in new[]
							{
								new Vector2I(1,2),
								new Vector2I(2,1)
							})
							{
								Vector2I finalDir =
									new Vector2I(dir.X * signX,dir.Y * signY);


								tiles.AddRange(GetMoveTilesAlongDirection(finalDir,idx,1));
							}
						}
					}

					break;



				case PieceType.Queen:

					foreach(var dir in GetDiagonalDirections())
					{
						tiles.AddRange(GetMoveTilesAlongDirection(dir,idx));
					}


					foreach(var dir in GetStraightDirections())
					{
						tiles.AddRange(GetMoveTilesAlongDirection(dir,idx));
					}

					break;



				case PieceType.King:

					foreach(var dir in GetDiagonalDirections())
					{
						tiles.AddRange(GetMoveTilesAlongDirection(dir,idx,1));
					}


					foreach(var dir in GetStraightDirections())
					{
						tiles.AddRange(GetMoveTilesAlongDirection(dir,idx,1));
					}

					break;
			}


			return tiles;
		}

		private Vector2I? GetCaptureTileAlongDirection(
			Vector2I direction,
			Vector2I startIdx,
			Player player,
			int limit = -1)
		{
			int currentIndex = 1;

			while (true)
			{
				Vector2I targetIdx = startIdx + direction * currentIndex;

				if (!grid.IsInBounds(targetIdx))
					return null;

				TileInfo tileInfo =
					grid[targetIdx];

				if (tileInfo.isOccupied &&
					tileInfo.player == player)
				{
					return null;
				}

				if (tileInfo.isOccupied &&
					tileInfo.player != player)
				{
					return targetIdx;
				}

				currentIndex++;

				if (limit > 0 &&
					currentIndex >= limit)
				{
					return null;
				}
			}
		}

		private List<Vector2I> GetMoveTilesAlongDirection(
			Vector2I direction,
			Vector2I startIdx,
			int limit = -1)
		{
			List<Vector2I> tiles = new();

			int currentIndex = 1;

			while (true)
			{
				Vector2I targetIdx =
					startIdx + direction * currentIndex;


				if (!grid.IsInBounds(targetIdx))
					break;

				TileInfo tileInfo = grid[targetIdx];

				if (!tileInfo.isOccupied)
				{
					tiles.Add(targetIdx);
				}
				else
				{
					break;
				}

				currentIndex++;

				if (limit > 0 &&
					currentIndex >= limit)
				{
					break;
				}
			}


			return tiles;
		}

		public GameOutcomeResult GetGameStatus()
		{
			GameOutcomeResult result = new()
			{
				gameOutcone = GameOutcome.Running
			};

			bool blackCantMove =
				GetAvailableMoves(Player.Black).Count == 0;


			bool whiteCantMove =
				GetAvailableMoves(Player.White).Count == 0;

			if (blackCantMove && whiteCantMove)
			{
				result.gameOutcone = GameOutcome.Draw;
				return result;
			}


			if (blackCantMove)
			{
				result.gameOutcone = GameOutcome.Win;
				result.winner = Player.White;
				return result;
			}

			if (whiteCantMove)
			{
				result.gameOutcone = GameOutcome.Win;
				result.winner = Player.Black;
				return result;
			}

			return result;
		}

		public void ExecuteMove(Move move)
		{
			TileInfo tileInfo = grid[move.StartIdx];

			tileInfo.isOccupied = false;

			Player player = tileInfo.player;

			if (tileInfo.pieceType == PieceType.King)
			{
				playerStates[player] = playerStates[player] with {HasKingBeenMoved = true};
			}

			if (tileInfo.pieceType == PieceType.Rock &&
				move.StartIdx.X == 0)
			{
				playerStates[player] = playerStates[player] with {HasLeftTowerBeenMoved = true};
			}

			if (tileInfo.pieceType == PieceType.Rock &&
				move.StartIdx.X == 7)
			{
				playerStates[player] = playerStates[player] with {HasRightTowerBeenMoved = true};
			}

			grid[move.StartIdx] = tileInfo;

			TileInfo destination = grid[move.EndIdx];

			destination.isOccupied = true;
			destination.player = tileInfo.player;
			destination.pieceType = tileInfo.pieceType;

			if (move.PiecePromoted)
			{
				destination.pieceType = move.PieceTypePromoted;
			}

			grid[move.EndIdx] = destination;

			if (move.IsCastling)
			{
				TileInfo tower = grid[move.TowerPieceStart];

				tower.isOccupied = false;

				grid[move.TowerPieceStart] = tower;

				TileInfo towerDestination = grid[move.TowerPieceEnd];

				towerDestination.isOccupied = true;
				towerDestination.player = tower.player;
				towerDestination.pieceType = tower.pieceType;

				grid[move.TowerPieceEnd] = towerDestination;
			}
		}
	}

	public struct Move
	{
		public Vector2I StartIdx { get; set; }
		public Vector2I EndIdx { get; set; }

		public bool PiecePromoted { get; set; }
		public PieceType PieceTypePromoted { get; set; }

		public bool PieceCaptured { get; set; }
		public Vector2I PieceCapturedIdx { get; set; }

		public bool IsCastling { get; set; }
		public Vector2I TowerPieceStart { get; set; }
		public Vector2I TowerPieceEnd { get; set; }

	}


}