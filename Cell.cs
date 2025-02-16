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
	public float ReputationInterpolationFactor = 0.3f;
	
	public float CooperationChanceCache = 0f;
	public float ReputationFactorCache  = 0f;
	public float ReputationInterpolationFactorCache  = 0f;
	




	private const float MutationFactor = 0.1f;
	private const float EvolutionInterpolationFactor = 0.7f;
	
	public const bool DiscreteStrategy = false;
	public const bool NoRandomCooperate = true;

	public readonly ConcurrentDictionary<Cell,float> KnownReputations = new ConcurrentDictionary<Cell, float>();

	public object lockObject = new object();

	public List<Cell> Neighbours = new List<Cell>();


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

		if (World.CurrentParams.RepEnabled)
		{
			if (World.CurrentParams.GlobalRepFactor < -1f)
			{
				
				ReputationFactor = Random.Shared.NextSingle() * 2 - 1;
			}
			else
			{
				ReputationFactor = World.CurrentParams.GlobalRepFactor;
			}
			
			if(World.CurrentParams.GlobalRepInterpolationFactor< -1f)
			{
				ReputationInterpolationFactor = Random.Shared.NextSingle();
			}
			else
			{
				ReputationInterpolationFactor = World.CurrentParams.GlobalRepInterpolationFactor;
			}
			
		}
		
	}

	public void InitiliaseRep()
	{
		Neighbours = World.CalculateNeighbourhs(this);
		foreach (var n in Neighbours)
		{
			KnownReputations.TryAdd(n, 0.5f);
		}
	}
	public int GamesThisGeneration = 0;
	public int CoopThisGeneration = 0;
	private Color lastC = Color.Black;
	public Color GetColor()
	{
		if(GamesThisGeneration == 0) return lastC;
		float coopPercent = CoopThisGeneration / (float)GamesThisGeneration;
		lastC = new Color(1-coopPercent, coopPercent, 0);
		return lastC;
	}
	public Color GetColorOutline()
	{
		if (!World.CurrentParams.RepEnabled)
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
		if(!World.CurrentParams.RepEnabled) return;
		var currentRep = KnownReputations[opponent];
		var newRep = Single.Lerp(currentRep, opponentCooperated ? 1 : 0, ReputationInterpolationFactor);
		newRep = Single.Clamp(newRep, 0, 1);
		KnownReputations[opponent] = newRep;

	}
	public bool CooperateOrNot(Cell oponent)
	{
		
		float chance = CooperationChance;
		if (World.CurrentParams.RepEnabled)
		{
			
			if (ReputationFactor > 0)
			{
				chance = Single.Lerp(chance, KnownReputations[oponent], ReputationFactor);
			}
			else
			{
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

	public bool CanEvolve = true;
	public void UpdateStrategy(Cell candidate)
	{
		if (!CanEvolve) return;
		CanEvolve = false;
		if (DiscreteStrategy)
		{
			CooperationChance = candidate.CooperationChanceCache;
		}
		else
		{
			CooperationChance = Single.Lerp(CooperationChance, candidate.CooperationChanceCache, EvolutionInterpolationFactor);
			CooperationChance += (float)(Random.Shared.NextDouble()-0.5)*2 * MutationFactor;
			CooperationChance = Math.Clamp(CooperationChance, 0, 1);
			
		}
	
		
		if(!World.CurrentParams.RepEnabled) return;

		if (World.CurrentParams.EvolveRep)
		{
			ReputationFactor = Single.Lerp(ReputationFactor, candidate.ReputationFactorCache, EvolutionInterpolationFactor);
			ReputationFactor += (float) (Random.Shared.NextDouble() - 0.5) * 2 * MutationFactor;
			ReputationFactor = Math.Clamp(ReputationFactor, -1, 1f);
		}
		if (World.CurrentParams.EvolveInterpolation)
		{
			ReputationInterpolationFactor = Single.Lerp(ReputationInterpolationFactor, candidate.ReputationInterpolationFactorCache, EvolutionInterpolationFactor);
			ReputationInterpolationFactor += (float) (Random.Shared.NextDouble() - 0.5) * 2 * MutationFactor;
			ReputationInterpolationFactor = Math.Clamp(ReputationInterpolationFactor, 0f, 1f);
		}
		
	}

	



	public void CacheStrategy()
	{
		CooperationChanceCache = CooperationChance;
		ReputationFactorCache = ReputationFactor;
		ReputationInterpolationFactorCache = ReputationInterpolationFactor;
	}

	private float coopPercent = 0.5f;
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
