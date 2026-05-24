using Godot;
using System;
using System.Collections.Generic;

#nullable enable

[Tool]
public partial class World : Node2D
{
	private const int TileSize = 32;
	private static readonly ResourceSpawnRule[] s_resourceSpawnRules =
	{
		new(
			ResourceType.Fish,
			CanSpawnFish,
			GetFishSpawnMultiplier,
			world => world.FishSpawnChance,
			world => world.FishMinAmount,
			world => world.FishMaxAmount),
		new(
			ResourceType.Grains,
			tile => tile.Biome == BiomeType.Grassland || tile.Biome == BiomeType.Hills,
			GetGrainsSpawnMultiplier,
			world => world.GrainsSpawnChance,
			world => world.GrainsMinAmount,
			world => world.GrainsMaxAmount),
		new(
			ResourceType.Stone,
			tile => tile.Biome == BiomeType.Hills || tile.Biome == BiomeType.Mountain,
			GetStoneSpawnMultiplier,
			world => world.StoneSpawnChance,
			world => world.StoneMinAmount,
			world => world.StoneMaxAmount),
		new(
			ResourceType.Iron,
			tile => tile.Biome == BiomeType.Hills || tile.Biome == BiomeType.Mountain,
			GetIronSpawnMultiplier,
			world => world.IronSpawnChance,
			world => world.IronMinAmount,
			world => world.IronMaxAmount)
	};

	[ExportGroup("Generation")]
	[Export(PropertyHint.Range, "1,2147483647,1")]
	public int Seed { get; set; } = 12345;

	[Export]
	public bool RandomizeSeedOnPlay { get; set; } = true;

	[Export(PropertyHint.Range, "1,512,1")]
	public int GridWidth { get; set; } = 100;

	[Export(PropertyHint.Range, "1,512,1")]
	public int GridHeight { get; set; } = 50;

	[Export(PropertyHint.Range, "0.001,0.25,0.001")]
	public float BaseNoiseFrequency { get; set; } = 0.05f;

	[Export(PropertyHint.Range, "0.001,0.25,0.001")]
	public float MountainRidgeFrequency { get; set; } = 0.035f;

	[Export(PropertyHint.Range, "0.001,0.25,0.001")]
	public float MountainRegionFrequency { get; set; } = 0.012f;

	[Export(PropertyHint.Range, "0.5,2.0,0.05")]
	public float MountainUpliftStrength { get; set; } = 1.20f;

	[ExportToolButton("Rebuild World")]
	public Callable RebuildWorldButton => new Callable(this, MethodName.RebuildWorldFromInspector);

	[ExportToolButton("Reroll Seed")]
	public Callable RerollSeedButton => new Callable(this, MethodName.RerollSeedAndRebuildFromInspector);

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float GrainsSpawnChance { get; set; } = 0.08f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float StoneSpawnChance { get; set; } = 0.07f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float IronSpawnChance { get; set; } = 0.03f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float FishSpawnChance { get; set; } = 0.18f;

	[ExportGroup("Biome Thresholds")]
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float LakeMaxElevation { get; set; } = TerrainRules.DefaultBiomeSettings.LakeMaxElevation;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float MountainMinElevationExclusive { get; set; } = 0.74f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float HillsMinElevationExclusive { get; set; } = TerrainRules.DefaultBiomeSettings.HillsMinElevationExclusive;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float DesertMaxMoistureExclusive { get; set; } = TerrainRules.DefaultBiomeSettings.DesertMaxMoistureExclusive;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float DesertMinTemperatureExclusive { get; set; } = TerrainRules.DefaultBiomeSettings.DesertMinTemperatureExclusive;

	[ExportGroup("Forest Thresholds")]
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float ForestMinMoistureExclusive { get; set; } = TerrainRules.DefaultForestSettings.MinMoistureExclusive;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float ForestMinTemperatureExclusive { get; set; } = TerrainRules.DefaultForestSettings.MinTemperatureExclusive;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float ForestMaxTemperatureExclusive { get; set; } = TerrainRules.DefaultForestSettings.MaxTemperatureExclusive;

	[ExportGroup("Lakes")]

	[Export]
	public bool EnableOrganicLakeBasins { get; set; } = true;

	[Export(PropertyHint.Range, "0,16,1")]
	public int LakeBasinMinGrowthTiles { get; set; } = 1;

	[Export(PropertyHint.Range, "0,48,1")]
	public int LakeBasinMaxGrowthTiles { get; set; } = 7;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float LakeBasinExpansionChance { get; set; } = 0.62f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float LakeBasinMaxElevation { get; set; } = 0.36f;

	[Export(PropertyHint.Range, "1,12,1")]
	public int LakeBasinMaxDistance { get; set; } = 4;

	[ExportGroup("Rivers")]

	[Export]
	public bool EnableRivers { get; set; } = true;

	[Export(PropertyHint.Range, "0,64,1")]
	public int RiverSourceCount { get; set; } = 8;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float RiverSourceMinElevation { get; set; } = 0.62f;

	[Export(PropertyHint.Range, "0,64,1")]
	public int RiverSourceMinSpacing { get; set; } = 7;

	[Export(PropertyHint.Range, "0,8,1")]
	public int RiverAvoidExistingRadius { get; set; } = 1;

	[Export(PropertyHint.Range, "0,2,0.05")]
	public float RiverTurnPenalty { get; set; } = 0.22f;

