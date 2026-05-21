using Godot;
using System;

public partial class UI : Control
{
    private Panel _actionPanel;
    private Label _name;
    private Label _biome;
    private Label _terrain;
    private Label _elevation;
    private Label _forest;
    private Label _resources;

    public override void _Ready()
    {
        _actionPanel = GetNode<Panel>("%ActionPanel");
        _name = _actionPanel.GetNode<Label>("%Name");
        _biome = _actionPanel.GetNode<Label>("%Biome");
        _terrain = _actionPanel.GetNode<Label>("%Terrain");
        _elevation = _actionPanel.GetNode<Label>("%Elevation");
        _forest = _actionPanel.GetNode<Label>("%Forest");
        _resources = _actionPanel.GetNode<Label>("%Resources");
        HideActionPanel();
    }

    public void ShowTileInfo(Tile tile)
    {
        _name.Text = tile.Name;
        _biome.Text = $"Biome: {tile.Biome}";
        _elevation.Text = $"Elevation: {tile.Elevation:F2}, Moisture: {tile.Moisture:F2}, Temp: {tile.Temperature:F2}";
        _terrain.Text = $"Terrain: {tile.TerrainLabel}";
        _forest.Text = tile.HasForest ? "Has Forest" : "No Forest";
        _resources.Text = tile.HasAnyResources ? $"Resources: {tile.GetResourcesLabel()}" : "No Resources";
        _actionPanel.Visible = true;
    }

    public void HideActionPanel()
    {
        _actionPanel.Visible = false;
    }

    public void ShowActionPanel()
    {
        _actionPanel.Visible = true;
    }
}
