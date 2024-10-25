using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game_Theory_FYP;

public class Cell : IComparable<Cell>
{
	private static GraphicsDevice gref;
	private static Texture2D sharedTexture;
	public int Score;
	public readonly Point position;
	
	public readonly List<Cell> AlreadyPlayed = new List<Cell>();//store cells which have already been played with


	public float cooperationChance = 0.5f;
	public float reputationFactor = 0.5f;
	const float mutationFactor = 0.05f;

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
		cooperationChance = (float) Random.Shared.NextDouble();
	}
	public Texture2D GetTexture()
	{
		return sharedTexture;
	}
	public Color GetColor()
	{
		return new Color(1-cooperationChance, cooperationChance, 0);
	}
	

	public int CompareTo(Cell other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (other is null) return 1;
		return Score.CompareTo(other.Score);
	}

	public bool CooperateOrNot()
	{
		var val = Random.Shared.NextDouble();
		return val < cooperationChance;
	}

	public void UpdateStrategy(Cell candidate)
	{
		//get halfway point between the two strategies
		cooperationChance = (cooperationChance + candidate.cooperationChance) / 2;
		cooperationChance += (float) Random.Shared.NextDouble() * mutationFactor;
		
	}
}