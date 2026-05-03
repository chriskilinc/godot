using Godot;
using System;

public partial class CatAnimations : GodotObject
{
    public enum AnimationName
    {
        Action,
        Angry,
        Clean,
        Clean2,
        Idle,
        Jump,
        Sleep,
        Walk,
        WalkLunge
    }
}

// Usage: var key = CatAnimations.AnimationName.Walk.ToAnimationKey(); // "walk"
public static class AnimationNameExtensions
{
    public static string ToAnimationKey(this CatAnimations.AnimationName animationName)
    {
        return animationName switch
        {
            CatAnimations.AnimationName.Action => "action",
            CatAnimations.AnimationName.Angry => "angry",
            CatAnimations.AnimationName.Clean => "clean",
            CatAnimations.AnimationName.Clean2 => "clean2",
            CatAnimations.AnimationName.Idle => "idle",
            CatAnimations.AnimationName.Jump => "jump",
            CatAnimations.AnimationName.Sleep => "sleep",
            CatAnimations.AnimationName.Walk => "walk",
            CatAnimations.AnimationName.WalkLunge => "walkLunge",
            _ => throw new ArgumentOutOfRangeException(nameof(animationName), animationName, "Unknown animation name")
        };
    }
}
