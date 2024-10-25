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
	
	public readonly Dictionary<Cell,bool> AlreadyPlayed = new Dictionary<Cell, bool>();


	public float CooperationChance = 0.5f;
	public float ReputationFactor = 0f;
	
	public float CooperationChanceCache = 0f;
	public float ReputationFactorCache  = 0f;

	
	
	const float MutationFactor = 0.01f;
	const float BaseRepChange = 0.1f;

	readonly ConcurrentDictionary<Cell,float> _knownReputations = new ConcurrentDictionary<Cell, float>();
	readonly ConcurrentDictionary<Cell,float> _trust = new ConcurrentDictionary<Cell, float>();


	public object lockObject = new object();

	public Cell(Point position)
	{
		this.position = position;
		CooperationChance = (float) Random.Shared.NextDouble();
		ReputationFactor = 1f;
	}

	public void InitiliaseRep(List<Cell> neighbours)
	{
		foreach (var n in neighbours)
		{
			_knownReputations.TryAdd(n, 0.5f);
		}
	}

	public Color GetColor()
	{
		return new Color(1-CooperationChance, CooperationChance, 0);
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
		chance += ReputationFactor * _knownReputations[oponent];
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
		if(!_knownReputations.ContainsKey(opponent)) return;
		var trust = _trust[teller];
		var changeMagnitude = opponentCooperated ? BaseRepChange : -BaseRepChange;
		changeMagnitude *= trust;
		_knownReputations[opponent] += changeMagnitude;
		_knownReputations[opponent] = Math.Clamp(_knownReputations[opponent], -1, 1);
		
	}

	public void CacheTrust()
	{
		_trust.Clear();
		foreach (var kvp in _knownReputations)
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