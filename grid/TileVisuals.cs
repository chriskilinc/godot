using Godot;

public sealed class TileVisuals
{
    private const float HalfSize = 16.0f;

    private readonly Node2D _owner;
    private readonly Sprite2D _sprite;
    private Line2D _selectionOutline = null;
    private Polygon2D _baseSurface = null;

    public TileVisuals(Node2D owner, Sprite2D sprite)
    {
        _owner = owner;
        _sprite = sprite;
    }

    public void Initialize()
    {
        EnsureBaseSurface();
        CreateSelectionOutline();
    }

    public void SetSelected(bool selected)
    {
        if (_selectionOutline is null)
        {
            return;
        }

        _selectionOutline.Visible = selected;
    }

    public void UpdateBiomeVisual(BiomeType biome)
    {
        var biomeColor = TerrainRules.GetBiomeColor(biome);
        _sprite.Modulate = biomeColor;

        if (_baseSurface is not null)
        {
            _baseSurface.Color = biomeColor;
        }
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

        foreach (var point in CreateSquarePolygon(HalfSize))
        {
            _selectionOutline.AddPoint(point);
        }

        _owner.AddChild(_selectionOutline);
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
            Polygon = CreateSquarePolygon(HalfSize)
        };

        _owner.AddChild(_baseSurface);
    }

    private static Vector2[] CreateSquarePolygon(float halfExtent)
    {
        return new[]
        {
            new Vector2(-halfExtent, -halfExtent),
            new Vector2(halfExtent, -halfExtent),
            new Vector2(halfExtent, halfExtent),
            new Vector2(-halfExtent, halfExtent)
        };
    }
}