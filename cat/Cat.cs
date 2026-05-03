using Godot;
using System;

public partial class Cat : Node2D
{
	[Export] public float MoveSpeed { get; set; } = 140.0f;
	[Export] public float JumpVelocity { get; set; } = 300.0f;
	[Export] public float Gravity { get; set; } = 900.0f;
	[Export] public float AirControl { get; set; } = 0.65f;

	private AnimatedSprite2D _animatedSprite = null!;
	private Vector2 _velocity = Vector2.Zero;
	private float _groundY;
	private int _facingDirection = 1;
	private bool _wasJumpPressed;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_groundY = Position.Y;
		PlayAnimation(CatAnimations.AnimationName.Idle);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		bool moveLeft = Input.IsActionPressed("player_left");
		bool moveRight = Input.IsActionPressed("player_right");
		bool jumpPressed = Input.IsActionPressed("player_jump");

		float horizontalInput = (moveRight ? 1.0f : 0.0f) - (moveLeft ? 1.0f : 0.0f);
		bool onGround = Position.Y >= _groundY - 0.001f;

		if (onGround)
		{
			Position = new Vector2(Position.X, _groundY);
			if (_velocity.Y > 0.0f)
			{
				_velocity.Y = 0.0f;
			}
		}

		if (horizontalInput != 0.0f)
		{
			_facingDirection = horizontalInput > 0.0f ? 1 : -1;
			_animatedSprite.FlipH = _facingDirection < 0;
		}

		float targetHorizontalVelocity = horizontalInput * MoveSpeed;
		if (onGround)
		{
			_velocity.X = targetHorizontalVelocity;
		}
		else
		{
			float airAccel = MoveSpeed * AirControl * 8.0f;
			_velocity.X = Mathf.MoveToward(_velocity.X, targetHorizontalVelocity, airAccel * dt);
		}

		bool jumpJustPressed = jumpPressed && !_wasJumpPressed;
		if (jumpJustPressed && onGround)
		{
			_velocity.Y = -JumpVelocity;

			// Keep jump momentum in the chosen direction, or current facing if no A/D input.
			if (horizontalInput != 0.0f)
			{
				_velocity.X = horizontalInput * MoveSpeed;
			}
			else
			{
				_velocity.X = _facingDirection * (MoveSpeed * 0.6f);
			}

			onGround = false;
		}
		_wasJumpPressed = jumpPressed;

		if (!onGround)
		{
			_velocity.Y += Gravity * dt;
		}

		Position += _velocity * dt;

		if (Position.Y > _groundY)
		{
			Position = new Vector2(Position.X, _groundY);
			_velocity.Y = 0.0f;
			onGround = true;
		}

		UpdateAnimation(onGround, horizontalInput);
	}

	private void UpdateAnimation(bool onGround, float horizontalInput)
	{
		if (!onGround)
		{
			PlayAnimation(CatAnimations.AnimationName.WalkLunge); // WalkLunge vs Jump? - Jump is more of an attack maybe
			return;
		}

		if (Mathf.Abs(horizontalInput) > 0.01f)
		{
			PlayAnimation(CatAnimations.AnimationName.Walk);
			return;
		}

		PlayAnimation(CatAnimations.AnimationName.Idle);
	}

	private void PlayAnimation(CatAnimations.AnimationName animationName)
	{
		string animationKey = animationName.ToAnimationKey();
		if (_animatedSprite.Animation != animationKey)
		{
			_animatedSprite.Play(animationKey);
		}
	}
}
