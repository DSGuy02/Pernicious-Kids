// Code written by Oladeji Sanyaolu (Singleton/Multiplayer) 27/10/2022

using Godot;
using Godot.Collections;
using System;

public partial class Multiplayer : Node
{
	// Signals
	[Signal] public delegate void PlayersUpdated(Dictionary<string, Variant> playerList);
	[Signal] public delegate void PlayerJoinedScene(int _playerId);
	[Signal] public delegate void UPNPCompleted(Error error);
	[Signal] public delegate void ServerCreationFailed(Error error);
	[Signal] public delegate void NetworkConnectionFailed(Error error);
	
	// Properties
	public static int CustomClientPort
	{
		get { return _customClientPort; }
		set { _customClientPort = value; }
	}
	public static int PlayerId
	{
		get { return _playerId; }
		set { _playerId = value; }
	}
	
	public static bool DedicatedServer
	{
		get { return _dedicatedServer; }
		set { _dedicatedServer = value; }
	}
	public static bool PrivateServer
	{
		get { return _privateServer; }
		set { _privateServer = value; }
	}
	public static bool InGame
	{
		get { return _inGame; }
		set { _inGame = value; }
	}

	public static string ServerDisconnectedPrompt
	{
		get { return _serverDisconnectedPrompt; }
		set { _serverDisconnectedPrompt = value; }
	}
	public static string IpAddress
	{
		get { return _ipAddress; }
		set { _ipAddress = value; }
	}
	public static string PublicIpAddress
	{
		get { return _publicIpAddress; }
		set { _publicIpAddress = value; }
	}
	public static string ServerRegion
	{
		get { return _serverRegion; }
		set { _serverRegion = value; }
	}

	public static MultiplayerAPI MultiplayerApi
	{
		get { return _multiplayerApi; }
	}

	public static Dictionary<string, Variant> Players
	{
		get { return _players; }
	}

	// Private variables
	// Constants
	private static readonly int DEFAULTPORT = 24800;
	private static readonly int MAXCLIENTS = 5;
	private static readonly int INBANDWIDTH = 20000;
	private static readonly int OUTBANDWIDTH = 20000;

	private static readonly string[] SERVERREGIONS = new string[] {
		"NA",
		"SA",
		"EUR",
		"AFR",
		"ASA",
		"OCN",
	};

	private static readonly string GAMEVERSION = "v0.1";
	private static readonly string GAMEVERSIONRSTRIP = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMOPQRSTUVWXYZ";

	// Variables with properties
	private static int _customClientPort;
	private static int _playerId;

	private static bool _dedicatedServer = false;
	private static bool _privateServer = false;
	private static bool _inGame = false;

	private static string _serverDisconnectedPrompt;
	private static string _ipAddress;
	private static string _publicIpAddress;
	private static string _serverRegion;

	private static ENetMultiplayerPeer _server;
	private static ENetMultiplayerPeer _client;

	private static MultiplayerAPI _multiplayerApi;

	private static UPNP _upnp;
	private static Thread _thread;

	private static Dictionary<string, Variant> _players;
	private static Dictionary<string, Variant> _playerData = new Dictionary<string, Variant>()
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
	public static Error CreateServer()
	{
		Error error;

		var saveServerPort = (int) Save.SaveData["ServerPort"];
		
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
		
		return error;
	}

	public static Error JoinServer(string serverIP, int serverPort = 24800)
	{
		return Error.Ok;
	}

	/*
		GODOT methods
	*/
	public override void _Ready()
	{
		resetIpAddress();
	}
}
