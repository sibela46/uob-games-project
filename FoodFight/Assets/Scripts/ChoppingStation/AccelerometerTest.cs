﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AccelerometerTest : MonoBehaviour {

    // Initialise different screens
    public Transform defaultCanvas;
    public Transform startCanvas;
    public Transform warningCanvas;
    public Transform endCanvas;

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

    private static Ingredient currentIngredient;

    private void Start()
    {
        //set up scene
        source = GetComponent<AudioSource>();
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        //call to centre block(knife) to the middle of the screen
        InvokeRepeating("CenterKnife", 0f, 5.0f);

        //setting up ingredient

        CheckIngredientValid();

        Time.timeScale = 0;
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

        // Check if ingredient has been chopped a certain number of times
        // and pass it to Player.

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
            chopCount++;
            //currentIngredient.noOfChops--;
            chops.text = chopCount.ToString();
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
        if (chopCount > 50)
        {
            status.text =  "Ingredient chopped";
            defaultCanvas.gameObject.SetActive(false);
            endCanvas.gameObject.SetActive(true);
            Time.timeScale = 0;
        }
        else
        {
            //status.text =  "keep chopping";
            status.text = "";
        }
    }

    void CheckIngredientValid()
    {
        //check if currentIngredient is valid
        if (currentIngredient.isChoppable)
        {
            outCome.text = "";
        }
        else
        {
            outCome.text = "ingredient cannot be chopped";
            Time.timeScale = 0;     //stops the minigame if ingredient cannot be chopped
        }
        
    }
}
