using Godot;
using System;

public partial class World : Node2D
{
	private const int GridWidth = 50;
	private const int GridHeight = 50;
	private const int TileSize = 32;

	private readonly PackedScene _tileScene = GD.Load<PackedScene>("res://tile.tscn");
	private Tile[,] _tiles = new Tile[GridWidth, GridHeight];
	private Tile? _selectedTile;
	private float _offsetX;
	private float _offsetY;

	public override void _Ready()
	{
		_offsetX = (GridWidth - 1) * TileSize * 0.5f;
		_offsetY = (GridHeight - 1) * TileSize * 0.5f;

		for (int y = 0; y < GridHeight; y++)
		{
			for (int x = 0; x < GridWidth; x++)
			{
				var tile = _tileScene.Instantiate<Tile>();
				tile.Name = $"Tile_{x}_{y}";
				tile.Position = new Vector2(x * TileSize - _offsetX, y * TileSize - _offsetY);
				AddChild(tile);
				_tiles[x, y] = tile;
			}
		}
	}

	public bool TrySelectTileAtWorld(Vector2 worldPosition)
	{
		var x = Mathf.RoundToInt((worldPosition.X + _offsetX) / TileSize);
		var y = Mathf.RoundToInt((worldPosition.Y + _offsetY) / TileSize);

		if (x < 0 || y < 0 || x >= GridWidth || y >= GridHeight)
		{
			return false;
		}

		SelectTile(_tiles[x, y]);
		return true;
	}

	private void SelectTile(Tile tile)
	{
		if (_selectedTile == tile)
		{
			GD.Print(tile.Name);
			return;
		}

		_selectedTile?.SetSelected(false);
		_selectedTile = tile;
		_selectedTile.SetSelected(true);
		GD.Print(_selectedTile.Name);
	}
}
