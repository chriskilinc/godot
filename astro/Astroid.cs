using Godot;
using System;

public partial class Astroid : RigidBody2DWrap
{
	[Signal]
	public delegate void AstroidHitEventHandler();

	[Export]
	public PackedScene debrisScene { get; set; }
	[Export]
	int debrisAmount = 3;

	public float startingForce { get; set; } = 75f;

	public override void _Ready()
	{
		base._Ready();
		ApplyImpulse(Utility.RandomUnitVector() * Utility.RandomRange(startingForce / 2, startingForce * 2)); // apply a random impulse to the astroid to get it moving
	}

	public void _on_astroid_hit()
	{
		GD.Print("Astroid hit!" + Name);
		if (debrisScene != null)
		{
			for (int i = 0; i < debrisAmount; i++)
			{
				SpawnDebris();
			}
		}
		QueueFree();
	}

	private void SpawnDebris()
	{
		var debris = debrisScene.Instantiate<Astroid>();
		debris.GlobalPosition = GlobalPosition;
		debris.startingForce = startingForce / 2; // reduce the starting force
		GetParent().AddChild(debris);
	}
}
