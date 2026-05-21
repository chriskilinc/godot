using Godot;

[Tool]
public partial class Tile : Node2D
{
    private Node2D _resourceMarkers = null;
    private TileVisuals _visuals = null;
    private readonly TileResources _resources = new();

    public float Elevation { get; private set; }
    public float Moisture { get; private set; }
    public float Temperature { get; private set; }
    public BiomeType Biome { get; private set; } = BiomeType.Grassland;
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
        HasForest = TerrainRules.DetermineForest(Biome, Moisture, Temperature, forestSettings);
        RefreshTerrainVisuals();
    }

    public void SetSelected(bool selected)
    {
        if (_visuals is null)
        {
            return;
        }

        _visuals.SetSelected(selected);
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

    private void RefreshTerrainVisuals()
    {
        if (_visuals is not null)
        {
            _visuals.UpdateBiomeVisual(Biome);
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
