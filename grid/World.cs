using Godot;
using System;

#nullable enable

[Tool]
public partial class World : Node2D
{
	private const int TileSize = 32;
	private static readonly ResourceSpawnRule[] s_resourceSpawnRules =
	{
		new(
			ResourceType.Fish,
			tile => tile.Biome == BiomeType.Lake,
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
	public float MountainMinElevationExclusive { get; set; } = TerrainRules.DefaultBiomeSettings.MountainMinElevationExclusive;

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
	private UI? _ui;
	private TerrainRules.BiomeSettings _biomeSettings = TerrainRules.DefaultBiomeSettings;
	private TerrainRules.ForestSettings _forestSettings = TerrainRules.DefaultForestSettings;
	private float _offsetX;
	private float _offsetY;

	public override void _Ready()
	{
		InitializeUi();

		if (!Engine.IsEditorHint())
		{
			RebuildWorld();
		}
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
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		Seed = rng.RandiRange(1, int.MaxValue);
		NotifyPropertyListChanged();
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
		SpawnResourcesForTile(tile);
		return tile;
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
		var uplift = rangeStrength * (0.14f + foothillBias * 0.34f);

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
		return Mathf.Clamp(moistureBias * tempBias * forestBonus, 0.35f, 1.8f);
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
		var deepWaterBias = Mathf.Clamp((0.35f - tile.Elevation) * 3.5f, 0.7f, 1.7f);
		var coolWaterBias = Mathf.Clamp(1.15f - (tile.Temperature * 0.5f), 0.75f, 1.2f);
		return Mathf.Clamp(deepWaterBias * coolWaterBias, 0.65f, 1.9f);
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
		GD.Print($"{tile.Name} | BaseBiome: {tile.Biome} | Terrain: {tile.TerrainLabel} | Forest: {tile.HasForest} | Resources: {tile.GetResourcesLabel()} | E:{tile.Elevation:0.00} M:{tile.Moisture:0.00} T:{tile.Temperature:0.00}");
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
