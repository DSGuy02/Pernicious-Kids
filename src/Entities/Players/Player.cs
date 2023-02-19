// Code written by Oladeji Sanyaolu (Player) 28/10/2022

using Godot;
using System;

public partial class Player : CharacterBody3D
{
	/*
		Public
	*/
	// Properties
	public MultiplayerApi MultiplayerApi
	{
		get { return _multiplayerApi; }
	}

	public bool CaptureMouse
	{
		get { return _captureMouse; }
		set
		{
			_captureMouse = value;
			
			if (_captureMouse)
				Input.MouseMode = Input.MouseModeEnum.Captured;
			else
				Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}
	public bool Crouched
	{
		get { return _crouched; }
		set { _crouched = value; }
	}

	public string Username
	{
		get { return _username; }
		set { _username = value; }
	}


	public Vector3 CameraPosition
	{
		get { return _cameraPosition; }
		set 
		{ 
			_cameraPosition = value;

			if (_cameraTween != null)
				_cameraTween.Kill();
			
			_cameraTween = CreateTween();
			_cameraTween.TweenProperty(_head, "position", _cameraPosition, 0.5).SetTrans(Tween.TransitionType.Linear);
		}
	}

	/* 
		Private
	*/
	// Constants
	private readonly int JUMPVELOCITY = 2;
	private readonly int MAXSPEED = 10;
	private readonly int NORSPEED = 5;
	private readonly int MINSPEED = 1;
	private readonly int MAXSTAMINA = 100;

	private readonly float SPEEDWEIGHT = 0.1f;
	private readonly float STAMINAWEIGHT = 0.02f;

	// Export variables
	[Export] private NodePath HeadNodePath;
	[Export] private NodePath ModelNodePath;
	[Export] private NodePath CameraNodePath;
	[Export] private NodePath NetworkTickRateNodePath;

	// Custom signals
	// [Signal] delegate void MySignal(); // Just for future references
	// [Signal] delegate void MySignalWithArguments(string foo, int bar); // Just for future references
	
	// Other variables
	private MultiplayerApi _multiplayerApi;

	private bool _captureMouse = false;
	private bool _crouched = false;

	private string _username;

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle(); // Get the gravity from the project settings to be synced with RigidBody nodes.
	private float _mouseSensitivity = 0.08f;
	private float _controllerSensitivity = 1.0f;
	private float _stamina = 100f;
	private float _speed = 5.0f;

	private int _healthPoint = 100;
	
	private int _cameraPositionValue = 0;

	private Vector3[] _cameraPositions = {
		new Vector3(0, 0, 0),
		new Vector3(0, 1.5f, 3),
	};
	private Vector3 _cameraPosition = Vector3.Zero;

	// Puppet position for mulitplayer
	private Vector3 _puppetPosition = Vector3.Zero;
	private Vector3 _puppetVelocity = Vector3.Zero;
	private Vector2 _puppetRotation = Vector2.Zero;

	// Node variables
	private Node3D _head;
	private Node3D _model;
	private Camera3D _camera;
	private Timer _networkTickRate;
	private Tween _movementTween;
	private Tween _cameraTween;

	/*
		Private Methods
	*/
	private bool isMaster() // If the player is in single player, or controls the particular player in multiplayer
	{
		if (_multiplayerApi.HasMultiplayerPeer())
		{
			if (IsMultiplayerAuthority())
				return true;
			else
				return false;
		}
		
		return true;
	}
	
	private Vector3 getInput()
	{
		Vector3 inputDir = Vector3.Zero;
		
		inputDir += Input.GetAxis("forward", "backward") * GlobalTransform.Basis.Z; // Up and Down Movement
		inputDir += Input.GetAxis("leftward", "rightward") * GlobalTransform.Basis.X; // Left and Right Movement

		inputDir = inputDir.Normalized();
		
		return inputDir;
	}

	private void handleControllerCamera()
	{
		// Create the local variables
		Vector3 controllerDir = Vector3.Zero;
		Vector3 newRotation = Vector3.Zero;
		Vector3 newHeadRotation = Vector3.Zero;

		// Collect the controller input and multiply it with the mouse sensitivity
		controllerDir.Y += Input.GetAxis("camera_left", "camera_right");
		controllerDir.X += Input.GetAxis("camera_up", "camera_down");

		controllerDir = controllerDir.Normalized(); // Normalise it to make it move faster and equally across 2 axis

		// Set the new rotation, we have to convert it to radians (as Rotation is based on radians) and negate it as it would be flipped, then we multiply it by the controller sensitivity
		newRotation.Y += Mathf.DegToRad(-controllerDir.Y * _controllerSensitivity);
		newHeadRotation.X += Mathf.DegToRad(-controllerDir.X * _controllerSensitivity);

		// Apply the new values
		Rotation += newRotation;
		_head.Rotation += newHeadRotation;

		// Don't allow the rotation to go upside down
		var clampHeadRotation = _head.Rotation;
		clampHeadRotation.X = Mathf.Clamp(clampHeadRotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
		_head.Rotation = clampHeadRotation;
	}

	private void handleMovement(double delta)
	{
		Vector3 velocity = Velocity;
		bool sprint = Input.IsActionPressed("sprint");

		if (!IsOnFloor()) // Let's have gravity
			velocity.Y -= _gravity * (float)delta;
		
		if (isMaster()) // This is our character
		{
			handleControllerCamera();

			if (!Crouched)
			{
				if (sprint) // Make the player run and consume their energy
				{
					if (_stamina > 2)
					{
						_speed = Mathf.Lerp(_speed, MAXSPEED, SPEEDWEIGHT);
						_stamina = Mathf.Lerp(_stamina, 0, STAMINAWEIGHT);
					} else { // Revert only the player's speed
						_speed = Mathf.Lerp(_speed, NORSPEED, SPEEDWEIGHT * 2);
					}
				}
			} else {
				_speed = Mathf.Lerp(_speed, MINSPEED, SPEEDWEIGHT * 1.5f);
			}

			if (Crouched || !sprint) { // Revert the player's speed and slowly restore their energy if they're crouched
				_speed = Mathf.Lerp(_speed, NORSPEED, SPEEDWEIGHT * 2);
				_stamina = Mathf.Lerp(_stamina, MAXSTAMINA, STAMINAWEIGHT / 2);
			}

			GD.Print("Speed: " + _speed);
			GD.Print("Stamina: " + _stamina);
			GD.Print("Crouched: " + Crouched);
			//GD.Print("Rotation: " + Rotation.ToString());
			//GD.Print("Head Rotation: " + _head.Rotation);

			Vector3 desiredVelocity = getInput() * _speed;

			velocity.X = desiredVelocity.X;
			velocity.Z = desiredVelocity.Z;

			if (Input.IsActionPressed("jump") && IsOnFloor())
				velocity.Y += JUMPVELOCITY;
		
		} else { // This is not our character

			var newGlobalTransform = GlobalTransform;
			var newHeadRotation = _head.Rotation;

			newGlobalTransform.Origin = _puppetPosition;
			newGlobalTransform.Basis = new Basis(Vector3.Up, _puppetRotation.Y);

			GlobalTransform = newGlobalTransform.Orthonormalized();

			velocity.X = _puppetVelocity.X;
			velocity.Z = _puppetVelocity.Z;

			newHeadRotation.X = _puppetRotation.X;
			
			_head.Rotation = newHeadRotation;

			//GD.Print(GetMultiplayerAuthority() + "'s new rotation: " + newRotation);
			//GD.Print(GetMultiplayerAuthority() + "'s new head rotation: " + newHeadRotation);

			//GD.Print(GetMultiplayerAuthority() + "'s rotation: " + Rotation);
			//GD.Print(GetMultiplayerAuthority() + "'s head rotation: " + _head.Rotation);
		}

		// Apply the whole physics
		Velocity = velocity;
		MoveAndSlide();
	}

	/*
		Network Methods
	*/
	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void puppetUpdateState(Vector3 pPosition, Vector3 pVelocity, Vector2 pRotation)
	{
		_puppetPosition = pPosition;
		_puppetVelocity = pVelocity;
		_puppetRotation = pRotation;

		// Tween the transformation (position and rotation i think) at 0.1 second, this should compensate for lag
		if (_movementTween != null)
			_movementTween.Kill();
		
		_movementTween = CreateTween();
		_movementTween.TweenProperty(this, "global_transform", new Transform3D(GlobalTransform.Basis, pPosition), 0.1).SetTrans(Tween.TransitionType.Linear);
	}

	/*
		Public methods
	*/
	public void Setup()
	{
		// Get the multiplayer API
		var multiplayer = (Multiplayer) GetNode<Multiplayer>("/root/Multiplayer");
		_multiplayerApi = (multiplayer.CustomMultiplayerAPI != null) ? multiplayer.CustomMultiplayerAPI : GetTree().GetMultiplayer();

		// Set the Nodes from the NodePaths
		_head = (Node3D) GetNode(HeadNodePath) as Node3D;
		_camera = (Camera3D) GetNode(CameraNodePath) as Camera3D;
		_networkTickRate = (Timer) GetNode(NetworkTickRateNodePath) as Timer;
		_model = (Node3D) GetNode(ModelNodePath);

		// Set the camera and model to the right player
		_camera.Current = isMaster();
		// _model.Visible = !isMaster(); // TODO: should it be invisible to the player master?

		// Set the camera position
		CameraPosition = _cameraPositions[_cameraPositionValue];

		// Enable the process
		SetPhysicsProcess(true);
		SetProcessInput(true);

		// Connect Signals
		_networkTickRate.Timeout += () => _onNetworkTickRateTimeout();
	}

	/*
		Signal Callbacks (Private)
	*/
	private void _onNetworkTickRateTimeout()
	{
		if (_multiplayerApi.HasMultiplayerPeer())
			if (IsMultiplayerAuthority())
				Rpc(nameof(puppetUpdateState), GlobalTransform.Origin, Velocity, new Vector2(_head.Rotation.X, Rotation.Y));
			else
				_networkTickRate.Stop();
		else
			_networkTickRate.Stop();
	}

	/*
		GODOT Methods
	*/
	public override void _Ready()
	{
		AddToGroup("Player");
		SetPhysicsProcess(false);
		SetProcessInput(false);
	}
	
	public override void _PhysicsProcess(double delta)
	{
		handleMovement(delta);
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (isMaster())
		{
			if (inputEvent is InputEventMouseMotion) // Camera movement
			{
				// Move the camera based on the mouse movement
				InputEventMouseMotion inputEventMouseMotion = (InputEventMouseMotion) inputEvent;
				RotateY(Mathf.DegToRad(-inputEventMouseMotion.Relative.X * _mouseSensitivity)); // Rotate along the mouse-X
				_head.RotateX(Mathf.DegToRad(-inputEventMouseMotion.Relative.Y * _mouseSensitivity)); // Rotate along the mouse-Y

				// Don't let the camera move beyound a certain point in the X axis
				var newHeadRotation = _head.Rotation;
				newHeadRotation.X = Mathf.Clamp(newHeadRotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
				_head.Rotation = newHeadRotation;
			
			} else if (inputEvent is InputEventKey)
			{
				InputEventKey inputEventKey = (InputEventKey) inputEvent;

				if (inputEventKey.IsActionPressed("toggle_mouse")) // TOggle having the mouse confined or visible
					CaptureMouse = !CaptureMouse;
				else if (inputEventKey.IsActionPressed("crouch")) // Make the player crouch
					Crouched = !Crouched;
			}

			if (inputEvent.IsActionPressed("change_camera"))
			{
				if (_cameraPositionValue == 0)
					_cameraPositionValue = 1;
				else _cameraPositionValue = 0;

				CameraPosition = _cameraPositions[_cameraPositionValue];
			}
		}
	}

}
