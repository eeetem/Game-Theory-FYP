using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace Game_Theory_FYP;

public class Cell : IComparable<Cell>
{
	private const float MutationFactor = 0.05f;
	private const float EvolutionInterpolationFactor = 0.7f;

	public const bool NoRandomCooperate = true;

	//key, you coop, opponent coop
	public readonly ConcurrentDictionary<Cell, ValueTuple<bool, bool>> AlreadyPlayed = new();

	public readonly ConcurrentDictionary<Cell, float> KnownReputations = new();
	public readonly Point Position;

	public bool CanEvolve = true;


	public float CooperationChance = 0.5f;
	private float _cooperationChanceCache;
	
	public int CoopThisGeneration = 0;
	public int GamesThisGeneration = 0;
	public float IndependantVar;
	private float _indepententVarCahce;
	private Color _lastC = Color.Black;

	public readonly object LockObject = new();

	public List<Cell> Neighbours = new();
	public float ReputationFactor;
	private float _reputationFactorCache;
	public float ReputationInterpolationFactor = 0.3f;
	private float _reputationInterpolationFactorCache;
	public int Score;


	public Cell(Point position)
	{
		this.Position = position;
		IndependantVar = Random.Shared.NextSingle(World.CurrentParams.GlobalCoopFactorRangeStart, World.CurrentParams.GlobalCoopFactorRangeEnd);

		CooperationChance = Random.Shared.NextSingle(World.CurrentParams.GlobalCoopFactorRangeStart, World.CurrentParams.GlobalCoopFactorRangeEnd);

		if (World.CurrentParams.RepEnabled)
		{
			ReputationFactor = Random.Shared.NextSingle(World.CurrentParams.GlobalRepFactorRangeStart, World.CurrentParams.GlobalRepFactorRangeEnd);
			ReputationInterpolationFactor = Random.Shared.NextSingle(World.CurrentParams.GlobalRepInterpolationFactorRangeStart, World.CurrentParams.GlobalRepInterpolationFactorRangeEnd);
		}
	}

	public int CompareTo(Cell other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (other is null) return 1;
		return Score.CompareTo(other.Score);
	}


	public void InitiliaseRep()
	{
		Neighbours = World.CalculateNeighbourhs(this);
		foreach (var n in Neighbours) KnownReputations.TryAdd(n, 0.5f);
	}

	public Color GetColor()
	{
		if (GamesThisGeneration == 0) return _lastC;
		float coopPercent = CoopThisGeneration / (float) GamesThisGeneration;
		_lastC = new Color(1 - coopPercent, coopPercent, 0);
		return _lastC;
	}

	public Color GetColorOutline()
	{
		if (!World.CurrentParams.RepEnabled) return new Color(1 - CooperationChance, CooperationChance, 0);
		float normRep = (ReputationFactor + 1) / 2;
		return new Color(1 - normRep, normRep, 0);
	}

	public void AdjustReputation(Cell opponent, bool opponentCooperated)
	{
		if (!World.CurrentParams.RepEnabled) return;
		float currentRep = KnownReputations[opponent];
		float newRep = float.Lerp(currentRep, opponentCooperated ? 1 : 0, Math.Clamp(ReputationInterpolationFactor, 0f, 1f));
		newRep = float.Clamp(newRep, 0, 1);
		KnownReputations[opponent] = newRep;
	}

	public bool CooperateOrNot(Cell oponent)
	{
		float chance = Math.Clamp(CooperationChance, 0f, 1f);

		if (World.CurrentParams.RepEnabled)
		{
			if (ReputationFactor > 0)
			{
				chance = float.Lerp(chance, KnownReputations[oponent], ReputationFactor);
			}
			else
			{
				bool positiveChangeForRep = KnownReputations[oponent] - chance > 0;
				float negativeTarget = positiveChangeForRep ? 0 : 1;
				float maxDist = Math.Abs(chance - KnownReputations[oponent]);
				float targetDist = Math.Abs(chance - negativeTarget);
				if (targetDist > maxDist)
					negativeTarget = chance + (positiveChangeForRep ? -maxDist : maxDist);

				chance = float.Lerp(chance, negativeTarget, -ReputationFactor);
			}
		}


		float val = 0.5f;
		if (!NoRandomCooperate)
			val = Random.Shared.NextSingle();
		return val < chance;
	}

	public void UpdateStrategy(Cell candidate)
	{
		if (!CanEvolve) return;
		CanEvolve = false;
		
		CooperationChance = float.Lerp(CooperationChance, candidate._cooperationChanceCache, EvolutionInterpolationFactor);
		//CooperationChance += (float)(Random.Shared.NextDouble()-0.5)*2 * MutationFactor;


		IndependantVar = float.Lerp(IndependantVar, candidate._indepententVarCahce, EvolutionInterpolationFactor);
		//IndependantVar += (float)(Random.Shared.NextDouble()-0.5)*2 * MutationFactor;
		
		Clamp();
		if (!World.CurrentParams.RepEnabled) return;

		if (World.CurrentParams.EvolveRep) ReputationFactor = float.Lerp(ReputationFactor, candidate._reputationFactorCache, EvolutionInterpolationFactor);
		//ReputationFactor += (float) (Random.Shared.NextDouble() - 0.5) * 2 * MutationFactor;

		if (World.CurrentParams.EvolveInterpolation) ReputationInterpolationFactor = float.Lerp(ReputationInterpolationFactor, candidate._reputationInterpolationFactorCache, EvolutionInterpolationFactor);
		//ReputationInterpolationFactor += (float) (Random.Shared.NextDouble() - 0.5) * 2 * MutationFactor;
		Clamp();
	}


	public void CacheStrategy()
	{
		_cooperationChanceCache = CooperationChance;
		_reputationFactorCache = ReputationFactor;
		_reputationInterpolationFactorCache = ReputationInterpolationFactor;
		_indepententVarCahce = IndependantVar;
	}



	public void Mutate()
	{
		CooperationChance += (float) (Random.Shared.NextDouble() - 0.5) * 2 * MutationFactor;
		IndependantVar += (float) (Random.Shared.NextDouble() - 0.5) * 2 * MutationFactor;
		if (!World.CurrentParams.RepEnabled) return;

		if (World.CurrentParams.EvolveRep) ReputationFactor += (float) (Random.Shared.NextDouble() - 0.5) * 2 * MutationFactor;

		if (World.CurrentParams.EvolveInterpolation) ReputationInterpolationFactor += (float) (Random.Shared.NextDouble() - 0.5) * 2 * MutationFactor;

		Clamp();
	}
	
	public void Clamp()
	{
		CooperationChance = Math.Clamp(CooperationChance, 0, 1);
		IndependantVar = Math.Clamp(IndependantVar, 0, 1);
		if (!World.CurrentParams.RepEnabled) return;
		ReputationFactor = Math.Clamp(ReputationFactor, 0, 1);
		ReputationInterpolationFactor = Math.Clamp(ReputationInterpolationFactor, 0, 1);
	}
}