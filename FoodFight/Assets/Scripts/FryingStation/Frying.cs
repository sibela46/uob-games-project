﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;

public class Frying : MonoBehaviour {

	private readonly string stationID = "1";

	public Button goBackBtn, putBtn, pickBtn, clearBtn, combineBtn;
	public Text test_text;
	public Material success;
	public Renderer background;
	public Player player;
	public AudioClip fryingSound;

	/* Phone motion stuff */
	private float accelerometerUpdateInterval = 1.0f / 60.0f;
	private float lowPassKernelWidthInSeconds = 1.0f;
	private float shakeDetectionThreshold = 2.0f;
	private float lowPassFilterFactor;
	private Vector3 lowPassValue;

	/* Shaking stuff */
	private float shakeSpeed = 10.0f; // Speed of pan shake
	private float shakeAmount = 1.2f; // Amplitude of pan shake
	private bool shouldShake = false;
	private int negSinCount = 0, posSinCount = 0;
	private Vector3 originalPos;
	private float lastShake;
	private int minimumShakeInterval = 1; // (seconds)
	private AudioSource source;

	/* Ingredient stuff */
	public List<Ingredient> panContents = new List<Ingredient>();
	private List<GameObject> panContentsObjects = new List<GameObject>();

	/* Other */

	void Start () {

		Screen.orientation = ScreenOrientation.Portrait;

		test_text.text = "Pan shakes: 0";
		lowPassFilterFactor = accelerometerUpdateInterval / lowPassKernelWidthInSeconds;
		shakeDetectionThreshold *= shakeDetectionThreshold;
		lowPassValue = Input.acceleration;
		originalPos = gameObject.transform.position;
		lastShake = Time.time;
		source = GetComponent<AudioSource>();

		List<Ingredient> ingredientsFromStation = Player.ingredientsFromStation;

		clearPan();

		foreach (Ingredient ingredient in ingredientsFromStation) {
			addIngredientToPan(ingredient);
		}
	}

	void Update () {
		/* Ensure correct buttons are interactable */
		updateButtonStates();

		if (panContents.Count == 1) {

			/* Read accelerometer data */
			Vector3 acceleration = Input.acceleration;
			lowPassValue = Vector3.Lerp(lowPassValue, acceleration, lowPassFilterFactor);
			Vector3 deltaAcceleration = acceleration - lowPassValue;

			shakeIfNeeded();

			/* For desktop tests. */
			if (Input.GetKeyDown(KeyCode.DownArrow)) {
				tryStartShake();
			}

			if (deltaAcceleration.sqrMagnitude >= shakeDetectionThreshold) {
				/* Shake detected! */
				tryStartShake();
			}

		} else {
			if (panContents.Count == 0) {
				test_text.text = "Pan is empty";
			} else {
				test_text.text = "Combine ingredients to cook";
			}
			/* TODO: What happens when pan is empty or too full */
		}
		if (Input.GetKeyDown(KeyCode.E)) {
			foreach (Ingredient ingredient in panContents) {
				Debug.Log(ingredient.Name);
			}
		}
	}

	private void tryStartShake() {
		/* Make sure shake is not too soon after previous shake */
		if ((Time.time - lastShake) > minimumShakeInterval) {
			shouldShake = true;

			source.PlayOneShot(fryingSound);

			/* Increment the number of pan tosses of all ingredients in pan */
			foreach (Ingredient ingredient in panContents) {
				ingredient.numberOfPanFlips++;
				if (FoodData.Instance.isCooked(ingredient)) {
					test_text.text = "Ingredient cooked!";
					background.material = success;
				} else {
					/* Update shake text */
					test_text.text = "Pan shakes: " + ingredient.numberOfPanFlips;
				}
				lastShake = Time.time;
			}

		}
	}

	/* Manages the sinusoidal movement of the pan */
	private void shakeIfNeeded() {
		if (shouldShake) {
			float xTransform = -1 * Mathf.Sin((Time.time - lastShake) * shakeSpeed) * shakeAmount;

			if (negSinCount > 0 && posSinCount > 0 && xTransform < 0) {
				gameObject.transform.position = originalPos;
				negSinCount = 0; posSinCount = 0;
				shouldShake = false;
			}	else if (xTransform < 0) {
				transform.Translate(0, 0, xTransform);
				negSinCount++;
			} else if (xTransform > 0) {
				transform.Translate(0, 0, xTransform);
				posSinCount++;
			}
		}
	}

	public void placeHeldIngredientInPan()
	{
		/* Add ingredient */
		if (Player.currentIngred != null) {
			addIngredientToPan(Player.currentIngred);

			/* Notify server that player has placed ingredient */
			player = GameObject.Find("Player").GetComponent<Player>();
			player.notifyServerAboutIngredientPlaced(Player.currentIngred);

			Player.removeCurrentIngredient();
		} else {
			/* TODO: What happens when player is not holding an ingredient */
			test_text.text = "No held ingredient";
		}
	}

	public void combineIngredientsInPan()
	{
		if (panContents.Count > 1) {
			/* Try and combine the ingredients */
			Ingredient combinedFood = FoodData.Instance.TryCombineIngredients(panContents);

			if (combinedFood.Name == "mush") {
				test_text.text = "Ingredients do not combine";
			} else {
				/* Set the pan contents to the new combined recipe */
				clearStation();

				addIngredientToPan(combinedFood);
				player.notifyServerAboutIngredientPlaced(combinedFood);
			}
		} else {
			test_text.text = "No ingredients to combine";
		}
	}

	public void clearStation() {
		clearPan();
		player = GameObject.Find("Player").GetComponent<Player>();
		player.clearIngredientsInStation(stationID);
	}

	public void pickUpIngredient()
	{
		if (panContents.Count == 1) {
			/* Set the players current ingredient to the pan contents */
			foreach (Ingredient ingredient in panContents) {
				Player.currentIngred = ingredient;
			}

			/* Clear the pan */
			clearPan();
			player = GameObject.Find("Player").GetComponent<Player>();
			player.clearIngredientsInStation(stationID);
		} else {
			/* What to do if there are more than (or fewer than) 1 ingredients in the pan*/
			Debug.Log("Unable to pick up");
			test_text.text = "Unable to pick up";
		}
	}

	private void addIngredientToPan(Ingredient ingredient)
	{
		GameObject model = (GameObject) Resources.Load(ingredient.Model, typeof(GameObject));
		Transform modelTransform = model.GetComponentsInChildren<Transform>(true)[0];

    	Quaternion modelRotation = modelTransform.rotation;
		Vector3 modelPosition = modelTransform.position;
		GameObject inst = Instantiate(model, modelPosition, modelRotation);
		panContents.Add(ingredient);
		panContentsObjects.Add(inst);
	}

	private void updateButtonStates() {
		setButtonInteractable(putBtn, Player.isHoldingIngredient());
		setButtonInteractable(clearBtn, panContents.Count > 0);
		setButtonInteractable(pickBtn, panContents.Count == 1);
		setButtonInteractable(combineBtn, panContents.Count > 1);
	}

	private void setButtonInteractable(Button btn, bool interactable) {
		btn.interactable = interactable;
	}

	private void clearPan()
	{
		foreach (GameObject go in panContentsObjects) Destroy(go);

		panContents.Clear();
		panContentsObjects.Clear();
	}

	public void goBack()
	{
		/* TODO: Need to notify server of local updates to ingredients in pan before leaving */
		/* Notify server that player has left the station */
		player = GameObject.Find("Player").GetComponent<Player>();
		player.notifyAboutStationLeft("1");
		SceneManager.LoadScene("PlayerMainScreen");
	}
}
