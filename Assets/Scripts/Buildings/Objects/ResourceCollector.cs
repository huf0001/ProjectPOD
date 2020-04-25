﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resource collectors to collect resources.
/// </summary>
public class ResourceCollector : MonoBehaviour
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------  

    //Serialized Fields----------------------------------------------------------------------------                                                    

    [SerializeField] private EResource resource;
    [SerializeField] private int collectionRate;

    //Non-Serialized Fields------------------------------------------------------------------------                                                    

    private Building building;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Basic Public Properties----------------------------------------------------------------------                                                                                                                          

    /// <summary>
    /// The building this resource collector class is a component of.
    /// </summary>
    public Building Building { get => building; }

    //Complex Public Properties--------------------------------------------------------------------                                                    



    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    private void Awake()
    {
        building = gameObject.GetComponent<Building>();
    }

    /// <summary>
    /// Start() is run on the frame when a script is enabled just before any of the Update methods are called for the first time. 
    /// Start() runs after Awake().
    /// </summary>
    //private void Start()
    //{

    //}

    //Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Update() is run every frame.
    /// </summary>
    //private void Update()
    //{

    //}

    /// <summary>
    /// FixedUpdate() is run at a fixed interval independant of framerate.
    /// </summary>
    //private void FixedUpdate()
    //{

    //}

    //Recurring Methods (Update())------------------------------------------------------------------------------------------------------------------  



    //Recurring Methods (FixedUpdate())--------------------------------------------------------------------------------------------------------------



    //Recurring Methods (Other)----------------------------------------------------------------------------------------------------------------------



    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Update the resource contribution of the building this resource collector class is a component of.
    /// </summary>
    public void Activate()
    {
        switch(resource)
        {
            case EResource.Power:
                ResourceController.Instance.PowerSupply += collectionRate;
                break;
            case EResource.Water:
                ResourceController.Instance.WaterSupply += collectionRate;
                break;
        }
    }

    /// <summary>
    /// Update the resource contribution of the building this resource collector class is a component of.
    /// </summary>
    public void Deactivate()
    {
        switch (resource)
        {
            case EResource.Power:
                ResourceController.Instance.PowerSupply -= collectionRate;
                break;
            case EResource.Water:
                ResourceController.Instance.WaterSupply -= collectionRate;
                break;
        }
    }

    //Utility Methods--------------------------------------------------------------------------------------------------------------------------------  


}