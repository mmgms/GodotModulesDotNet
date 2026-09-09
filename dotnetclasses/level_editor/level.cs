using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataStructures;
using Godot;

namespace LevelEditor2D;

public struct TileInfo
{
	public int roomId;
	public TileInfo(){
		roomId = -1;
	}
}

public class Room
{
	public delegate void Deleted();
	public event Deleted OnDeleted;
	public Color debugColor;
	public String name;
	public void delete()
	{
		OnDeleted?.Invoke();
	}
}

public class Door
{
	public Vector2I fromTile;
	public Vector2I toTile;
}

public class Level
{
	public delegate void RoomAdded(int id, Room room);
	public event RoomAdded OnRoomAdded;
	public delegate void RoomDeleted(int id);
	public event RoomDeleted OnRoomDeleted;

	public Grid2D<TileInfo> tiles;
	public Dictionary<int, Room> rooms;
	public Dictionary<int, Door> doors;
	

	private int nextRoomId;
	private int nextDoorId;


	public Level(Vector2I size)
	{
		tiles = new Grid2D<TileInfo>(size.X, size.Y);
		tiles.Fill(new TileInfo{roomId = -1});
		rooms = new Dictionary<int, Room>();
		doors = new Dictionary<int, Door>();
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
			debugColor = color != null ? color.Value : GenericUtils.Colors.GetRandomColor(0.5f, 0.7f),
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

	public IEnumerable<int> getDoorIdsFromTileIdx(Vector2I idx)
	{
		return doors.Keys.Where((x) => doors[x].fromTile == idx || doors[x].toTile == idx );
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

	public void removeRoom(int id)
	{
		foreach (var tile in getTilesPerRoom(id))
		{
			tiles[tile.Point] = tiles[tile.Point] with {roomId = -1};
			removeDoorsAttachedToTile(tile.Point);
		}
		rooms[id].delete();
		rooms.Remove(id);
		OnRoomDeleted?.Invoke(id);
	}

	public void removeDoor(int id)
	{
		doors.Remove(id);
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
	

} 
