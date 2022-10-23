// Code written by Oladeji Sanyaolu (Player/Sam) 22/10/2022

using Godot;
using System;

public partial class Sam : CharacterBody3D
{
	/*
		Public
	*/
	// Properties
	public bool MultiplayerActive {
		get { return multiplayerActive; }
		set { multiplayerActive = value; }
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

	// Godot variables
	private bool multiplayerActive = false;
	private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle(); // Get the gravity from the project settings to be synced with RigidBody nodes.
	private int Speed = 5;

	private float MouseSensitivity = 0.08f;
	
	private Vector3 PuppetPosition = Vector3.Zero;
	private Vector3 PuppetVelocity = Vector3.Zero;
	private Vector2 PuppetRotation = Vector2.Zero;

	// Node variables
	private Node3D Head;
	private Camera3D Camera;
	private Node3D Model;
	private Timer NetworkTickRate;
	private Tween movementTween;
	
	/*
		Private Methods
	*/
	private bool isMaster() // If the player is in single player, or controls the particular player in multiplayer
	{	
		if (!multiplayerActive || IsMultiplayerAuthority())
			return true;
		
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
	private void puppetUpdateState(Vector3 p_position, Vector3 p_velocity, Vector2 p_rotation)
	{
		PuppetPosition = p_position;
		PuppetVelocity = p_velocity;
		PuppetRotation = p_rotation;
	}

	/*
		GODOT Methods
	*/
	public override void _Ready()
	{
		// Set the Nodes from the NodePaths
		Head = (Node3D) GetNode(HeadNodePath) as Node3D;
		Camera = (Camera3D) GetNode(CameraNodePath) as Camera3D;
		NetworkTickRate = (Timer) GetNode(NetworkTickRateNodePath) as Timer;
		Model = (Node3D) GetNode(ModelNodePath);

		// Set the camera and model to the right player
		Camera.Current = isMaster();
		Model.Visible = isMaster();
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor()) // Let's have gravity
			velocity.y -= gravity * (float)delta;
		
		if (isMaster()) // This is our character
		{
			Vector3 desired_velocity = getInput() * Speed;

			velocity.x = desired_velocity.x;
			velocity.z = desired_velocity.z;

			if (Input.IsActionPressed("jump") && IsOnFloor())
				velocity.y += JUMPVELOCITY;
		} else { // This is not our character
			//GlobalTransform.origin = PuppetPosition;

			velocity.x = PuppetVelocity.x;
			velocity.z = PuppetVelocity.z;

			//Rotation.y = PuppetRotation.y;
			//head.Rotation.x = PuppetRotation.x;
		}

		// Apply the whole physics
		Velocity = velocity;
		MoveAndSlide();
	}
	public override void _Input(InputEvent inputEvent)
	{
		if (isMaster())
		{
			if (inputEvent is InputEventMouseMotion)
			{
				InputEventMouseMotion _inputEventMouseMotion = (InputEventMouseMotion) inputEvent;
				RotateY(Mathf.DegToRad(-_inputEventMouseMotion.Relative.x * MouseSensitivity));
				Head.RotateX(Mathf.DegToRad(-_inputEventMouseMotion.Relative.y * MouseSensitivity));
				//Head.Rotate.x = Mathf.Clamp(Head.Rotation.x, Mathf.DegToRad(-90), Mathf.DegToRad(90));
			}
		}
	}

}
