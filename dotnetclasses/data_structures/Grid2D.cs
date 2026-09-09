namespace DataStructures;

using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public class Grid2D<T> : IEnumerable<Grid2D<T>.IterData>
{
    private readonly T[] _data;
    private readonly int _sizeX;
    private readonly int _sizeY;

    private static readonly Vector2I[] FourDirections =
    {
        Vector2I.Down,
        Vector2I.Left,
        Vector2I.Right,
        Vector2I.Up
    };

    private static readonly Vector2I[] EightDirections =
    {
        Vector2I.Down,
        Vector2I.Left,
        Vector2I.Right,
        Vector2I.Up,
        Vector2I.Down + Vector2I.Left,
        Vector2I.Down + Vector2I.Right,
        Vector2I.Up + Vector2I.Left,
        Vector2I.Up + Vector2I.Right
    };

    public readonly struct IterData
    {
        public Vector2I Point { get; }
        public T Data { get; }

        public IterData(Vector2I point, T data)
        {
            Point = point;
            Data = data;
        }
    }

    public Grid2D(int sizeX, int sizeY)
    {
        _sizeX = sizeX;
        _sizeY = sizeY;
        _data = new T[sizeX * sizeY];
    }

    public Grid2D<T> Clone()
    {
        var copy = new Grid2D<T>(_sizeX, _sizeY);
        Array.Copy(_data, copy._data, _data.Length);
        return copy;
    }

    public Vector2I Size => new(_sizeX, _sizeY);

    public Vector2I Center => new(_sizeX / 2, _sizeY / 2);

    public bool IsOnEdge(Vector2I pos) => IsOnEdge(pos.X, pos.Y);

    public bool IsOnEdge(int x, int y)
    {
        if (!IsInBounds(x, y))
            throw new ArgumentOutOfRangeException();

        return x == 0 || y == 0 || x == _sizeX - 1 || y == _sizeY - 1;
    }

    public void Fill(T value)
    {
        Array.Fill(_data, value);
    }

    public T Get(int x, int y)
    {
        if (!IsInBounds(x, y))
            throw new ArgumentOutOfRangeException();

        return _data[x + y * _sizeX];
    }

    public void Set(int x, int y, T value)
    {
        if (!IsInBounds(x, y))
            throw new ArgumentOutOfRangeException();

        _data[x + y * _sizeX] = value;
    }

    public T Get(Vector2I pos) => Get(pos.X, pos.Y);

    public void Set(Vector2I pos, T value) => Set(pos.X, pos.Y, value);

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 &&
               y >= 0 &&
               x < _sizeX &&
               y < _sizeY;
    }

    public bool IsInBounds(Vector2I pos) => IsInBounds(pos.X, pos.Y);

    public IEnumerable<Vector2I> GetNeighbours4(Vector2I pos)
    {
        if (!IsInBounds(pos))
            throw new ArgumentOutOfRangeException();

        foreach (var dir in FourDirections)
        {
            var n = pos + dir;
            if (IsInBounds(n))
                yield return n;
        }

    }

    public IEnumerable<Vector2I> GetNeighbours4NoBoundsCheck(Vector2I pos)
    {
        if (!IsInBounds(pos))
            throw new ArgumentOutOfRangeException();

        foreach (var dir in FourDirections)
		{
			
            yield return pos + dir;
		}

    }

    public IEnumerable<Vector2I> GetNeighbours8(Vector2I pos)
    {
        if (!IsInBounds(pos))
            throw new ArgumentOutOfRangeException();

        foreach (var dir in EightDirections)
        {
            var n = pos + dir;
            if (IsInBounds(n))
                yield return n;
        }

    }

    public IEnumerable<Vector2I> GetNeighbours8NoBoundsCheck(Vector2I pos)
    {
        if (!IsInBounds(pos))
            throw new ArgumentOutOfRangeException();

        foreach (var dir in EightDirections)
            yield return pos + dir;

    }

    public Vector2I DataIndexToVector2I(int index)
    {
        return new Vector2I(index % _sizeX, index / _sizeX);
    }

    public IEnumerator<IterData> GetEnumerator()
    {
        for (int i = 0; i < _data.Length; i++)
            yield return new IterData(DataIndexToVector2I(i), _data[i]);
    }

	public IEnumerable<IterData> GetEnumerable()
	{
		var enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			yield return enumerator.Current;
		}
	}


    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public T this[int x, int y]
    {
        get => Get(x, y);
        set => Set(x, y, value);
    }

    public T this[Vector2I pos]
    {
        get => Get(pos);
        set => Set(pos, value);
    }
}