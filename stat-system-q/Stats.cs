using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class Stats : Resource
{
    public enum BuffableStat
    {
        MaxHealth,
        Attack,
        Defense
    }

    private const double BaseLevelExperience = 100.0;

    [Export]
    private int MaxLevel { get; set; } = 100;

    [Export]
    private Curve MaxHealthCurve { get; set; } = GD.Load<Curve>("res://curves/MaxHealthCurve.tres");
    [Export]
    private Curve AttackCurve { get; set; } = GD.Load<Curve>("res://curves/AttackCurve.tres");
    [Export]
    private Curve DefenseCurve { get; set; } = GD.Load<Curve>("res://curves/DefenseCurve.tres");

    [Signal]
    public delegate void HealthDepletedEventHandler();

    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void ExperienceChangedEventHandler(int currentExperience, int level);

    [Signal]
    public delegate void LeveledUpEventHandler(int previousLevel, int currentLevel);

    [Export]
    private int BaseMaxHealth { get; set; } = 100;
    [Export]
    private int BaseAttack { get; set; } = 10;
    [Export]
    private int BaseDefense { get; set; } = 5;

    private readonly List<StatBuff> _buffs = [];
    private int _experience = 0;

    public int Experience
    {
        get => _experience;
        set => SetExperience(value);
    }

    public int Level => Math.Min(CalculateLevelForExperience(Experience), GetClampedMaxLevel());

    public int CurrentMaxHealth { get; private set; }
    public int CurrentAttack { get; private set; }
    public int CurrentDefense { get; private set; }

    private int _health = 100;
    public int Health
    {
        get => _health;
        set => SetHealth(value);
    }

    public Stats()
    {
        GD.Print("Stats created with BaseMaxHealth: ", BaseMaxHealth, ", BaseAttack: ", BaseAttack, ", BaseDefense: ", BaseDefense);
        RecalculateStatsFromLevel();
        Health = CurrentMaxHealth;
    }

    public void GainExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (_experience > int.MaxValue - amount)
        {
            SetExperience(int.MaxValue);
            return;
        }

        SetExperience(_experience + amount);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetHealth(_health - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetHealth(_health + amount);
    }

    public void AddBuff(StatBuff buff)
    {
        if (buff == null)
        {
            return;
        }

        _buffs.Add(buff);
        RecalculateStatsFromLevel();
    }

    public void AddBuff(BuffableStat stat, double percent)
    {
        AddBuff(new StatBuff(StatBuff.BuffType.MULTIPLICATIVE, stat, percent));
    }

    public void RemoveBuff(StatBuff buff)
    {
        if (buff == null)
        {
            return;
        }

        if (_buffs.Remove(buff))
        {
            RecalculateStatsFromLevel();
        }
    }

    public void ClearBuffs(BuffableStat stat)
    {
        if (_buffs.RemoveAll(buff => buff.TargetStat == stat) > 0)
        {
            RecalculateStatsFromLevel();
        }
    }

    public void ClearAllBuffs()
    {
        if (_buffs.Count == 0)
        {
            return;
        }

        _buffs.Clear();
        RecalculateStatsFromLevel();
    }

    private void SetExperience(int value)
    {
        var normalizedExperience = Math.Max(0, value);
        if (_experience == normalizedExperience)
        {
            return;
        }

        var previousLevel = Level;
        _experience = normalizedExperience;
        var currentLevel = Level;

        if (currentLevel != previousLevel)
        {
            RecalculateStatsFromLevel();

            if (currentLevel > previousLevel)
            {
                EmitSignal(SignalName.LeveledUp, previousLevel, currentLevel);
            }
        }

        EmitSignal(SignalName.ExperienceChanged, _experience, currentLevel);
    }

    private void RecalculateStatsFromLevel()
    {
        var previousMaxHealth = CurrentMaxHealth;
        var levelProgress = GetLevelProgress();
        CurrentMaxHealth = CalculateBuffedStatValue(BuffableStat.MaxHealth, BaseMaxHealth, MaxHealthCurve, levelProgress);
        CurrentAttack = CalculateBuffedStatValue(BuffableStat.Attack, BaseAttack, AttackCurve, levelProgress);
        CurrentDefense = CalculateBuffedStatValue(BuffableStat.Defense, BaseDefense, DefenseCurve, levelProgress);

        if (previousMaxHealth <= 0)
        {
            _health = CurrentMaxHealth;
            return;
        }

        var healthRatio = (double)_health / previousMaxHealth;
        SetHealth((int)Math.Round(CurrentMaxHealth * healthRatio));
    }

    private static int CalculateLevelForExperience(int experience)
    {
        var nonNegativeExperience = Math.Max(0, experience);
        return (int)Math.Floor(Math.Max(1.0, Math.Sqrt(nonNegativeExperience / BaseLevelExperience) + 0.5));
    }

    private double GetLevelProgress()
    {
        var clampedMaxLevel = Math.Max(2, GetClampedMaxLevel());
        var normalizedLevel = (double)(Level - 1) / (clampedMaxLevel - 1);
        return Math.Clamp(normalizedLevel, 0.0, 1.0);
    }

    private int GetClampedMaxLevel()
    {
        return Math.Max(1, MaxLevel);
    }

    private static double GetCurveMultiplier(Curve curve, double progress)
    {
        if (curve == null)
        {
            return 1.0;
        }

        return Math.Max(0.01, curve.SampleBaked((float)progress));
    }

    private int CalculateBuffedStatValue(BuffableStat stat, int baseStatValue, Curve curve, double levelProgress)
    {
        var scaledBaseValue = Math.Max(1.0, baseStatValue * GetCurveMultiplier(curve, levelProgress));
        var additiveBuffAmount = 0.0;
        var subtractiveBuffAmount = 0.0;
        var multiplicativeBuffAmount = 0.0;

        foreach (var buff in _buffs)
        {
            if (buff.TargetStat != stat)
            {
                continue;
            }

            switch (buff.Type)
            {
                case StatBuff.BuffType.ADDITIVE:
                    additiveBuffAmount += buff.Amount;
                    break;
                case StatBuff.BuffType.SUBTRACTIVE:
                    subtractiveBuffAmount += buff.Amount;
                    break;
                case StatBuff.BuffType.MULTIPLICATIVE:
                    multiplicativeBuffAmount += buff.Amount;
                    break;
            }
        }

        var finalValue = (scaledBaseValue + additiveBuffAmount - subtractiveBuffAmount) * (1.0 + multiplicativeBuffAmount);
        return Math.Max(1, (int)Math.Round(finalValue));
    }

    private void SetHealth(int value)
    {
        var clampedHealth = Mathf.Clamp(value, 0, CurrentMaxHealth);
        if (_health == clampedHealth)
        {
            return;
        }

        var previousHealth = _health;
        _health = clampedHealth;

        if (previousHealth > 0 && _health == 0)
        {
            EmitSignal(SignalName.HealthDepleted);
        }

        EmitSignal(SignalName.HealthChanged, _health, CurrentMaxHealth);
    }
}
