using Godot;
using System;

public partial class slime : Node2D
{
    private float _speed = 60;
    private float _direction = 1;

    RayCast2D _rayCastRight;
    RayCast2D _rayCastLeft;
    AnimatedSprite2D _animatedSprite;

    public override void _Ready()
    {
        GD.Print("Slime ready");
        _rayCastRight = GetNode<RayCast2D>("RayCastRight");
        _rayCastLeft = GetNode<RayCast2D>("RayCastLeft");
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public override void _Process(double delta)
    {
        // move the slime to the right by 1 pixel every frame times delta
        Position += new Vector2(_direction * _speed, 0) * (float)delta;

        // if the raycast to the right hits something, change direction to left
        if (_rayCastRight.IsColliding())
        {
            _direction = -1;
            _animatedSprite.FlipH = true;
        }
        else if (_rayCastLeft.IsColliding())
        {
            _direction = 1;
            _animatedSprite.FlipH = false;
        }
    }
}
