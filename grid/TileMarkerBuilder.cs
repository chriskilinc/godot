using Godot;
using System.Collections.Generic;

public static class TileMarkerBuilder
{
    private const float MarkerSize = 4.0f;

    public static Node2D CreateMarkerLayer()
    {
        return new Node2D
        {
            Name = "ResourceMarkers",
            ZIndex = 5
        };
    }

    public static void Refresh(Node2D markerLayer, bool hasForest, ulong forestSeed, int grainsAmount, int stoneAmount, int ironAmount, int fishAmount)
    {
        ClearChildren(markerLayer);

        if (hasForest)
        {
            AddForestMarkers(markerLayer, forestSeed);
        }

        AddMarkerIfPresent(markerLayer, grainsAmount, new Color(0.98f, 0.87f, 0.19f), new Vector2(-7.0f, -7.0f));
        AddMarkerIfPresent(markerLayer, stoneAmount, new Color(0.76f, 0.76f, 0.80f), new Vector2(7.0f, -7.0f));
        AddMarkerIfPresent(markerLayer, ironAmount, new Color(0.83f, 0.37f, 0.28f), new Vector2(-7.0f, 7.0f));
        AddMarkerIfPresent(markerLayer, fishAmount, new Color(0.24f, 0.93f, 0.97f), new Vector2(7.0f, 7.0f));
    }

    public static ulong BuildForestSeed(string nameText, float elevation, float moisture, float temperature)
    {
        ulong seed = 1469598103934665603UL;

        foreach (var character in nameText)
        {
            seed ^= character;
            seed *= 1099511628211UL;
        }

        seed ^= (ulong)(elevation * 100000.0f);
        seed ^= (ulong)(moisture * 100000.0f) << 1;
        seed ^= (ulong)(temperature * 100000.0f) << 2;
        return seed;
    }

    private static void ClearChildren(Node2D markerLayer)
    {
        foreach (var child in markerLayer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private static void AddMarkerIfPresent(Node2D markerLayer, int amount, Color color, Vector2 position)
    {
        if (amount <= 0)
        {
            return;
        }

        markerLayer.AddChild(CreateMarker(color, position));
    }

    private static void AddForestMarkers(Node2D markerLayer, ulong seed)
    {
        var rng = new RandomNumberGenerator
        {
            Seed = seed
        };

        var count = 6 + rng.RandiRange(0, 3);
        var positions = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            var position = GetSpreadOutForestPosition(rng, positions);
            positions.Add(position);

            var greenShade = rng.RandfRange(0.16f, 0.32f);
            var color = new Color(0.04f, greenShade, 0.05f);
            markerLayer.AddChild(CreateMarker(color, position));
        }
    }

    private static Vector2 GetSpreadOutForestPosition(RandomNumberGenerator rng, List<Vector2> existing)
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

    private static Polygon2D CreateMarker(Color color, Vector2 position)
    {
        return new Polygon2D
        {
            Color = color,
            Position = position,
            Polygon = CreateSquarePolygon(MarkerSize * 0.5f)
        };
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