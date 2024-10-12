using System;
using Microsoft.Xna.Framework;

namespace Game_Theory_FYP;

public abstract class GameStrategy
{
	public static GameStrategy GetRandom()
	{
		var r = Random.Shared.NextInt64(4);
		switch (r)
		{
			default:
			case 0:
				return new AlwaysCoop();
			case 1:
				return new AlwaysDefect();
			case 2:
				return new FiftyFifty();
			case 3:
				return new RandomBetreyal();
		}
		
	}

	public abstract bool CooperateOrNot();
	public abstract Color GetDisplayColor();

}