using Godot;
using System;

[GlobalClass]
public partial class StatBuff : Resource
{
    public enum BuffType
    {
        MULTIPLICATIVE,
        ADDITIVE,
        SUBTRACTIVE
    }

    [Export]
    public BuffType Type { get; set; }

    [Export]
    public Stats.BuffableStat TargetStat { get; set; }

    [Export]
    public double Amount { get; set; }

    // public BuffType TypeValue => Type;
    // public Stats.BuffableStat TargetStatValue => TargetStat;
    // public double AmountValue => Amount;

    public StatBuff()
    {
    }

    public StatBuff(BuffType type, Stats.BuffableStat targetStat, double amount)
    {
        Type = type;
        TargetStat = targetStat;
        Amount = amount;
    }
}
