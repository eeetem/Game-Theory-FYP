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

	
	
	const float MutationFactor = 0.01f;
	public const float BaseRepChange = 0.1f;

	public readonly ConcurrentDictionary<Cell,float> KnownReputations = new ConcurrentDictionary<Cell, float>();
	readonly ConcurrentDictionary<Cell,float> _trust = new ConcurrentDictionary<Cell, float>();


	public object lockObject = new object();

	public Cell(Point position)
	{
		this.position = position;
		CooperationChance = (float) Random.Shared.NextDouble();
		ReputationFactor = (float) (Random.Shared.NextDouble() * 2 - 1.0);
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
		var coopPercent = 0.5f;
		int cooped = 0;
		foreach (var cell in new ConcurrentDictionary<Cell, (bool,bool)>(AlreadyPlayed))
		{
			if (cell.Value.Item1)
			{
				cooped++;
			}
		}
		coopPercent = cooped / (float) AlreadyPlayed.Count;
		return new Color(1-coopPercent, coopPercent, 0);
	}
	public Color GetColorRep()
	{
		var normRep = (ReputationFactor + 1)/2;
		return new Color(1-normRep, normRep, 0);
	}

	public int CompareTo(Cell other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (other is null) return 1;
		return Score.CompareTo(other.Score);
	}

	public bool CooperateOrNot(Cell oponent)
	{
		var chance = CooperationChance;
		chance += ReputationFactor * KnownReputations[oponent];
		var val = Random.Shared.NextDouble();
		return val < chance;
	}

	public void UpdateStrategy(Cell candidate)
	{
		CooperationChance = candidate.CooperationChanceCache;
		CooperationChance += (float)(Random.Shared.NextDouble()-0.5) * MutationFactor;
		CooperationChance = Math.Clamp(CooperationChance, 0, 1);
		
		ReputationFactor = candidate.ReputationFactorCache;
		ReputationFactor += (float)(Random.Shared.NextDouble()-0.5) * MutationFactor;
		ReputationFactor = Math.Clamp(ReputationFactor, -1, 1);
	
	}

	public void TellReputation(Cell teller, Cell opponent, bool opponentCooperated)
	{
		if(!KnownReputations.ContainsKey(opponent)) return;
		var trust = _trust[teller];
		var changeMagnitude = opponentCooperated ? BaseRepChange : -BaseRepChange;
		changeMagnitude *= trust;
		KnownReputations[opponent] += changeMagnitude;
		KnownReputations[opponent] = Math.Clamp(KnownReputations[opponent], -1, 1);
		
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
}