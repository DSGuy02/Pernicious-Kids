// Code written by Oladeji Sanyaolu (Player/Sam) 22/10/2022

using Godot;
using System;

public partial class Sam : CharacterBody3D
{
	/*
		Public
	*/
	// Properties
	public bool multiplayerActive {
		get { return _multiplayerActive; }
		set { _multiplayerActive = value; }
	}

	public Vector3 cameraPosition {
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
	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle(); // Get the gravity from the project settings to be synced with RigidBody nodes.
	private float _mouseSensitivity = 0.08f;

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
		Vector3 _inputDir = Vector3.Zero;

		// Up and Down Movement
		if (Input.IsActionPressed("forward"))
			_inputDir += -GlobalTransform.basis.z;
		if (Input.IsActionPressed("backward"))
			_inputDir += GlobalTransform.basis.z;
		
		// Left and Right Movement
		if (Input.IsActionPressed("leftward"))
			_inputDir += -GlobalTransform.basis.x;
		if (Input.IsActionPressed("rightward"))
			_inputDir += GlobalTransform.basis.x;
		
		_inputDir = _inputDir.Normalized();

		return _inputDir;
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
		_model.Visible = isMaster();

		// Set the camera position
		cameraPosition = _cameraPositions[_cameraPositionValue];

		// Connect Signals
		_networkTickRate.Timeout += () => _onNetworkTickRateTimeout();
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor()) // Let's have gravity
			velocity.y -= _gravity * (float)delta;
		
		if (isMaster()) // This is our character
		{
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
	public override void _Input(InputEvent inputEvent)
	{
		if (isMaster())
		{
			if (inputEvent is InputEventMouseMotion) // Camera movement
			{
				InputEventMouseMotion _inputEventMouseMotion = (InputEventMouseMotion) inputEvent;
				RotateY(Mathf.DegToRad(-_inputEventMouseMotion.Relative.x * _mouseSensitivity));
				_head.RotateX(Mathf.DegToRad(-_inputEventMouseMotion.Relative.y * _mouseSensitivity));

				// Don't let the camera move beyound a certain point
				var newHeadRotation = _head.Rotation;
				newHeadRotation.x = Mathf.Clamp(newHeadRotation.x, Mathf.DegToRad(-90), Mathf.DegToRad(90));
				_head.Rotation = newHeadRotation;
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
