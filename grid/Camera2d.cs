using Godot;
using System;

public partial class Camera2d : Camera2D
{
	private World? _world;

	[Export]
	public float MoveSpeed { get; set; } = 900.0f;

	[Export]
	public float ZoomStep { get; set; } = 0.1f;

	[Export]
	public float MinZoom { get; set; } = 0.5f;

	[Export]
	public float MaxZoom { get; set; } = 3.0f;

	public override void _Ready()
	{
		Enabled = true;
		MakeCurrent();
		_world = GetParent<World>();
	}

	public override void _Process(double delta)
	{
		var input = Input.GetVector("camera_left", "camera_right", "camera_up", "camera_down");
		if (input == Vector2.Zero)
		{
			return;
		}

		Position += input * MoveSpeed * (float)delta;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
		{
			return;
		}

		if (mouseButton.ButtonIndex == MouseButton.WheelDown)
		{
			ApplyZoom(-ZoomStep);
		}
		else if (mouseButton.ButtonIndex == MouseButton.WheelUp)
		{
			ApplyZoom(ZoomStep);
		}
		else if (mouseButton.ButtonIndex == MouseButton.Left)
		{
			_world?.TrySelectTileAtWorld(GetGlobalMousePosition());
		}
	}

	private void ApplyZoom(float step)
	{
		var nextZoom = Mathf.Clamp(Zoom.X + step, MinZoom, MaxZoom);
		Zoom = new Vector2(nextZoom, nextZoom);
	}
}
