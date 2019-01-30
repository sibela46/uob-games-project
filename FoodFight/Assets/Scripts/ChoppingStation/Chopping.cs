﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Chopping : MonoBehaviour {

    // Initialise different screens
    public Transform defaultCanvas;
    public Transform startCanvas;
    public Transform warningCanvas;
    public Transform endCanvas;

    public Player player;

    private float maxAcc = 0.5f;  //The highest acceleration recorded so far
    private int chopCount = 0;    //number of chops
    //private bool gameStarted = false;

    // Output text to be displayed on screen
    public Text yAcc;      //only for testing
    public Text chops;
    public Text outCome;
    public Text status;
    public Image shakeImage;

    // Sounds to accompany up and down acceleration
    public AudioClip downSound;
    public AudioClip upSound;

    public GameObject blood;
    private AudioSource source;

    private static Ingredient currentIngred;
    public List<Ingredient> boardContents = new List<Ingredient>();
    public List<Ingredient> newBoardContents = new List<Ingredient>();

    private void Start()
    {
        currentIngred = Player.currentIngred;
        //set up scene
        source = GetComponent<AudioSource>();
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        //call to centre block(knife) to the middle of the screen
        InvokeRepeating("CenterKnife", 0f, 5.0f);

        //setting up ingredient

        CheckIngredientValid();

        Time.timeScale = 0;

        boardContents = new List<Ingredient>();
    }

    void Update()
    {

        ChoppingStatus();

        MoveKnife();

        if (Input.acceleration.y > maxAcc)
        {
            maxAcc = Input.acceleration.y;
            yAcc.text = maxAcc.ToString();
            Destroy(shakeImage);
        }

        CheckDownMovement();
        CheckUpMovement();
        CheckChopSpeed();
        instantiateIngredientsInStation();
    }

    public void StartGame()
    {
        if (defaultCanvas.gameObject.activeInHierarchy == false)
        {
            startCanvas.gameObject.SetActive(false);
            defaultCanvas.gameObject.SetActive(true);
            Time.timeScale = 1;
        }
    }

    void CenterKnife()
    {
        transform.position = new Vector3(0, 0, 0);
        source.PlayOneShot(downSound);
    }

    void MoveKnife()
    {
        if (Input.acceleration.y > 3.0f || Input.acceleration.y < -3.0f)
        {
            transform.Translate(0, Input.acceleration.y * 0.5f, 0);
        }
    }

    void CheckUpMovement()
    {
        if (Input.acceleration.y < -3.0f)
        {
            source.PlayOneShot(downSound);
        }
    }

    void CheckDownMovement()
    {
        if (Input.acceleration.y > 3.0f)
        {
            source.PlayOneShot(downSound);
            //display number of chops completed
            Player.currentIngred.numberOfChops++;
            chops.text = Player.currentIngred.numberOfChops.ToString();
        }
    }

    void CheckChopSpeed()
    {
        if (maxAcc > 5.0f || maxAcc < -5.0f)
        {
            outCome.text = "YOU CHOPPED OFF YOUR FINGER!";
            defaultCanvas.gameObject.SetActive(false);
            warningCanvas.gameObject.SetActive(true);
            Time.timeScale = 0;
            //Instantiate(blood, new Vector3(transform.position.x, transform.position.y, 0f), Quaternion.identity);
        }
        else if (maxAcc < 3.5f && maxAcc > -3.5f)
        {
            outCome.text = "CHOP HARDER!";
        }
        else
        {
            outCome.text = "";
        }
    }

    void ChoppingStatus()
    {
        /* Checks whether the ingredient has been chopped the appropriate number of times, based on the json */
        if (FoodData.Instance.isChopped(Player.currentIngred))
        {
            status.text = "Ingredient chopped";
            defaultCanvas.gameObject.SetActive(false);
            endCanvas.gameObject.SetActive(true);
            Time.timeScale = 0;
        }
        /* If not, keep chopping */
        else
        {
            status.text = "";
        }
    }

    void CheckIngredientValid()
    {
        if (currentIngred != null)
        {
        }
    }

    public void goBack()
    {
        if (chopCount > 20)
        {
            // Sends the chopped ingredient to server
            player = GameObject.Find("Player").GetComponent<Player>();
            player.notifyServerAboutIngredientPlaced();
        }
        SceneManager.LoadScene("PlayerMainScreen");
    }

    public void instantiateIngredientsInStation()
    {
        /* If available, add the held ingredient to the pan */
        newBoardContents = Player.ingredientsFromStation;

        /* Draw ingredient models in pan */
        foreach (Ingredient ingredient in newBoardContents)
        {
            if (boardContents.IndexOf(ingredient) < 0)
            {
                GameObject model = (GameObject)Resources.Load(ingredient.Model, typeof(GameObject));
                model = Instantiate(model, new Vector3(0, 0, 0), Quaternion.identity);
                model.transform.SetParent(startCanvas);
                boardContents.Add(ingredient);
            }
        }

    }

    public void putIngredient()
    {
        if (currentIngred != null)
        {
            GameObject model = (GameObject)Resources.Load(currentIngred.Model, typeof(GameObject));
            model = Instantiate(model, new Vector3(0, 0, 0), Quaternion.identity);
            model.transform.SetParent(startCanvas);
        }
    }
}
