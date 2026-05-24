using Godot;
using System.Collections.Generic;

public sealed class TileVisuals
{
    private const float HalfSize = 16.0f;

    private readonly Node2D _owner;
    private readonly Sprite2D _sprite;
    private Line2D _selectionOutline = null;
    private Polygon2D _baseSurface = null;
    private Node2D _riverOverlay = null;

    public TileVisuals(Node2D owner, Sprite2D sprite)
    {
        _owner = owner;
        _sprite = sprite;
    }

    public void Initialize()
    {
        EnsureBaseSurface();
        EnsureRiverOverlay();
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

    public void UpdateBiomeVisual(BiomeType biome, WaterKind waterKind, float waterDepth, RiverConnections connections)
    {
        var biomeColor = TerrainRules.GetBiomeColor(biome);
        _sprite.Modulate = biomeColor;

        if (_baseSurface is not null)
        {
            _baseSurface.Color = biomeColor;
        }

        UpdateWaterOverlay(waterKind, waterDepth, connections);
    }

    private void UpdateWaterOverlay(WaterKind waterKind, float waterDepth, RiverConnections connections)
    {
        if (_riverOverlay is null)
        {
            return;
        }

        ClearRiverOverlay();

        if (waterKind != WaterKind.River)
        {
            return;
        }

        var clampedDepth = Mathf.Clamp(waterDepth, 0.0f, 1.0f);
        var channelWidth = Mathf.Lerp(2.0f, 7.0f, clampedDepth);
        var channelColor = new Color(
            Mathf.Lerp(0.21f, 0.16f, clampedDepth),
            Mathf.Lerp(0.58f, 0.44f, clampedDepth),
            Mathf.Lerp(0.84f, 0.75f, clampedDepth),
            0.95f);

        var hasAnyConnection = false;
        var arms = GetRiverArms(connections);
        foreach (var arm in arms)
        {
            hasAnyConnection = true;
        }

        if (!hasAnyConnection)
        {
            AddRiverSegment(
                new Vector2(0.0f, HalfSize - 1.0f),
                new Vector2(0.0f, -HalfSize + 1.0f),
                channelWidth,
                channelColor);
            return;
        }

        DrawRiverShape(arms, channelWidth, channelColor);
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

    private void EnsureRiverOverlay()
    {
        if (_riverOverlay is not null)
        {
            return;
        }

        _riverOverlay = new Node2D
        {
            Name = "RiverOverlay",
            ZIndex = 2
        };

        _owner.AddChild(_riverOverlay);
    }

    private void ClearRiverOverlay()
    {
        if (_riverOverlay is null)
        {
            return;
        }

        foreach (var child in _riverOverlay.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void DrawRiverShape(Vector2[] arms, float channelWidth, Color channelColor)
    {
        if (arms.Length == 0)
        {
            return;
        }

        if (arms.Length == 1)
        {
            AddRiverSegment(Vector2.Zero, arms[0], channelWidth, channelColor);
            return;
        }

        if (arms.Length == 2)
        {
            AddRiverSegment(arms[0], arms[1], channelWidth, channelColor);
            return;
        }

        FindPrimaryChannelPair(arms, out var firstIndex, out var secondIndex);
        AddRiverSegment(arms[firstIndex], arms[secondIndex], channelWidth, channelColor);

        var branchIndex = PickBranchIndex(arms.Length, firstIndex, secondIndex);
        if (branchIndex < 0)
        {
            return;
        }

        var branchColor = new Color(channelColor.R, channelColor.G, channelColor.B, channelColor.A * 0.86f);
        AddRiverSegment(Vector2.Zero, arms[branchIndex], channelWidth * 0.74f, branchColor);
    }

    private void AddRiverSegment(Vector2 start, Vector2 end, float channelWidth, Color channelColor)
    {
        if (_riverOverlay is null)
        {
            return;
        }

        var line = new Line2D
        {
            DefaultColor = channelColor,
            Width = channelWidth,
            Antialiased = true,
            RoundPrecision = 8,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round
        };

        line.AddPoint(start);

        var segment = end - start;
        var segmentLength = segment.Length();
        if (segmentLength > 0.01f)
        {
            var startDirection = start.LengthSquared() > 0.01f ? start.Normalized() : Vector2.Zero;
            var endDirection = end.LengthSquared() > 0.01f ? end.Normalized() : Vector2.Zero;
            var directionDot = startDirection == Vector2.Zero || endDirection == Vector2.Zero
                ? -1.0f
                : startDirection.Dot(endDirection);

            if (directionDot > -0.90f)
            {
                var tangent = segment / segmentLength;
                var normal = new Vector2(-tangent.Y, tangent.X);
                var seed = Mathf.Abs((_owner.Name.ToString() + start + end).GetHashCode());
                var sign = (seed & 1) == 0 ? -1.0f : 1.0f;
                var bendStrength = Mathf.Clamp(segmentLength * 0.16f, 0.8f, 2.2f);
                var midpoint = (start + end) * 0.5f;
                line.AddPoint(midpoint + normal * bendStrength * sign);
            }
        }

        line.AddPoint(end);
        _riverOverlay.AddChild(line);
    }

    private static void FindPrimaryChannelPair(Vector2[] arms, out int firstIndex, out int secondIndex)
    {
        firstIndex = 0;
        secondIndex = 1;

        var bestDot = float.MaxValue;
        for (int i = 0; i < arms.Length; i++)
        {
            for (int j = i + 1; j < arms.Length; j++)
            {
                var dot = arms[i].Normalized().Dot(arms[j].Normalized());
                if (dot >= bestDot)
                {
                    continue;
                }

                bestDot = dot;
                firstIndex = i;
                secondIndex = j;
            }
        }
    }

    private int PickBranchIndex(int armCount, int firstIndex, int secondIndex)
    {
        var remainingCount = armCount - 2;
        if (remainingCount <= 0)
        {
            return -1;
        }

        var seed = Mathf.Abs(_owner.Name.ToString().GetHashCode());
        var target = seed % remainingCount;
        var seen = 0;

        for (int i = 0; i < armCount; i++)
        {
            if (i == firstIndex || i == secondIndex)
            {
                continue;
            }

            if (seen == target)
            {
                return i;
            }

            seen++;
        }

        return -1;
    }

    private static Vector2[] GetRiverArms(RiverConnections connections)
    {
        var edge = HalfSize - 1.0f;
        var arms = new List<Vector2>();

        if (connections.HasFlag(RiverConnections.North))
        {
            arms.Add(new Vector2(0.0f, -edge));
        }

        if (connections.HasFlag(RiverConnections.NorthEast))
        {
            arms.Add(new Vector2(edge, -edge));
        }

        if (connections.HasFlag(RiverConnections.East))
        {
            arms.Add(new Vector2(edge, 0.0f));
        }

        if (connections.HasFlag(RiverConnections.SouthEast))
        {
            arms.Add(new Vector2(edge, edge));
        }

        if (connections.HasFlag(RiverConnections.South))
        {
            arms.Add(new Vector2(0.0f, edge));
        }

        if (connections.HasFlag(RiverConnections.SouthWest))
        {
            arms.Add(new Vector2(-edge, edge));
        }

        if (connections.HasFlag(RiverConnections.West))
        {
            arms.Add(new Vector2(-edge, 0.0f));
        }

        if (connections.HasFlag(RiverConnections.NorthWest))
        {
            arms.Add(new Vector2(-edge, -edge));
        }

        return arms.ToArray();
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