// Code written by Oladeji Sanyaolu (World/NetworkSetup) 27/10/2022

using Godot;
using Godot.Collections;
using System;

public partial class NetworkSetup : Control
{
	// Private variables
	// Multiplayer
	private Multiplayer _multiplayer;
	private MultiplayerApi _multiplayerApi;

	// Nodes
	private Control _hostJoinControl;
	private Control _waitRoomControl;

	private ItemList _playerList;

	private Button _createServer;
	private Button _joinServer;
	private Button _startGame;

	/*
		Private methods
	*/
	private void showControl(Control newControl)
	{
		_hostJoinControl.Hide();
		_waitRoomControl.Hide();

		newControl.Show();
	}

	[Rpc(CallLocal=true)]
	private void gotoWorld()
	{
		GetTree().ChangeSceneToFile("res://src/World/TestWorld.tscn");
	}

	/*
		Signal Callbacks
	*/
	private void _onCreateServerPressed()
	{
		var error = _multiplayer.CreateServer();
		if (error == Error.Ok)
		{
			_multiplayer.RegisterPlayer();
			showControl(_waitRoomControl);
		}
	}

	private void _onJoinServerPressed()
	{
		var error = _multiplayer.JoinServer(_multiplayer.IpAddress);
	}

	private void _onStartGamePressed()
	{
		if (_multiplayerApi.IsServer())
			Rpc(nameof(gotoWorld));
	}

	// Network signals
	private void _onMultiplayerPlayersUpdated(Dictionary<string, Variant> playerList)
	{
		_playerList.Clear();
		
		foreach(var player in playerList)
		{
			var playerValue = (Dictionary) player.Value;
			var playerName = (string) playerValue["Username"];

			_playerList.AddItem(playerName);
		}
	}
	private void _onMultiplayerApiConnectedToServer()
	{
		GD.Print("Connected to Server Successfully");
		_multiplayer.RegisterPlayer();
		showControl(_waitRoomControl);
	}
	/*
		Godot methods
	*/
	public override void _Ready()
	{
		// Get the nodes from their NodePaths
		_multiplayerApi = GetTree().GetMultiplayer();
		_multiplayer = (Multiplayer) GetNode<Multiplayer>("/root/Multiplayer");

		_hostJoinControl = (Control) GetNode<Control>("HostJoin");
		_waitRoomControl = (Control) GetNode<Control>("WaitRoom");

		_playerList = (ItemList) GetNode<ItemList>("WaitRoom/PlayerList");

		_createServer = (Button) GetNode<Button>("HostJoin/CreateServer");
		_joinServer = (Button) GetNode<Button>("HostJoin/JoinServer");
		_startGame = (Button) GetNode<Button>("WaitRoom/StartGame");

		showControl(_hostJoinControl);

		// Connect the signals
		_createServer.Pressed += () => _onCreateServerPressed();
		_joinServer.Pressed += () => _onJoinServerPressed();
		_startGame.Pressed += () => _onStartGamePressed();

		// Network signals
		_multiplayer.PlayersUpdated += _onMultiplayerPlayersUpdated;
		_multiplayerApi.ConnectedToServer += () => _onMultiplayerApiConnectedToServer();
	}
}
