using Godot;
using System;

public static class TerrainRules
{
    public static readonly BiomeSettings DefaultBiomeSettings = new(
        0.23f,
        0.78f,
        0.60f,
        0.15f,
        0.60f);
	public static readonly ForestSettings DefaultForestSettings = new(
		0.48f,
		0.22f,
		0.86f);

    private static readonly BiomeRule[] s_biomeRules =
    {
        new(BiomeType.Lake, maxElevationSelector: settings => settings.LakeMaxElevation),
        new(BiomeType.Mountain, minElevationExclusiveSelector: settings => settings.MountainMinElevationExclusive),
        new(BiomeType.Hills, minElevationExclusiveSelector: settings => settings.HillsMinElevationExclusive)
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

    public static float CalculateFertility(BiomeType biome, float elevation, float moisture, float temperature, bool hasForest, WaterKind waterKind)
    {
        if (waterKind == WaterKind.Lake || waterKind == WaterKind.Ocean)
        {
            return 0.0f;
        }

        var moistureSuitability = Mathf.Clamp(1.0f - (Mathf.Abs(moisture - 0.62f) * 1.7f), 0.0f, 1.0f);
        var temperatureSuitability = Mathf.Clamp(1.0f - (Mathf.Abs(temperature - 0.57f) * 1.6f), 0.0f, 1.0f);
        var elevationSuitability = Mathf.Clamp(1.0f - (Mathf.Max(0.0f, elevation - 0.58f) * 1.6f), 0.25f, 1.0f);
        var lowlandBonus = Mathf.Clamp((0.38f - elevation) * 0.35f, -0.08f, 0.10f);

        var biomeModifier = biome switch
        {
            BiomeType.Grassland => 0.12f,
            BiomeType.Hills => -0.06f,
            BiomeType.Mountain => -0.35f,
            BiomeType.Desert => -0.20f,
            _ => 0.0f
        };

        var forestBonus = hasForest ? 0.05f : 0.0f;
        var fertility = (moistureSuitability * 0.45f)
            + (temperatureSuitability * 0.30f)
            + (elevationSuitability * 0.25f)
            + lowlandBonus
            + biomeModifier
            + forestBonus;

        if (waterKind == WaterKind.River)
        {
            // River tiles remain somewhat fertile (floodplain effect), but less than solid farmland.
            fertility = (fertility * 0.55f) + 0.12f;
        }

        return Mathf.Clamp(fertility, 0.0f, 1.0f);
    }

    public static Color GetBiomeColor(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Lake => new Color(0.28f, 0.43f, 0.58f),
            BiomeType.Desert => new Color(0.52f, 0.52f, 0.47f),
            BiomeType.Grassland => new Color(0.34f, 0.50f, 0.33f),
            BiomeType.Hills => new Color(0.30f, 0.44f, 0.31f),
            BiomeType.Mountain => new Color(0.56f, 0.57f, 0.58f),
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
