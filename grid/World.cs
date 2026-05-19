using Godot;
using System;

public partial class World : Node2D
{
	private const int GridWidth = 100;
	private const int GridHeight = 50;
	private const int TileSize = 32;
	private const float NoiseFrequency = 0.05f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float GrainsSpawnChance { get; set; } = 0.08f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float StoneSpawnChance { get; set; } = 0.07f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float IronSpawnChance { get; set; } = 0.03f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float FishSpawnChance { get; set; } = 0.18f;

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
	private Tile[,] _tiles = new Tile[GridWidth, GridHeight];
	private Tile? _selectedTile;
	private readonly FastNoiseLite _elevationNoise = new();
	private readonly FastNoiseLite _moistureNoise = new();
	private readonly FastNoiseLite _temperatureNoise = new();
	private readonly FastNoiseLite _mountainRidgeNoise = new();
	private readonly FastNoiseLite _mountainRegionNoise = new();
	private readonly RandomNumberGenerator _resourceRng = new();
	private float _offsetX;
	private float _offsetY;

	public override void _Ready()
	{
		_resourceRng.Randomize();
		ConfigureNoise();

		_offsetX = (GridWidth - 1) * TileSize * 0.5f;
		_offsetY = (GridHeight - 1) * TileSize * 0.5f;

		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				var baseElevation = Sample01(_elevationNoise, x, y);
				var elevation = ApplyMountainRanges(baseElevation, x, y);

				var tile = _tileScene.Instantiate<Tile>();
				tile.Name = $"Tile_{x}_{y}";
				tile.Position = new Vector2(x * TileSize - _offsetX, y * TileSize - _offsetY);
				tile.InitializeTerrain(
					elevation,
					Sample01(_moistureNoise, x, y),
					SampleTemperature01(x, y));
				SpawnResourcesForTile(tile);
				AddChild(tile);
				_tiles[x, y] = tile;
			}
		}
	}

	public bool TrySelectTileAtWorld(Vector2 worldPosition)
	{
		var x = Mathf.RoundToInt((worldPosition.X + _offsetX) / TileSize);
		var y = Mathf.RoundToInt((worldPosition.Y + _offsetY) / TileSize);

		if (x < 0 || y < 0 || x >= GridWidth || y >= GridHeight)
		{
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
		PrintTileInfo(_selectedTile);
	}

	private void ConfigureNoise()
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		_elevationNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_elevationNoise.Frequency = NoiseFrequency;
		_elevationNoise.Seed = rng.RandiRange(1, int.MaxValue);

		_moistureNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		_moistureNoise.Frequency = NoiseFrequency;
		_moistureNoise.Seed = rng.RandiRange(1, int.MaxValue);

		_temperatureNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_temperatureNoise.Frequency = NoiseFrequency;
		_temperatureNoise.Seed = rng.RandiRange(1, int.MaxValue);

		_mountainRidgeNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		_mountainRidgeNoise.Frequency = 0.035f;
		_mountainRidgeNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
		_mountainRidgeNoise.FractalOctaves = 3;
		_mountainRidgeNoise.Seed = rng.RandiRange(1, int.MaxValue);

		_mountainRegionNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_mountainRegionNoise.Frequency = 0.012f;
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
		var latitude = Mathf.Abs(((float)y / (GridHeight - 1)) - 0.5f) * 2.0f;
		var latitudeTemp = 1.0f - latitude;
		var noiseTemp = Sample01(_temperatureNoise, x, y);
		return Mathf.Clamp((latitudeTemp * 0.7f) + (noiseTemp * 0.3f), 0.0f, 1.0f);
	}

	private void SpawnResourcesForTile(Tile tile)
	{
		if (tile.Biome == BiomeType.Lake)
		{
			var fishChance = AdjustedChance(FishSpawnChance * GetFishSpawnMultiplier(tile));
			TrySpawnResource(tile, ResourceType.Fish, fishChance, FishMinAmount, FishMaxAmount);
			return;
		}

		if (tile.Biome == BiomeType.Grassland || tile.Biome == BiomeType.Hills)
		{
			var grainsChance = AdjustedChance(GrainsSpawnChance * GetGrainsSpawnMultiplier(tile));
			TrySpawnResource(tile, ResourceType.Grains, grainsChance, GrainsMinAmount, GrainsMaxAmount);
		}

		if (tile.Biome == BiomeType.Hills || tile.Biome == BiomeType.Mountain)
		{
			var stoneChance = AdjustedChance(StoneSpawnChance * GetStoneSpawnMultiplier(tile));
			var ironChance = AdjustedChance(IronSpawnChance * GetIronSpawnMultiplier(tile));
			TrySpawnResource(tile, ResourceType.Stone, stoneChance, StoneMinAmount, StoneMaxAmount);
			TrySpawnResource(tile, ResourceType.Iron, ironChance, IronMinAmount, IronMaxAmount);
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
}
