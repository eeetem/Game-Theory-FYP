using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Particles;

namespace Game_Theory_FYP;

public static class World
{

	private static WrappedGrid<Cell> grid;
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

		grid = new WrappedGrid<Cell>(250);
		for (int x = 0; x < grid.Size; x++)
		{
			for (int y = 0; y < grid.Size; y++)
			{
				grid.InternalGrid[x, y] = new Cell(new Point(x,y));
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
			if (msTimeTillTick <= 0 && !tickProcessing)
			{
				Tick();
				msTimeTillTick = 10;
			}
		}


		laststate = state;
	}
	
	
	private static object lockObject = new object();
	private static bool tickProcessing = false;
	public static void Tick()
	{

		lock (lockObject)
		{
			Task t = new Task(delegate
			{

				tickProcessing = true;
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

				tickProcessing = false;

			});

			t.Start();
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

					neighbours.Add(grid.GetElement(x,y));
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
		bool aCooperate = a.CooperateOrNot();
		bool bCooperate = b.CooperateOrNot();

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
			neigh.Add(c); // consider yourself as well

			var scores = neigh.Select(cell => cell.Score).OrderBy(score => score).ToList();
			double median = scores.Count % 2 == 0 ? 
				(scores[scores.Count / 2 - 1] + scores[scores.Count / 2]) / 2.0 : 
				scores[scores.Count / 2];

			
			double temperature = 1.0; // Adjust this parameter as needed
			double fermiEnergy = median; // Adjust this parameter as needed
			var k = 1;

			List<double> probabilities = neigh.Select(n =>
			{
				double energy = n.Score; // Assuming score is directly proportional to energy
				return 1.0 / (1.0 + Math.Exp((energy - fermiEnergy) / (temperature * k))); // Fermi-Dirac distribution
			}).ToList();
			
			
			// Generate a random number between 0 and totalScore
			Random rand = new Random();
			double randNum = rand.NextDouble() * probabilities.Sum();

			// Select the cell based on the random number
			double currentSum = 0;
			Cell selectedCell = null;
			int idx = 0;
			foreach (var cell in neigh)
			{
				currentSum += probabilities[idx];
				if (randNum <= currentSum)
				{
					selectedCell = cell;
					break;
				}

				idx++;
			}
			
			//c.SetStrategy(selectedCell.GetStrategy());
		}

		currentState = GameState.PlayGames;
	}

	public static void Draw(SpriteBatch spriteBatch, GameTime gameTime)
	{
		spriteBatch.Begin(transformMatrix: Camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);
		var cameraBoundingRectangle = Camera.GetBoundingRectangle();
		float gridSize = 50f; // Adjust according to your cell size

		// Get the top-left corner of the visible area in the world space
		Vector2 cameraTopLeft = cameraBoundingRectangle.TopLeft;
		
		// Calculate the offset for wrapping
		int startX = (int) Math.Floor(cameraTopLeft.X / gridSize);
		int startY = (int) Math.Floor(cameraTopLeft.Y / gridSize);

		for (int x = startX; x < startX + (cameraBoundingRectangle.Width / gridSize) + 2; x++)
		{
			for (int y = startY; y < startY + (cameraBoundingRectangle.Height / gridSize) + 2; y++)
			{
				var wrappedX = x % grid.Size;
				var wrappedY = y % grid.Size;

				var c = grid.GetElement(wrappedX, wrappedY);

				Vector2 pos = new Vector2(x * gridSize, y * gridSize);

				spriteBatch.Draw(c.GetTexture(), pos, c.GetColor());
				if (Camera.GetZoomLevel() > 0.3f)
				{
					spriteBatch.DrawText(c.Score.ToString(), pos, Color.White);
				}
			}
		}
		spriteBatch.End();
	}

}

