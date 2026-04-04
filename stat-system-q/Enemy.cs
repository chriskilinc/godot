using Godot;
using System;

public partial class Enemy : Node2D
{
    [Export]
    public Stats Stats { get; set; }
}
