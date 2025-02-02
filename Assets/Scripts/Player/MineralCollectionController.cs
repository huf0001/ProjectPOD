﻿using Rewired;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A player script for collecting minerals.
/// </summary>
public class MineralCollectionController : SerializableSingleton<MineralCollectionController>
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------  

    //Serialized Fields----------------------------------------------------------------------------                                                    

    [SerializeField] private Camera camera;
	[SerializeField] private MiningBeam miningBeam;

    //Non-Serialized Fields------------------------------------------------------------------------                                                    

    private Player playerInputManager;
    private bool collectMinerals;
	private bool isOnMineral;
    private LayerMask mineralsLayerMask;
    private bool mining;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Basic Public Properties----------------------------------------------------------------------

    /// <summary>
    /// Is the player currently mining a mineral?
    /// </summary>
    public bool Mining { get => mining; }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        mineralsLayerMask = LayerMask.GetMask("Minerals");
        mining = false;
    }

    /// <summary>
    /// Start() is run on the frame when a script is enabled just before any of the Update methods are called for the first time. 
    /// Start() runs after Awake().
    /// </summary>
    private void Start()
    {
        playerInputManager = ReInput.players.GetPlayer(GetComponent<PlayerID>().Value);
    }

    //Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Update() is run every frame.
    /// </summary>
    private void Update()
    {
        GetInput();
        CollectMinerals();
    }

    //Recurring Methods (Update())------------------------------------------------------------------------------------------------------------------  

    /// <summary>
    /// Gets the player's input from the keyboard and mouse / gamepad they're using.
    /// </summary>
    private void GetInput()
    {
        collectMinerals = playerInputManager.GetButton("Mine");
    }

    /// <summary>
    /// Checks if the player wants to collect minerals, and collects those minerals if they do.
    /// </summary>
    private void CollectMinerals()
    {
        if (!BuildingSpawnController.Instance.SpawningBuilding)
        {
            //Debug.Log("Mining");
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, mineralsLayerMask))
            {
				//Debug.Log("Raycast hit minerals");
				Mineral mineral = hit.collider.GetComponentInParent<Mineral>();
				DisplayMineralInfo(mineral);

				if (collectMinerals && mineral != null && mineral.OreCount > 0)
                {
                    mining = true;
                    miningBeam.OnMineEnable(mineral.MiningPoint);
                    mineral.Mine();
                    //Debug.Log($"Raycast hit mineral node. Mined {mined} minerals");

                    AudioManager.Instance.PlaySound(AudioManager.ESound.Mining, this.gameObject);
                    //ResourceController.Instance.Ore += mined; (Moved this function to Ore.cs)
                }
				else
				{
                    mining = false;
					miningBeam.OnMineDisable();
				}
			}
			else
			{
				miningBeam.OnMineDisable();
				HideMineralInfo();
                AudioManager.Instance.StopSound(AudioManager.ESound.Mining, this.gameObject);
            }
        }
        else
        {
            AudioManager.Instance.StopSound(AudioManager.ESound.Mining, this.gameObject);
        }
	}

	/// <summary>
	/// Trigger hovering dialogue box if mouse hovers over the mineral deposit.
	/// </summary>
	private void DisplayMineralInfo(Mineral mineral)
	{
		if (!isOnMineral)
		{
			HoveringDialogueManager.Instance.ShowDialogue(mineral.GetComponent<HoverDialogueBoxPreset>());
			isOnMineral = true;
		}
	}
	
	/// <summary>
	/// Hide hovering dialogue box if mouse leaves the mineral deposit.
	/// </summary>
	private void HideMineralInfo()
	{
		if (isOnMineral)
		{
			HoveringDialogueManager.Instance.HideDialogue();
			isOnMineral = false;
		}
	}
}
