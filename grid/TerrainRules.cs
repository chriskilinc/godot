using Godot;

public static class TerrainRules
{
    public static BiomeType DetermineBiome(float elevation, float moisture, float temperature)
    {
        if (elevation < 0.25f)
        {
            return BiomeType.Lake;
        }

        if (elevation > 0.80f)
        {
            return BiomeType.Mountain;
        }

        if (elevation > 0.62f)
        {
            return BiomeType.Hills;
        }

        if (moisture < 0.25f && temperature > 0.6f)
        {
            return BiomeType.Desert;
        }

        return BiomeType.Grassland;
    }

    public static bool DetermineForest(BiomeType biome, float moisture, float temperature)
    {
        if (biome != BiomeType.Grassland && biome != BiomeType.Hills)
        {
            return false;
        }

        return moisture > 0.55f && temperature > 0.30f && temperature < 0.80f;
    }

    public static Color GetBiomeColor(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Lake => new Color("5574a0"),
            BiomeType.Desert => new Color(0.63f, 0.57f, 0.40f),
            BiomeType.Grassland => new Color(0.33f, 0.49f, 0.31f),
            BiomeType.Hills => new Color(0.30f, 0.42f, 0.29f),
            BiomeType.Mountain => new Color(0.49f, 0.49f, 0.51f),
            _ => Colors.White
        };
    }
}
