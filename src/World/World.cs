using Godot;
using System;

public partial class World : Node3D
{
	// Private field
	private MultiplayerAPI _multiplayerApi;

	/*
		Private methods
	*/

	/*
		Public methods
	*/
	public void SetupWorld()
	{

	}
	/*
		Godot methods
	*/
	public override void _Ready()
	{
		AddToGroup("World");
	}
}
