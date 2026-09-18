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
	public bool isRoad {get; set;}
	public int buildingId {get; set;} = -1;
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
public class Building
{
	public delegate void Deleted();
	public event Deleted OnDeleted;
	public String name {get; set;}
	public Color debugColor {get; set;}
	public void delete()
	{
		OnDeleted?.Invoke();
	}
}

public class Level
{
	public delegate void RoomAdded(int id, Room room);
	public event RoomAdded OnRoomAdded;
	public delegate void BuildingAdded(int id, Building building);
	public event BuildingAdded OnBuildingAdded;
	public delegate void RoomDeleted(int id);
	public event RoomDeleted OnRoomDeleted;
	[JsonConverter(typeof(Grid2DJsonConverter<TileInfo>))]
	public Grid2D<TileInfo> tiles {get; set;}
	public Dictionary<int, Room> rooms {get; set;}
	public Dictionary<int, RoomObject> roomObjects {get; set;}
	public Dictionary<int, Door> doors {get; set;}
	public Dictionary<int, Building> buildings {get; set;}

	public int nextRoomId {get; set;}
	public int nextDoorId {get; set;}
	public int nextRoomObjectId {get; set;}
	public int nextBuildingId {get; set;}

	public Level(){}

	public Level(Vector2I size)
	{
		tiles = new Grid2D<TileInfo>(size.X, size.Y);
		tiles.Fill(new TileInfo{roomId = -1});
		rooms = new Dictionary<int, Room>();
		doors = new Dictionary<int, Door>();
		roomObjects = new Dictionary<int, RoomObject>();
		buildings = new Dictionary<int, Building>();
	}

	public Vector2I getGridSize()
	{
		return tiles.Size;
	}

	public int getNextRoomId()
	{
		return nextRoomId;
	}

	public int getNextBuildingId()
	{
		return nextBuildingId;
	}

	public int getNextRoomObjectId()
	{
		return nextRoomObjectId;
	}

