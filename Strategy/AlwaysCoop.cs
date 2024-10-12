using Microsoft.Xna.Framework;

namespace Game_Theory_FYP;

public class AlwaysCoop : GameStrategy
{
	public override bool CooperateOrNot()
	{
		return true;
	}

	public override Color GetDisplayColor()
	{
		return Color.Green;
	}
}