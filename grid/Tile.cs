using Godot;

[Tool]
public partial class Tile : Node2D
{
    private Node2D _resourceMarkers = null;
    private TileVisuals _visuals = null;
    private readonly TileResources _resources = new();
    private bool _isBulkUpdating;
    private bool _terrainDirty;
    private bool _resourceDirty;

    public float Elevation { get; private set; }
    public float Moisture { get; private set; }
    public float Temperature { get; private set; }
    public float Fertility { get; private set; }
    public BiomeType Biome { get; private set; } = BiomeType.Grassland;
    public WaterKind WaterKind { get; private set; } = WaterKind.None;
    public bool IsWater => WaterKind != WaterKind.None;
    public float WaterDepth { get; private set; }
    public RiverConnections RiverConnections { get; private set; } = RiverConnections.None;
    public bool HasForest { get; private set; }
    public string TerrainLabel => HasForest ? $"Forested {Biome}" : Biome.ToString();
    public bool HasAnyResources => _resources.HasAny;

    public override void _Ready()
    {
        _visuals = new TileVisuals(this, GetNodeOrNull<Sprite2D>("Sprite2D"));
        _visuals.Initialize();
        RefreshTerrainVisuals();
    }

    public void InitializeTerrain(
        float elevation,
        float moisture,
        float temperature,
        TerrainRules.BiomeSettings biomeSettings,
        TerrainRules.ForestSettings forestSettings)
    {
        Elevation = Mathf.Clamp(elevation, 0.0f, 1.0f);
        Moisture = Mathf.Clamp(moisture, 0.0f, 1.0f);
        Temperature = Mathf.Clamp(temperature, 0.0f, 1.0f);
        Biome = TerrainRules.DetermineBiome(Elevation, Moisture, Temperature, biomeSettings);
        UpdateWaterStateFromBiome();
        HasForest = TerrainRules.DetermineForest(Biome, Moisture, Temperature, forestSettings);
        RecalculateFertility();
        RefreshTerrainVisuals();
    }

    private void RecalculateFertility()
    {
        Fertility = TerrainRules.CalculateFertility(Biome, Elevation, Moisture, Temperature, HasForest, WaterKind);
    }

    private void UpdateWaterStateFromBiome()
    {
        if (Biome == BiomeType.Lake)
        {
            WaterKind = WaterKind.Lake;
            WaterDepth = Mathf.Clamp((0.35f - Elevation) * 3.5f, 0.1f, 1.0f);
            RiverConnections = RiverConnections.None;
            return;
        }

        WaterKind = WaterKind.None;
        WaterDepth = 0.0f;
        RiverConnections = RiverConnections.None;
    }

    public void SetSelected(bool selected)
    {
        if (_visuals is null)
        {
            return;
        }

        _visuals.SetSelected(selected);
    }

    public void BeginBulkUpdate()
    {
        _isBulkUpdating = true;
        _terrainDirty = false;
        _resourceDirty = false;
    }

    public void EndBulkUpdate()
    {
        if (!_isBulkUpdating)
        {
            return;
        }

        _isBulkUpdating = false;

        if (_terrainDirty)
        {
            _terrainDirty = false;
            _resourceDirty = false;
            RefreshTerrainVisuals();
            return;
        }

        if (_resourceDirty)
        {
            _resourceDirty = false;
            RefreshResourceMarkers();
        }
    }

    public void AddResource(ResourceType resourceType, int amount)
    {
        _resources.Add(resourceType, amount);
        RefreshResourceMarkers();
    }

    public int GetResourceAmount(ResourceType resourceType)
    {
        return _resources.GetAmount(resourceType);
    }

    public bool TryDepleteResource(ResourceType resourceType, int amount, out int depleted)
    {
        var didDeplete = _resources.TryDeplete(resourceType, amount, out depleted);
        if (didDeplete)
        {
            RefreshResourceMarkers();
        }

        return didDeplete;
    }

    public string GetResourcesLabel()
    {
        return _resources.ToDebugLabel();
    }

    public void SetWater(WaterKind waterKind, float waterDepth)
    {
        var clampedDepth = Mathf.Clamp(waterDepth, 0.0f, 1.0f);
        var changed = false;

        if (Biome == BiomeType.Lake && waterKind != WaterKind.Lake)
        {
            return;
        }

        if (waterKind == WaterKind.None)
        {
            changed = WaterKind != WaterKind.None || WaterDepth > 0.0f;
            WaterKind = WaterKind.None;
            WaterDepth = 0.0f;
            RiverConnections = RiverConnections.None;
            RecalculateFertility();
            if (changed)
            {
                RefreshTerrainVisuals();
            }
            return;
        }

        if (WaterKind == WaterKind.Lake && waterKind != WaterKind.Lake)
        {
            return;
        }

        if (WaterKind == waterKind)
        {
            var previousDepth = WaterDepth;
            WaterDepth = Mathf.Max(WaterDepth, clampedDepth);
            RecalculateFertility();
            if (WaterDepth != previousDepth)
            {
                RefreshTerrainVisuals();
            }
            return;
        }

        WaterKind = waterKind;
        WaterDepth = clampedDepth;
        if (waterKind != WaterKind.River)
        {
            RiverConnections = RiverConnections.None;
        }
        RecalculateFertility();
        RefreshTerrainVisuals();
    }

    public void SetRiverConnections(RiverConnections connections)
    {
        if (RiverConnections == connections)
        {
            return;
        }

        RiverConnections = connections;
        RefreshTerrainVisuals();
    }

    public void ConvertToLake(float waterDepth)
    {
        Biome = BiomeType.Lake;
        HasForest = false;
        WaterKind = WaterKind.Lake;
        WaterDepth = Mathf.Clamp(waterDepth, 0.1f, 1.0f);
        RiverConnections = RiverConnections.None;
        RecalculateFertility();
        RefreshTerrainVisuals();
    }

    public void ClearResources()
    {
        _resources.Clear();
        RefreshResourceMarkers();
    }

    private void RefreshTerrainVisuals()
    {
        if (_isBulkUpdating)
        {
            _terrainDirty = true;
            return;
        }

        if (_visuals is not null)
        {
            _visuals.UpdateBiomeVisual(Biome, WaterKind, WaterDepth, RiverConnections);
        }

        RefreshResourceMarkers();
    }

    private void EnsureResourceMarkerLayer()
    {
        if (_resourceMarkers is not null)
        {
            return;
        }

        _resourceMarkers = TileMarkerBuilder.CreateMarkerLayer();
        AddChild(_resourceMarkers);
    }

    private void RefreshResourceMarkers()
    {
        if (_isBulkUpdating)
        {
            _resourceDirty = true;
            return;
        }

        if (!IsInsideTree())
        {
            return;
        }

        EnsureResourceMarkerLayer();
        if (_resourceMarkers is null)
        {
            return;
        }

        TileMarkerBuilder.Refresh(
            _resourceMarkers,
            HasForest,
            TileMarkerBuilder.BuildForestSeed(Name.ToString(), Elevation, Moisture, Temperature),
            _resources.GetAmount(ResourceType.Grains),
            _resources.GetAmount(ResourceType.Stone),
            _resources.GetAmount(ResourceType.Iron),
            _resources.GetAmount(ResourceType.Fish));
    }

}
