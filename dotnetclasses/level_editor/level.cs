using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using DataStructures;
using Godot;

namespace LevelEditor2D;

public struct TileInfo
{
	public int roomId {get; set;}
	public TileInfo(){
		roomId = -1;
	}
}

public class Room
{
	public delegate void Deleted();
	public event Deleted OnDeleted;
	public Color debugColor {get; set;}
	public String name {get; set;}
	public void delete()
	{
		OnDeleted?.Invoke();
	}
}

public class Door
{
	public Vector2I fromTile {get; set;}
	public Vector2I toTile {get; set;}
}

public class RoomObject
{
	public Vector2 centerPos {get; set;}
	public Vector2I size {get; set;}
	public int rotation {get; set;}
}

public class Level
{
	public delegate void RoomAdded(int id, Room room);
	public event RoomAdded OnRoomAdded;
	public delegate void RoomDeleted(int id);
	public event RoomDeleted OnRoomDeleted;
	[JsonConverter(typeof(Grid2DJsonConverter<TileInfo>))]
	public Grid2D<TileInfo> tiles {get; set;}
	public Dictionary<int, Room> rooms {get; set;}
	public Dictionary<int, RoomObject> roomObjects {get; set;}
	public Dictionary<int, Door> doors {get; set;}
	

	public int nextRoomId {get; set;}
	public int nextDoorId {get; set;}
	public int nextRoomObjectId {get; set;}

	public Level(){}

	public Level(Vector2I size)
	{
		tiles = new Grid2D<TileInfo>(size.X, size.Y);
		tiles.Fill(new TileInfo{roomId = -1});
		rooms = new Dictionary<int, Room>();
		doors = new Dictionary<int, Door>();
		roomObjects = new Dictionary<int, RoomObject>();
	}

	public Vector2I getGridSize()
	{
		return tiles.Size;
	}

	public int getNextRoomId()
	{
		return nextRoomId;
	}

	public int getNextRoomObjectId()
	{
		return nextRoomObjectId;
	}

	public int addNewRoom(int extId=-1, Color? color=null, string name=null)
	{
		var id = nextRoomId;
		if (extId >= 0)
		{
			id = extId;
		}
		var room = new Room
		{
			debugColor = color != null ? color.Value : GenericUtils.Colors.GetRandomColor(0.5f, 0.7f, 0.5f),
			name = name != null ? name : ""
		};
		rooms[id] = room;
		if (extId < 0)
		{
			nextRoomId += 1;
		}
		OnRoomAdded?.Invoke(id, room);
		return id;
	}
	public int addNewDoor(Vector2I from, Vector2I to, int extId = -1)
	{
		var id = nextDoorId;
		if (extId >= 0)
		{
			id = extId;
		}
		var door = new Door
		{
			fromTile = from,
			toTile = to
		};
		doors[id] = door;
		if (extId < 0)
		{
			nextDoorId += 1;
		}
		return id;
	}
	public int addNewRoomObject(Vector2 centerPos, int rotation, Vector2I size, int extId = -1)
	{
		var id = nextRoomObjectId;
		if (extId >= 0)
		{
			id = extId;
		}
		var roomObject = new RoomObject
		{
			centerPos = centerPos,
			rotation = rotation,
			size = size
		};
		roomObjects[id] = roomObject;
		if (extId < 0)
		{
			nextRoomObjectId += 1;
		}
		return id;
	}
	
	public void removeLastAddedRoomObject()
	{
		removeRoomObject(nextRoomObjectId - 1);
		nextRoomObjectId -= 1;
	}

	public void removeLastAddedRoom()
	{
		removeRoom(nextRoomId - 1);
		nextRoomId -= 1;
	}
	
	public void removeLastAddedDoor()
	{
		removeRoom(nextDoorId - 1);
		nextDoorId -= 1;
	}

	public TileInfo getTile(Vector2I idx)
	{
		Debug.Assert(tiles.IsInBounds(idx));
		return tiles[idx];
	}
	public Room getRoom(int id)
	{
		return rooms.GetValueOrDefault(id, null);
	}

	public Door getDoor(int id)
	{
		return doors.GetValueOrDefault(id, null);
	}

	public RoomObject GetRoomObject(int id)
	{
		return roomObjects.GetValueOrDefault(id, null);
	}

	public IEnumerable<int> getDoorIdsFromTileIdx(Vector2I idx)
	{
		return doors.Keys.Where((x) => doors[x].fromTile == idx || doors[x].toTile == idx );
	}

	public IEnumerable<Vector2I> getFloodFilled(Vector2I idx)
	{
		Debug.Assert(tiles.IsInBounds(idx));
		var roomId = tiles[idx].roomId;
		return GenericUtils.GraphSearchUtils<Vector2I>.FloodFill(idx, (x) => getRoomTileNeighbours(x, roomId));
	}


