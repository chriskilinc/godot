using Godot;

public class Utility
{
  public static float RandomRange(float min, float max)
  {
    return (float)GD.RandRange(min, max);
  }

  public static Vector2 RandomUnitVector()
  {
    var angle = RandomRange(0.0f, Mathf.Tau);
    return Vector2.Up.Rotated(angle);
  }
}