using Godot;
using System;

public partial class HealthBar : ProgressBar
{
    private Node2D _parent;
    private Vector2 _screenOffset;

    public override void _Ready()
    {
        _parent = GetParent<Node2D>();
        _screenOffset = GlobalPosition - _parent.GlobalPosition;
        TopLevel = true;
    }

    public override void _Process(double delta)
    {
        if (_parent == null || !IsInstanceValid(_parent))
        {
            return;
        }

        GlobalPosition = _parent.GlobalPosition + _screenOffset;
        Rotation = 0;
    }
}
