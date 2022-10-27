// Code written by Oladeji Sanyaolu (World/NetworkSetup) 27/10/2022

using Godot;
using System;

public partial class NetworkSetup : Control
{
	private Button _createServer;
	private Button _joinServer;

	/*
		Signal Callbacks
	*/
	private void _onCreateServerPressed()
	{
		GD.Print("Create Server!");

		Multiplayer multiplayer = (Multiplayer) GetNode<Multiplayer>("/root/Multiplayer");
		var error = multiplayer.CreateServer();
	}

	private void _onJoinServerPressed()
	{
		GD.Print("Join Server!");
	}

	/*
		Godot methods
	*/
	public override void _Ready()
	{
		// Get the nodes from their NodePaths
		_createServer = (Button) GetNode<Button>("CreateServer");
		_joinServer = (Button) GetNode<Button>("JoinServer");

		// Connect the signals
		_createServer.Pressed += () => _onCreateServerPressed();
		_joinServer.Pressed += () => _onJoinServerPressed();
	}
}
