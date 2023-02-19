using Godot;
using Godot.Collections;
using System;

public partial class World : Node3D
{
	// Private field
	private Multiplayer _multiplayer;
	private MultiplayerApi _multiplayerApi;
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
			var playerKey = (int) player.Key;
			var playerValue = (Dictionary) player.Value;
			var playerName = (string) playerValue["Username"];
			
			var sam = (PackedScene) ResourceLoader.Load("res://src/Entities/Players/Sam.tscn");
			var sam_instance = (Player) sam.Instantiate();
			sam_instance.SetMultiplayerAuthority(playerKey);
			AddChild(sam_instance);
			sam_instance.GlobalPosition = new Vector3(0, 10, 0);
			sam_instance.Setup();
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
