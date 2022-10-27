// Code written by Oladeji Sanyaolu (Singletons/Save) 25/10/2022

using Godot;
using Godot.Collections;
using System;

public partial class Save : Node
{
	// Properties
	public Dictionary<string, Variant> saveData
	{
		get { return _saveData; }
	}

	// Private variables
	// Constants
	private readonly string SAVEGAMEPATH = "user://savedata.pernk";
	private readonly string[,] RANDOMUSERNAME = new string[,]
	{
		{
			"John",
			"Bean",
			"Dee",
			"Mr.",
			"Mrs.",
			"Ms.",
			"Bev",
			"Iris",
			"Perry",
			"Kate",
		},
		{
			"",
			" Cage",
			" Doe",
			" West",
			" Jim",
			" Liz",
			" Lola",
			" Jess",
			" Trace",
			" Chip",

		},
	};

	// Other
	private Dictionary<string, Variant> _saveData;
	private RandomNumberGenerator _randomNumberGenerator;
	/*
		Private methods
	*/
	private Dictionary<string, Variant> getData()
	{
		Dictionary<string, Variant> savedData;

		if (!FileAccess.FileExists(SAVEGAMEPATH))
		{
			_randomNumberGenerator = new RandomNumberGenerator();
			_randomNumberGenerator.Randomize();

			_saveData = new Dictionary<string, Variant>()
			{
				{ "Username", 
					RANDOMUSERNAME[0,_randomNumberGenerator.RandiRange(0, RANDOMUSERNAME.GetLength(0))]
					+ RANDOMUSERNAME[1,_randomNumberGenerator.RandiRange(0, RANDOMUSERNAME.GetLength(1))] },
				{ "MouseSensitivity", 0.08f },
				{ "ControllerSensitivity", 1.0f },
				{ "TargetFramerate", 0 },
				{ "ShowFramerate", false },
				{ "UseUPNP", false },
				{ "ServerPort", 24800 },
				{ "ServerRegion", "" },
				{ "MusicVolumeDB", 10 },
				{ "SoundVolumeDB", 10 },
				{ "JingleVolumeDB", 10 },
			};

			saveGame();

			return _saveData;
		}


		FileAccess fileAccess = FileAccess.Open(SAVEGAMEPATH, FileAccess.ModeFlags.Read);
		
		string content = fileAccess.GetLine();
		
		Variant data = JSON.ParseString(content);

		savedData = (Dictionary<string, Variant>) data;

		return savedData;
	}

	private void saveGame()
	{
		FileAccess saveGameData = FileAccess.Open(SAVEGAMEPATH, FileAccess.ModeFlags.Write);
		saveGameData.StoreLine(JSON.Stringify(saveData));
	}
	/*
		Public methods
	*/
	public void SaveValue(string save_data_key, Variant save_data_value)
	{
		_saveData[save_data_key] = save_data_value;
		saveGame();
	}

	/*
		GODOT methods
	*/
	public override void _Ready()
	{
		_saveData = getData();
	}

}
