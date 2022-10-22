using Godot;
using System;

public partial class Test : Node2D
{
	private Sprite2D sprite;
	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Icon");
	}
	public override void _Process(double delta)
	{
		sprite.GlobalPosition += new Vector2(10 * (float)delta, 0);
	}
}
