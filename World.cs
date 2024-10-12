using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Particles;

namespace Game_Theory_FYP;

public static class World
{

	private static Cell[,] grid;
	private const int Size = 100;
	private static GameState currentState = GameState.PlayGames;
	private static KeyboardState laststate;
	
	private enum GameState
	{
		PlayGames,
		AnnounceReputation,
		AdjustStrategy
	}
	public static void Init()
	{
		
		grid = new Cell[Size, Size];
		for (int x = 0; x < Size; x++)
		{
			for (int y = 0; y < Size; y++)
			{
				//Color randomColor = Color.FromNonPremultiplied(Random.Shared.Next(256), Random.Shared.Next(256), Random.Shared.Next(256),255);

				grid[x, y] = new Cell(new Point(x,y));
			}
		}

		laststate = Keyboard.GetState();
	}


	private static bool pause = true;
	private static float msTimeTillTick = 100;
	public static void Update(GameTime gameTime)
	{
		var state = Keyboard.GetState();
		if (state.IsKeyDown(Keys.Enter) && laststate.IsKeyUp(Keys.Enter))
		{
			Tick();
		}
		if (state.IsKeyDown(Keys.Space) && laststate.IsKeyUp(Keys.Space))
		{
			pause = !pause;
		}

		if (!pause)
		{
			msTimeTillTick -= gameTime.ElapsedGameTime.Milliseconds;
			if (msTimeTillTick <= 0)
			{
				Tick();
				msTimeTillTick = 10;
			}
		}


		laststate = state;
	}
	
	

	public static void Tick()
	{
		switch (currentState)
		{
			case GameState.PlayGames:
				PlayGames();
				break;
			case GameState.AnnounceReputation:
				//todo
				break;
			case GameState.AdjustStrategy:
				AdjustStrategy();
				break;
		}
	}

	public static List<Cell> GetCellNeighbours(Cell c)
	{
		List<Cell> neighbours = new List<Cell>(25);

		for (int dx = -2; dx <= 2; dx++)
		{
			for (int dy = -2; dy <= 2; dy++)
			{
				if (dx != 0 || dy != 0) // Exclude the cell itself
				{
					int x = c.position.X + dx;
					int y = c.position.Y + dy;
					if(y<0 || y>=Size) continue;
					if(x<0 || x>=Size)continue;
					neighbours.Add(grid[x,y]);
				}
			}
		}

		return neighbours;
	}


	public static void PlayGames()
	{
		foreach (var c in grid)
		{
			c.Score = 0;
			c.AlreadyPlayed.Clear();
		}

		foreach (var c in grid)
		{
			var neig = GetCellNeighbours(c);
			foreach (var n in neig)
			{
				if (c.AlreadyPlayed.Contains(n)) continue;//dont repeat games with same neighbours
				PlayGame(c,n);
			}
		}

		currentState = GameState.AdjustStrategy;
	}

	private static void PlayGame(Cell a, Cell b)
	{
		bool aCooperate = a.GetStrategy().CooperateOrNot();
		bool bCooperate = b.GetStrategy().CooperateOrNot();

		if (aCooperate && bCooperate)
		{
			a.Score += 3;
			b.Score += 3;
		}else if (aCooperate && !bCooperate)
		{
			b.Score += 5;
		}
		else if (bCooperate && !aCooperate)
		{
			a.Score += 5;
		}
		else
		{
			a.Score += 1;
			b.Score += 1;
		}
		a.AlreadyPlayed.Add(b);
		b.AlreadyPlayed.Add(a);
	}
	
	private static void AdjustStrategy()
	{
		foreach (var c in grid)
		{
			var neigh = GetCellNeighbours(c);
			neigh.Add(c);//consider yourself aswell
			
			// Sort the list if needed
			var ordered = neigh.OrderBy(cell => cell.Score);

			// Calculate the total score
			double totalScore = ordered.Sum(cell => cell.Score);

			// Generate a random number between 0 and totalScore
			Random rand = new Random();
			double randNum = rand.NextDouble() * totalScore;

			// Select the cell based on the random number
			double currentSum = 0;
			Cell selectedCell = null;
			foreach (var cell in ordered)
			{
				currentSum += cell.Score;
				if (randNum <= currentSum)
				{
					selectedCell = cell;
					break;
				}
			}
			
			c.SetStrategy(selectedCell.GetStrategy());
		}

		currentState = GameState.PlayGames;
	}
	public static void Draw(SpriteBatch spriteBatch, GameTime gameTime)
	{
		spriteBatch.Begin(transformMatrix: Camera.GetViewMatrix(), samplerState: SamplerState.PointClamp,sortMode:SpriteSortMode.Texture);

		for (int x = 0; x < Size; x++)
		{
			for (int y = 0; y < Size; y++)
			{
				var c = grid[x, y];
				Vector2 pos = new Vector2(x * 50, y * 50);
				
				spriteBatch.Draw(c.GetTexture(), pos, c.GetColor());
				spriteBatch.DrawText(c.Score.ToString(),pos,Color.White);
			}
		}
		
		
		spriteBatch.End();
	}
}