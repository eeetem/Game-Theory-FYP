using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Particles;
using OxyPlot;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SkiaSharp;

namespace Game_Theory_FYP;

public static class World
{

	private static WrappedGrid<Cell> grid;
	private static GameState currentState = GameState.PlayGames;
	private static KeyboardState lastkeyboardstate;
	private static MouseState lastmousestate;
	
	private enum GameState
	{
		PlayGames,
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

		lastkeyboardstate = Keyboard.GetState();
		PrintDetails();
	}


	private static bool pause = true;
	private static float msTimeTillTick = 100;
	public static void Update(GameTime gameTime)
	{
		var kstate = Keyboard.GetState();
		var mstate = Mouse.GetState();
		

		if (!pause)
		{
			msTimeTillTick -= gameTime.ElapsedGameTime.Milliseconds;
			if (msTimeTillTick <= 0 && !tickProcessing)
			{
				Tick();
				msTimeTillTick = 10;
			}
		}
		if(!Game1.instance.IsActive) return;
		if (kstate.IsKeyDown(Keys.Enter) && lastkeyboardstate.IsKeyUp(Keys.Enter))
		{
			Tick();
		}
		if (kstate.IsKeyDown(Keys.Space) && lastkeyboardstate.IsKeyUp(Keys.Space))
		{
			pause = !pause;
		}

		if (kstate.IsKeyDown(Keys.LeftShift))
		{
			pause = true;
		}

		if(kstate.IsKeyDown(Keys.Back) && lastkeyboardstate.IsKeyUp(Keys.Back))
		{
			DrawGraph();
		}
		
		if (mstate.LeftButton == ButtonState.Pressed)
		{
			GetCellUnderMouse(mstate).CooperationChance = 1;
			
		}

		if (mstate.RightButton == ButtonState.Pressed)
		{
			GetCellUnderMouse(mstate).CooperationChance = 0;
		}
		
		lastmousestate = mstate;
		lastkeyboardstate = kstate;
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


	
	public class Details
	{
		public float AvgCoop { get; set; }
		public float AvgRepFactor { get; set; }
		public float AvgScore { get; set; }
		public int Generation { get; set; }
		public int CoopGames { get; set; }
		public int BetrayedGames { get; set; }
		public int DefectedGames { get; set; }
		public float AvgRep { get; set; }
	}
	private static List<Details> detailsList = new List<Details>();

	public static void PrintDetails()
	{
		//calculate average coopratation, score, reputation
		float avgCoopfactor = 0;
		float avgRepFactor = 0;
		float avgRep = 0;
		float avgScore = 0;

		foreach (var c in grid)
		{
			avgCoopfactor += c.CooperationChance;
			avgRepFactor += c.ReputationFactor;
			avgScore += c.Score;

			float avgRepForCell = c.KnownReputations.Values.Sum();
			avgRepForCell /= c.KnownReputations.Count;
			avgRep += avgRepForCell;


		}
		var total = grid.Size * grid.Size;
		avgCoopfactor /= total;
		avgRepFactor /= total;
		avgScore /= total;
		avgRep /= total;
		
		Console.WriteLine("Average Cooperation Factor: " + avgCoopfactor);
		Console.WriteLine("Average Reputation Factor: " + avgRepFactor);
		Console.WriteLine("Average Reputation: " + avgRep);
		if(gamesBetrayed!=0)
			 Console.WriteLine("Percentage Cooperative Actions: " + (gamesCooped +gamesBetrayed)/ (float)(gamesCooped+gamesBetrayed+gamesDefected));
		Console.WriteLine("Average Score: " + avgScore);
		Console.WriteLine("Total Games: " + totalGames );
		detailsList.Add(new Details
		{
			AvgCoop = avgCoopfactor*100,
			AvgRepFactor = avgRepFactor*100,
			AvgScore = avgScore,
			Generation = Generation,
			CoopGames = gamesCooped,
			BetrayedGames = gamesBetrayed,
			DefectedGames = gamesDefected,
			AvgRep = avgRep*100,
		});
	}

	public static void DrawGraph()
{
    var plotModel1 = new PlotModel { Title = "Continuous Strategy Without Randomness" };

    var avgCoopSeries = new LineSeries { Title = "Avg Cooperation Factor", MarkerType = MarkerType.Circle };
    var avgRepFactorSeries = new LineSeries { Title = "Avg Reputation Factor", MarkerType = MarkerType.Circle };
    var avgScoreSeries = new LineSeries { Title = "Avg Score", MarkerType = MarkerType.Circle , Color = OxyColors.Red};
    var avgRepSeries = new LineSeries { Title = "Avg Reputation", MarkerType = MarkerType.Circle, Color = OxyColors.Purple};

    foreach (var detail in detailsList)
    {
        avgCoopSeries.Points.Add(new DataPoint(detail.Generation, detail.AvgCoop));
        avgRepFactorSeries.Points.Add(new DataPoint(detail.Generation, detail.AvgRepFactor));
        avgScoreSeries.Points.Add(new DataPoint(detail.Generation, detail.AvgScore));
        avgRepSeries.Points.Add(new DataPoint(detail.Generation, detail.AvgRep));
    }

    plotModel1.Series.Add(avgCoopSeries);
	plotModel1.Series.Add(avgRepFactorSeries);
	plotModel1.Series.Add(avgScoreSeries);
	plotModel1.Series.Add(avgRepSeries);

    plotModel1.IsLegendVisible = true;
    var legend1 = new Legend
    {
        LegendPosition = LegendPosition.TopRight,
        LegendPlacement = LegendPlacement.Outside,
        LegendOrientation = LegendOrientation.Horizontal,
        LegendBorderThickness = 5
    };
    plotModel1.Legends.Add(legend1);

    using (var stream = new MemoryStream())
    {
        var pngExporter = new PngExporter { Width = 1280, Height = 720 };
        pngExporter.Export(plotModel1, stream);
        File.WriteAllBytes("plot1.png", stream.ToArray());
    }

    var plotModel2 = new PlotModel { Title = "Continuous Strategy Without Randomness" };

    var coopGamesSeries = new LineSeries { Title = "Mutual Cooperative Games", MarkerType = MarkerType.Circle };
    var betrayedGamesSeries = new LineSeries { Title = "Betrayed Games", MarkerType = MarkerType.Circle, Color = OxyColors.Red};
    var defectedGamesSeries = new LineSeries { Title = "Mutual Defected Games", MarkerType = MarkerType.Circle, Color = OxyColors.Orange};

    foreach (var detail in detailsList)
    {
        coopGamesSeries.Points.Add(new DataPoint(detail.Generation, detail.CoopGames));
        betrayedGamesSeries.Points.Add(new DataPoint(detail.Generation, detail.BetrayedGames));
        defectedGamesSeries.Points.Add(new DataPoint(detail.Generation, detail.DefectedGames));
    }

    plotModel2.Series.Add(coopGamesSeries);
    plotModel2.Series.Add(betrayedGamesSeries);
    plotModel2.Series.Add(defectedGamesSeries);

    plotModel2.IsLegendVisible = true;
    var legend2 = new Legend
    {
        LegendPosition = LegendPosition.TopRight,
        LegendPlacement = LegendPlacement.Outside,
        LegendOrientation = LegendOrientation.Horizontal,
        LegendBorderThickness = 5
    };
    plotModel2.Legends.Add(legend2);

    using (var stream = new MemoryStream())
    {
        var pngExporter = new PngExporter { Width = 1280, Height = 720 };
        pngExporter.Export(plotModel2, stream);
        File.WriteAllBytes("plot2.png", stream.ToArray());
    }
}
	
	const int NeighbourhoodSize = 1;
	public static List<Cell> GetCellNeighbours(Cell c)
	{
		List<Cell> neighbours = new List<Cell>(8);

		for (int dx = -NeighbourhoodSize; dx <= NeighbourhoodSize; dx++)
		{
			for (int dy = -NeighbourhoodSize; dy <= NeighbourhoodSize; dy++)
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

	static int gamesCooped = 0;
	static int gamesDefected = 0;
	static int gamesBetrayed = 0;
	static int totalGames = 0;
	private static object countLock = new object();
	public static void PlayGames()
	{
		Console.WriteLine("Generation: "+Generation);
		Generation++;
		
		gamesCooped = 0;
		gamesDefected = 0;
		gamesBetrayed = 0;
		totalGames = 0;
		
		Parallel.ForEach(grid, c =>
		{
			c.Score = 0;
			c.AlreadyPlayed.Clear();
		});
		
		int cellsAccounted = 0;
		var res = Parallel.ForEach(grid, c =>
		{
			Interlocked.Increment(ref cellsAccounted);	
			var neig = GetCellNeighbours(c);
			foreach (var n in neig)
			{
				PlayGame(c,n);
			}
		});
		if(cellsAccounted != grid.Size*grid.Size)
			throw new Exception("Cells Accounted is not equal to grid size");

		Parallel.ForEach(grid, c =>
		{
			c.CalcCoopPercent();
		});
		currentState = GameState.AdjustStrategy;
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
				Interlocked.Increment(ref totalGames);
				if (aCooperate && bCooperate)
				{
					Interlocked.Add(ref a.Score, 3);
					Interlocked.Add(ref b.Score, 3);
					Interlocked.Increment(ref gamesCooped);
		
				}else if (aCooperate && !bCooperate)
				{
					Interlocked.Add(ref b.Score, 5);
					Interlocked.Increment(ref gamesBetrayed);
					
				}
				else if (bCooperate && !aCooperate)
				{
					Interlocked.Add(ref a.Score, 5);
					Interlocked.Increment(ref gamesBetrayed);
				}
				else
				{
					
					Interlocked.Add(ref a.Score, 1);
					Interlocked.Add(ref b.Score, 1);
					Interlocked.Increment(ref gamesDefected);
				}

				a.AdjustReputation(b, bCooperate);
				b.AdjustReputation(a, aCooperate);
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
	private static Cell GetCellUnderMouse(MouseState mstate)
	{
		float gridSize = 50f; // Adjust according to your cell size
		// Get the mouse position in screen space
		Vector2 mousePosition = new Vector2(mstate.X, mstate.Y);

		// Transform the mouse position to world space
		Vector2 worldMousePosition = Vector2.Transform(mousePosition, Matrix.Invert(Camera.GetViewMatrix()));

		// Calculate the cell coordinates
		int cellX = (int)((worldMousePosition.X) / gridSize);
		int cellY = (int)((worldMousePosition.Y ) / gridSize);
	//	Console.WriteLine(cellX + " " + cellY);


		// Get the cell from the grid
		return grid.GetElement(cellX, cellY);
	}

	private static void AdjustStrategy()
	{


		Parallel.ForEach(grid, c =>
		{
			c.CacheStrategy();
		});

		Parallel.For(0, (grid.Size*grid.Size), i =>
		{
			//pick random cell
			var c = grid.GetElement(Random.Shared.Next(grid.Size),Random.Shared.Next(grid.Size));
			var neighr = GetCellNeighbours(c);
			
			var random = Random.Shared.Next(neighr.Count);
			var neig = neighr[random];

			float temp = 0.1f;
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
				
				spriteBatch.DrawRectangle(new Rectangle((int) pos.X, (int) pos.Y, (int) gridSize, (int) gridSize), c.GetColorOutline(),5f);
				if (Camera.GetZoomLevel() > 0.8f)
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

				if (neighbor.AlreadyPlayed.TryGetValue(HighlightedCell, out var value))
				{
					// Draw two smaller rectangles side by side
					if (value.Item2)
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

				if (HighlightedCell.AlreadyPlayed.TryGetValue(neighbor, out var value1))
				{
					// Draw two smaller rectangles side by side
					if (value1.Item2)
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
				if (HighlightedCell.KnownReputations.TryGetValue(neighbor, out float reputation))
				{
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