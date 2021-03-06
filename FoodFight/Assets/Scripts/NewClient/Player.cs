﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEditor;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {

  private GameObject networkClient;
  private Client network;

  /* Current station:
  -1 - Main Screen
  0  - Cupboard
  1  - Chopping
  2  - Frying
  3  - Plating */
  public static string currentStation = "-1";

  public static Ingredient currentIngred;
  public static List<Ingredient> ingredientsFromStation;

  public Material redBackground, blueBackground;
	public Renderer background;
  public static Text errorText;
  public Text myScoreText, otherScoreText;

  // NFC Stuff:
  private NFCHandler nfcHandler = new NFCHandler();

  void Start () {
    Screen.orientation = ScreenOrientation.Portrait;
    if (Client.gameState.Equals(ClientGameState.MainMode)) {
      networkClient = GameObject.Find("Client");
      network = networkClient.GetComponent<Client>();
      DontDestroyOnLoad(GameObject.Find("Player"));
      Destroy(GameObject.Find("SimulatedPlayer"));
      if (network.getTeam().Equals("blue")) {
        background.material = blueBackground;
      } else if (network.getTeam().Equals("red")) {
        background.material = redBackground;
      }
    }
  }

  void Update () {
    /* For desktop testing */
    if (Input.GetKeyDown(KeyCode.R)) checkStation("0");
    if (Input.GetKeyDown(KeyCode.T)) checkStation("1");
    if (Input.GetKeyDown(KeyCode.Y)) checkStation("2");
    if (Input.GetKeyDown(KeyCode.U)) checkStation("3");
    if (Input.GetKeyDown(KeyCode.E)) Debug.Log(Player.currentIngred.Model);

    /* Check for any NFC scans, forwarding to checkStation if present */
    string lastTag = nfcHandler.getScannedTag();
    if (lastTag != "-1" && currentStation == "-1") {
      Handheld.Vibrate();
      checkStation(lastTag);
    }

    UpdateScore();
  }

  /* Alerts the server about the ingredient placed at the current station */
  public void notifyServerAboutIngredientPlaced(Ingredient ingredient) {
    string message;

    if (ingredient != null) message = Ingredient.SerializeObject(ingredient);
    else message = "Error: no ingredient to add";

    network.SendMyMessage("add", message);
  }

  /* Clear all the ingredients in the current station */
  public void clearIngredientsInStation() {
    ingredientsFromStation.Clear();
    network.SendMyMessage("clear", "");
  }

  /* Notifies the server when the player leaves a station */
  public void notifyAboutStationLeft() {
    network.SendMyMessage("leave", "");
    resetCurrentStation();
  }

  /* Resets the player's current ingredient */
  public static void removeCurrentIngredient() {
    currentIngred = null;
  }

  /* Checks if the player is currently holding anything */
  public static bool isHoldingIngredient() {
    return currentIngred != null;
  }

  /* Resets the current station of the player locally */
  public static void resetCurrentStation() {
    currentStation = "-1";
  }

  /* Sends the score to the server after a player plates a dish */
  public void sendScoreToServer(Ingredient recipe) {
    string message = Ingredient.SerializeObject(recipe);
    network.SendMyMessage("score", message);
  }

  /* Updates the score of the player after plating a dish */
  public void UpdateScore() {
    string currentScene = SceneManager.GetActiveScene().name;
    if (currentScene == "PlayerMainScreen" && Client.gameState == ClientGameState.MainMode) { /* Scene where those two texts exist */
      myScoreText = GameObject.Find("MyScore").GetComponent<Text>();
      otherScoreText = GameObject.Find("OtherScore").GetComponent<Text>();
      myScoreText.text = "My score " + "\n" + network.myScore.ToString();
      otherScoreText.text = "Other score " + "\n" + network.otherScore.ToString();
    }
  }

  /* Sends throw to the server after a player throws a dish */
  public void sendThrowToServer(Ingredient recipe) {
    string message = Ingredient.SerializeObject(recipe);
    network.SendMyMessage("throw", message);
  }

  /* Notifies the server when the player logs into a station */
  private void checkStation(string text) {
    if (currentStation != text) {

      resetErrorText();
      Client.resetDisabledTimer();

      // Tell the server which station you're logging in at.
      switch (text) {
        case "0":
          currentStation = text;
          network.SendMyMessage("station", text);
          break;
        case "1":
          currentStation = text;
          network.SendMyMessage("station", text);
          break;
        case "2":
          currentStation = text;
          network.SendMyMessage("station", text);
          break;
        case "3":
          currentStation = text;
          network.SendMyMessage("station", text);
          break;
        case "8":
          // Join red team
          if (!network.isConnected) {
            network.Connect();
          }
          network.onClickRed();
          break;
        case "9":
          // Join blue team
          if (!network.isConnected) {
            network.Connect();
          }
          network.onClickBlue();
          break;
        default:
          currentStation = "-1";
          break;
      }
    }
  }

  public static void displayDisabledStation(float disabledTimer) {
    TimeSpan t = TimeSpan.FromSeconds(disabledTimer);
    errorText = GameObject.Find("ErrorText").GetComponent<Text>();
    errorText.text = "This station is disabled for another " + t.Seconds + " seconds.";
	}

	public static void displayOccupiedStation() {
    errorText = GameObject.Find("ErrorText").GetComponent<Text>();
    errorText.text = "Oh no! This station is occupied.";
	}

  public static void resetErrorText() {
    errorText = GameObject.Find("ErrorText").GetComponent<Text>();
    errorText.text = "";
  }

  /* Ignore the code below, I forgot to get nfc-s so had to create buttons for stations - xoxo, Sisi */
  public void goToCupboard() {
    checkStation("0");
  }

  public void goToChopping() {
    checkStation("1");
  }

  public void goToFrying() {
    checkStation("2");
  }

  public void goToPlating() {
    checkStation("3");
  }
}
