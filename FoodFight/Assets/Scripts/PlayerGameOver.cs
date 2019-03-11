﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerGameOver : MonoBehaviour {

	public Text MainText;
	public Text RedScoreText;
	public Text BlueScoreText;
	private GameObject Background;

	private Client client;

	private GameEndState gameEndState;

	// Use this for initialization
	void Start () {
		Background = GameObject.Find("Background").GetComponent<GameObject>();

		DontDestroyOnLoad(GameObject.Find("Client"));
		client = GameObject.Find("Client").GetComponent<Client>();

		gameEndState = client.getGameEndState();
		string team = client.getTeam();

		MainText.text = gameEndState.winningTeamStr() + " Team Wins!";

		RedScoreText.text = "Red Score: " + gameEndState.getRedScore().ToString();
		BlueScoreText.text = "Blue Score: " + gameEndState.getBlueScore().ToString();

		if (team.Equals(gameEndState.winningTeamStr())) {
			Winner();
		} else {
			if (gameEndState.getWinningTeam() != GameEndState.EndState.DRAW) {
				Loser();
			} else {
				Draw();
			}
		}
	}

	// Update is called once per frame
	void Update () {

	}

	void Winner() {
		MainText.text = "You win!";
	}

	void Loser() {
		MainText.text = "You lose!";
	}

	void Draw() {
		MainText.text = "Draw!";
	}
}
