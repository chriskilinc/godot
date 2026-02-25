using Godot;
using System;

public partial class RigidBody2DWrap : RigidBody2D
{
  Sprite2D sprite;
  Vector2 viewportSize;
  Vector2 spriteSize;

  public override void _Ready()
  {
    sprite = GetNode<Sprite2D>("Sprite2D");
    spriteSize = sprite.Texture.GetSize() * sprite.Scale;
    viewportSize = GetViewportRect().Size;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState2D state)
  {
    base._IntegrateForces(state);
    WrapToViewport(state);
  }

  private void WrapToViewport(PhysicsDirectBodyState2D state)
  {
    // if we are off the left side of the screen, set pos to right side of the screen
    if (state.Transform.Origin.X + spriteSize.X / 2 < 0)
    {
      var transform = state.Transform;
      transform.Origin.X = viewportSize.X + spriteSize.X / 2;
      state.Transform = transform;
    }

    // if we are off the right side of the screen, set pos to left side of the screen
    if (state.Transform.Origin.X - spriteSize.X / 2 > viewportSize.X)
    {
      var transform = state.Transform;
      transform.Origin.X = -spriteSize.X / 2;
      state.Transform = transform;
    }
    if (state.Transform.Origin.Y + spriteSize.Y / 2 < 0)
    {
      var transform = state.Transform;
      transform.Origin.Y = viewportSize.Y + spriteSize.Y / 2;
      state.Transform = transform;
    }
    if (state.Transform.Origin.Y - spriteSize.Y / 2 > viewportSize.Y)
    {
      var transform = state.Transform;
      transform.Origin.Y = -spriteSize.Y / 2;
      state.Transform = transform;
    }
  }
}