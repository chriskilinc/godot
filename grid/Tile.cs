using Godot;
using System;

public partial class Tile : Node2D
{
    private const float HalfSize = 16.0f;
    private Line2D? _selectionOutline;

    public override void _Ready()
    {
        CreateSelectionOutline();
    }

    public void SetSelected(bool selected)
    {
        if (_selectionOutline is null)
        {
            return;
        }

        _selectionOutline.Visible = selected;
    }

    private void CreateSelectionOutline()
    {
        _selectionOutline = new Line2D
        {
            DefaultColor = new Color(0.95f, 0.95f, 0.2f, 1.0f),
            Width = 2.0f,
            Closed = true,
            Antialiased = true,
            Visible = false,
            ZIndex = 10
        };

        _selectionOutline.AddPoint(new Vector2(-HalfSize, -HalfSize));
        _selectionOutline.AddPoint(new Vector2(HalfSize, -HalfSize));
        _selectionOutline.AddPoint(new Vector2(HalfSize, HalfSize));
        _selectionOutline.AddPoint(new Vector2(-HalfSize, HalfSize));
        AddChild(_selectionOutline);
    }
}