	private void removeDoorsAttachedToTile(Vector2I tile)
	{
		foreach (var door in doors)
		{
			if(door.Value.fromTile == tile || door.Value.toTile == tile)
			{
				removeDoor(door.Key);
				break;
			}
		}
	}

	private void removeRoomObjectAttachedToTile(Vector2I tile)
	{
		var objectId = getRoomObjectAttachedToTile(tile);
		if(objectId > 0)
		{
			removeRoomObject(objectId);
		}
	}


	public bool canAddRoomObject(Vector2 centerPos, int rotation, Vector2I extents)
	{	
		var roomIds = new HashSet<int>();
		for (var i=0; i< extents.X; i++)
		{
			for (var j=0; j< extents.Y; j++)
			{
				var gridIdx = MathUtils.Funtions.GetGridIdxFromCenterAndRotation(extents, new Vector2I(i, j), centerPos, rotation);
				if (!tiles.IsInBounds(gridIdx))
				{
					return false;
				}
				roomIds.Add(tiles[gridIdx].roomId);
				if (roomIds.Count > 1)
				{
					return false;
				}

				if (getRoomObjectAttachedToTile(gridIdx) > 0)
				{
					return false;
				}
			}
		}
		return true;
	}

	public int getRoomObjectAttachedToTile(Vector2I tile)
	{
		foreach (var roomObject in roomObjects)
		{
			for (var i=0; i< roomObject.Value.size.X; i++)
			{
				for (var j=0; j< roomObject.Value.size.Y; j++)
				{
					var gridIdx = MathUtils.Funtions.GetGridIdxFromCenterAndRotation(roomObject.Value.size, new Vector2I(i, j), roomObject.Value.centerPos, roomObject.Value.rotation);
					if (gridIdx == tile)
					{
						return roomObject.Key;
					}
				}
			}
		}
		return -1;
	}

	public void removeRoom(int id)
	{
		foreach (var tile in getTilesPerRoom(id))
		{
			tiles[tile.Point] = tiles[tile.Point] with {roomId = -1};
			removeDoorsAttachedToTile(tile.Point);
			removeRoomObjectAttachedToTile(tile.Point);
		}
		rooms[id].delete();
		rooms.Remove(id);
		OnRoomDeleted?.Invoke(id);
	}

	public void removeDoor(int id)
	{
		doors.Remove(id);
	}

	public void removeRoomObject(int id)
	{
		roomObjects.Remove(id);
	}

	public IEnumerable<Grid2D<TileInfo>.IterData> getTilesPerRoom(int roomId)
	{
		return tiles.GetEnumerable().Where((x) => x.Data.roomId == roomId);
	}
	
	public bool canAssigTileToRoom(Vector2I idx, int roomId)
	{
		Debug.Assert(tiles.IsInBounds(idx));
		var previousIdx  = tiles[idx].roomId;
		if (previousIdx == roomId)
		{
			GD.Print($"Tile Already assigned to room with id: {roomId}");
			return false;
		}

		var tileCount = getTilesPerRoom(roomId).Count();
		if (roomId >= 0 && tileCount == 0)
		{
			return true;
		}
		
		var neighbourCount = getRoomTileNeighbours(idx, roomId).Where((pos) => tiles[pos].roomId == roomId).Count();
		if (roomId >= 0 && neighbourCount == 0)
		{
			GD.Print("Not enough neighbours");
			return false;
		}

		if (previousIdx < 0)
		{
			return true;
		}

		tiles[idx] = tiles[idx] with {roomId = roomId};

		var disjointSets = GenericUtils.DisjointSet<Vector2I>.FindDisjointSets(getTilesPerRoom(previousIdx).Select((x) => x.Point), (x) => getRoomTileNeighbours(x, previousIdx));
		if (disjointSets.ToList().Count > 1)
		{
			GD.Print($"Creates Disjoint Sets for room with id({previousIdx})");
			tiles[idx] = tiles[idx] with {roomId = previousIdx};
			return false;
		}

		tiles[idx] = tiles[idx] with {roomId = previousIdx};
		return true;

	}

	public bool canAddDoor(Vector2I from, Vector2I to)
	{
		if (doors.Values.Any((x) => (x.fromTile == from && x.toTile == to) || (x.fromTile == to && x.toTile == from)))
		{
			GD.Print("Door already there");
			return false;
		}
		if (!tiles.IsInBounds(from) || !tiles.IsInBounds(to))
		{
			GD.Print("Door Not in bounds.");
			return false;
		}
		if (tiles[from].roomId == tiles[to].roomId)
		{
			GD.Print("Can Only add room between rooms");
			return false;
		}
		return true;
	}

