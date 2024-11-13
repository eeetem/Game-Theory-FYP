using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
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

		grid = new WrappedGrid<Cell>(100);
		for (int x = 0; x < grid.Size; x++)
		{
			for (int y = 0; y < grid.Size; y++)
			{
				grid.InternalGrid[x, y] = new Cell(new Point(x,y));
			}
		}
		
		for (int x = 0; x < grid.Size; x++)
		{
			for (int y = 0; y < grid.Size; y++)
			{
				var c = grid.GetElement(x,y);
				c.InitiliaseRep(GetCellNeighbours(c));
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

		if (state.IsKeyDown(Keys.LeftShift))
		{
			pause = true;
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

	private static int Generation = 0;
	public static void Tick()
	{

		Task t = new Task(delegate
		{
			lock (lockObject)
			{
				tickProcessing = true;
				switch (currentState)
				{
					case GameState.PlayGames:
						Console.WriteLine("Playing Games");
						PlayGames();
						break;
					case GameState.AnnounceReputation:
						Console.WriteLine("Announceing Reputation");
						AnnounceReputation();
						break;
					case GameState.AdjustStrategy:
						Console.WriteLine("Adjusting Strategy");
						AdjustStrategy();
						break;
				}

				tickProcessing = false;
			}
		});

		t.Start();
	}

	private static void AnnounceReputation()
	{

		if (!Cell.RepEnabled)
		{
			currentState = GameState.AdjustStrategy;
			return;
		}
		Parallel.ForEach(grid, c =>
		{
			c.CacheTrust();
		});
		Parallel.ForEach(grid ,c =>
		{
			var neighours = GetCellNeighbours(c);
			foreach (var neighour in neighours)
			{
				foreach (var opponent in c.AlreadyPlayed)
				{
					if (opponent.Key == neighour)
					{
						continue;
					}

					neighour.TellReputation(c, opponent.Key, opponent.Value.Item2);
				}
			}

		});
		currentState = GameState.AdjustStrategy;
	}

	public static void PrintDetails()
	{
		//calculate average coopratation, score, reputation
		float avgCoop = 0;
		float avgRepFactor = 0;
		float avgScore = 0;
		foreach (var c in grid)
		{
			avgCoop += c.CooperationChance;
			avgRepFactor += c.ReputationFactor;
			avgScore += c.Score;
		}
		
		avgCoop /= grid.Size * grid.Size;
		avgRepFactor /= grid.Size * grid.Size;
		avgScore /= grid.Size * grid.Size;
		
		Console.WriteLine("Average Cooperation: " + avgCoop);
		Console.WriteLine("Average Reputation Factor: " + avgRepFactor);
		Console.WriteLine("Average Score: " + avgScore);

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
		Console.WriteLine("Generation: "+Generation);
		Generation++;

		Parallel.ForEach(grid, c =>
		{
			c.Score = 0;
			c.AlreadyPlayed.Clear();
		});
		
		Parallel.ForEach(grid, c =>
		{
			var neig = GetCellNeighbours(c);
			foreach (var n in neig)
			{
				PlayGame(c,n);
			}
		});
	

		currentState = GameState.AnnounceReputation;
	}

	private static void PlayGame(Cell a, Cell b)
	{
		object l1;
		object l2;
		if (a.position.X + a.position.Y*grid.Size > b.position.X + b.position.Y*grid.Size)
		{
			l1 = a.lockObject;
			l2 = b.lockObject;
		}else
		{
			l1 = b.lockObject;
			l2 = a.lockObject;
		}
		lock (l1)
		{
			lock (l2)
			{
				
				if (a.AlreadyPlayed.ContainsKey(b))
				{
					return;
				}
				bool aCooperate = a.CooperateOrNot(b);
				bool bCooperate = b.CooperateOrNot(a);

				if (aCooperate && bCooperate)
				{
					a.Score += 3;
					b.Score += 3;
					a.KnownReputations[b] += Cell.BaseRepChange;
					b.KnownReputations[a] += Cell.BaseRepChange;
				}else if (aCooperate && !bCooperate)
				{
					b.Score += 5;
					a.KnownReputations[b] -= Cell.BaseRepChange;
					b.KnownReputations[a] += Cell.BaseRepChange;
				}
				else if (bCooperate && !aCooperate)
				{
					a.Score += 5;
					a.KnownReputations[b] += Cell.BaseRepChange;
					b.KnownReputations[a] -= Cell.BaseRepChange;
				}
				else
				{
					a.Score += 1;
					b.Score += 1;
					a.KnownReputations[b] -= Cell.BaseRepChange;
					b.KnownReputations[a] -= Cell.BaseRepChange;
				}

				if (!a.AlreadyPlayed.TryAdd(b,(aCooperate, bCooperate)))
				{
					throw new Exception("Played Game With Already Played");
				}
				if(!b.AlreadyPlayed.TryAdd(a,(bCooperate,aCooperate)))
				{
					throw new Exception("Played Game With Already Played");
				}
			}
		}
	}

	private static void AdjustStrategy()
	{


		Parallel.ForEach(grid, c =>
		{
			c.CacheStrategy();
		});

		Parallel.For(0, (grid.Size*grid.Size)/2, i =>
		{
			//pick random cell
			var c = grid.GetElement(Random.Shared.Next(grid.Size),Random.Shared.Next(grid.Size));
			var neighr = GetCellNeighbours(c);
			
			var random = Random.Shared.Next(neighr.Count);
			var neig = neighr[random];

			float temp = 1;
			double prob = 1 / (Math.Pow(Math.E, -temp*(neig.Score - c.Score)));
			if (Random.Shared.NextDouble() < prob)
			{
				c.UpdateStrategy(neig);//cell can copy multiple cells in 1 go?
			}
		});
		PrintDetails();

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
		
		// Get the mouse position in screen space
		var mouseState = Mouse.GetState();
		Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);

		// Transform the mouse position to world space
		Vector2 worldMousePosition = Vector2.Transform(mousePosition, Matrix.Invert(Camera.GetViewMatrix()));
		Cell HighlightedCell = null;
		Vector2 HighletCellDrawpos = Vector2.Zero;
		for (int x = startX; x < startX + (cameraBoundingRectangle.Width / gridSize) + 2; x++)
		{
			for (int y = startY; y < startY + (cameraBoundingRectangle.Height / gridSize) + 2; y++)
			{
				var wrappedX = x % grid.Size;
				var wrappedY = y % grid.Size;

				var c = grid.GetElement(wrappedX, wrappedY);

				var pos = new Vector2(x * gridSize, y * gridSize);

				spriteBatch.FillRectangle(new Rectangle((int) pos.X, (int) pos.Y, (int) gridSize, (int) gridSize), c.GetColor());
				
				spriteBatch.DrawRectangle(new Rectangle((int) pos.X, (int) pos.Y, (int) gridSize, (int) gridSize), c.GetColorRep(),5f);
				if (Camera.GetZoomLevel() > 0.3f)
				{
					spriteBatch.DrawText(c.Score.ToString(), pos, Color.White);
				}
				//highlight tile under mouse
				if (new Rectangle((int) pos.X, (int) pos.Y, (int) gridSize, (int) gridSize).Contains(worldMousePosition))
				{
					HighlightedCell = c;
					HighletCellDrawpos = pos;
				}
				
			}
		}
		if(HighlightedCell != null)
		{
			
			spriteBatch.DrawRectangle(new Rectangle((int) HighletCellDrawpos.X, (int) HighletCellDrawpos.Y, (int) gridSize, (int) gridSize), Color.White);
			
			//draw coop and rep factors of the highlighted cell
			spriteBatch.DrawText("C:" + HighlightedCell.CooperationChance.ToString("0.00"), new Vector2(HighletCellDrawpos.X, HighletCellDrawpos.Y+10), Color.White);
			spriteBatch.DrawText("R:" + HighlightedCell.ReputationFactor.ToString("0.00"), new Vector2(HighletCellDrawpos.X, HighletCellDrawpos.Y + 32), Color.White);
			
			var neighbors = GetCellNeighbours(HighlightedCell);
			foreach (var neighbor in neighbors)
			{
				Vector2 relativePos = new Vector2(neighbor.position.X - HighlightedCell.position.X, neighbor.position.Y - HighlightedCell.position.Y);
				spriteBatch.DrawRectangle(new Rectangle((int) (HighletCellDrawpos.X + relativePos.X * gridSize), (int) (HighletCellDrawpos.Y + relativePos.Y * gridSize), (int) gridSize, (int) gridSize), Color.White);

				int smallerSize = (int)(gridSize * 0.6); // Adjust the factor to make the rectangles smaller
				int innerSize = (int)(smallerSize * 0.8); // Make the inner rectangles smaller
				int halfInnerSize = innerSize / 2; // Half size for side-by-side rectangles

// Draw white background
				spriteBatch.FillRectangle(new Rectangle(
					(int)(HighletCellDrawpos.X + relativePos.X * gridSize + (gridSize - smallerSize) / 2),
					(int)(HighletCellDrawpos.Y + relativePos.Y * gridSize + (gridSize - smallerSize) / 2),
					smallerSize, smallerSize), Color.White);

				if (neighbor.AlreadyPlayed.ContainsKey(HighlightedCell))
				{
					// Draw two smaller rectangles side by side
					if (neighbor.AlreadyPlayed[HighlightedCell].Item2)
					{
						spriteBatch.FillRectangle(new Rectangle(
							(int)(HighletCellDrawpos.X + relativePos.X * gridSize + (gridSize - innerSize) / 2),
							(int)(HighletCellDrawpos.Y + relativePos.Y * gridSize + (gridSize - innerSize) / 2),
							halfInnerSize, innerSize), Color.Green);
					}
					else
					{
						spriteBatch.FillRectangle(new Rectangle(
							(int)(HighletCellDrawpos.X + relativePos.X * gridSize + (gridSize - innerSize) / 2),
							(int)(HighletCellDrawpos.Y + relativePos.Y * gridSize + (gridSize - innerSize) / 2),
							halfInnerSize, innerSize), Color.Red);
					}
					spriteBatch.DrawText("O", new Vector2(
						(int)(HighletCellDrawpos.X + relativePos.X * gridSize + (gridSize - innerSize) / 2),
						(int)(HighletCellDrawpos.Y + relativePos.Y * gridSize + (gridSize - innerSize) / 2)), Color.White);

				}

				if (HighlightedCell.AlreadyPlayed.ContainsKey(neighbor))
				{
					// Draw two smaller rectangles side by side
					if (HighlightedCell.AlreadyPlayed[neighbor].Item2)
					{
						spriteBatch.FillRectangle(new Rectangle(
							(int)(HighletCellDrawpos.X + relativePos.X * gridSize + (gridSize - innerSize) / 2 + halfInnerSize),
							(int)(HighletCellDrawpos.Y + relativePos.Y * gridSize + (gridSize - innerSize) / 2),
							halfInnerSize, innerSize), Color.Green);
					}
					else
					{
						spriteBatch.FillRectangle(new Rectangle(
							(int)(HighletCellDrawpos.X + relativePos.X * gridSize + (gridSize - innerSize) / 2 + halfInnerSize),
							(int)(HighletCellDrawpos.Y + relativePos.Y * gridSize + (gridSize - innerSize) / 2),
							halfInnerSize, innerSize), Color.Red);
					}
					spriteBatch.DrawText("N", new Vector2(
						(int)(HighletCellDrawpos.X + relativePos.X * gridSize + (gridSize - innerSize) / 2 + halfInnerSize),
						(int)(HighletCellDrawpos.Y + relativePos.Y * gridSize + (gridSize - innerSize) / 2)), Color.White);

				}
				
				// Draw reputation value
				if (HighlightedCell.KnownReputations.ContainsKey(neighbor))
				{
					
					float reputation = HighlightedCell.KnownReputations[neighbor];
					
					// Normalize the reputation value from [-1, 1] to [0, 1]
					float normalizedReputation = (reputation + 1) / 2;

					// Interpolate between red and green based on the normalized reputation value
					Color reputationColor = Color.Lerp(Color.Red, Color.Green, normalizedReputation);

					
					spriteBatch.DrawText(reputation.ToString("0.00"), new Vector2(
						(int)(HighletCellDrawpos.X + relativePos.X * gridSize + (gridSize - smallerSize) / 2),
						(int)(HighletCellDrawpos.Y + relativePos.Y * gridSize + (gridSize - smallerSize) / 2+32)),reputationColor);
				}
			}
		}
	
		spriteBatch.End();
	}

}