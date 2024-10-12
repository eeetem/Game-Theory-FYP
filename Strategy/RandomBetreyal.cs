using System;
using Microsoft.Xna.Framework;

namespace Game_Theory_FYP;

public class RandomBetreyal : GameStrategy
{
	public override bool CooperateOrNot()
	{
		var val = Random.Shared.NextDouble();
		return val < 0.9; //cooperate 90% of the time
	}
	public override Color GetDisplayColor()
	{
		return Color.Peru;
	}
}