using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;

namespace Game_Theory_FYP;

public class Game1 : Game
{
	private GraphicsDeviceManager _graphics;
	private SpriteBatch _spriteBatch;
	public static Game1 instance;


	public Game1()
	{
		_graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;
		Window.AllowUserResizing = true;
		_graphics.SynchronizeWithVerticalRetrace = false;
		IsFixedTimeStep = true;
		_graphics.ApplyChanges();
		instance = this;
	}

	protected override void Initialize()
	{
		base.Initialize();
		Camera.Init(GraphicsDevice,Window);
		TextRenderer.Init(Content,GraphicsDevice);
		List<SimulationParameters> parameterSets = new List<SimulationParameters>();

		for (float repFactor = -0.1f; repFactor <= 0.91f; repFactor += 0.1f)
		{
			for (float repInterpolationFactor = 0.1f; repInterpolationFactor <= 0.91f; repInterpolationFactor += 0.1f)
			{
				parameterSets.Add(new SimulationParameters
				{
					GlobalRepFactor = repFactor,
					GlobalRepInterpolationFactor = repInterpolationFactor
				});
			}
		}

		SimulationParameters param = parameterSets[0];
		parameterSets.RemoveAt(0);
		param.OnSimulationEnd += Grid2D;
		while (parameterSets.Count>0)
		{
			var nextParam = parameterSets[0];
			parameterSets.RemoveAt(0);
			nextParam.NextSimulation = param;
			param = nextParam;
			param.OnSimulationEnd += Grid2D;
		}
		World.Init(param);

	
	}
	
	private double[,] heatMapDataCoop = new double[11, 9];
	private double[,] heatMapDataDefected = new double[11, 9];
	private double[,] heatMapDataBetrayed= new double[11, 9];

	private void Grid2D(object sender, List<World.Details> e)
	{
		var simulationParameters = sender as SimulationParameters;
		if (simulationParameters == null) return;

		// Create a new plot model for cooperation games
		var plotModelCoop = new PlotModel { Title = "2D Grid - Cooperation Games" };
		var palette = OxyPalette.Interpolate(100, OxyColors.Red, OxyColors.Lime);

		// Color axis for cooperation games
		plotModelCoop.Axes.Add(new LinearColorAxis
		{
			Palette = palette,
			Title = "Cooperation Level"
		});

		// X axis
		plotModelCoop.Axes.Add(new LinearAxis
		{
			Position = AxisPosition.Bottom,
			Title = "Global Rep Factor"
		});

		// Y axis
		plotModelCoop.Axes.Add(new LinearAxis
		{
			Position = AxisPosition.Left,
			Title = "Global Rep Interpolation Factor"
		});

		// Update the heat map data with a new data point for cooperation games
		int xIndex = (int)((simulationParameters.GlobalRepFactor+0.1)  * 10); // Adjust the scaling as needed
		int yIndex = (int)(simulationParameters.GlobalRepInterpolationFactor * 10) -1; // Adjust the scaling as needed

		if (xIndex >= 0 && xIndex < heatMapDataCoop.GetLength(0) && yIndex >= 0 && yIndex < heatMapDataCoop.GetLength(1))
		{
			float coop = (float)e.Last().CoopGames / (float)(e.Last().CoopGames + e.Last().BetrayedGames + e.Last().DefectedGames);
			heatMapDataCoop[xIndex, yIndex] = coop;
			float defected = (float)e.Last().DefectedGames / (float)(e.Last().CoopGames + e.Last().BetrayedGames + e.Last().DefectedGames);
			heatMapDataDefected[xIndex, yIndex] = defected;
			float betrayed = (float)e.Last().BetrayedGames / (float)(e.Last().CoopGames + e.Last().BetrayedGames + e.Last().DefectedGames);
			heatMapDataBetrayed[xIndex, yIndex] = betrayed;
		}
		// Create a heat map series for cooperation games
		var heatMapSeriesCoop = new HeatMapSeries
		{
			X0 = -0.1,
			X1 = 0.9,
			Y0 = 0.1,
			Y1 = 0.9,
			Interpolate = false,
			RenderMethod = HeatMapRenderMethod.Bitmap,
			Data = heatMapDataCoop
		};

		// Add the heat map series to the plot model for cooperation games
		plotModelCoop.Series.Add(heatMapSeriesCoop);
		// Define corrected plot boundaries
		double xMin = -0.1;
		double xMax = 0.9;
		double yMin = 0.1;  // Changed from 0.0 to 0.1
		double yMax = 1.0;  // Changed from 0.9 to 1.0

		// Calculate grid size
		int xSize = heatMapDataCoop.GetLength(0);
		int ySize = heatMapDataCoop.GetLength(1);

		// Calculate cell sizes
		double cellWidth = (xMax - xMin) / xSize;
		double cellHeight = (yMax - yMin) / ySize;

		// Generate annotations
		for (int x = 0; x < xSize; x++)
		{
			for (int y = 0; y < ySize; y++)
			{
				double coopValue = heatMapDataCoop[x, y];
				double defectedValue = heatMapDataDefected[x, y];
				double betrayedValue = heatMapDataBetrayed[x, y];

				if (coopValue > 0 || defectedValue > 0 || betrayedValue > 0)
				{
					// Calculate exact center of cell with corrected Y coordinates
					double xPos = xMin + (x * cellWidth) + (cellWidth / 2);
					double yPos = yMin + (y * cellHeight) + (cellHeight / 2);

					string text = $"C:{coopValue:F2}\nD:{defectedValue:F2}\nB:{betrayedValue:F2}";

					var annotation = new TextAnnotation
					{
						Text = text,
						TextPosition = new DataPoint(xPos, yPos),
						TextHorizontalAlignment = HorizontalAlignment.Center,
						TextVerticalAlignment = VerticalAlignment.Top,
						TextColor = coopValue > 0.5 ? OxyColors.Black : OxyColors.White,
						Font = "Arial",
						FontSize = 12,
						Stroke = OxyColors.Black,
						StrokeThickness = 0
					};
					plotModelCoop.Annotations.Add(annotation);
				}
			}
		}
		// Save the cooperation games plot as a PNG file
		var pngExporterCoop = new PngExporter { Width = 1280, Height = 720 };
		using (var stream = new FileStream($"grid_coop.png", FileMode.Create))
		{
			pngExporterCoop.Export(plotModelCoop, stream);
		}

	}


	protected override void LoadContent()
	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);
	}

	protected override void Update(GameTime gameTime)
	{
		if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			Exit();

		Camera.Update(gameTime);
		
		World.Update(gameTime);
		
		

		base.Update(gameTime);
	}
	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.DarkGray);
		
		World.Draw(_spriteBatch, gameTime);


		base.Draw(gameTime);
	}
}