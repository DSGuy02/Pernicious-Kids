// Code written by Oladeji Sanyaolu (Singleton/Multiplayer) 27/10/2022
// To be honest, most of this code was from my other game 'RPMania', but I rewrote it in C# and adapted it for this game (plus made some improvements)

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

	public MultiplayerAPI CustomMultiplayerAPI
	{
		get { return _customMultiplayerAPI; }
	}

	public Dictionary<int, Dictionary> Players
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

	private MultiplayerAPI _customMultiplayerAPI;

	private UPNP _upnp;
	private Thread _thread;

	private Dictionary<int, Dictionary> _players = new Dictionary<int, Dictionary>();
	private Dictionary<string, Variant> _playerData = new Dictionary<string, Variant>()
	{
		{ "Username", "" },
		{ "Character", new Godot.Collections.Array() },
		{ "Index", 0 },
		{ "Colour", "ffffff" },
		{ "Version", 0 },
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
	private void resetMultiplayerConnection(bool resetIp = true)
	{
		var multiplayer = (_customMultiplayerAPI != null) ? _customMultiplayerAPI : GetTree().GetMultiplayer();
		
		PlayerId = 0;
		_players.Clear();

		if (resetIp) resetIpAddress();

		if (multiplayer.HasMultiplayerPeer())
		{
			multiplayer = null;
			if (_server != null)
				_server.CloseConnection();
			if (_client != null)
				_client.CloseConnection();
			
			// Ending thread
			if (_thread != null)
				if (_thread.IsAlive()) _thread.WaitToFinish();
			
			if (_upnp != null)
			{
				_upnp.DeletePortMapping(DEFAULTPORT);
				_upnp = null;
			}
		}
	}

	// Thread methods
	private void threadUPNPSetup(int serverPort, bool useIpv6)
	{
		if (serverPort != DEFAULTPORT)
		{
			GD.PrintErr("Aborted. Server port must be the game's default port (" + DEFAULTPORT + ") to use UPNP");
			return;
		}

		// UPNP queries take some time.
		_upnp = new UPNP();
		_upnp.DiscoverIpv6 = useIpv6;
		
		var error = _upnp.Discover(2000, 2, "InternetGatewayDevice");

		if (error != (int) Error.Ok)
		{
			EmitSignal(nameof(UPNPCompleted), error);
			return;
		}

		if (_upnp.GetGateway() != null && _upnp.GetGateway().IsValidGateway())
		{
			_upnp.AddPortMapping(serverPort, serverPort, (string) ProjectSettings.GetSetting("application/config/name"), "UDP");
			_upnp.AddPortMapping(serverPort, serverPort, (string) ProjectSettings.GetSetting("application/config/name"), "TCP");
			EmitSignal(nameof(UPNPCompleted), error);
			return;
		}

		GD.Print("That's strange....UPNP was initialised, but gateway was invalid. Code: " + error);
		_upnp = null;
	}

	// Private network methods
	// Sync
	[RPC]
	private void syncUpdatePlayers(Dictionary<int, Dictionary> newPlayers)
	{
		_players = newPlayers;
		EmitSignal(nameof(PlayersUpdated), _players);
	}
	

	[RPC]
	private void syncEmitUpdatePlayers()
	{
		EmitSignal(nameof(PlayersUpdated), _players);
	}

	// Remote
	[RPC]
	private void remoteSendPlayerInfo(int id, Dictionary playerData) // Only the server should call this
	{
		playerData["index"] = GetPlayerCount();
		_players[id] = playerData;

		Rpc(nameof(syncUpdatePlayers), _players);
	}

	[RPC]
	private void remoteUpdatePlayerCharacter(int id, Godot.Collections.Array newPlayerChar, string newPlayerColour = "ffffff") // Only the server should call this
	{
		_players[id]["character"] = newPlayerChar; // Set the new character for the player
		_players[id]["colour"] = newPlayerColour;

		Rpc(nameof(syncUpdatePlayers), _players);
	}

	[RPC]
	private void remoteUpdatePlayers(Dictionary newPlayers) // Only the server should call this
	{
		Rpc(nameof(syncUpdatePlayers), newPlayers);
	}

	/*
		Public methods
	*/

	// Return methods
	public int GetPlayerCount()
	{
		int count = 0;

		for (int i = 0; i < Players.Count; i++)
			count++;
		
		return count;
	}

	public string GetPlayerUsername(int playerId)
	{
		return (string) _players[playerId]["Username"];
	}

	public string GetPlayerColour(int playerId)
	{
		return (string) _players[playerId]["Colour"];
	}

	public string GetPlayerIndex(int playerId)
	{
		return (string) _players[playerId]["Index"];
	}

	public bool PlayerExists(int playerId)
	{
		return false;//return _players[playerId] != null;
	}
	
	public string GetGameVersionRStripped()
	{
		return GAMEVERSION.RStrip(GAMEVERSIONRSTRIP);
	}

	public bool CheckClientVersion()
	{
		var multiplayer = (_customMultiplayerAPI != null) ? _customMultiplayerAPI : GetTree().GetMultiplayer();

		if (multiplayer.HasMultiplayerPeer())
		{
			if ((int) _players[PlayerId]["version"] == (int)_players[1]["version"])
			{
				return true;
			}
		}

		return false;
	}

	// Non-return methods
	// Handling player stuff
	public void RegisterPlayer(int id = 0)
	{
		var multiplayer = (_customMultiplayerAPI != null) ? _customMultiplayerAPI : GetTree().GetMultiplayer();
		
		if (id == 0)
			id = multiplayer.GetUniqueId();
		
		if (PlayerExists(id)) // If the player is already in the player list we don't need to register them again
			return;//UpdatePlayerCharacter(new Godot.Collections.Array(), "ffffff", id);
		else
		{
			Save save = GetNode<Save>("/root/Save");

			PlayerId = id;
			
			_playerData["Username"] = save.SaveData["Username"];
			_playerData["Character"] = new Godot.Collections.Array();

			_players[PlayerId] = (Dictionary) _playerData;

			GD.Print("Registered: " + PlayerId);

			if (multiplayer.IsServer())
				Rpc(nameof(syncEmitUpdatePlayers));
			else
				RpcId(1, nameof(remoteSendPlayerInfo), PlayerId, _playerData);
			
		}
	}
	public void DeregisterPlayer(int id)
	{
		_players.Remove(id);
		EmitSignal(nameof(PlayersUpdated), _players);
		GD.Print("Deregistered: " + id);
	}

	public void UpdatePlayerCharacter(Godot.Collections.Array newPlayerChar, string newPlayerColour, int id = 0)
	{
		var multiplayer = (_customMultiplayerAPI != null) ? _customMultiplayerAPI : GetTree().GetMultiplayer();

		if (id == 0) id = multiplayer.GetUniqueId();

		_players[id]["Character"] = newPlayerChar;
		_players[id]["Colour"] = newPlayerColour;
		
		if (multiplayer.IsServer())
			Rpc(nameof(syncUpdatePlayers), _players);
		else
			RpcId(1, nameof(remoteUpdatePlayerCharacter), id, newPlayerChar, newPlayerColour);
	}

	public void SwapPlayerIndex(int playerId, int otherPlayerId)
	{
		var multiplayer = (_customMultiplayerAPI != null) ? _customMultiplayerAPI : GetTree().GetMultiplayer();

		int tempValue;

		tempValue = (int) _players[playerId]["Index"];

		_players[playerId]["Index"] = _players[otherPlayerId]["Index"];
		_players[otherPlayerId]["Index"] = tempValue;

		if (multiplayer.IsServer())
			Rpc(nameof(syncUpdatePlayers), _players);
		else
			RpcId(1, nameof(remoteUpdatePlayers), _players);
	}

	// Public Multiplayer methods
	// Remote
	public void RemoteChangeScene(string newScene, bool currentlyInGame, Dictionary<int, Dictionary> playersList = null) // Will I even use this????
	{
		if (currentlyInGame)
		{
			InGame = currentlyInGame;
			_players = playersList;
			GetTree().ChangeSceneToFile(newScene);
		} else {
			InGame = false;
			GetTree().ChangeSceneToFile("");
		}
	}

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
		
		// _customMultiplayerAPI = new MultiplayerAPI(); // The compiler complains that the object doesn't have a constructor....
		//_customMultiplayerAPI.MultiplayerPeer = _server; // ....yet at runtime, it complains that it needs to be instanced....?
		//GetTree().SetMultiplayer(_customMultiplayerAPI); // Can't do this yet

		GetTree().GetMultiplayer().MultiplayerPeer = _server; // Screw it, I'll just use the SceneTree's default multiplayer
		
		GD.Print("Created server on port: " + port);

		return error;
	}

	public Error JoinServer(string serverIP, int serverPort = 0)
	{
		Error error;
		Save save = (Save) GetNode<Save>("/root/Save");

		IpAddress = serverIP;
		
		if (serverPort == 0)
			serverPort = DEFAULTPORT;

		int customClientPort = serverPort;
		int port = (customClientPort > 2000) ? customClientPort : DEFAULTPORT;

		_client = new ENetMultiplayerPeer();
		error = _client.CreateClient(IpAddress, port, INBANDWIDTH, OUTBANDWIDTH);

		if (error != Error.Ok)
		{
			GD.PrintErr("Failed to initialise client connection! Error code: " + error);
			return error;
		}

		// _customMultiplayerAPI = new MultiplayerAPI(); // The compiler complains that the object doesn't have a constructor....
		//_customMultiplayerAPI.MultiplayerPeer = _server; // ....yet at runtime, it complains that it needs to be instanced....?
		//GetTree().SetMultiplayer(_customMultiplayerAPI); // Can't do this yet

		GetTree().GetMultiplayer().MultiplayerPeer = _client; // Screw it, I'll just use the SceneTree's default multiplayer

		GD.Print("Connected to server!");

		return error;
	}

	/*
		Signal Callbacks
	*/
	private void _onUPNPCompleted(Error result)
	{
		if (result == Error.Ok)
		{
			GD.Print("UPNP Initialised successfully! Code: " + result);
			PublicIpAddress = _upnp.QueryExternalAddress();
			GD.Print("Public IP: " + PublicIpAddress);
		} else {
			GD.Print("UPNP Failed to initialise! Code: " + result);
			_upnp = null;
		}
	}

	private void _onServerDisconnected()
	{
		if (ServerDisconnectedPrompt == "")
			ServerDisconnectedPrompt = "Disconnected";
		
		resetMultiplayerConnection();
		//GetTree().ChangeSceneToFile("");
	}
	/*
		GODOT methods
	*/
	public override void _Ready()
	{
		var multiplayerApi = (CustomMultiplayerAPI != null) ? CustomMultiplayerAPI : GetTree().GetMultiplayer();

		resetIpAddress();

		// Connect signals
		// Custom
		UPNPCompleted += _onUPNPCompleted; // This has a parameter so yeah

		// Multiplayer
		multiplayerApi.ServerDisconnected += () => _onServerDisconnected();
	}

    public override void _ExitTree()
    {
        if (_thread != null && _thread.IsAlive()) // To ensure the game quits properly
			_thread.WaitToFinish();
    }
}
