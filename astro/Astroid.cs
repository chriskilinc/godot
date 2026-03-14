using Godot;
using System;

public partial class Astroid : RigidBody2DWrap
{
	const float startingForce = 75f;

	public override void _Ready()
	{
		base._Ready();
		ApplyImpulse(Utility.RandomUnitVector() * Utility.RandomRange(startingForce / 2, startingForce * 2)); // apply a random impulse to the astroid to get it moving
	}
}