	public int getBuildingFromTile(Vector2I idx)
	{
		var roomId = tiles[idx].roomId; 
		if (roomId < 0)
		{
			return -1;
		}
		var buildingId = rooms[roomId].buildingId;
		if (buildingId < 0)
		{
			return -1;
		}
		return buildingId;

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

	public int addNewBuilding( int extId = -1, Color? color=null, string name=null)
	{
		var id = nextBuildingId;
		if (extId >= 0)
		{
			id = extId;
		}
		var building = new Building
		{
			debugColor = color != null ? color.Value : GenericUtils.Colors.GetRandomColor(0.5f, 0.7f, 0.5f),
			name = name != null ? name : ""
		};
		buildings[id] = building;
		if (extId < 0)
		{
			nextBuildingId += 1;
		}
		OnBuildingAdded?.Invoke(id, building);
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
		removeDoor(nextDoorId - 1);
		nextDoorId -= 1;
	}

	public void removeLastAddedBuilding()
	{
		removeBuilding(nextBuildingId - 1);
		nextBuildingId -= 1;
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

	public Building getBuilding(int id)
	{
		return buildings.GetValueOrDefault(id, null);
	}

	public void assignRoomToBuildingId(int roomId, int buildingId)
	{
		Debug.Assert(rooms.ContainsKey(roomId));
		rooms[roomId].buildingId = buildingId;
	}

	public IEnumerable<int> getAdjecentRooms(int roomId)
	{
		if (!roomAdjecencyGraph.ContainsKey(roomId))
		{
			yield break;
		}
		foreach (var id in roomAdjecencyGraph[roomId])
		{
			yield return id;
		}
	}

	public int getDoorIdBetweenTiles(Vector2I tileA, Vector2I tileB)
	{
		foreach (var door in doors)
		{
			if ((door.Value.fromTile == tileA && door.Value.toTile == tileB) ||(door.Value.fromTile == tileB && door.Value.toTile == tileA) )
			{
				return door.Key;
			}
		}
		return -1;
	}

	public RoomObject getRoomObject(int id)
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
		return GenericUtils.GraphSearchUtils.FloodFill<Vector2I>(idx, (x) => getRoomTileNeighbours(x, roomId));
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

	public void removeBuilding(int id)
	{
		buildings[id].delete();
		buildings.Remove(id);
	}

	public void removeRoomObject(int id)
	{
		roomObjects.Remove(id);
	}

	public IEnumerable<Grid2D<TileInfo>.IterData> getTilesPerRoom(int roomId)
	{
		return tiles.GetEnumerable().Where((x) => x.Data.roomId == roomId);
	}

	public IEnumerable<int> getRoomsPerBuilding(int buildingId)
	{
		foreach (var room in rooms)
		{
			if (room.Value.buildingId == buildingId)
			{
				yield return room.Key;
			}
		}
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

	public struct BuildingConnection
	{
		public enum ConnectionType {Door, Road}
		public int toBuilding;
		public ConnectionType type;
		public int doorId;
	}

	private Dictionary<int, List<RoomConnection>> roomsGraph;
	private Dictionary<int, List<int>> roomAdjecencyGraph;

	private Dictionary<int, List<BuildingConnection>> buildingsGraph;
	private Dictionary<int, List<int>> buildingAdjcencyGraph;

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
					if (!roomAdjecencyGraph.TryGetValue(roomId, out List<int> value))
					{
						value = new List<int>();
						roomAdjecencyGraph[roomId] = value;
					}

					value.Add(otherId);
				}
			}
		}
	}

	public void cacheBuildingGraph()
	{
		cacheRoomsGraph();
		cacheRoomAdjacencyGraph();
		buildingsGraph = new Dictionary<int, List<BuildingConnection>>();
		foreach (var buildingId in buildings.Keys)
		{
			foreach (var otherBuildingId in buildings.Keys)
			{
				if (otherBuildingId == buildingId)
				{
					continue;
				}
				foreach(var conn in getBuildingConnection(buildingId, otherBuildingId))
				{	
					addBuildingConnection(buildingId, conn, false);
				}
			}
		}
	}

	public void cacheBuildingAdjecencyGraph()
	{
		cacheRoomAdjacencyGraph();
		buildingAdjcencyGraph = new Dictionary<int, List<int>>();
		foreach (var buildingId in buildings.Keys)
		{
			foreach (var otherBuildingId in buildings.Keys)
			{
				if (otherBuildingId == buildingId)
				{
					continue;
				}
				if (areBuildingAdjacent(otherBuildingId, buildingId))
				{
					if (!buildingAdjcencyGraph.TryGetValue(buildingId, out List<int> value))
					{
						value = new List<int>();
						buildingAdjcencyGraph[buildingId] = value;
					}

					value.Add(otherBuildingId);
				}
			}
		}
	}


	public IEnumerable<int> getBuildingsIds()
	{
		//var disjointSets = GenericUtils.DisjointSet<int>.FindDisjointSets(rooms.Keys, (roomId) => roomAdjecencyGraph.GetValueOrDefault(roomId, new List<int>()));
		return buildings.Keys; 
	}

	public IEnumerable<int> getRoomIdPerBuilding(int id)
	{
		foreach (var room in rooms)
		{
			if (room.Value.buildingId == id)
			{
				yield return room.Key;
			}
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

	public IEnumerable<BuildingConnection> getBuildingNeighBours(int buildingId)
	{
		if (!buildingsGraph.TryGetValue(buildingId, out List<BuildingConnection> value))
		{
			yield break;
		}

		foreach (var connection in value)
		{
			yield return connection;
		}
	}

	private void addRoomsConnection(int roomA, int roomB, int doorId, bool bidirectional=true)
	{
		if (!roomsGraph.TryGetValue(roomA, out List<RoomConnection> value))
		{
			value = new List<RoomConnection>();
			roomsGraph[roomA] = value;
		}

		value.Add(new RoomConnection{toRoom = roomB, doorId=doorId});
		if (bidirectional)
		{
			addRoomsConnection(roomB, roomA, doorId, false);
		}
	}

	
	private void addBuildingConnection(int buildingA, BuildingConnection connection, bool bidirectional=true)
	{
		if (!buildingsGraph.TryGetValue(buildingA, out List<BuildingConnection> value))
		{
			value = new List<BuildingConnection>();
			buildingsGraph[buildingA] = value;
		}

		value.Add(connection);
		if (bidirectional)
		{
			addBuildingConnection(connection.toBuilding, connection with {toBuilding=buildingA}, false);
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
		if (getRoom(roomA).isRoad || getRoom(roomB).isRoad)
		{
			return false;
		}
		foreach (var tile in getTilesPerRoom(roomA))
		{
			if (tiles.GetNeighbours4(tile.Point).Any((x) => getTile(x).roomId == roomB))
			{
				return true;
			}
		}
		return false;
	}

	private bool areBuildingAdjacent(int buildingA, int buildingB)
	{
		foreach (var roomA in getRoomIdPerBuilding(buildingA))
		{
			foreach (var roomB in getRoomIdPerBuilding(buildingB))
			{
				if (roomAdjecencyGraph.TryGetValue(roomA, out List<int> value) && value.Contains(roomB))
				{
					return true;
				}
			}
		}
		return false;
	}

	private IEnumerable<BuildingConnection> getBuildingConnection(int buildingA, int buildingB)
	{
		foreach (var roomA in getRoomIdPerBuilding(buildingA))
		{
			foreach (var roomB in getRoomIdPerBuilding(buildingB))
			{
				if(getRoom(roomA).isRoad && getRoom(roomB).isRoad)
				{
					
					yield return new BuildingConnection{toBuilding = buildingB, type = BuildingConnection.ConnectionType.Road};
					continue;
				}
				if (roomsGraph.TryGetValue(roomA, out List<RoomConnection> value))
				{
					foreach (var conn in value)
					{
						if (conn.toRoom == roomB)
						{
							yield return new BuildingConnection{toBuilding = buildingB, doorId = conn.doorId};
						}
					}
				}
			}
		}	
	}
	

} 
