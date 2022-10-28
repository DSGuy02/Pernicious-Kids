using Godot;
using System;

public partial class World : Node3D
{
	// Private field
	private Multiplayer _multiplayer;
	private MultiplayerAPI _multiplayerApi;
	/*
		Private methods
	*/

	/*
		Public methods
	*/
	// Constructor
	public World()
	{
		
	}
	public void SetupWorld()
	{
		// Get the multiplayer API
		_multiplayer = (Multiplayer) GetNode<Multiplayer>("/root/Multiplayer");
		_multiplayerApi = (_multiplayer.CustomMultiplayerAPI != null) ? _multiplayer.CustomMultiplayerAPI : GetTree().GetMultiplayer();

		foreach(var player in _multiplayer.Players)
		{
			//GD.Print(player);
		}
	}
	/*
		Godot methods
	*/
	public override void _Ready()
	{
		AddToGroup("World");
	}
}
