// Code written by Oladeji Sanyaolu (Player/Sam) 22/10/2022

using Godot;
using System;

public partial class Sam : CharacterBody3D
{
	/*
		Public
	*/
	// Properties
	public bool multiplayerActive
	{
		get { return _multiplayerActive; }
		set { _multiplayerActive = value; }
	}
	public bool confineHideMouse
	{
		get { return _confineHideMouse; }
		set
		{
			_confineHideMouse = value;
			
			if (_confineHideMouse)
				Input.MouseMode = Input.MouseModeEnum.ConfinedHidden;
			else
				Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}


	public Vector3 cameraPosition
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
	
	// Export variables
	[Export] private NodePath HeadNodePath;
	[Export] private NodePath ModelNodePath;
	[Export] private NodePath CameraNodePath;
	[Export] private NodePath NetworkTickRateNodePath;

	// Custom signals
	// [Signal] delegate void MySignal(); // Just for future references
	// [Signal] delegate void MySignalWithArguments(string foo, int bar); // Just for future references
	
	// Godot variables
	private bool _multiplayerActive = false;
	private bool _confineHideMouse = false;
	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle(); // Get the gravity from the project settings to be synced with RigidBody nodes.
	private float _mouseSensitivity = 0.08f;
	private float _stamina = 100f;

	private int _healthPoint = 100;
	private int _speed = 5;
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
		if (_multiplayerActive)
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
		
		inputDir += Input.GetAxis("forward", "backward") * GlobalTransform.basis.z; // Up and Down Movement
		inputDir += Input.GetAxis("leftward", "rightward") * GlobalTransform.basis.x; // Left and Right Movement

		inputDir = inputDir.Normalized();
		
		return inputDir;
	}

	private void handleControllerCamera()
	{
		// Create the local variables
		Vector3 controllerDir = Vector3.Zero;
		Vector3 newRotationY = Vector3.Zero;
		Vector3 newHeadRotationX = Vector3.Zero;

		// Collect the controller input and multiply it with the mouse sensitivity
		controllerDir.y += Input.GetAxis("camera_left", "camera_right") * _mouseSensitivity;
		controllerDir.x += Input.GetAxis("camera_up", "camera_down") * _mouseSensitivity;

		controllerDir = controllerDir.Normalized(); // Normalise it to make it move faster and equally across 2 axis

		// Set the new rotation, we have to convert it to radians (as Rotation is based on radians) and negate it as it would be flipped
		newRotationY.y += Mathf.DegToRad(-controllerDir.y);
		newHeadRotationX.x += Mathf.DegToRad(-controllerDir.x);

		// Apply the new values
		Rotation += newRotationY;
		_head.Rotation += newHeadRotationX;
	}

	private void handleMovement(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor()) // Let's have gravity
			velocity.y -= _gravity * (float)delta;
		
		if (isMaster()) // This is our character
		{
			handleControllerCamera();

			Vector3 desiredVelocity = getInput() * _speed;

			velocity.x = desiredVelocity.x;
			velocity.z = desiredVelocity.z;

			if (Input.IsActionPressed("jump") && IsOnFloor())
				velocity.y += JUMPVELOCITY;
		
		} else { // This is not our character

			var newGlobalTransform = GlobalTransform;
			var newRotation = Rotation;
			var newHeadRotation = _head.Rotation;

			newGlobalTransform.origin = _puppetPosition;
			newRotation.y = _puppetRotation.y;
			newHeadRotation.x = _puppetRotation.x;

			GlobalTransform = newGlobalTransform;

			velocity.x = _puppetVelocity.x;
			velocity.z = _puppetVelocity.z;

			Rotation = newRotation;
			_head.Rotation = newHeadRotation;
		}

		// Apply the whole physics
		Velocity = velocity;
		MoveAndSlide();
	}

	/*
		Network Methods
	*/
	private void puppetUpdateState(Vector3 pPosition, Vector3 pVelocity, Vector2 pRotation)
	{
		_puppetPosition = pPosition;
		_puppetVelocity = pVelocity;
		_puppetRotation = pRotation;

		// Tween the transformation (position and rotation i think) at 0.1 second, this should compensate for lag
		if (_movementTween != null)
			_movementTween.Kill();
		
		_movementTween = CreateTween();
		_movementTween.TweenProperty(this, "global_transform", new Transform3D(GlobalTransform.basis, pPosition), 0.1).SetTrans(Tween.TransitionType.Linear);
	}

	/*
		Signal Callbacks
	*/
	private void _onNetworkTickRateTimeout()
	{
		if (_multiplayerActive)
			if (IsMultiplayerAuthority())
				Rpc(nameof(puppetUpdateState), GlobalTransform.origin, Velocity, new Vector2(_head.Rotation.x, Rotation.y));
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
		// Set the Nodes from the NodePaths
		_head = (Node3D) GetNode(HeadNodePath) as Node3D;
		_camera = (Camera3D) GetNode(CameraNodePath) as Camera3D;
		_networkTickRate = (Timer) GetNode(NetworkTickRateNodePath) as Timer;
		_model = (Node3D) GetNode(ModelNodePath);

		// Set the camera and model to the right player
		_camera.Current = isMaster();
		// _model.Visible = !isMaster(); // TODO: should it be invisible to the player master?

		// Set the camera position
		cameraPosition = _cameraPositions[_cameraPositionValue];

		// Connect Signals
		_networkTickRate.Timeout += () => _onNetworkTickRateTimeout();
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
				RotateY(Mathf.DegToRad(-inputEventMouseMotion.Relative.x * _mouseSensitivity));
				_head.RotateX(Mathf.DegToRad(-inputEventMouseMotion.Relative.y * _mouseSensitivity));

				// Don't let the camera move beyound a certain point in the X axis
				var newHeadRotation = _head.Rotation;
				newHeadRotation.x = Mathf.Clamp(newHeadRotation.x, Mathf.DegToRad(-90), Mathf.DegToRad(90));
				_head.Rotation = newHeadRotation;
			
			} else if (inputEvent is InputEventKey)
			{
				InputEventKey inputEventKey = (InputEventKey) inputEvent;

				if (inputEventKey.IsActionPressed("toggle_mouse")) // TOggle having the mouse confined or visible
				{
					confineHideMouse = !confineHideMouse;
				}
			}

			if (inputEvent.IsActionPressed("change_camera"))
			{
				if (_cameraPositionValue == 0)
					_cameraPositionValue = 1;
				else _cameraPositionValue = 0;

				cameraPosition = _cameraPositions[_cameraPositionValue];
			}
		}
	}

}
