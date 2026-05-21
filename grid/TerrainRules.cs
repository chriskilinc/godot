using Godot;
using System;

public static class TerrainRules
{
    public static readonly BiomeSettings DefaultBiomeSettings = new(
        0.25f,
        0.80f,
        0.62f,
        0.25f,
        0.60f);
	public static readonly ForestSettings DefaultForestSettings = new(
		0.55f,
		0.30f,
		0.80f);

    private static readonly BiomeRule[] s_biomeRules =
    {
        new(BiomeType.Lake, maxElevationSelector: settings => settings.LakeMaxElevation),
        new(BiomeType.Mountain, minElevationExclusiveSelector: settings => settings.MountainMinElevationExclusive),
        new(BiomeType.Hills, minElevationExclusiveSelector: settings => settings.HillsMinElevationExclusive),
        new(
            BiomeType.Desert,
            maxMoistureExclusiveSelector: settings => settings.DesertMaxMoistureExclusive,
            minTemperatureExclusiveSelector: settings => settings.DesertMinTemperatureExclusive)
    };
    private static readonly ForestRule[] s_forestRules =
    {
        new(
            biome => biome == BiomeType.Grassland || biome == BiomeType.Hills,
            minMoistureExclusiveSelector: settings => settings.MinMoistureExclusive,
            minTemperatureExclusiveSelector: settings => settings.MinTemperatureExclusive,
            maxTemperatureExclusiveSelector: settings => settings.MaxTemperatureExclusive)
    };

    public static BiomeType DetermineBiome(float elevation, float moisture, float temperature)
    {
        return DetermineBiome(elevation, moisture, temperature, DefaultBiomeSettings);
    }

    public static BiomeType DetermineBiome(float elevation, float moisture, float temperature, BiomeSettings settings)
    {
        foreach (var rule in s_biomeRules)
        {
            if (rule.Matches(elevation, moisture, temperature, settings))
            {
                return rule.Biome;
            }
        }

        return BiomeType.Grassland;
    }

    public static bool DetermineForest(BiomeType biome, float moisture, float temperature)
    {
        return DetermineForest(biome, moisture, temperature, DefaultForestSettings);
    }

    public static bool DetermineForest(BiomeType biome, float moisture, float temperature, ForestSettings settings)
    {
        foreach (var rule in s_forestRules)
        {
            if (rule.Matches(biome, moisture, temperature, settings))
            {
                return true;
            }
        }

        return false;
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

    public readonly struct BiomeSettings(
        float lakeMaxElevation,
        float mountainMinElevationExclusive,
        float hillsMinElevationExclusive,
        float desertMaxMoistureExclusive,
        float desertMinTemperatureExclusive)
    {
        public float LakeMaxElevation { get; } = lakeMaxElevation;
        public float MountainMinElevationExclusive { get; } = mountainMinElevationExclusive;
        public float HillsMinElevationExclusive { get; } = hillsMinElevationExclusive;
        public float DesertMaxMoistureExclusive { get; } = desertMaxMoistureExclusive;
        public float DesertMinTemperatureExclusive { get; } = desertMinTemperatureExclusive;
    }

    public readonly struct ForestSettings(
        float minMoistureExclusive,
        float minTemperatureExclusive,
        float maxTemperatureExclusive)
    {
        public float MinMoistureExclusive { get; } = minMoistureExclusive;
        public float MinTemperatureExclusive { get; } = minTemperatureExclusive;
        public float MaxTemperatureExclusive { get; } = maxTemperatureExclusive;
    }

    private readonly struct BiomeRule(
        BiomeType biome,
        Func<BiomeSettings, float> maxElevationSelector = null,
        Func<BiomeSettings, float> minElevationExclusiveSelector = null,
        Func<BiomeSettings, float> maxMoistureExclusiveSelector = null,
        Func<BiomeSettings, float> minTemperatureExclusiveSelector = null)
    {
        public BiomeType Biome { get; } = biome;

        public bool Matches(float elevation, float moisture, float temperature, BiomeSettings settings)
        {
            if (maxElevationSelector is not null && elevation >= maxElevationSelector(settings))
            {
                return false;
            }

            if (minElevationExclusiveSelector is not null && elevation <= minElevationExclusiveSelector(settings))
            {
                return false;
            }

            if (maxMoistureExclusiveSelector is not null && moisture >= maxMoistureExclusiveSelector(settings))
            {
                return false;
            }

            if (minTemperatureExclusiveSelector is not null && temperature <= minTemperatureExclusiveSelector(settings))
            {
                return false;
            }

            return true;
        }
    }

    private readonly struct ForestRule(
        Func<BiomeType, bool> canSpawn,
        Func<ForestSettings, float> minMoistureExclusiveSelector = null,
        Func<ForestSettings, float> minTemperatureExclusiveSelector = null,
        Func<ForestSettings, float> maxTemperatureExclusiveSelector = null)
    {
        public bool Matches(BiomeType biome, float moisture, float temperature, ForestSettings settings)
        {
            if (!canSpawn(biome))
            {
                return false;
            }

            if (minMoistureExclusiveSelector is not null && moisture <= minMoistureExclusiveSelector(settings))
            {
                return false;
            }

            if (minTemperatureExclusiveSelector is not null && temperature <= minTemperatureExclusiveSelector(settings))
            {
                return false;
            }

            if (maxTemperatureExclusiveSelector is not null && temperature >= maxTemperatureExclusiveSelector(settings))
            {
                return false;
            }

            return true;
        }
    }
}
