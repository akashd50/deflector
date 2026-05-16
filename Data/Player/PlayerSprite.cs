using Godot;
using System;

public partial class PlayerSprite : Sprite2D
{
	public override void _Ready()
	{
		// Modulate = new Color(5.5f, 5.5f, 5.5f);
		Modulate = new Color(1.0f, 1.0f, 1.0f);
	}
}
