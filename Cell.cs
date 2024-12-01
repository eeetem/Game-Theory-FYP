using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game_Theory_FYP;

public class Cell : IComparable<Cell>
{
	public int Score;
	public readonly Point position;
	
	//key, you coop, opponent coop
	public readonly ConcurrentDictionary<Cell,(bool,bool)> AlreadyPlayed = new ConcurrentDictionary<Cell, (bool,bool)>();


	public float CooperationChance = 0.5f;
	public float ReputationFactor = 0f;
	
	public float CooperationChanceCache = 0f;
	public float ReputationFactorCache  = 0f;
	
	

	
	
	const float MutationFactor = 0.1f;
	public const float ReputationInterpolationFactor = 0.3f;
	const float interpolationFactor = 0.7f;
	
	
	public const bool RepEnabled = true;
	public const bool EvolveRep = true;
	public const bool DiscreteStrategy = false;
	public const bool ReputationTelling = RepEnabled && false;
	public const bool NoRandomCooperate = true;

	public readonly ConcurrentDictionary<Cell,float> KnownReputations = new ConcurrentDictionary<Cell, float>();
	readonly ConcurrentDictionary<Cell,float> _trust = new ConcurrentDictionary<Cell, float>();


	public object lockObject = new object();

	public Cell(Point position)
	{
		this.position = position;
		

		if (DiscreteStrategy)
		{
			//CooperationChance = Random.Shared.NextInt64(0, 2);
			CooperationChance = 1;
		}
		else
		{
			CooperationChance = Random.Shared.NextSingle();
		}

		if (RepEnabled)
			ReputationFactor = Random.Shared.NextSingle();
	}

	public void InitiliaseRep(List<Cell> neighbours)
	{
		foreach (var n in neighbours)
		{
			KnownReputations.TryAdd(n, 0.5f);
		}
	}

	public Color GetColor()
	{
		return new Color(1-coopPercent, coopPercent, 0);
	}
	public Color GetColorOutline()
	{
		if (!RepEnabled)
		{
			return new Color(1-CooperationChance, CooperationChance, 0);
		}
		var normRep = (ReputationFactor + 1)/2;
		return new Color(1-normRep, normRep, 0);
	}

	public int CompareTo(Cell other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (other is null) return 1;
		return Score.CompareTo(other.Score);
	}
	public void AdjustReputation(Cell opponent, bool opponentCooperated)
	{
		if(!RepEnabled) return;
		var currentRep = KnownReputations[opponent];
		var newRep = Single.Lerp(currentRep, opponentCooperated ? 1 : 0, ReputationInterpolationFactor);
		newRep = Single.Clamp(newRep, 0, 1);
		KnownReputations[opponent] = newRep;

	}
	public bool CooperateOrNot(Cell oponent)
	{
		
		float chance = CooperationChance;
		if (RepEnabled)
		{
			
			if (ReputationFactor > 0)
			{
				chance = Single.Lerp(chance, KnownReputations[oponent], ReputationFactor);
			}
			else
			{ //todo stop from going too far
				bool positiveChangeForRep = KnownReputations[oponent] - chance > 0;
				float negativeTarget = positiveChangeForRep ? 0 : 1;
				float maxDist = Math.Abs(chance -  KnownReputations[oponent]);
				float targetDist = Math.Abs(chance - negativeTarget);
				if(targetDist > maxDist)
					 negativeTarget = chance + (positiveChangeForRep ? -maxDist : maxDist);
				
				chance = Single.Lerp(chance, negativeTarget, -ReputationFactor);
			
			}
		}

		float val = 0.5f;
		if(!NoRandomCooperate)
			val = Random.Shared.NextSingle();
		return val < chance;
	}

	public void UpdateStrategy(Cell candidate)
	{

		if (DiscreteStrategy)
		{
			CooperationChance = candidate.CooperationChanceCache;
		}
		else
		{
			CooperationChance = Single.Lerp(CooperationChance, candidate.CooperationChanceCache, interpolationFactor);
			CooperationChance += (float)(Random.Shared.NextDouble()-0.5)*2 * MutationFactor;
			CooperationChance = Math.Clamp(CooperationChance, 0, 1);
		}
	
		
		if(!RepEnabled || !EvolveRep) return;
		
		ReputationFactor = Single.Lerp(ReputationFactor, candidate.ReputationFactorCache, interpolationFactor);
		ReputationFactor += (float)(Random.Shared.NextDouble()-0.5)*2 * MutationFactor;
		ReputationFactor = Math.Clamp(ReputationFactor, -1, 1);
	
	}

	public void TellReputation(Cell teller, Cell opponent, bool opponentCooperated)
	{
		if(!ReputationTelling) return;
		
		if(!KnownReputations.ContainsKey(opponent)) return;
		var trust = _trust[teller];
	//	var changeMagnitude = opponentCooperated ? BaseRepChange : -BaseRepChange;
	///	changeMagnitude *= trust;
		//KnownReputations[opponent] += changeMagnitude;
		//KnownReputations[opponent] = Math.Clamp(KnownReputations[opponent], -1, 1);
		
	}

	public void CacheTrust()
	{
		_trust.Clear();
		foreach (var kvp in KnownReputations)
		{
			_trust.TryAdd(kvp.Key, kvp.Value);
		}
	}

	public void CacheStrategy()
	{
		CooperationChanceCache = CooperationChance;
		ReputationFactorCache = ReputationFactor;
	}
	
	float coopPercent = 0.5f;
	public void CalcCoopPercent()
	{
		int cooped = 0;
		foreach (var cell in new ConcurrentDictionary<Cell, (bool,bool)>(AlreadyPlayed))
		{
			if (cell.Value.Item1)
			{
				cooped++;
			}
		}
		coopPercent = cooped / (float) AlreadyPlayed.Count;
	}
}