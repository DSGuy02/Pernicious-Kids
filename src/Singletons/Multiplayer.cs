// Code written by Oladeji Sanyaolu (Singleton/Multiplayer) 27/10/2022

using Godot;
using Godot.Collections;
using System;

public partial class Multiplayer : Node
{
	// Signals
	[Signal] public delegate void PlayersUpdatedEventHandler(Dictionary<string, Variant> playerList);
	[Signal] public delegate void PlayerJoinedSceneEventHandler(int _playerId);
	[Signal] public delegate void UPNPCompletedEventHandler(Error error);
	[Signal] public delegate void ServerCreationFailedEventHandler(Error error);
	[Signal] public delegate void NetworkConnectionFailedEventHandler(Error error);
	
	// Properties
	public int CustomClientPort
	{
		get { return _customClientPort; }
		set { _customClientPort = value; }
	}
	public int PlayerId
	{
		get { return _playerId; }
		set { _playerId = value; }
	}
	
	public bool DedicatedServer
	{
		get { return _dedicatedServer; }
		set { _dedicatedServer = value; }
	}
	public bool PrivateServer
	{
		get { return _privateServer; }
		set { _privateServer = value; }
	}
	public bool InGame
	{
		get { return _inGame; }
		set { _inGame = value; }
	}

	public string ServerDisconnectedPrompt
	{
		get { return _serverDisconnectedPrompt; }
		set { _serverDisconnectedPrompt = value; }
	}
	public string IpAddress
	{
		get { return _ipAddress; }
		set { _ipAddress = value; }
	}
	public string PublicIpAddress
	{
		get { return _publicIpAddress; }
		set { _publicIpAddress = value; }
	}
	public string ServerRegion
	{
		get { return _serverRegion; }
		set { _serverRegion = value; }
	}

	public MultiplayerAPI API
	{
		get { return _multiplayerApi; }
	}

	public Dictionary<string, Variant> Players
	{
		get { return _players; }
	}

	// Private variables
	// Constants
	private readonly int DEFAULTPORT = 24800;
	private readonly int MAXCLIENTS = 5;
	private readonly int INBANDWIDTH = 20000;
	private readonly int OUTBANDWIDTH = 20000;

	private readonly string[] SERVERREGIONS = new string[] {
		"NA",
		"SA",
		"EUR",
		"AFR",
		"ASA",
		"OCN",
	};

	private readonly string GAMEVERSION = "v0.1";
	private readonly string GAMEVERSIONRSTRIP = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMOPQRSTUVWXYZ";

	// Variables with properties
	private int _customClientPort;
	private int _playerId;

	private bool _dedicatedServer = false;
	private bool _privateServer = false;
	private bool _inGame = false;

	private string _serverDisconnectedPrompt;
	private string _ipAddress;
	private string _publicIpAddress;
	private string _serverRegion;

	private ENetMultiplayerPeer _server;
	private ENetMultiplayerPeer _client;

	private MultiplayerAPI _multiplayerApi;

	private UPNP _upnp;
	private Thread _thread;

	private Dictionary<string, Variant> _players;
	private Dictionary<string, Variant> _playerData = new Dictionary<string, Variant>()
	{
		{ "username", "" },
		{ "index", 0 },
		{ "colour", "ffffff" },
		{ "version", 0 },
	};


	/*
		Private methods
	*/
	private void resetIpAddress()
	{
		PublicIpAddress = "";
		
		if (OS.GetName() == "Windows")
			IpAddress = IP.GetLocalAddresses()[3];
		else if (OS.GetName() == "Android")
			IpAddress = IP.GetLocalAddresses()[0];
		else
			IpAddress = IP.GetLocalAddresses()[3];
		
		foreach(string ip in IP.GetLocalAddresses())
		{
			if (ip.BeginsWith("192.168.") && !ip.EndsWith(".1"))
				IpAddress = ip;
		}
	}

	/*
		Public methods
	*/

	// Multiplayer Creation
	public Error CreateServer()
	{
		Error error;
		Save save = (Save) GetNode<Save>("/root/Save");
		
		var saveServerPort = (int) save.SaveData["ServerPort"];
		
		int clientLimit = DedicatedServer ? MAXCLIENTS : MAXCLIENTS - 1;
		int port = (saveServerPort >= 3000) ? saveServerPort : DEFAULTPORT;
		
		_server = new ENetMultiplayerPeer();
		error = _server.CreateServer(port, clientLimit, INBANDWIDTH, OUTBANDWIDTH);

		if (error != Error.Ok)
		{
			GD.PrintErr("Server Creation failed! Error code: " + error);
			return error;
		}

		GD.Print("Created server on port: " + port);
		
		API.MultiplayerPeer = _server;

		GetTree().SetMultiplayer(API);

		return error;
	}

	public Error JoinServer(string serverIP, int serverPort = 24800)
	{
		Error error;
		Save save = (Save) GetNode<Save>("/root/Save");

		IpAddress = serverIP;

		int customClientPort = serverPort;
		int port = (customClientPort > 2000) ? customClientPort : DEFAULTPORT;

		_client = new ENetMultiplayerPeer();
		error = _client.CreateClient(IpAddress, port, INBANDWIDTH, OUTBANDWIDTH);

		if (error != Error.Ok)
		{
			GD.PrintErr("Failed to initialise client connection! Error code: " + error);
			return error;
		}

		API.MultiplayerPeer = _client;
		GetTree().SetMultiplayer(API);

		return error;
	}

	/*
		GODOT methods
	*/
	public override void _Ready()
	{
		resetIpAddress();
	}
}
