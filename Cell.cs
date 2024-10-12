using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game_Theory_FYP;

public class Cell : IComparable<Cell>
{
	private static GraphicsDevice gref;
	private static Texture2D sharedTexture;
	private Color _color;
	private GameStrategy Strategy;
	public int Score;
	public readonly Point position;
	
	public readonly List<Cell> AlreadyPlayed = new List<Cell>();//store cells which have already been played with

	public static void Init(GraphicsDevice g)
	{
		gref = g;
		var size = 50;
		sharedTexture = new Texture2D(gref, size, size);
		
		var data = new Color[size*size];
		for (int i = 0; i < data.Length; ++i) data[i] = Color.White;
		sharedTexture.SetData(data);
		
		
	}


	public Cell(Point position)
	{
		this.position = position;
		Strategy = GameStrategy.GetRandom();
	}
	
	public Texture2D GetTexture()
	{
		return sharedTexture;
	}

	public Color GetColor()
	{
		return Strategy.GetDisplayColor();
	}

	public GameStrategy GetStrategy()
	{
		return Strategy;
	}
	public void SetStrategy(GameStrategy s)
	{
		Strategy = s;
	}

	public int CompareTo(Cell other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (other is null) return 1;
		return Score.CompareTo(other.Score);
	}
}