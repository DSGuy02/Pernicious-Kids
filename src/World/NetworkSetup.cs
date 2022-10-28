// Code written by Oladeji Sanyaolu (World/NetworkSetup) 27/10/2022

using Godot;
using System;

public partial class NetworkSetup : Control
{
	// Private variables
	// Nodes
	private Multiplayer _multiplayer;
	private MultiplayerAPI _multiplayerApi;
	private Button _createServer;
	private Button _joinServer;

	/*
		Signal Callbacks
	*/
	private void _onCreateServerPressed()
	{
		var error = _multiplayer.CreateServer();
		if (error == Error.Ok)
		{
			_multiplayer.RegisterPlayer();
			GetTree().ChangeSceneToFile("res://src/World/TestWorld.tscn");
		}
	}

	private void _onJoinServerPressed()
	{
		var error = _multiplayer.JoinServer(_multiplayer.IpAddress);
	}

	// Network signals
	private void _onMultiplayerConnectedToServer()
	{
		GD.Print("Connected to Server Successfully");
	}
	/*
		Godot methods
	*/
	public override void _Ready()
	{
		// Get the nodes from their NodePaths
		_multiplayerApi = GetTree().GetMultiplayer();
		_multiplayer = (Multiplayer) GetNode<Multiplayer>("/root/Multiplayer");
		_createServer = (Button) GetNode<Button>("CreateServer");
		_joinServer = (Button) GetNode<Button>("JoinServer");

		// Connect the signals
		_createServer.Pressed += () => _onCreateServerPressed();
		_joinServer.Pressed += () => _onJoinServerPressed();
		
		// Network signals
		_multiplayerApi.ConnectedToServer += () => _onMultiplayerConnectedToServer();
	}
}
