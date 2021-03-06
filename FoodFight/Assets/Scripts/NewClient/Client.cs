using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Client : MonoBehaviour {

  private const int MAX_CONNECTION = 10;
  public int port = 8000;
	private readonly string serverIPBase = "192.168.0."; // The base IP
	private string serverIPSuffix = "100"; // The default IP suffix
  public int hostId = 0;
	public int connectionId, reliableChannel;

	public bool isConnected = false;
	public bool startGame = false;
	public static bool isJoined = false;
	private string currentScene = "";
	public string team;

	public List<Ingredient> ingredientsInStation = new List<Ingredient>();
	public int myScore = 0;
	public int otherScore = 0;
	public static ClientGameState gameState = ClientGameState.ConnectState;
	public GameEndState gameEndState;
	public GameObject simulatedClient;

	public GameObject buttonPrefab, startPanel, warningText, connectButton, joinButton, text, tutorialButton;

  public static float disabledTimer = 0.0f;

	public void Start() {
    DontDestroyOnLoad(GameObject.Find("Client"));
		Screen.orientation = ScreenOrientation.Portrait;

		/* When player logs back into main mode after completing the tutorial, destroy the tutorial instances */
		if (gameState.Equals(ClientGameState.JoinState) || gameState.Equals(ClientGameState.ConnectState) || gameState.Equals(ClientGameState.MainMode)) {
			Destroy(GameObject.Find("SimulatedPlayer"));
			Destroy(GameObject.Find("SimulatedClient(Clone)"));
		}

		if (gameState.Equals(ClientGameState.JoinState)) {
			GameObject mainCanvas = GameObject.Find("MainMenuCanvas");
			if(mainCanvas) mainCanvas.SetActive(false);
			warningText = GameObject.Find("ConnectionFailedText");
			joinButton = GameObject.Find("JoinButton");
			text = GameObject.Find("OrText");
			tutorialButton = GameObject.Find("TutorialModeButton");
		}
	}

	public void Update() {
		listenForData();

    if (disabledTimer > 0) {
      disabledTimer -= Time.deltaTime;
      Player.displayDisabledStation(disabledTimer);
      if (disabledTimer <= 0) {
        Player.resetErrorText();
      }
    }

		if (isConnected) {
			isJoined = true;
		}

		currentScene = SceneManager.GetActiveScene().name;
		if (currentScene == "PlayerStartScreen") changeStartScreenButtons();
	}

	/* Change start screen buttons to either Connect or Join */
	private void changeStartScreenButtons() {
		if (gameState.Equals(ClientGameState.ConnectState)) {
			startPanel = GameObject.Find("startPanel");
			startPanel.gameObject.SetActive(true);
			connectButton.SetActive(true);
			joinButton.SetActive(false);
			text.SetActive(false);
			tutorialButton.SetActive(false);
		}
		else if (gameState.Equals(ClientGameState.JoinState)) {
			startPanel = GameObject.Find("startPanel");
      startPanel.gameObject.SetActive(true);
			if (warningText) warningText.SetActive(false);
			if (connectButton) connectButton.SetActive(false);
			if (joinButton) joinButton.SetActive(true);
			if (text) text.SetActive(true);
			if (tutorialButton) tutorialButton.SetActive(true);
		}
	}

	/* Did not enter tutorial mode */
	public void SkipTutorialMode() {
		Destroy(GameObject.Find("SimulatedClient(Clone)"));
	}

  /* On click of the Tutorial Mode button */
	public void OnTutorialStartClick() {
		SceneManager.LoadScene("TutorialIntro");
	}

	/* Called after tutorial introduction is complete */
  public void StartTutorial() {
		Instantiate(simulatedClient, new Vector3(0, 0, 0), Quaternion.identity);
    SceneManager.LoadScene("PlayerMainScreen");
    gameState = ClientGameState.TutorialMode;
		DontDestroyOnLoad(GameObject.Find("SimulatedClient(Clone)"));
  }

	public void JoinGame() {
		SkipTutorialMode();

		if (isJoined) {
			SceneManager.LoadScene("PickTeamScreen");
		} else {
			warningText.SetActive(true);
		}
	}

	public void Connect() {

		SkipTutorialMode();

		NetworkTransport.Init();
		ConnectionConfig connectConfig = new ConnectionConfig();

		byte error;
    string myIPaddress;

		/* Network configuration */
		connectConfig.AckDelay = 33;
		connectConfig.AllCostTimeout = 20;
		connectConfig.ConnectTimeout = 1000;
		connectConfig.DisconnectTimeout = 5000;
		connectConfig.FragmentSize = 500;
		connectConfig.MaxCombinedReliableMessageCount = 10;
		connectConfig.MaxCombinedReliableMessageSize = 100;
		connectConfig.MaxConnectionAttempt = 32;
		connectConfig.MaxSentMessageQueueSize = 4096;
		connectConfig.MinUpdateTimeout = 20;
		connectConfig.NetworkDropThreshold = 40; // we had to set these high to avoid UNet disconnects during lag spikes
		connectConfig.OverflowDropThreshold = 40; //
		connectConfig.PacketSize = 1500;
		connectConfig.PingTimeout = 500;
		connectConfig.ReducedPingTimeout = 100;
		connectConfig.ResendTimeout = 500;

		reliableChannel = connectConfig.AddChannel(QosType.ReliableSequenced);
		HostTopology topo = new HostTopology(connectConfig, MAX_CONNECTION);

		if (hostId >= 0) {
			NetworkTransport.RemoveHost(hostId);
		}
		hostId = NetworkTransport.AddHost(topo, port, null /*ipAddress*/);
		connectionId = NetworkTransport.Connect(hostId, getServerIp(), port, 0, out error);

		/* Check if there is an error */
		if ((NetworkError)error != NetworkError.Ok)
		{
			//Output this message in the console with the Network Error
			Debug.Log("There was this error : " + (NetworkError)error);
			isConnected = false;
			warningText.SetActive(true);
		}
		else {
			isConnected = true;
		}
	}

	private string getServerIp() {
		return serverIPBase + serverIPSuffix;
	}

	public string getTeam() {
		return team;
	}

	// Client always listen for incoming data
	public void listenForData() {
		if (!isConnected) {
			return;
		}

		int recHostId; // Player ID
		int connectionId; // Connection ID
		int channelID; // ID of channel connected to recHostId.
		byte[] recBuffer = new byte[4096];
		int bufferSize = 4096;
		int dataSize;
		byte error;

		NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelID,
																												recBuffer, bufferSize, out dataSize, out error);

		switch (recData) {
			case NetworkEventType.Nothing:
					break;
			case NetworkEventType.ConnectEvent:
					Debug.Log("Player " + connectionId + " has been connected to server.");
					gameState = ClientGameState.JoinState;
					break;
			case NetworkEventType.DataEvent:
					string message = OnData(hostId, connectionId, channelID, recBuffer, bufferSize, (NetworkError)error);
          manageMessageEvents(message);
					break;
			case NetworkEventType.DisconnectEvent:
					Debug.Log("Player " + connectionId + " has been disconnected to server");
					NetworkTransport.Disconnect(hostId, connectionId, out error);
					NetworkTransport.RemoveHost(hostId);
					startGame = false;
					isConnected = false;
					Player.removeCurrentIngredient();
					Player.currentStation = "-1";
					SceneManager.LoadScene("DisconnectScreen");
					break;
			case NetworkEventType.BroadcastEvent:
					Debug.Log("Broadcast event.");
					break;
		}
	}

	//This function is called when data is sent
	private string OnData(int hostId, int connectionId, int channelId, byte[] data, int size, NetworkError error)
	{
		//Here the message being received is deserialized and output to the console
		Stream serializedMessage = new MemoryStream(data);
		BinaryFormatter formatter = new BinaryFormatter();
		string message = formatter.Deserialize(serializedMessage).ToString();

		//Output the deserialized message as well as the connection information to the console
		Debug.Log("OnData(hostId = " + hostId + ", connectionId = "
				+ connectionId + ", channelId = " + channelId + ", data = "
				+ message + ", size = " + size + ", error = " + error.ToString() + ")");

		return message;
	}

	// Sends a message across the network
	public void SendMyMessage(string messageType, string textInput){
		byte error;
		byte[] buffer = new byte[4096];
		int bufferSize = 4096;
		Stream message = new MemoryStream(buffer);
		BinaryFormatter formatter = new BinaryFormatter();
		//Serialize the message
		string messageToSend = messageType + "&" + textInput;
		formatter.Serialize(message, messageToSend);
		Debug.Log("Sending " + messageToSend);
		//Send the message from the "client" with the serialized message and the connection information
		Debug.Log("Host: " + hostId + " connection id: " + connectionId + " channel " + reliableChannel);
		NetworkTransport.Send(hostId, connectionId, reliableChannel, buffer, (int)message.Position, out error);

		//If there is an error, output message error to the console
		if ((NetworkError)error != NetworkError.Ok)
		{
				Debug.Log("Message send error: " + (NetworkError)error);
		}
	}

	//This function serialises the object of type Ingredient to an XML string
	public string SerialiseIngredient(Ingredient ingredient) {
		byte[] buffer = new byte[4096];
		int bufferSize = 4096;
		Stream message = new MemoryStream(buffer);
		BinaryFormatter formatter = new BinaryFormatter();
		//Serialize the message
		Ingredient ingredientString = ingredient;
		formatter.Serialize(message, ingredientString);

		return ingredientString.ToString();
	}

	// Splits up a string based on a given character
	private string[] decodeMessage(string message, char character)
	{
		string[] splitted = message.Split(character);
		return splitted;
	}

	// This is where all the work happens.
  public void manageMessageEvents(string message) {
		string[] decodedMessage = decodeMessage(message, '&');
		string messageType = decodedMessage[0];
		string messageContent = decodedMessage[1];

		switch (messageType) {
			case "station": // Player wants to find out what ingredients are in a station after they log in
				OnStationEnter(messageType, messageContent);
				break;
			case "endgame": // Called when the game is ended with the name of the winning team and the relevant scores
				OnGameEnd(messageType, messageContent);
				break;
			case "newgame": // Called after the new game button is pressed on the server, resets the whole game state
				Debug.Log("Resetting game state");
				OnGameReset();
				break;
      case "connect": // Called after a join team event, for the player to find out which team they are on and load the lobby
				OnConnect(messageContent);
        break;
      case "start": // Broadcasted from the server when the required number of players is reached
        Debug.Log("Starting game...");
        startGame = true;
				gameState = ClientGameState.MainMode;
        break;
			case "score": // Called after serving a recipe to update local score on phone
				OnScoreChange(messageContent);
				break;
			case "add":
				Debug.Log("Adding ingredient failed.");
				break;
			case "leave":
				Debug.Log("Leaving station failed.");
				break;
			case "clear":
				Debug.Log("Clearing ingredients failed.");
				break;
      default:
        Debug.Log("Invalid message type.");
        break;
		}
	}

	private void OnStationEnter(string messageType, string messageContent) {
		string[] data = decodeMessage(messageContent, '$');
		string stationId = data.Length > 0 ? data[0] : "";
    string stationDisableTime = data.Length > 1 ? data[1] : "";

    if (stationId != "") {
      if (Kitchen.isValidStation(stationId)) {

				/* Ingredients are separated by $, so iterate over them and deserialise, adding them to the list of ingredients */
				for (int i = 1; i < data.Length; i++) {
					if (data[i] != ""){
						string receivedIngredient = data[i];
						Ingredient received = new Ingredient();
						received = Ingredient.XmlDeserializeFromString<Ingredient>(receivedIngredient, received.GetType());
						ingredientsInStation.Add(received);
					} else {
						Debug.Log("No ingredients currently exist in that station");
					}
				}

				/* Log the player into the station with that list of ingredients */
				logAppropriateStation(stationId);
				/* Clear out the list of ingredients in that station, since the player already has a referance to them */
				ingredientsInStation = new List<Ingredient>();
			} else if (stationId.Equals("Station disabled")) {
				Debug.Log("Station is disabled.");
        if (!stationDisableTime.Equals("")) {
          disabledTimer = float.Parse(stationDisableTime);
        }
				Player.resetCurrentStation();
			} else if (stationId.Equals("Station occupied")) {
				Debug.Log("Station is already occupied.");
				Player.displayOccupiedStation();
				Player.resetCurrentStation();
			} else if (stationId.Equals("Already at station")) {
				Debug.Log("Already at station.");
			} else {
				Debug.Log("Error: invalid station");
			}
    } else {
			Debug.Log("Error: no station sent");
		}

	}

	private void OnGameEnd(string messageType, string messageContent) {
		string[] details = decodeMessage(messageContent, '$');

		/* Check if content is empty */
		if (messageContent != "") {
			string winningTeam = details[0];
			string redScoreStr = details[1];
			string blueScoreStr = details[2];

			/* Check if all details are present */
			if (winningTeam != "" && redScoreStr != "" && blueScoreStr != "") {
				int redScore = 0;
				int blueScore = 0;

				/* Convert the received string to integer (score) */
				int.TryParse(redScoreStr, out redScore);
				int.TryParse(blueScoreStr, out blueScore);

				/* Initialise the game end state and load the scene */
				gameEndState = new GameEndState(winningTeam, redScore, blueScore);
				Debug.Log("END GAME: " + winningTeam + " " + redScore + " " + blueScore);
				Player.removeCurrentIngredient();
				Player.currentStation = "-1";
				SceneManager.LoadScene("PlayerGameOverScreen");
			} else {
				SendMyMessage(messageType, "Error: one of the details is missing");
			}
		} else {
			SendMyMessage(messageType, "Error: no details about end game");
		}
	}

	public void OnGameReset() {
		/* Reset the player's saved variables */
		ingredientsInStation = new List<Ingredient>();
		Player.ingredientsFromStation = ingredientsInStation;
		Player.removeCurrentIngredient();
		Player.resetCurrentStation();
		SimulatedPlayer.currentIngred = null;
		SimulatedPlayer.ingredientInChopping = null;
		SimulatedPlayer.ingredientsInFrying = ingredientsInStation;
		SimulatedPlayer.ingredientsInPlating = ingredientsInStation;

		/* Reset the scores */
		myScore = 0;
		otherScore = 0;

		/* Update game state */
		gameState = ClientGameState.JoinState;
		startGame = false;

		/* Go back to start screen */
		SceneManager.LoadScene("PlayerStartScreen");
	}

	private void OnConnect(string messageContent) {
		if (messageContent == "red" || messageContent == "blue") {
			team = messageContent;
			SceneManager.LoadScene("LobbyScreen");
		} else {
			/* Error: could not proceed to lobby */
			PickTeam panel = GameObject.Find("buttonsPanel").GetComponent<PickTeam>();
			panel.displayNotRunningText();
			Debug.Log("Error: [" + messageContent + "]");
		}
	}

	private void OnScoreChange(string messageContent) {
		if (messageContent != "") {
			string[] teamScores = decodeMessage(messageContent, '$');
			myScore = Int32.Parse(teamScores[0]);
			otherScore = Int32.Parse(teamScores[1]);
		} else {
			Debug.Log("Error: no content provided on score message.");
		}
	}

	private void logAppropriateStation(string stationId) {
		string currentScene = SceneManager.GetActiveScene().name;

		Debug.Log(currentScene);

		switch(stationId) {
			case "0": // Cupboard Minigame
				if (!currentScene.Equals("CupboardStation")) {
					Player.ingredientsFromStation = ingredientsInStation;
					SceneManager.LoadScene("CupboardStation");
				}
				break;
			case "1": // Chopping Minigame
				if (!currentScene.Equals("NewChoppingStation")) {
					Player.ingredientsFromStation = ingredientsInStation;
					SceneManager.LoadScene("NewChoppingStation");
				}
				break;
			case "2": // Frying Minigame
				if (!currentScene.Equals("FryingStation")) {
					Player.ingredientsFromStation = ingredientsInStation;
					SceneManager.LoadScene("FryingStation");
				}
				break;
			case "3": // Plating Minigame
				if (!currentScene.Equals("PlatingStation")) {
					Player.ingredientsFromStation = ingredientsInStation;
					SceneManager.LoadScene("PlatingStation");
				}
				break;
			default:
					break;
		}
	}

	// All of the functions below are used for buttons
	public void onClickRed() {
		SendMyMessage("connect", "red");
	}

	public void onClickBlue() {
		SendMyMessage("connect", "blue");
	}

  public static void resetDisabledTimer() {
    disabledTimer = 0.0f;
  }

}
