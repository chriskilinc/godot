using Godot;
using System;
using System.Collections.Generic;

public sealed class TileResources
{
    private readonly Dictionary<ResourceType, int> _resources = new();

    public bool HasAny => _resources.Count > 0;

    public void Add(ResourceType resourceType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (_resources.TryGetValue(resourceType, out var existingAmount))
        {
            _resources[resourceType] = existingAmount + amount;
            return;
        }

        _resources[resourceType] = amount;
    }

    public int GetAmount(ResourceType resourceType)
    {
        return _resources.TryGetValue(resourceType, out var amount) ? amount : 0;
    }

    public bool TryDeplete(ResourceType resourceType, int amount, out int depleted)
    {
        depleted = 0;
        if (amount <= 0)
        {
            return false;
        }

        if (!_resources.TryGetValue(resourceType, out var available) || available <= 0)
        {
            return false;
        }

        depleted = Mathf.Min(available, amount);
        var remaining = available - depleted;
        if (remaining <= 0)
        {
            _resources.Remove(resourceType);
        }
        else
        {
            _resources[resourceType] = remaining;
        }

        return depleted > 0;
    }

    public string ToDebugLabel()
    {
        if (_resources.Count == 0)
        {
            return "None";
        }

        var parts = new List<string>();
        foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
        {
            if (_resources.TryGetValue(resourceType, out var amount) && amount > 0)
            {
                parts.Add($"{resourceType}:{amount}");
            }
        }

        return string.Join(", ", parts);
    }
}