	[Export(PropertyHint.Range, "2,512,1")]
	public int RiverMinLength { get; set; } = 12;

	[Export(PropertyHint.Range, "4,2048,1")]
	public int RiverMaxLength { get; set; } = 220;

	[Export]
	public bool EnableRiverTerminalLakes { get; set; } = true;

	[Export(PropertyHint.Range, "1,48,1")]
	public int RiverTerminalLakeMinTiles { get; set; } = 3;

	[Export(PropertyHint.Range, "1,64,1")]
	public int RiverTerminalLakeMaxTiles { get; set; } = 12;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float RiverTerminalLakeExpansionChance { get; set; } = 0.68f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float RiverTerminalLakeMaxElevation { get; set; } = 0.72f;

	[ExportGroup("Resource Spawning")]

	[Export(PropertyHint.Range, "0.1,3,0.05")]
	public float ResourceSpawnDensity { get; set; } = 1.35f;

	[Export]
	public int GrainsMinAmount { get; set; } = 20;

	[Export]
	public int GrainsMaxAmount { get; set; } = 60;

	[Export]
	public int StoneMinAmount { get; set; } = 15;

	[Export]
	public int StoneMaxAmount { get; set; } = 50;

	[Export]
	public int IronMinAmount { get; set; } = 8;

	[Export]
	public int IronMaxAmount { get; set; } = 28;

	[Export]
	public int FishMinAmount { get; set; } = 12;

	[Export]
	public int FishMaxAmount { get; set; } = 36;

	private readonly PackedScene _tileScene = GD.Load<PackedScene>("res://tile.tscn");
	private Tile[,] _tiles = new Tile[1, 1];
	private Tile? _selectedTile;
	private readonly FastNoiseLite _elevationNoise = new();
	private readonly FastNoiseLite _moistureNoise = new();
	private readonly FastNoiseLite _temperatureNoise = new();
	private readonly FastNoiseLite _mountainRidgeNoise = new();
	private readonly FastNoiseLite _mountainRegionNoise = new();
	private readonly RandomNumberGenerator _resourceRng = new();
	private readonly RandomNumberGenerator _riverRng = new();
	private UI? _ui;
	private TerrainRules.BiomeSettings _biomeSettings = TerrainRules.DefaultBiomeSettings;
	private TerrainRules.ForestSettings _forestSettings = TerrainRules.DefaultForestSettings;
	private float _offsetX;
	private float _offsetY;
	private static readonly Vector2I[] s_riverStepOffsets =
	{
		new Vector2I(-1, -1),
		new Vector2I(0, -1),
		new Vector2I(1, -1),
		new Vector2I(1, 0),
		new Vector2I(1, 1),
		new Vector2I(0, 1),
		new Vector2I(-1, 1),
		new Vector2I(-1, 0)
	};

	public override void _Ready()
	{
		InitializeUi();

		if (!Engine.IsEditorHint())
		{
			if (RandomizeSeedOnPlay)
			{
				Seed = GenerateRuntimeSeed(Seed);
				GD.Print($"Runtime world seed: {Seed}");
			}

			RebuildWorld();
		}
	}

	private static int GenerateRuntimeSeed(int previousSeed)
	{
		var nextSeed = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue);
		if (nextSeed == previousSeed)
		{
			nextSeed = nextSeed == int.MaxValue - 1 ? nextSeed - 1 : nextSeed + 1;
		}

