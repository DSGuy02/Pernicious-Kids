// Code written by Oladeji Sanyaolu (World/NetworkSetup) 27/10/2022

using Godot;
using System;

public partial class NetworkSetup : Control
{
	// Private variables
	// Nodes
	private MultiplayerAPI _multiplayer;
	private Button _createServer;
	private Button _joinServer;

	/*
		Signal Callbacks
	*/
	private void _onCreateServerPressed()
	{
		Multiplayer multiplayer = (Multiplayer) GetNode<Multiplayer>("/root/Multiplayer");
		var error = multiplayer.CreateServer();
	}

	private void _onJoinServerPressed()
	{
		Multiplayer multiplayer = (Multiplayer) GetNode<Multiplayer>("/root/Multiplayer");
		var error = multiplayer.JoinServer(multiplayer.IpAddress);
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
		_multiplayer = GetTree().GetMultiplayer();
		_createServer = (Button) GetNode<Button>("CreateServer");
		_joinServer = (Button) GetNode<Button>("JoinServer");

		// Connect the signals
		_createServer.Pressed += () => _onCreateServerPressed();
		_joinServer.Pressed += () => _onJoinServerPressed();
		
		// Network signals
		_multiplayer.ConnectedToServer += () => _onMultiplayerConnectedToServer();
	}
}
