using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Game_Theory_FYP;

public static class TextRenderer
{
	private static ConcurrentDictionary<string, Texture2D> Textures = new ConcurrentDictionary<string, Texture2D>();

	private static readonly List<string> MissingTextures = new List<string>();
	private static ContentManager content = null!;
	private static GraphicsDevice graphicsDevice = null!;

	private static readonly object SyncObj = new object();
	public static void Init(ContentManager contentManager, GraphicsDevice g)
	{
		content = contentManager;
		graphicsDevice = g;
	}
	public static void DrawText(this SpriteBatch spriteBatch, string text, Vector2 position, Color c)
	{
		DrawText(spriteBatch,  text,  position, 1,100,  c);
	}
	public static void DrawText(this SpriteBatch spriteBatch, string text, Vector2 position,float scale, Color c)
	{
		DrawText(spriteBatch,  text,  position, scale,100,  c);
	}
	
	public static void DrawText(this SpriteBatch spriteBatch, string text, Vector2 position, float scale, int width, Color c)
	{
		int row = 0;
		int charsinRow = 0;
		Color originalColor = c;

		for (int index = 0; index < text.Length; index++)
		{
			char car = text[index];
			car = Char.ToLower(car);
			

			if (car == '\n')
			{
				row++;
				charsinRow = 0;
				continue;
			}

			if (car == '[')
			{
				//extrat color
				string color = "";
				for (int i = index + 1; i < text.Length; i++)
				{
					if (text[i] == ']')
					{
						index = i;
						break;
					}
					color += text[i];
					index = i+1;
				}

				if (color == "-")
				{
					c = originalColor;
				}
				else
				{
					var prop = typeof(Color).GetProperty(color);
					if (prop != null)
						c = (Color)(prop.GetValue(null, null) ?? Color.White);
					
				}
				continue;


			}

			if (car == ' ')
			{
				int nextSpaceCounter = 0;
				int nextSpace = 0;
				//look for next space
				for (int i = index+1; i < text.Length; i++)
				{
					nextSpaceCounter++;
					if (text[i] == ' ' || text[i] == '\n' || text[i] == '[')
					{
						nextSpace = nextSpaceCounter;
						break;
					}

				}

				if (charsinRow + nextSpace > width)
				{
					row++;
					charsinRow = 0;
					continue;
				}
			}
			if (charsinRow > width)
			{
				row++;
				charsinRow = 0;
			}

			if(car == ' ')
			{
				charsinRow++;
				continue;
			}

			var t = GetTextTexture(car);

			spriteBatch.Draw(t, position + new Vector2(8 * charsinRow, 11 * row) * scale, null, c, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
			charsinRow++;
		}
	}
	public static Texture2D GetTextTexture(char c)
	{
		string texId;
		switch (c)
		{
			case ' ':
				return GetTexture("");
			case '.':
				texId = "period";
				break;
			case ',':
				texId = "comma";
				break;
			case '+':
				texId = "plus";
				break;
			case '-':
				texId = "minus";
				break;
			case '!':
				texId = "exclamationmark";
				break;
			case '?':
				texId = "questionmark";
				break;
			case ':':
				texId = "colon";
				break;
			case ';':
				texId = "semicolon";
				break;
			case '\'':
				texId = "apostrophe";
				break;
			case '(':
				texId = "leftParentheses";
				break;
			case ')':
				texId = "rightParentheses";
				break;
			case '#':
				texId = "hash";
				break;
			case '=':
				texId = "equal";
				break;
			case '\"':
				texId = "quote";
				break;
			case '\\':
				texId = "backslash";
				break;
			case '/':
				texId = "forwardslash";
				break;
			case '_':
				texId = "underscore";
				break;
			default:
				texId = "" + c;
				texId = texId.ToLowerInvariant();
				break;
		}

		Texture2D t;
		if(HasTexture("text/" + texId)){
			t= GetTexture("text/" + texId);
		}else{
			t = GetTexture("text/broken");
			
		}

		return t;
	}

	public static bool HasTexture(string name)
	{
		if (Textures.ContainsKey(name))
		{
			return true;
		}

		if (MissingTextures.Contains(name))
		{
			return false;
		}


		try
		{
			GetTexture(name);
			return true;
		}
		catch (ContentLoadException)
		{
			MissingTextures.Add(name);
			return false;
		}
	}

	public static Texture2D GetTexture(string name)
		{
	
			if (Textures.TryGetValue(name, out var texture))
			{
				return texture;
			}
			if (name != "")
			{
			
				Textures.TryAdd(name, content.Load<Texture2D>(name));
			
			}
			else
			{
				var tex = new Texture2D(graphicsDevice, 1, 1);
				//make it white
				var data = new Color[1];
				for (int i = 0; i < data.Length; ++i) data[i] = Color.White;
				tex.SetData(data);
		
				Textures.TryAdd(name, tex);
			
			}
		

			return Textures[name];
		
		}
}