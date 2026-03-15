using Godot;
using System;

public partial class UI : Control
{
  private Label scoreLabel;
  private Label livesLabel;
  private Label levelLabel;
  private Panel GameOverPanel;

  private Main mainNode;

  public override void _Ready()
  {
    scoreLabel = GetNode<Label>("%Score");
    livesLabel = GetNode<Label>("%Lives");
    levelLabel = GetNode<Label>("%Level");
    GameOverPanel = GetNode<Panel>("%GameOverPanel");

    mainNode = GetParent<Main>();
    if (mainNode != null)
    {
      mainNode.Connect(Main.SignalName.ScoreUpdated, new Callable(this, nameof(UpdateScore)));
      mainNode.Connect(Main.SignalName.LivesUpdated, new Callable(this, nameof(UpdateLives)));
      mainNode.Connect(Main.SignalName.LevelUpdated, new Callable(this, nameof(UpdateLevel)));
      mainNode.Connect(Main.SignalName.GameOver, new Callable(this, nameof(OnGameOver)));
    }

    GameOverPanel.Visible = false;
  }

  public void _on_restart_button_pressed()
  {
    GD.Print("[UI] Restart button pressed");
    GameOverPanel.Visible = false;
    mainNode.SetupNewGame();
  }

  public void UpdateScore(int score)
  {
    scoreLabel.Text = $"Score: {score}";
  }

  public void UpdateLives(int lives)
  {
    livesLabel.Text = $"Lives: {lives}";
  }

  public void UpdateLevel(int level)
  {
    levelLabel.Text = $"Level: {level}";
  }

  private void OnGameOver()
  {
    GD.Print("[UI] Game over!");
    GameOverPanel.Visible = true;
  }
}