		return nextSeed;
	}

	public bool TrySelectTileAtWorld(Vector2 worldPosition)
	{
		var coordinates = WorldToGrid(worldPosition);
		var x = coordinates.X;
		var y = coordinates.Y;

		if (x < 0 || y < 0 || x >= CurrentGridWidth || y >= CurrentGridHeight)
		{
			ClearSelection();
			_ui?.HideActionPanel();
			return false;
		}

		SelectTile(_tiles[x, y]);
		return true;
	}

	private void SelectTile(Tile tile)
	{
		if (_selectedTile == tile)
		{
			PrintTileInfo(tile);
			return;
		}

		_selectedTile?.SetSelected(false);
		_selectedTile = tile;
		_selectedTile.SetSelected(true);
		_ui?.ShowTileInfo(_selectedTile);
		PrintTileInfo(_selectedTile);
	}

	private void ClearSelection()
	{
		if (_selectedTile is null)
		{
			return;
		}

		_selectedTile.SetSelected(false);
		_selectedTile = null;
	}

	private void InitializeGeneration()
	{
		_biomeSettings = BuildBiomeSettings();
		_forestSettings = BuildForestSettings();
		var rng = CreateSeededRng();
		_resourceRng.Seed = rng.Randi();
		_riverRng.Seed = rng.Randi();
		ConfigureNoise(rng);
	}

	private void RebuildWorld()
	{
		if (!IsInsideTree())
		{
			return;
		}

		ClearSelection();
		_ui?.HideActionPanel();
		ClearGeneratedTiles();
		InitializeGeneration();
		ComputeGridOffsets();
		GenerateWorld();
	}

	private void RebuildWorldFromInspector()
	{
		RebuildWorld();
	}

	private void RerollSeedAndRebuildFromInspector()
	{
		Seed = GenerateRuntimeSeed(Seed);
		NotifyPropertyListChanged();
		GD.Print($"Rerolled world seed: {Seed}");
		RebuildWorld();
	}

	private void InitializeUi()
	{
		_ui = GetNodeOrNull<UI>("HUD/UI");

		if (_ui is null)
		{
			GD.PrintErr("UI node not found. Tile info display will be disabled.");
		}
	}

	private void ComputeGridOffsets()
	{
		_offsetX = (CurrentGridWidth - 1) * TileSize * 0.5f;
		_offsetY = (CurrentGridHeight - 1) * TileSize * 0.5f;
	}

	private void GenerateWorld()
	{
		_tiles = new Tile[CurrentGridWidth, CurrentGridHeight];

		for (int y = 0; y < CurrentGridHeight; y++)
		{
			for (int x = 0; x < CurrentGridWidth; x++)
			{
				var tile = CreateTile(x, y);
				AddChild(tile);
				_tiles[x, y] = tile;
			}
		}

		BeginTileBulkUpdates();

		ApplyOrganicLakeBasins();
		GenerateRivers();
		SpawnResourcesForAllTiles();
		EndTileBulkUpdates();
	}

	private void BeginTileBulkUpdates()
	{
		for (int y = 0; y < CurrentGridHeight; y++)
		{
			for (int x = 0; x < CurrentGridWidth; x++)
			{
				_tiles[x, y].BeginBulkUpdate();
			}
		}
	}

	private void EndTileBulkUpdates()
	{
		for (int y = 0; y < CurrentGridHeight; y++)
		{
			for (int x = 0; x < CurrentGridWidth; x++)
			{
				_tiles[x, y].EndBulkUpdate();
			}
		}
	}

	private void ApplyOrganicLakeBasins()
	{
		if (!EnableOrganicLakeBasins)
		{
			return;
		}

		var lakeSeeds = new List<Vector2I>();
		for (int y = 0; y < CurrentGridHeight; y++)
		{
			for (int x = 0; x < CurrentGridWidth; x++)
			{
				if (_tiles[x, y].Biome == BiomeType.Lake)
				{
					lakeSeeds.Add(new Vector2I(x, y));
				}
			}
		}

		if (lakeSeeds.Count == 0)
		{
			return;
		}

		ShuffleCoordinates(lakeSeeds);
		var minGrowth = Mathf.Max(0, LakeBasinMinGrowthTiles);
		var maxGrowth = Mathf.Max(minGrowth, LakeBasinMaxGrowthTiles);

		foreach (var seed in lakeSeeds)
		{
			GrowLakeBasinFromSeed(seed, _riverRng.RandiRange(minGrowth, maxGrowth));
		}
	}

	private void GrowLakeBasinFromSeed(Vector2I seed, int targetGrowth)
	{
		if (targetGrowth <= 0)
		{
			return;
		}

		var frontier = new List<Vector2I>();
		var frontierKeys = new HashSet<int>();
		AddFrontierNeighbors(seed, frontier, frontierKeys);

		var grown = 0;
		while (grown < targetGrowth && frontier.Count > 0)
		{
			var nextIndex = PickBestLakeFrontierIndex(frontier, seed);
			var candidate = frontier[nextIndex];
			frontier.RemoveAt(nextIndex);
			frontierKeys.Remove(ToKey(candidate.X, candidate.Y));

			if (!CanGrowOrganicLakeTile(candidate, seed))
			{
				continue;
			}

			if (_tiles[candidate.X, candidate.Y].Biome != BiomeType.Lake && _riverRng.Randf() > LakeBasinExpansionChance)
			{
				continue;
			}

			if (_tiles[candidate.X, candidate.Y].Biome != BiomeType.Lake)
			{
				var distance = Mathf.Abs(candidate.X - seed.X) + Mathf.Abs(candidate.Y - seed.Y);
				var depth = Mathf.Clamp(0.72f - (distance * 0.08f), 0.42f, 0.86f);
				_tiles[candidate.X, candidate.Y].ConvertToLake(depth);
				grown++;
			}

			AddFrontierNeighbors(candidate, frontier, frontierKeys);
		}
	}

	private void AddFrontierNeighbors(Vector2I origin, List<Vector2I> frontier, HashSet<int> frontierKeys)
	{
		foreach (var offset in s_riverStepOffsets)
		{
			var nx = origin.X + offset.X;
			var ny = origin.Y + offset.Y;
			if (!IsWithinBounds(nx, ny) || IsEdgeCell(nx, ny))
			{
				continue;
			}

			var key = ToKey(nx, ny);
			if (frontierKeys.Contains(key))
			{
				continue;
			}

			frontier.Add(new Vector2I(nx, ny));
			frontierKeys.Add(key);
		}
	}

	private bool CanGrowOrganicLakeTile(Vector2I candidate, Vector2I seed)
	{
		if (!IsWithinBounds(candidate) || IsEdgeCell(candidate.X, candidate.Y))
		{
			return false;
		}

		var tile = _tiles[candidate.X, candidate.Y];
		if (tile.Biome == BiomeType.Mountain)
		{
			return false;
		}

		var distance = Mathf.Abs(candidate.X - seed.X) + Mathf.Abs(candidate.Y - seed.Y);
		if (distance > LakeBasinMaxDistance)
		{
			return false;
		}

		var allowedElevation = LakeBasinMaxElevation + (distance * 0.012f);
		return tile.Elevation <= allowedElevation;
	}

	private void ShuffleCoordinates(List<Vector2I> coordinates)
	{
		for (int i = coordinates.Count - 1; i > 0; i--)
		{
			var j = _riverRng.RandiRange(0, i);
			(coordinates[i], coordinates[j]) = (coordinates[j], coordinates[i]);
		}
	}

	private Tile CreateTile(int x, int y)
	{
		var baseElevation = Sample01(_elevationNoise, x, y);
		var elevation = ApplyMountainRanges(baseElevation, x, y);
		var moisture = Sample01(_moistureNoise, x, y);
		var temperature = SampleTemperature01(x, y);

		var tile = _tileScene.Instantiate<Tile>();
		tile.Name = $"Tile_{x}_{y}";
		tile.Position = GridToWorld(x, y);
		tile.InitializeTerrain(elevation, moisture, temperature, _biomeSettings, _forestSettings);
		return tile;
	}

	private void SpawnResourcesForAllTiles()
	{
		for (int y = 0; y < CurrentGridHeight; y++)
		{
			for (int x = 0; x < CurrentGridWidth; x++)
			{
				var tile = _tiles[x, y];
				tile.ClearResources();
				SpawnResourcesForTile(tile);
			}
		}
	}

	private void GenerateRivers()
	{
		if (!EnableRivers || RiverSourceCount <= 0)
		{
			return;
		}

		var sourceCandidates = CollectRiverSourceCandidates();
		if (sourceCandidates.Count == 0)
		{
			return;
		}

		var maxSources = Mathf.Clamp(RiverSourceCount, 0, sourceCandidates.Count);
		var attemptsRemaining = sourceCandidates.Count * 3;
		var created = 0;
		var usedSourceSet = new HashSet<int>();
		var acceptedSources = new List<Vector2I>();

		while (created < maxSources && attemptsRemaining > 0)
		{
			attemptsRemaining--;

			var candidateIndex = _riverRng.RandiRange(0, sourceCandidates.Count - 1);
			var source = sourceCandidates[candidateIndex];
			var sourceKey = ToKey(source.X, source.Y);
			if (!usedSourceSet.Add(sourceKey))
			{
				continue;
			}

			if (!IsSourceFarEnoughFromOthers(source, acceptedSources, RiverSourceMinSpacing))
			{
				continue;
			}

			if (HasRiverWithinRadius(source.X, source.Y, RiverAvoidExistingRadius))
			{
				continue;
			}

			if (!TryCarveRiverFromSource(source))
			{
				continue;
			}

			acceptedSources.Add(source);
			created++;
		}

		UpdateRiverConnections();
	}

	private void UpdateRiverConnections()
	{
		for (int y = 0; y < CurrentGridHeight; y++)
		{
			for (int x = 0; x < CurrentGridWidth; x++)
			{
				var tile = _tiles[x, y];
				if (tile.WaterKind != WaterKind.River)
				{
					tile.SetRiverConnections(RiverConnections.None);
					continue;
				}

				tile.SetRiverConnections(GetRiverConnectionsForTile(x, y));
			}
		}
	}

	private RiverConnections GetRiverConnectionsForTile(int x, int y)
	{
		RiverConnections connections = RiverConnections.None;

		if (IsConnectedRiverNeighbor(x, y - 1))
		{
			connections |= RiverConnections.North;
		}

		if (IsConnectedRiverNeighbor(x + 1, y - 1))
		{
			connections |= RiverConnections.NorthEast;
		}

		if (IsConnectedRiverNeighbor(x + 1, y))
		{
			connections |= RiverConnections.East;
		}

		if (IsConnectedRiverNeighbor(x + 1, y + 1))
		{
			connections |= RiverConnections.SouthEast;
		}

		if (IsConnectedRiverNeighbor(x, y + 1))
		{
			connections |= RiverConnections.South;
		}

		if (IsConnectedRiverNeighbor(x - 1, y + 1))
		{
			connections |= RiverConnections.SouthWest;
		}

		if (IsConnectedRiverNeighbor(x - 1, y))
		{
			connections |= RiverConnections.West;
		}

		if (IsConnectedRiverNeighbor(x - 1, y - 1))
		{
			connections |= RiverConnections.NorthWest;
		}

		return connections;
	}

	private bool IsConnectedRiverNeighbor(int x, int y)
	{
		if (!IsWithinBounds(x, y))
		{
			return false;
		}

		return _tiles[x, y].WaterKind == WaterKind.River;
	}

	private List<Vector2I> CollectRiverSourceCandidates()
	{
		var candidates = new List<Vector2I>();
		for (int y = 0; y < CurrentGridHeight; y++)
		{
			for (int x = 0; x < CurrentGridWidth; x++)
			{
				var tile = _tiles[x, y];
				if (tile.Biome == BiomeType.Mountain && tile.Elevation >= RiverSourceMinElevation)
				{
					candidates.Add(new Vector2I(x, y));
				}
			}
		}

		if (candidates.Count > 0)
		{
			return candidates;
		}

		for (int y = 0; y < CurrentGridHeight; y++)
		{
			for (int x = 0; x < CurrentGridWidth; x++)
			{
				var tile = _tiles[x, y];
				if (!tile.IsWater && tile.Elevation >= RiverSourceMinElevation)
				{
					candidates.Add(new Vector2I(x, y));
				}
			}
		}

		return candidates;
	}

	private bool TryCarveRiverFromSource(Vector2I source)
	{
		var visited = new HashSet<int>();
		var path = new List<Vector2I>();
		var current = source;
		Vector2I? previousStepOffset = null;
		var flatSteps = 0;
		var maxLength = Mathf.Max(RiverMaxLength, RiverMinLength + 1);
		var reachedExistingWater = false;
		var reachedMapEdge = false;

		for (int step = 0; step < maxLength; step++)
		{
			if (!IsWithinBounds(current))
			{
				break;
			}

			var currentTile = _tiles[current.X, current.Y];
			var key = ToKey(current.X, current.Y);
			if (!visited.Add(key))
			{
				break;
			}

			path.Add(current);

			if (step > 0 && currentTile.IsWater)
			{
				reachedExistingWater = true;
				break;
			}

			if (IsEdgeCell(current.X, current.Y) && path.Count >= RiverMinLength)
			{
				reachedMapEdge = true;
				break;
			}

			if (!TryGetNextRiverStep(current, previousStepOffset, visited, out var next))
			{
				break;
			}

			var currentElevation = _tiles[current.X, current.Y].Elevation;
			var nextElevation = _tiles[next.X, next.Y].Elevation;
			if (nextElevation >= currentElevation - 0.004f)
			{
				flatSteps++;
				if (flatSteps > 9)
				{
					break;
				}
			}
			else
			{
				flatSteps = 0;
			}

			previousStepOffset = next - current;
			current = next;
		}

		if (path.Count < RiverMinLength)
		{
			return false;
		}

		ApplyRiverPath(path);

		if (!reachedExistingWater && !reachedMapEdge)
		{
			TryCreateTerminalLake(path);
		}

		return true;
	}

	private bool TryGetNextRiverStep(Vector2I current, Vector2I? previousStepOffset, HashSet<int> visited, out Vector2I next)
	{
		next = default;
		var currentElevation = _tiles[current.X, current.Y].Elevation;
		var found = false;
		var bestScore = float.MaxValue;

		foreach (var offset in s_riverStepOffsets)
		{
			var nx = current.X + offset.X;
			var ny = current.Y + offset.Y;
			if (!IsWithinBounds(nx, ny))
			{
				continue;
			}

			if (visited.Contains(ToKey(nx, ny)))
			{
				continue;
			}

			if (WouldCreateDenseRiverCluster(current, nx, ny, visited))
			{
				continue;
			}

			var neighbor = _tiles[nx, ny];
			var drop = currentElevation - neighbor.Elevation;
			var uphillPenalty = Mathf.Max(0.0f, -drop) * 7.0f;
			var flatPenalty = drop < 0.006f ? 0.08f : 0.0f;
			var moistureBias = (1.0f - neighbor.Moisture) * 0.02f;
			var turnPenalty = 0.0f;

			if (previousStepOffset.HasValue)
			{
				var prev = previousStepOffset.Value;
				var offsetDir = new Vector2(offset.X, offset.Y).Normalized();
				var prevDir = new Vector2(prev.X, prev.Y).Normalized();
				var turnDot = offsetDir.Dot(prevDir);
				turnPenalty += (1.0f - turnDot) * RiverTurnPenalty;

				if (turnDot < -0.70f)
				{
					turnPenalty += RiverTurnPenalty * 1.6f;
				}
			}

			var nearbyRiverPenalty = neighbor.WaterKind == WaterKind.None
				? CountAdjacentRivers(nx, ny, current) * 0.14f
				: -0.35f;
			var meander = _riverRng.RandfRange(-0.02f, 0.02f);

			var score = neighbor.Elevation + uphillPenalty + flatPenalty + moistureBias + turnPenalty + nearbyRiverPenalty + meander;
			if (score >= bestScore)
			{
				continue;
			}

			bestScore = score;
			next = new Vector2I(nx, ny);
			found = true;
		}

		if (!found)
		{
			return false;
		}

		var nextElevation = _tiles[next.X, next.Y].Elevation;
		if (nextElevation > currentElevation + 0.04f)
		{
			return false;
		}

		return true;
	}

	private bool WouldCreateDenseRiverCluster(Vector2I current, int nx, int ny, HashSet<int> visited)
	{
		var touchedRiverNeighbors = 0;
		var touchedVisitedNeighbors = 0;

		foreach (var offset in s_riverStepOffsets)
		{
			var tx = nx + offset.X;
			var ty = ny + offset.Y;
			if (!IsWithinBounds(tx, ty))
			{
				continue;
			}

			if (tx == current.X && ty == current.Y)
			{
				continue;
			}

			if (_tiles[tx, ty].WaterKind == WaterKind.River)
			{
				touchedRiverNeighbors++;
			}

			if (visited.Contains(ToKey(tx, ty)))
			{
				touchedVisitedNeighbors++;
			}
		}

		return touchedRiverNeighbors >= 2 || touchedVisitedNeighbors >= 2;
	}

	private void TryCreateTerminalLake(List<Vector2I> riverPath)
	{
		if (!EnableRiverTerminalLakes)
		{
			return;
		}

		if (riverPath.Count == 0)
		{
			return;
		}

		var outlet = riverPath[riverPath.Count - 1];
		if (!IsWithinBounds(outlet) || IsEdgeCell(outlet.X, outlet.Y))
		{
			return;
		}

		var outletTile = _tiles[outlet.X, outlet.Y];
		if (outletTile.WaterKind != WaterKind.River)
		{
			return;
		}

		var lakeCells = BuildNaturalTerminalLakeCells(outlet, riverPath.Count);

		if (lakeCells.Count == 0)
		{
			return;
		}

		foreach (var cell in lakeCells)
		{
			var dx = Mathf.Abs(cell.X - outlet.X);
			var dy = Mathf.Abs(cell.Y - outlet.Y);
			var distance = dx + dy;
			var elevationBias = Mathf.Clamp((0.65f - _tiles[cell.X, cell.Y].Elevation) * 0.45f, -0.10f, 0.25f);
			var depth = Mathf.Clamp(0.78f - (distance * 0.14f) + elevationBias, 0.36f, 0.90f);
			_tiles[cell.X, cell.Y].ConvertToLake(depth);
		}
	}

	private List<Vector2I> BuildNaturalTerminalLakeCells(Vector2I outlet, int riverLength)
	{
		var minTiles = Mathf.Max(1, RiverTerminalLakeMinTiles);
		var maxTiles = Mathf.Max(minTiles, RiverTerminalLakeMaxTiles);
		var lengthBonus = Mathf.Clamp(riverLength / 18, 0, maxTiles - minTiles);
		var randomBonus = _riverRng.RandiRange(0, Mathf.Max(0, maxTiles - minTiles - lengthBonus));
		var targetSize = Mathf.Clamp(minTiles + lengthBonus + randomBonus, minTiles, maxTiles);

		var lakeKeys = new HashSet<int>();
		var frontier = new List<Vector2I> { outlet };
		var frontierKeys = new HashSet<int> { ToKey(outlet.X, outlet.Y) };

		while (lakeKeys.Count < targetSize && frontier.Count > 0)
		{
			var nextIndex = PickBestLakeFrontierIndex(frontier, outlet);
			var candidate = frontier[nextIndex];
			frontier.RemoveAt(nextIndex);
			frontierKeys.Remove(ToKey(candidate.X, candidate.Y));

			if (!CanBecomeTerminalLakeTile(candidate, outlet))
			{
				continue;
			}

			if (lakeKeys.Count > 0 && _riverRng.Randf() > RiverTerminalLakeExpansionChance)
			{
				continue;
			}

			lakeKeys.Add(ToKey(candidate.X, candidate.Y));

			foreach (var offset in s_riverStepOffsets)
			{
				var nx = candidate.X + offset.X;
				var ny = candidate.Y + offset.Y;
				if (!IsWithinBounds(nx, ny) || IsEdgeCell(nx, ny))
				{
					continue;
				}

				var neighborKey = ToKey(nx, ny);
				if (lakeKeys.Contains(neighborKey) || frontierKeys.Contains(neighborKey))
				{
					continue;
				}

				frontier.Add(new Vector2I(nx, ny));
				frontierKeys.Add(neighborKey);
			}
		}

		var lakeCells = new List<Vector2I>();
		foreach (var key in lakeKeys)
		{
			var y = key / CurrentGridWidth;
			var x = key - (y * CurrentGridWidth);
			lakeCells.Add(new Vector2I(x, y));
		}

		return lakeCells;
	}

	private int PickBestLakeFrontierIndex(List<Vector2I> frontier, Vector2I outlet)
	{
		var bestIndex = 0;
		var bestScore = float.MaxValue;

		for (int i = 0; i < frontier.Count; i++)
		{
			var candidate = frontier[i];
			var tile = _tiles[candidate.X, candidate.Y];
			var dx = candidate.X - outlet.X;
			var dy = candidate.Y - outlet.Y;
			var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
			var score = tile.Elevation + (distance * 0.05f) + _riverRng.RandfRange(0.0f, 0.03f);

			if (score >= bestScore)
			{
				continue;
			}

			bestScore = score;
			bestIndex = i;
		}

		return bestIndex;
	}

	private bool CanBecomeTerminalLakeTile(Vector2I candidate, Vector2I outlet)
	{
		if (!IsWithinBounds(candidate) || IsEdgeCell(candidate.X, candidate.Y))
		{
			return false;
		}

		var tile = _tiles[candidate.X, candidate.Y];
		if (tile.Biome == BiomeType.Mountain)
		{
			return false;
		}

		var manhattanDistance = Mathf.Abs(candidate.X - outlet.X) + Mathf.Abs(candidate.Y - outlet.Y);
		var allowedElevation = RiverTerminalLakeMaxElevation + (manhattanDistance * 0.015f);
		if (tile.Elevation > allowedElevation)
		{
			return false;
		}

		return tile.WaterKind == WaterKind.None || tile.WaterKind == WaterKind.River || tile.WaterKind == WaterKind.Lake;
	}

	private bool IsSourceFarEnoughFromOthers(Vector2I source, List<Vector2I> existingSources, int minSpacing)
	{
		if (minSpacing <= 0 || existingSources.Count == 0)
		{
			return true;
		}

		var minSpacingSq = minSpacing * minSpacing;
		foreach (var existing in existingSources)
		{
			var dx = source.X - existing.X;
			var dy = source.Y - existing.Y;
			if ((dx * dx) + (dy * dy) < minSpacingSq)
			{
				return false;
			}
		}

		return true;
	}

	private bool HasRiverWithinRadius(int x, int y, int radius)
	{
		if (radius <= 0)
		{
			return false;
		}

		for (int oy = -radius; oy <= radius; oy++)
		{
			for (int ox = -radius; ox <= radius; ox++)
			{
				if (ox == 0 && oy == 0)
				{
					continue;
				}

				var nx = x + ox;
				var ny = y + oy;
				if (!IsWithinBounds(nx, ny))
				{
					continue;
				}

				if (_tiles[nx, ny].WaterKind == WaterKind.River)
				{
					return true;
				}
			}
		}

		return false;
	}

	private int CountAdjacentRivers(int x, int y, Vector2I exclude)
	{
		var count = 0;
		foreach (var offset in s_riverStepOffsets)
		{
			var nx = x + offset.X;
			var ny = y + offset.Y;
			if (!IsWithinBounds(nx, ny))
			{
				continue;
			}

			if (nx == exclude.X && ny == exclude.Y)
			{
				continue;
			}

			if (_tiles[nx, ny].WaterKind == WaterKind.River)
			{
				count++;
			}
		}

		return count;
	}

	private void ApplyRiverPath(List<Vector2I> path)
	{
		var pathLength = path.Count;
		for (int i = 0; i < pathLength; i++)
		{
			var coord = path[i];
			var tile = _tiles[coord.X, coord.Y];
			if (tile.Biome == BiomeType.Lake)
			{
				continue;
			}

			var progress = pathLength <= 1 ? 0.0f : (float)i / (pathLength - 1);
			var depth = Mathf.Lerp(0.20f, 0.72f, progress);
			tile.SetWater(WaterKind.River, depth);
		}
	}

	private bool IsWithinBounds(Vector2I coords)
	{
		return IsWithinBounds(coords.X, coords.Y);
	}

	private bool IsWithinBounds(int x, int y)
	{
		return x >= 0 && y >= 0 && x < CurrentGridWidth && y < CurrentGridHeight;
	}

	private bool IsEdgeCell(int x, int y)
	{
		return x == 0 || y == 0 || x == CurrentGridWidth - 1 || y == CurrentGridHeight - 1;
	}

	private int ToKey(int x, int y)
	{
		return (y * CurrentGridWidth) + x;
	}

	private TerrainRules.BiomeSettings BuildBiomeSettings()
	{
		return new TerrainRules.BiomeSettings(
			LakeMaxElevation,
			MountainMinElevationExclusive,
			HillsMinElevationExclusive,
			DesertMaxMoistureExclusive,
			DesertMinTemperatureExclusive);
	}

	private TerrainRules.ForestSettings BuildForestSettings()
	{
		return new TerrainRules.ForestSettings(
			ForestMinMoistureExclusive,
			ForestMinTemperatureExclusive,
			ForestMaxTemperatureExclusive);
	}

	private void ClearGeneratedTiles()
	{
		foreach (var child in GetChildren())
		{
			if (child is Tile tile)
			{
				tile.QueueFree();
			}
		}
	}

	private Vector2 GridToWorld(int x, int y)
	{
		return new Vector2(x * TileSize - _offsetX, y * TileSize - _offsetY);
	}

	private Vector2I WorldToGrid(Vector2 worldPosition)
	{
		return new Vector2I(
			Mathf.RoundToInt((worldPosition.X + _offsetX) / TileSize),
			Mathf.RoundToInt((worldPosition.Y + _offsetY) / TileSize));
	}

	private RandomNumberGenerator CreateSeededRng()
	{
		var rng = new RandomNumberGenerator();
		rng.Seed = (ulong)Math.Max(1, Seed);
		return rng;
	}

	private void ConfigureNoise(RandomNumberGenerator rng)
	{
		_elevationNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_elevationNoise.Frequency = BaseNoiseFrequency;
		_elevationNoise.Seed = rng.RandiRange(1, int.MaxValue);

		_moistureNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		_moistureNoise.Frequency = BaseNoiseFrequency;
		_moistureNoise.Seed = rng.RandiRange(1, int.MaxValue);

		_temperatureNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_temperatureNoise.Frequency = BaseNoiseFrequency;
		_temperatureNoise.Seed = rng.RandiRange(1, int.MaxValue);

		_mountainRidgeNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		_mountainRidgeNoise.Frequency = MountainRidgeFrequency;
		_mountainRidgeNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
		_mountainRidgeNoise.FractalOctaves = 3;
		_mountainRidgeNoise.Seed = rng.RandiRange(1, int.MaxValue);

		_mountainRegionNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_mountainRegionNoise.Frequency = MountainRegionFrequency;
		_mountainRegionNoise.Seed = rng.RandiRange(1, int.MaxValue);
	}

	private static float Sample01(FastNoiseLite noise, int x, int y)
	{
		var sample = noise.GetNoise2D(x, y);
		return Mathf.Clamp((sample + 1.0f) * 0.5f, 0.0f, 1.0f);
	}

	private float ApplyMountainRanges(float baseElevation, int x, int y)
	{
		var ridgeSample = _mountainRidgeNoise.GetNoise2D(x * 0.62f, y * 1.30f);
		var ridge = 1.0f - Mathf.Abs(ridgeSample);
		ridge = Mathf.Pow(ridge, 3.6f);

		var region = Sample01(_mountainRegionNoise, x, y);
		region = Mathf.SmoothStep(0.45f, 0.85f, region);

		var foothillBias = Mathf.SmoothStep(0.30f, 0.72f, baseElevation);
		var rangeStrength = ridge * region;
		var uplift = rangeStrength * (0.14f + foothillBias * 0.34f) * MountainUpliftStrength;

		return Mathf.Clamp(baseElevation + uplift, 0.0f, 1.0f);
	}

	private float SampleTemperature01(int x, int y)
	{
		// Adds a simple hot-equator/cold-pole gradient, with noise for local variation.
		var normalizedY = CurrentGridHeight <= 1 ? 0.5f : (float)y / (CurrentGridHeight - 1);
		var latitude = Mathf.Abs(normalizedY - 0.5f) * 2.0f;
		var latitudeTemp = 1.0f - latitude;
		var noiseTemp = Sample01(_temperatureNoise, x, y);
		return Mathf.Clamp((latitudeTemp * 0.7f) + (noiseTemp * 0.3f), 0.0f, 1.0f);
	}

	private void SpawnResourcesForTile(Tile tile)
	{
		foreach (var rule in s_resourceSpawnRules)
		{
			if (!rule.CanSpawn(tile))
			{
				continue;
			}

			var chance = AdjustedChance(rule.GetBaseChance(this) * rule.GetMultiplier(tile));
			TrySpawnResource(
				tile,
				rule.ResourceType,
				chance,
				rule.GetMinAmount(this),
				rule.GetMaxAmount(this));
		}
	}

	private float AdjustedChance(float chance)
	{
		return Mathf.Clamp(chance * ResourceSpawnDensity, 0.0f, 0.95f);
	}

	private static float GetGrainsSpawnMultiplier(Tile tile)
	{
		var moistureBias = Mathf.Clamp(0.6f + tile.Moisture, 0.5f, 1.6f);
		var tempBias = 1.0f - Mathf.Abs(tile.Temperature - 0.55f);
		var forestBonus = tile.HasForest ? 1.15f : 1.0f;
		var fertilityBias = Mathf.Lerp(0.45f, 1.95f, tile.Fertility);
		var riverPenalty = tile.WaterKind == WaterKind.River ? 0.42f : 1.0f;
		return Mathf.Clamp(moistureBias * tempBias * forestBonus * fertilityBias * riverPenalty, 0.12f, 2.20f);
	}

	private static float GetStoneSpawnMultiplier(Tile tile)
	{
		var heightBias = Mathf.Clamp(0.55f + tile.Elevation, 0.55f, 1.9f);
		return tile.Biome == BiomeType.Mountain ? heightBias * 1.15f : heightBias;
	}

	private static float GetIronSpawnMultiplier(Tile tile)
	{
		var heightBias = Mathf.Clamp((tile.Elevation - 0.45f) * 2.0f, 0.25f, 1.65f);
		var dryBias = Mathf.Clamp(1.2f - tile.Moisture, 0.5f, 1.25f);
		var mountainBonus = tile.Biome == BiomeType.Mountain ? 1.25f : 1.0f;
		return Mathf.Clamp(heightBias * dryBias * mountainBonus, 0.2f, 2.0f);
	}

	private static float GetFishSpawnMultiplier(Tile tile)
	{
		var deepWaterBias = Mathf.Lerp(0.7f, 1.7f, tile.WaterDepth);
		var coolWaterBias = Mathf.Clamp(1.15f - (tile.Temperature * 0.5f), 0.75f, 1.2f);
		return Mathf.Clamp(deepWaterBias * coolWaterBias, 0.65f, 1.9f);
	}

	private static bool CanSpawnFish(Tile tile)
	{
		return tile.WaterKind == WaterKind.Lake || tile.WaterKind == WaterKind.River;
	}

	private void TrySpawnResource(Tile tile, ResourceType resourceType, float chance, int minAmount, int maxAmount)
	{
		if (_resourceRng.Randf() > Mathf.Clamp(chance, 0.0f, 1.0f))
		{
			return;
		}

		var safeMin = Mathf.Max(1, minAmount);
		var safeMax = Mathf.Max(safeMin, maxAmount);
		var amount = _resourceRng.RandiRange(safeMin, safeMax);
		tile.AddResource(resourceType, amount);
	}

	private static void PrintTileInfo(Tile tile)
	{
		var waterLabel = tile.IsWater ? $"{tile.WaterKind}({tile.WaterDepth:0.00})" : "None";
		GD.Print($"{tile.Name} | BaseBiome: {tile.Biome} | Terrain: {tile.TerrainLabel} | Water: {waterLabel} | Forest: {tile.HasForest} | Fertility: {tile.Fertility:0.00} | Resources: {tile.GetResourcesLabel()} | E:{tile.Elevation:0.00} M:{tile.Moisture:0.00} T:{tile.Temperature:0.00}");
	}

	private sealed class ResourceSpawnRule(
		ResourceType resourceType,
		Func<Tile, bool> canSpawn,
		Func<Tile, float> getMultiplier,
		Func<World, float> getBaseChance,
		Func<World, int> getMinAmount,
		Func<World, int> getMaxAmount)
	{
		public ResourceType ResourceType { get; } = resourceType;
		public Func<Tile, bool> CanSpawn { get; } = canSpawn;
		public Func<Tile, float> GetMultiplier { get; } = getMultiplier;
		public Func<World, float> GetBaseChance { get; } = getBaseChance;
		public Func<World, int> GetMinAmount { get; } = getMinAmount;
		public Func<World, int> GetMaxAmount { get; } = getMaxAmount;
	}

	private int CurrentGridWidth => Math.Max(1, GridWidth);
	private int CurrentGridHeight => Math.Max(1, GridHeight);
}