	public void assigTileToRoom(Vector2I idx, int roomId)
	{
		Debug.Assert(tiles.IsInBounds(idx));
		removeDoorsAttachedToTile(idx);
		removeRoomObjectAttachedToTile(idx);
		tiles[idx] = tiles[idx] with {roomId = roomId};

	}

	public IEnumerable<Vector2I> getRoomTileNeighbours(Vector2I pos, int roomId)
	{
		foreach (var neighPos in tiles.GetNeighbours4(pos))
		{
			if (tiles[neighPos].roomId == roomId)
			{
				yield return neighPos;
			}
		}
	}

	public IEnumerable<int> getRoomIdsSortedBySize()
	{
		var roomSizes = new Dictionary<int, int>();
		foreach(var tile in tiles)
		{
			var roomId = tile.Data.roomId; 
			if(roomId == -1)
			{
				continue;
			}
			if (roomSizes.ContainsKey(roomId))
			{
				roomSizes[roomId] += 1;
			}
			else
			{
				roomSizes[roomId] = 1;
			}
		}

		return rooms.Keys.AsEnumerable().OrderByDescending((x) => roomSizes.GetValueOrDefault(x, 0));
	}

	public IEnumerable<(Vector2I, Vector2I)> getRoomEdges(int roomId)
	{
		var roomTiles = getTilesPerRoom(roomId);
		foreach (var tile in roomTiles)
		{
			foreach (var neigh in tiles.GetNeighbours4(tile.Point))
			{
				if(tiles[neigh].roomId != roomId)
				{
					yield return (tile.Point, neigh);
				}
			}
		}
	}

	public struct RoomConnection
	{
		public int toRoom;

		public int doorId;
	}

	public class Building
	{
		public List<int> rooms;
	}

	private Dictionary<int, List<RoomConnection>> roomsGraph;
	private Dictionary<int, List<int>> roomAdjecencyGraph;

	public void cacheRoomsGraph()
	{
		roomsGraph = new Dictionary<int, List<RoomConnection>>();
		foreach (var roomId in rooms.Keys)
		{
			foreach (var otherId in rooms.Keys)
			{
				if (otherId == roomId)
				{
					continue;
				}
				foreach(var doorId in getDoorsBetweenRooms(roomId, otherId))
				{	
					addRoomsConnection(roomId, otherId, doorId, false);
				}
			}
		}
	}

	public void cacheRoomAdjacencyGraph()
	{
		roomAdjecencyGraph = new Dictionary<int, List<int>>();
		foreach (var roomId in rooms.Keys)
		{
			foreach (var otherId in rooms.Keys)
			{
				if (otherId == roomId)
				{
					continue;
				}
				if (areRoomsAdjecent(otherId, roomId))
				{
					if (!roomAdjecencyGraph.ContainsKey(roomId))
					{
						roomAdjecencyGraph[roomId] = new List<int>();
					}
					roomAdjecencyGraph[roomId].Add(otherId);
				}
			}
		}
	}

	public IEnumerable<Building> getBuildings()
	{
		var disjointSets = GenericUtils.DisjointSet<int>.FindDisjointSets(rooms.Keys, (roomId) => roomAdjecencyGraph.GetValueOrDefault(roomId, new List<int>()));
		foreach (var set in disjointSets)
		{
			yield return new Building
			{
				rooms = set.Elements	
			};
		} 
	}

	public IEnumerable<RoomConnection> getRoomNeighbours(int roomId)
	{
		if (!roomsGraph.ContainsKey(roomId))
		{
			yield break;
		}

		foreach (var connection in roomsGraph[roomId])
		{
			yield return connection;
		}
	}

	private void addRoomsConnection(int roomA, int roomB, int doorId, bool bidirectional=true)
	{
		if (!roomsGraph.ContainsKey(roomA))
		{
			roomsGraph[roomA] = new List<RoomConnection>();
		}
		roomsGraph[roomA].Add(new RoomConnection{toRoom = roomB, doorId=doorId});
		if (bidirectional)
		{
			addRoomsConnection(roomB, roomA, doorId, false);
		}
	}

	private IEnumerable<int> getDoorsBetweenRooms(int roomA, int roomB)
	{
		foreach (var door in doors)
		{
			var idA = getTile(door.Value.fromTile).roomId;
			var idB = getTile(door.Value.toTile).roomId;
			if ((idA == roomA && idB == roomB) || (idA == roomB && idB == roomA))
			{
				yield return door.Key;
			}
		}
	}

	private bool areRoomsAdjecent(int roomA, int roomB)
	{
		foreach (var tile in getTilesPerRoom(roomA))
		{
			if (tiles.GetNeighbours4(tile.Point).Any((x) => getTile(x).roomId == roomB))
			{
				return true;
			}
		}
		return false;
	}
	

} 
