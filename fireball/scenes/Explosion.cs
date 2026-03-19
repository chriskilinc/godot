using Godot;
using Godot.Collections;

public partial class Explosion : Node2D
{
    [Export]
    public float Radius { get; set; } = 128f;

    [Export]
    public float Force { get; set; } = 1500f;

    [Export]
    public float Damage { get; set; } = 10f;

    // Which collision layers the explosion should affect.
    [Export]
    public uint ExplosionMask { get; set; } = 0xffffffff;

    public AnimatedSprite2D AnimatedSprite2D { get; set; }
    public override void _Ready()
    {
        AnimatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        AnimatedSprite2D.Play();
        AnimatedSprite2D.AnimationFinished += OnAnimationFinished;

        // Trigger the physics effect once when the explosion appears.
        // ApplyExplosion();
    }

    private void ApplyExplosion()
    {
        var world = GetWorld2D();
        if (world == null)
        {
            return;
        }

        var spaceState = world.DirectSpaceState;
        if (spaceState == null)
        {
            return;
        }

        var shape = new CircleShape2D
        {
            Radius = Radius
        };

        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(0, GlobalPosition),
            CollisionMask = ExplosionMask
        };

        Array<Dictionary> results = spaceState.IntersectShape(query, 32);

        foreach (Dictionary result in results)
        {
            if (!result.ContainsKey("collider"))
            {
                continue;
            }

            var colliderVariant = (Variant)result["collider"];
            var colliderNode = colliderVariant.As<Node2D>();
            if (colliderNode == null)
            {
                continue;
            }

            // Apply physical impulse to rigid bodies.
            if (colliderNode is RigidBody2D rigidBody)
            {
                Vector2 offset = rigidBody.GlobalPosition - GlobalPosition;
                float distance = offset.Length();
                if (distance <= 0.01f || distance > Radius)
                {
                    continue;
                }

                Vector2 direction = offset / distance;
                float falloff = Mathf.Clamp(1.0f - (distance / Radius), 0f, 1f);
                Vector2 impulse = direction * Force * falloff;
                rigidBody.ApplyImpulse(impulse);
            }

            // Optional damage: call a user-defined method if present.
            if (Damage > 0 && colliderNode.HasMethod("ApplyDamage"))
            {
                colliderNode.Call("ApplyDamage", Damage);
            }
        }
    }

    private void OnAnimationFinished()
    {
        // Wait 1 second, then free this node.
        var tree = GetTree();
        if (tree == null)
        {
            QueueFree();
            return;
        }

        var timer = tree.CreateTimer(1.0);
        timer.Timeout += () => QueueFree();
    }
}
