using Microsoft.Xna.Framework;

namespace Game_Theory_FYP;

public class AlwaysDefect : GameStrategy
{
	public override bool CooperateOrNot()
	{
		return false;
	}

	public override Color GetDisplayColor()
	{
		return Color.Red;
	}
}