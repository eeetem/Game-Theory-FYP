using System;
using Microsoft.Xna.Framework;

namespace Game_Theory_FYP;

public class FiftyFifty : GameStrategy
{
	public override bool CooperateOrNot()
	{
		var val = Random.Shared.NextDouble();
		return val < 0.5;
	}
	public override Color GetDisplayColor()
	{
		return Color.Yellow;
	}
}