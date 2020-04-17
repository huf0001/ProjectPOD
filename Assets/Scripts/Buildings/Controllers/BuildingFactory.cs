﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A factory class for buildings.
/// </summary>
public class BuildingFactory : MonoBehaviour
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------  

    //Serialized Fields----------------------------------------------------------------------------                                                    

    //[Header("Cryo Egg Prefab")]
    //[SerializeField] private Building cryoEggPrefab;

    [Header("Resource Building Prefabs")]
    [SerializeField] private Building solarPanelPrefab;
    [SerializeField] private Building windTurbinePrefab;
    [SerializeField] private Building waterDrillPrefab;

    [Header("Terraforming Building Prefabs")]
    [SerializeField] private Building gasDiffuserPrefab;
    [SerializeField] private Building humidifierPrefab;
    [SerializeField] private Building greenhousePrefab;

    [Header("Defence Building Prefabs")]
    [SerializeField] private Building turretPrefab;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Singleton Public Property--------------------------------------------------------------------                                                    

    /// <summary>
    /// BuildingFactory's singleton public property.
    /// </summary>
    public static BuildingFactory Instance { get; protected set; }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There should never be more than one BuildingFactory.");
        }

        Instance = this;
    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Get buildings of a specified type from BuildingFactory.
    /// </summary>
    /// <param name="buildingType">The type of building you want BuildingFactory to get for you.</param>
    /// <returns></returns>
    public Building GetBuilding(EBuilding buildingType)
    {
        Building building;
        Vector3 position = Vector3.zero;
        Quaternion rotation = new Quaternion();

        switch(buildingType)
        {
            //TODO: remove redundant instantiation details, test if it still works.
            //case EBuilding.CryoEgg:
            //    building = Instantiate(cryoEggPrefab);
            //    break;
            case EBuilding.SolarPanel:
                building = Instantiate<Building>(solarPanelPrefab, position, rotation) ;
                break;
            case EBuilding.WindTurbine:
                building = Instantiate<Building>(windTurbinePrefab, position, rotation);
                break;
            case EBuilding.WaterDrill:
                building = Instantiate<Building>(waterDrillPrefab, position, rotation);
                break;
            case EBuilding.GasDiffuser:
                building = Instantiate<Building>(gasDiffuserPrefab, position, rotation);
                break;
            case EBuilding.Humidifier:
                building = Instantiate<Building>(humidifierPrefab, position, rotation);
                break;
            case EBuilding.Greenhouse:
                building = Instantiate<Building>(greenhousePrefab, position, rotation);
                break;
            case EBuilding.Turret:
                building = Instantiate<Building>(turretPrefab, position, rotation);
                break;
            default:
                Debug.LogError("Invalid EBuildingType value passed to BuildingFactory.GetBuilding().");
                return null;
        }

        building.Id = IdGenerator.Instance.GetNextId();
        BuildingController.Instance.RegisterBuilding(building);

        if (building.Terraformer != null)
        {
            EnvironmentalController.Instance.RegisterBuilding(building.Terraformer);
        }

        return building;
    }

    /// <summary>
    /// Destroy a building.
    /// </summary>
    /// <param name="building">The building to be destroyed.</param>
    /// <param name="consumingResources">Is the building consuming resources and does that consumption need to be cancelled now that it's being destroyed?</param>
    public void DestroyBuilding(Building building, bool consumingResources)
    {
        building.Operational = false;
        BuildingController.Instance.DeRegisterBuilding(building);

        if (building.Terraformer != null)
        {
            EnvironmentalController.Instance.RemoveBuilding(building.Id);
        }

        if (consumingResources)
        {
            ResourceController.Instance.PowerSupply += building.PowerConsumption;
            ResourceController.Instance.WaterSupply += building.WaterConsumption;
        }

        building.Health.Die();
    }
}
