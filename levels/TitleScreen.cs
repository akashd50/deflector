using Godot;
using System;

public partial class TitleScreen : Control
{
	private Button _startButton;
	private Button _quitButton;
	public override void _Ready()
	{
		_startButton = GetNode<Button>("StartButton");
		_startButton.Pressed += () =>
		{
			GetTree().ChangeSceneToFile("res://levels/level-1.tscn");
		};
		
		_quitButton = GetNode<Button>("QuitButton");
	}
}
