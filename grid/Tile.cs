using Godot;
using System.Collections.Generic;

public partial class Tile : Node2D
{
    private const float HalfSize = 16.0f;
    private const float MarkerSize = 4.0f;
    private Line2D? _selectionOutline;
    private Sprite2D? _sprite;
    private Polygon2D _baseSurface = null;
    private Node2D? _resourceMarkers;
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
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        EnsureBaseSurface();
        CreateSelectionOutline();
        UpdateBiomeVisual();
        EnsureResourceMarkerLayer();
        RefreshResourceMarkers();
    }

    public void InitializeTerrain(float elevation, float moisture, float temperature)
    {
        Elevation = Mathf.Clamp(elevation, 0.0f, 1.0f);
        Moisture = Mathf.Clamp(moisture, 0.0f, 1.0f);
        Temperature = Mathf.Clamp(temperature, 0.0f, 1.0f);
        Biome = TerrainRules.DetermineBiome(Elevation, Moisture, Temperature);
        HasForest = TerrainRules.DetermineForest(Biome, Moisture, Temperature);
        UpdateBiomeVisual();
    }

    public void SetSelected(bool selected)
    {
        if (_selectionOutline is null)
        {
            return;
        }

        _selectionOutline.Visible = selected;
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

    private void CreateSelectionOutline()
    {
        _selectionOutline = new Line2D
        {
            DefaultColor = new Color(0.95f, 0.95f, 0.2f, 1.0f),
            Width = 2.0f,
            Closed = true,
            Antialiased = true,
            Visible = false,
            ZIndex = 10
        };

        _selectionOutline.AddPoint(new Vector2(-HalfSize, -HalfSize));
        _selectionOutline.AddPoint(new Vector2(HalfSize, -HalfSize));
        _selectionOutline.AddPoint(new Vector2(HalfSize, HalfSize));
        _selectionOutline.AddPoint(new Vector2(-HalfSize, HalfSize));
        AddChild(_selectionOutline);
    }

    private void UpdateBiomeVisual()
    {
        var biomeColor = TerrainRules.GetBiomeColor(Biome);

        if (_sprite is not null)
        {
            _sprite.Modulate = biomeColor;
        }

        if (_baseSurface is not null)
        {
            _baseSurface.Color = biomeColor;
        }
    }

    private void EnsureBaseSurface()
    {
        if (_baseSurface is not null)
        {
            return;
        }

        _baseSurface = new Polygon2D
        {
            Name = "BaseSurface",
            ZIndex = -1,
            Polygon = new Vector2[]
            {
                new(-HalfSize, -HalfSize),
                new(HalfSize, -HalfSize),
                new(HalfSize, HalfSize),
                new(-HalfSize, HalfSize)
            }
        };

        AddChild(_baseSurface);
    }

    private void EnsureResourceMarkerLayer()
    {
        if (_resourceMarkers is not null)
        {
            return;
        }

        _resourceMarkers = new Node2D
        {
            Name = "ResourceMarkers",
            ZIndex = 5
        };
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

        foreach (var child in _resourceMarkers.GetChildren())
        {
            child.QueueFree();
        }

        if (HasForest)
        {
            AddForestMarkers();
        }

        AddMarkerIfPresent(ResourceType.Grains, new Color(0.98f, 0.87f, 0.19f), new Vector2(-7.0f, -7.0f));
        AddMarkerIfPresent(ResourceType.Stone, new Color(0.76f, 0.76f, 0.80f), new Vector2(7.0f, -7.0f));
        AddMarkerIfPresent(ResourceType.Iron, new Color(0.83f, 0.37f, 0.28f), new Vector2(-7.0f, 7.0f));
        AddMarkerIfPresent(ResourceType.Fish, new Color(0.24f, 0.93f, 0.97f), new Vector2(7.0f, 7.0f));
    }

    private void AddMarkerIfPresent(ResourceType resourceType, Color color, Vector2 position)
    {
        if (_resourceMarkers is null || _resources.GetAmount(resourceType) <= 0)
        {
            return;
        }

        var marker = new Polygon2D
        {
            Color = color,
            Position = position,
            Polygon = new Vector2[]
            {
                new(-MarkerSize * 0.5f, -MarkerSize * 0.5f),
                new(MarkerSize * 0.5f, -MarkerSize * 0.5f),
                new(MarkerSize * 0.5f, MarkerSize * 0.5f),
                new(-MarkerSize * 0.5f, MarkerSize * 0.5f)
            }
        };

        _resourceMarkers.AddChild(marker);
    }

    private void AddFixedMarker(Color color, Vector2 position)
    {
        if (_resourceMarkers is null)
        {
            return;
        }

        var marker = new Polygon2D
        {
            Color = color,
            Position = position,
            Polygon = new Vector2[]
            {
                new(-MarkerSize * 0.5f, -MarkerSize * 0.5f),
                new(MarkerSize * 0.5f, -MarkerSize * 0.5f),
                new(MarkerSize * 0.5f, MarkerSize * 0.5f),
                new(-MarkerSize * 0.5f, MarkerSize * 0.5f)
            }
        };

        _resourceMarkers.AddChild(marker);
    }

    private void AddForestMarkers()
    {
        if (_resourceMarkers is null)
        {
            return;
        }

        var rng = new RandomNumberGenerator
        {
            Seed = BuildForestSeed()
        };

        var count = 6 + rng.RandiRange(0, 3);
        var positions = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            var position = GetSpreadOutForestPosition(rng, positions);
            positions.Add(position);

            var greenShade = rng.RandfRange(0.16f, 0.32f);
            var color = new Color(0.04f, greenShade, 0.05f);
            AddFixedMarker(color, position);
        }
    }

    private Vector2 GetSpreadOutForestPosition(RandomNumberGenerator rng, List<Vector2> existing)
    {
        const float min = -12.0f;
        const float max = 12.0f;
        const float minDistance = 3.2f;

        for (int attempt = 0; attempt < 18; attempt++)
        {
            var candidate = new Vector2(rng.RandfRange(min, max), rng.RandfRange(min, max));
            var farEnough = true;

            foreach (var point in existing)
            {
                if (candidate.DistanceTo(point) < minDistance)
                {
                    farEnough = false;
                    break;
                }
            }

            if (farEnough)
            {
                return candidate;
            }
        }

        return new Vector2(rng.RandfRange(min, max), rng.RandfRange(min, max));
    }

    private ulong BuildForestSeed()
    {
        ulong seed = 1469598103934665603UL;
        var nameText = Name.ToString();
        foreach (var character in nameText)
        {
            seed ^= character;
            seed *= 1099511628211UL;
        }

        seed ^= (ulong)(Elevation * 100000.0f);
        seed ^= (ulong)(Moisture * 100000.0f) << 1;
        seed ^= (ulong)(Temperature * 100000.0f) << 2;
        return seed;
    }
}
