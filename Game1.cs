using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Game_Theory_FYP;

public class Game1 : Game
{
	private GraphicsDeviceManager _graphics;
	private SpriteBatch _spriteBatch;

	public Game1()
	{
		_graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;
		Window.AllowUserResizing = true;
		_graphics.SynchronizeWithVerticalRetrace = false;
		IsFixedTimeStep = true;
		_graphics.ApplyChanges();

	}

	protected override void Initialize()
	{
		base.Initialize();
		Camera.Init(GraphicsDevice,Window);
		TextRenderer.Init(Content,GraphicsDevice);
		Cell.Init(GraphicsDevice);
		World.Init();
		
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
		
		var framerate = (1 / gameTime.ElapsedGameTime.TotalSeconds);
		
		Console.WriteLine(framerate);

		base.Draw(gameTime);
	}
}