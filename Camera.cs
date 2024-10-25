using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;

namespace Game_Theory_FYP;

public static class Camera
{

	private static OrthographicCamera Cam { get; set; } = null!;
	public readonly static AudioListener AudioListener = new AudioListener();

	private static Vector2 velocity = new Vector2();
	private static float zoomVelocity = 0;


	public static void Init(GraphicsDevice graphicsDevice, GameWindow window)
	{
		var viewportAdapter = new BoxingViewportAdapter(window, graphicsDevice, window.ClientBounds.Width, window.ClientBounds.Height);
		Cam = new OrthographicCamera(viewportAdapter);
		Cam.MinimumZoom = window.ClientBounds.Width / 30000f;
		Cam.MaximumZoom =  window.ClientBounds.Width/400f;
		Cam.Position = MoveTarget;
	}

		
	public static Vector2 GetPos()
	{
		return Cam.Center;
	}
	public static float GetZoom()
	{
		return Cam.Zoom;
	}

	//function that checks if a specfic position is visible
	//public static bool IsOnScreen(Vector2Int vec)
	//{
	//	vec = Utility.GridToWorldPos(vec);
	//	return Cam.BoundingRectangle.Contains((Vector2)vec);
	//}

	public static Matrix GetViewMatrix()
	{
		return Cam.GetViewMatrix();
	}

	static Vector2 MoveTarget;
	public static bool ForceMoving { get; private set; }
	public static bool noCancel = false;
	private static float preForceZoom;

	//public static void SetPos(Point vec, bool force = false)
	//{
//
	//	vec = Utility.GridToWorldPos(vec);
	//	//vec.X -= Cam.BoundingRectangle.Width / 2;
	//	//	vec.Y -= Cam.BoundingRectangle.Height / 2;
	//	MoveTarget = vec - (Point) Cam.Origin;
	//	noCancel = force;
	//	ForceMoving = true;
//
	//}


	private static Vector2 lastMousePos;
	private static Vector2 GetMovementDirection()
	{
		var state = Keyboard.GetState();
		var mouseState = Mouse.GetState();
		if (mouseState.MiddleButton == ButtonState.Pressed && !noCancel)
		{
			var lastpos = lastMousePos;
			lastMousePos = new Vector2(mouseState.Position.X,mouseState.Position.Y);
			if (lastpos != new Vector2(0, 0))
			{
				ForceMoving = false;
				return Vector2.Clamp((lastpos - new Vector2(mouseState.Position.X, mouseState.Position.Y)) / 15f, new Vector2(-9, -9), new Vector2(9, 9));
			}
		}
		else
		{
			lastMousePos = new Vector2(0,0);
		}

		var movementDirection = Vector2.Zero;
		if (state.IsKeyDown(Keys.S))
		{
			movementDirection += Vector2.UnitY;
		}
		if (state.IsKeyDown(Keys.W))
		{
			movementDirection -= Vector2.UnitY;
		}
		if (state.IsKeyDown(Keys.A))
		{
			movementDirection -= Vector2.UnitX;
		}
		if (state.IsKeyDown(Keys.D))
		{
			movementDirection += Vector2.UnitX;
		}

		if (movementDirection.Length() != 0 && !noCancel)
		{
			ForceMoving = false;
			return movementDirection;
		}//overrideforcemove

		if (ForceMoving)
		{
			Vector2 difference = MoveTarget - Cam.Position;
			if (difference.Length() < 25)
			{
				ForceMoving = false;
				noCancel = false;
			}
			difference = difference / 400f;
				
				
			var vec =  Vector2.Clamp(difference,new Vector2(-3,-3),new Vector2(3,3));
			return vec;
		}
			
			
		return movementDirection;
	}

	public static Vector2 GetMouseWorldPos()
	{
		var state = Mouse.GetState();
		return Vector2.Transform(new Vector2(state.Position.X, state.Position.Y), Cam.GetInverseViewMatrix());
	}


	private static int lastScroll;
	public static void Update(GameTime gameTime)
	{
		var state = Mouse.GetState();
		float diff = (state.ScrollWheelValue - lastScroll);
		if (Keyboard.GetState().IsKeyDown(Keys.OemPlus))
		{
			diff = 25;
		}else if (Keyboard.GetState().IsKeyDown(Keys.OemMinus))
		{
			diff = -25;
		}
		
		diff*=(Cam.Zoom/3000);
		lastScroll = state.ScrollWheelValue;
		zoomVelocity += diff*gameTime.GetElapsedSeconds()*25f;
		Cam.ZoomIn(zoomVelocity);
		zoomVelocity *= gameTime.GetElapsedSeconds()*45;

		float movementSpeed = 400*(Cam.MaximumZoom/Cam.Zoom);
		Vector2 move = GetMovementDirection();
		velocity += move*gameTime.GetElapsedSeconds()*25f* movementSpeed;
		Cam.Move(velocity  * gameTime.GetElapsedSeconds());
		//Cam.Position = Vector2.Clamp(Cam.Position, new Vector2(-1000, -1000), new Vector2(10000, 10000));
		velocity *= gameTime.GetElapsedSeconds()*45;

		AudioListener.Position =  new Vector3(Cam.Center,0);
		AudioListener.Velocity = new Vector3(velocity,10);
	}


	
	public static RectangleF GetBoundingRectangle()
{
    var boundingRectangle = Cam.BoundingRectangle;

    // Define the margin
    float margin = 55; // Adjust this value to your needs

    // Create a new rectangle with the margin
    var expandedRectangle = new RectangleF(
        boundingRectangle.Left - margin,
        boundingRectangle.Top - margin,
        boundingRectangle.Width + 2 * margin,
        boundingRectangle.Height + 2 * margin
    );

    return expandedRectangle;
}

	public static double GetZoomLevel()
	{
		return Cam.Zoom;
	}

	public static Vector2 ScreenToWorld(Vector2 screenPosition)
	{
		// Transform the screen position to world coordinates using the camera's inverse view matrix
		return Vector2.Transform(screenPosition, Cam.GetInverseViewMatrix());
	}

}