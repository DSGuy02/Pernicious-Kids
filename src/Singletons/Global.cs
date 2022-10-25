// Code written by Oladeji Sanyaolu (Singletons/Global) 25/10/2022

using Godot;
using System;

public partial class Global : Node
{
	// Properties
	public Vector2 gameDimensions
	{
		get { return _gameDimensions; }
	}

	public bool viewUsername
	{
		get { return _viewUsername; }
		set { viewUsername = value; }
	}

	public bool viewFramerate
	{
		get { return _viewFramerate; }
		set { _viewFramerate = value; }
	}

	// Private variables
	private Vector2 _gameDimensions;
	private bool _viewUsername = true;
	private bool _viewFramerate = false;

	/*
		Public Methods
	*/

	// Setter and Getter for audio volumes
	public void SetAudioBusVolumeDb(String audioBus, float audioVolumeDb)
	{
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(audioBus), audioVolumeDb);
	}

	public float GetAudioBusVolumeDb(String audioBus)
	{
		return AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex(audioBus));
	}

	/*
		GODOT Methods
	*/
	public override void _Ready()
	{
		_gameDimensions = new Vector2(
			(int) ProjectSettings.GetSetting("display/window/size/viewport_width"),
			(int) ProjectSettings.GetSetting("display/window/size/viewport_height")
		);
	}

    public override void _UnhandledInput(InputEvent inputEvent)
    {
		if (inputEvent is InputEventKey) // It must confirm that the current InputEvent was infact, an InputEventKey
		{
			InputEventKey inputEventKey = (InputEventKey) inputEvent;
			
			// Set fullscreen
			if (inputEventKey.Pressed && inputEventKey.Keycode == Key.F5)
			{
				if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen)
					DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
				else
					DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			}

			// Set use UPNP
			if (inputEvent.IsActionPressed("use_upnp"))
			{

			}
			// View/Hide framerate
		}
	}

}
