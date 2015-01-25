﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class Controller : MonoBehaviour  {
	private static Controller _instance; 
	public static Controller Instance { 
		get { return _instance; }
	}

	public StaffController Staff;

	private Song _curSong; 
	public Song CurSong { 
		get { return _curSong; }
	}

	private float _curEnergy; 
	private float _lastEnergy;
	public float CurrentEnergy { 
		get { return _curEnergy; }
	}

	private float _startStartTimeStamp;
	private float _songTime;
	public float SongPlayTime { 
		get { return _songTime; }
	}

	private bool _songPlaying = false;
	public bool IsPlaying { 
		get { return _songPlaying; }
	}
	
	private const float kBaseScoreMultiplier = .05f; //100 pts per second of accurate song playing

	//User Input
	private int _userNoteID; //This is the note that the user is currently using
	private int _newUserNote;
	public static int MinNoteID = 0;
	public static int MaxNoteID = 24;


	public AudioSource AudioBG; //This is the BG music that is always playing
	public AudioSource AudioUser; //This is the user created music that they play along with

	public int CurPoints; //How many points has the user accumulated so far
	public UnityEngine.UI.Slider SliderEnergy; //This is the UI reference to the points that the player currently has

	public PlayingNote UserNote;
	public Text TxtPlayingNote;
	private bool _isUserPlayingNote = false;


	void Awake() { 
		//Singleton Check
		if(_instance != null && _instance != this) { 
			Destroy(this);
		} if(_instance == null) { 
			_instance = this; 
		} 
	}

	void Start() { 
		_curEnergy = _lastEnergy = 0.5f;
		UserNote.gameObject.SetActive(false);
		LoadTestSong();
		StartCoroutine(CoroutineWaitAndStartSong());
	}
	
	// Update is called once per frame
	void Update () {
		if(_curSong != null && _songPlaying) { 
			Staff.UpdateStaff();
			HandleInput();	
			ScorePoints();
		}
	}

	private void HandleInput() { 
		//Handle the keyboard input so that we can move up and down the staff
		_newUserNote = _userNoteID;
		if(Input.GetButtonDown("NoteDown")) { 
			//We move up or down the note hierarchy
			_newUserNote -= 2;
		} else if(Input.GetButtonDown("NoteUp")) { 
			_newUserNote += 2;
		}

		//Bounds the user note
		_newUserNote = Mathf.Clamp(_newUserNote, MinNoteID, MaxNoteID);
		SetUserNote(_newUserNote);

		//If the user is trying to activate the sound
		SetActivateNote(Input.GetButton("NoteActivate"));
	}

	//Update the user note that the user is going to play, mainly used for mouse play
	public void SetUserNote(int newNote) { 
		//Display the user's input note currently
		if(newNote != _userNoteID) { 
			//Debug.Log ("Showing Note: " + newNote);
			UserNote.ShowNote(newNote);
			_userNoteID = newNote;
			if(_isUserPlayingNote) { 
				UpdateActiveNoteText();
			}
		}
	}

	public void SetActivateNote(bool isActive) { 
		if(isActive == _isUserPlayingNote) { 
			return; //already set
		}

		Debug.Log ("SetActivateNote: " + isActive);
		_isUserPlayingNote = isActive;
		UpdateActiveNoteText();
	}

	private void UpdateActiveNoteText() { 
		if(_isUserPlayingNote) { 
			TxtPlayingNote.text = Note.ConvertNoteIDToName(_userNoteID);
		} else { 
			TxtPlayingNote.text = string.Empty;
		}
	}

	//Score points based on the values that are enabled
	private void ScorePoints() { 
		//Based on the current play mask determine if we are playing the current note or not
		_songTime = Time.time - _startStartTimeStamp;
		_curSong.UpdateSongTime(_songTime);
		//Now we will get the current notes that are playing and determine how many bit flags match
		int numFailNotes = 0;
		foreach(Note toCheck in _curSong.CurrentNotes) { 
			if(toCheck.NoteID == _userNoteID && _isUserPlayingNote) {
				_curEnergy += Time.deltaTime * kBaseScoreMultiplier * 2f;
			} else { 
				numFailNotes++;
			}
		}

		if(_curSong.CurrentNotes.Count == 0 && _isUserPlayingNote) { 
			numFailNotes++;
		}

		_curEnergy += Time.deltaTime * -kBaseScoreMultiplier * Mathf.Min(numFailNotes, 1);
		_curEnergy = Mathf.Clamp01(_curEnergy); //Clamp CurEnergy to be between 0 and 1


		if(_isUserPlayingNote) { 
			UserNote.ShowPlaying(numFailNotes == 0);
			if(numFailNotes == 0) { 
				if(AudioUser.volume < 1.0f) {
					AudioUser.volume = 1.0f;
				}
			}
		} else { 
			UserNote.StopPlaying();
		}


		if(numFailNotes != 0 && AudioUser.volume > 0f) { 
			AudioUser.volume = 0.0f;
		} 

		//For every note that we are still playing, we will determine what we gain/lost
		UpdateEnergyUI();
	}

	private void UpdateEnergyUI() { 
		//Only update if we have a change 
		if(_curEnergy != _lastEnergy) {
			_lastEnergy = _curEnergy;
			SliderEnergy.value = _curEnergy;
		}
	}

	private void LoadTestSong() { 
		//Create a temporary song
		_curSong = Song.CreateTestSong();
		Staff.LoadSong(_curSong); //Load up all of the song notes into the UI
	}

	private IEnumerator CoroutineWaitAndStartSong() { 
		yield return new WaitForSeconds(2.0f); //Just wait 2 seconds and then play the song
		AudioBG.Play();
		AudioUser.Play();
		AudioUser.volume = 0f;
		_startStartTimeStamp = Time.time;
		_songPlaying = true;
	}
}
