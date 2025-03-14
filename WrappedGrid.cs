using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Game_Theory_FYP;

public class WrappedGrid<T> : IEnumerable<T>
{
	public readonly T[,] InternalGrid;
	public readonly int Size;

	public WrappedGrid(int size)
	{
		Size = size;
		InternalGrid = new T[size, size];
	}

	public IEnumerator<T> GetEnumerator()
	{
		foreach (var item in InternalGrid) yield return item;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public T GetElement(int x, int y)
	{
		return GetElement(new Point(x, y));
	}

	public T GetElement(Point p)
	{
		p.X = (p.X % Size + Size) % Size; // Ensures p.X is within the range [0, size-1]
		p.Y = (p.Y % Size + Size) % Size; // Ensures p.X is within the range [0, size-1]

		return InternalGrid[p.X, p.Y];
	}
}