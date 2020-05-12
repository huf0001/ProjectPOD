﻿using System.Collections.Generic;
using UnityEngine;
using Rewired;

/// <summary>
/// The player. Player controls the player's movement and shooting. For building spawning, see BuildingSpawningController.
/// </summary>
public class Player : MonoBehaviour
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------

    //Serialized Fields----------------------------------------------------------------------------

    [Header("Player Objects")]
    [SerializeField] private Transform drone;
    //[SerializeField] private Transform droneModel;
    [SerializeField] private Camera camera;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform terraformerHoldPoint;
    [SerializeField] private Transform laserCannonTip;
    [SerializeField] private Transform laserBatteryPoint;

    [Header("Prefabs")]
    [SerializeField] private LaserBolt laserBoltPrefab;

    [Header("Player Stats")]
    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private int laserBatteryCapacity;
    [SerializeField] private float shootCooldown;

    [Header("Player Inputs")]
    [SerializeField] private int playerID = 0;
    [SerializeField] private Rewired.Player player;


    //Non-Serialized Fields------------------------------------------------------------------------

    //Variables for moving & determining if rotation is necessary
    private Vector3 movement;
    private Vector3 previousMovement = Vector3.zero;

    //Variables for rotating smoothly
    private Quaternion newRotation;
    private Quaternion oldRotation;
    private float slerpProgress = 1;

    private Rigidbody rigidbody;
    private float hoverHeight;

    //Laser Bolt Variables
    private bool shooting;
    private float timeOfLastShot;
    private List<LaserBolt> laserBattery = new List<LaserBolt>();

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    public float GetMovementSpeed {get => movementSpeed;}
    public Rewired.Player GetRewiredPlayer {get => player;}

    //Singleton Public Property--------------------------------------------------------------------

    /// <summary>
    /// Singleton public property for the player.
    /// </summary>
    public static Player Instance { get; protected set; }

    //Basic Public Properties----------------------------------------------------------------------

    /// <summary>
    /// A pool for laser bolts that aren't in use.
    /// </summary>
    public List<LaserBolt> LaserBattery { get => laserBattery; }

    /// <summary>
    /// The physical location of the pool of laser bolts in-scene.
    /// </summary>
    public Transform LaserBatteryPoint { get => laserBatteryPoint; }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There should never be 2 or more Players.");
        }

        Instance = this;

        for (int i = 0; i < laserBatteryCapacity; i++)
        {
            LaserBolt l = Instantiate<LaserBolt>(laserBoltPrefab, laserBatteryPoint.position, laserBoltPrefab.transform.rotation);
            l.transform.parent = laserBatteryPoint;
            laserBattery.Add(l);
        }

        timeOfLastShot = shootCooldown * -1;
        rigidbody = GetComponent<Rigidbody>();
        hoverHeight = drone.position.y;
    }

    void Start()
    {
        player = ReInput.players.GetPlayer(playerID);
    }


    //Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Update() is run every frame.
    /// </summary>
    private void Update()
    {
        //Input manager checks every frame, not at fixed update's rate, so calls to input manager should be made every frame
        GetInput();
    }

    /// <summary>
    /// FixedUpdate() is run at a fixed interval independant of framerate.
    /// </summary>
    private void FixedUpdate()
    {
        UpdateDrone();
    }

    //Recurring Methods (Update())-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the player's input from the keyboard and mouse / gamepad they're using.
    /// </summary>
    private void GetInput()
    {
        float moveHorizontal = player.GetAxis("Horizontal");
        float moveVertical = player.GetAxis("Vertical");

        movement = new Vector3(moveHorizontal, 0, -moveVertical);
        shooting = InputController.Instance.ButtonHeld("Shoot");
    }

    //Recurring Methods (FixedUpdate())--------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Updates the player based on their input.
    /// </summary>
    private void UpdateDrone()
    {
        Look();
        Move();
        CheckShooting();
    }

    /// <summary>
    /// Changes where the player is looking based on their input.
    /// </summary>
    private void Look()
    {
        /*
        //Player wants to move in a new direction? Update Slerp variables.
        if (movement != previousMovement)
        {
            slerpProgress = 0;
            oldRotation = drone.transform.rotation;
            //newRotation = Quaternion.LookRotation(movement);
            //newRotation = Quaternion.LookRotation(ReInput.controllers.Mouse.screenPosition);
            //newRotation = Quaternion.LookRotation(MousePositionOnTerrain.Instance.GetWorldPosition);
            //print("World Position from Player: " + MousePositionOnTerrain.Instance.GetWorldPosition);
            drone.transform.LookAt(MousePositionOnTerrain.Instance.GetWorldPosition);
        }

        //Still turning? Rotate towards direction player wants to move in, but smoothly.
        /*if (slerpProgress < 1 && movement != Vector3.zero)
        {
            rigidbody.velocity = movement;
        }
        else if (slerpProgress < 1)
        {
            slerpProgress = Mathf.Min(1, slerpProgress + rotationSpeed * Time.deltaTime);
            drone.transform.rotation = Quaternion.Slerp(oldRotation, newRotation, slerpProgress);
        }*/
        drone.transform.LookAt(MousePositionOnTerrain.Instance.GetWorldPosition);

    }

    /// <summary>
    /// Moves the player forward based on their input.
    /// </summary>
    private void Move()
    {
        if (movement != Vector3.zero)
        {
            drone.transform.Translate(movement * movementSpeed * Time.deltaTime, Space.World);
            cameraTarget.transform.position = drone.transform.position;
        }

        //Toggle gravity if something has pushed the player up above hoverHeight
        if (rigidbody.useGravity)
        {
            if (drone.position.y <= hoverHeight)
            {
                drone.position = new Vector3(drone.position.x, hoverHeight, drone.position.z);
                rigidbody.useGravity = false;
            }
        }
        else
        {
            if (drone.position.y > hoverHeight)   //TODO: account for terrain pushing the player up
            {
                rigidbody.useGravity = true;
            }
        }

        //Positioning this line here accounts for the player having been moved by an external force (e.g. pushed by enemies)
        cameraTarget.position = drone.position;     
    }

    /// <summary>
    /// Checks if the player wants to shoot based on their input, and fires laser bolts if they do.
    /// </summary>
    private void CheckShooting()
    {
        if (shooting && laserBattery.Count > 0 && Time.time - timeOfLastShot > shootCooldown)
        {
            timeOfLastShot = Time.time;
            LaserBolt laserBolt = laserBattery[0];
            laserBattery.Remove(laserBolt);
            laserBolt.transform.parent = null;
            laserBolt.transform.position = laserCannonTip.position;
            laserBolt.Shoot((transform.forward * 2 - transform.up).normalized);
        }
    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Handles the destruction of laser bolts.
    /// </summary>
    /// <param name="laserBolt">The laser bolt to destroy.</param>
    public void DestroyLaserBolt(LaserBolt laserBolt)
    {
        laserBolt.Active = false;
        laserBolt.Collider.enabled = false;
        laserBolt.Rigidbody.isKinematic = true;
        laserBolt.transform.position = laserBatteryPoint.position;
        laserBolt.transform.parent = laserBatteryPoint;
        laserBattery.Add(laserBolt);
    }
}
