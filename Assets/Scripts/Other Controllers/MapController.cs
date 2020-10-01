﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A controller class for tracking which parts of the map have buildings, can be spawned to by aliens, etc.
/// </summary>
public class MapController : SerializableSingleton<MapController>
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------  

    //Serialized Fields----------------------------------------------------------------------------                                                    

    [Header("Map Size")]
    [SerializeField] private int xMax;
    [SerializeField] private int zMax;
    [SerializeField] private Vector2 centre;

    [Header("No Alien Spawning Area")]
    [SerializeField] private Vector3 noAliensBottomLeft;
    [SerializeField] private Vector3 noAliensTopRight;

    [Header("Tutorial Alien Spawning Area")]
    [SerializeField] private Vector3 tutorialBottomLeft;
    [SerializeField] private Vector3 tutorialTopRight;

    [Header("Pathfinding")]
    [SerializeField] private Alien[] pathfinders;
    [SerializeField] private bool pauseLoop;
    [SerializeField] private float timeLimitPerFrame;

    [Header("Testing")]
    [SerializeField] private bool debugPathfinding;

    //Non-Serialized Fields------------------------------------------------------------------------                                                    

    private PositionData[,] positions;
    private LayerMask groundLayerMask;

    private List<Vector3> gameplayAlienSpawnPoints;
    private List<Vector3> tutorialAlienSpawnPoints;
    private List<Vector3> currentAlienSpawnPoints;
    private List<Vector3> majorityAlienSpawnPoints;
    private List<Vector3> minorityAlienSpawnPoints;

    private bool initialised;
    private bool finishedCalculatingPaths;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------  

    //Basic Public Properties----------------------------------------------------------------------    

    /// <summary>
    /// The list of positions that are spawnable in the current wave.
    /// </summary>
    public List<Vector3> CurrentAlienSpawnPoints { get => currentAlienSpawnPoints; }

    /// <summary>
    /// Has MapController finished calculating paths for all types of aliens for each position on the map?
    /// </summary>
    public bool FinishedCalculatingPaths { get => finishedCalculatingPaths; }

    /// <summary>
    /// Has MapController's Start() method run yet?
    /// </summary>
    public bool Initialised { get => initialised; }

    /// <summary>
    /// The list of positions that are spawnable in the current wave between the angles denoting 70% of spawnings.
    /// </summary>
    public List<Vector3> MajorityAlienSpawnPoints { get => majorityAlienSpawnPoints; }

    /// <summary>
    /// The list of positions that are spawnable in the current wave between the angles denoting 30% of spawnings.
    /// </summary>
    public List<Vector3> MinorityAlienSpawnPoints { get => minorityAlienSpawnPoints; }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        positions = new PositionData[xMax + 1, zMax + 1];
        gameplayAlienSpawnPoints = new List<Vector3>();
        tutorialAlienSpawnPoints = new List<Vector3>();
        majorityAlienSpawnPoints = new List<Vector3>();
        minorityAlienSpawnPoints = new List<Vector3>();
        groundLayerMask = LayerMask.GetMask("Ground");
        initialised = false;
        finishedCalculatingPaths = false;
    }

    /// <summary>
    /// Start() is run on the frame when a script is enabled just before any of the Update methods are called for the first time. 
    /// Start() runs after Awake().
    /// </summary>
    private void Start()
    {
        Debug.Log($"MapController.Start()");
        
        if (!initialised)
        {
            Initialise();
        }
    }

    /// <summary>
    /// The functionality of MapController's Start() method.
    /// </summary>
    public void Initialise()
    {
        Debug.Log($"MapController.Initialise()");
        float alienSpawnHeight = AlienFactory.Instance.AlienInstantiationHeight;
        int noAlienXMin = (int)Mathf.Round(noAliensBottomLeft.x);
        int noAlienXMax = (int)Mathf.Round(noAliensTopRight.x);
        int noAlienZMin = (int)Mathf.Round(noAliensBottomLeft.z);
        int noAlienZMax = (int)Mathf.Round(noAliensTopRight.z);
        int tuteAlienXMin = (int)Mathf.Round(tutorialBottomLeft.x);
        int tuteAlienXMax = (int)Mathf.Round(tutorialTopRight.x);
        int tuteAlienZMin = (int)Mathf.Round(tutorialBottomLeft.z);
        int tuteAlienZMax = (int)Mathf.Round(tutorialTopRight.z);

        for (int i = 0; i <= xMax; i++)
        {
            for (int j = 0; j <= zMax; j++)
            {
                positions[i, j] = new PositionData(
                    i, //X coordinate
                    j, //Z coordinate
                    MathUtility.Instance.Angle(centre, new Vector2(i, j)), //Angle from centre to position
                    !(i < tuteAlienXMin || i > tuteAlienXMax || j < tuteAlienZMin || j > tuteAlienZMax), //Is inside the tutorial spawn area, i.e. is not outside it?
                    !(i < noAlienXMin || i > noAlienXMax || j < noAlienZMin || j > noAlienZMax) //Is inside the alien exclusion area, i.e. is not outside it?
                );

                if (positions[i, j].IsBuildable)
                {
                    if (!positions[i, j].AliensBanned)
                    {
                        gameplayAlienSpawnPoints.Add(new Vector3(i, alienSpawnHeight, j));
                    }

                    if (positions[i, j].IsInTutorialAlienSpawnArea)
                    {
                        tutorialAlienSpawnPoints.Add(new Vector3(i, alienSpawnHeight, j));
                    }
                }
            }
        }
        
        initialised = true;
        StartCoroutine(CalculatePaths());
    }

    /// <summary>
    /// Calculates paths from every map position to the cryo egg.
    /// </summary>
    /// <returns></returns>
    private IEnumerator CalculatePaths()
    {
        System.Diagnostics.Stopwatch totalStopwatch = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch loopStopwatch = new System.Diagnostics.Stopwatch();
        totalStopwatch.Restart();
        loopStopwatch.Restart();

        //Debug.Log($"MapController.CalculatePaths(), starting");
        NavMeshPath calculatedPath = null;
        float alienSpawnHeight = AlienFactory.Instance.AlienInstantiationHeight;
        List<Alien> pathfinderInstances = new List<Alien>();
        Transform cryoEggColliderTransform = CryoEgg.Instance.ColliderTransform;

        foreach (Alien prefab in pathfinders)
        {
            Alien pathfinder = Instantiate(prefab.gameObject).GetComponent<Alien>();

            if (pathfinder != null)
            {
                pathfinderInstances.Add(pathfinder);
                Destroy(pathfinder.GetComponent<Actor>());
                Destroy(pathfinder.GetComponent<AlienFX>());
                Destroy(pathfinder.GetComponent<Animator>());
                Destroy(pathfinder.GetComponent<Collider>());
                Destroy(pathfinder.GetComponent<Health>());
                Destroy(pathfinder.GetComponent<Rigidbody>());

                Transform[] transforms = pathfinder.GetComponentsInChildren<Transform>();

                for (int i = 0; i < transforms.Length; i++)
                {
                    try
                    {
                        if (transforms[i] != null && transforms[i] != pathfinder.transform)
                        {
                            Destroy(transforms[i].gameObject);
                        }
                    }
                    catch
                    {

                    }
                }

                pathfinder.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError($"MapController.CalculatePaths(), can't instantiate alien from prefab {pathfinder}");
            }
        }

        if (pauseLoop && loopStopwatch.ElapsedMilliseconds >= timeLimitPerFrame)
        {
            yield return null;
            loopStopwatch.Restart();
        }

        for (int i = 0; i < pathfinderInstances.Count; i++)
        {
            Alien alien = pathfinderInstances[i];
            NavMeshAgent agent = alien.NavMeshAgent;
            alien.gameObject.SetActive(true);

            foreach (PositionData p in positions)
            { 
                Vector3 pos = new Vector3(p.X, alienSpawnHeight, p.Z);
                RaycastHit hit;

                if (Physics.Raycast(pos, Vector3.down, out hit, 20, groundLayerMask))
                {
                    alien.transform.position = new Vector3(pos.x, hit.point.y, pos.z);
                    NavMeshHit navHit;

                    if (NavMesh.SamplePosition(agent.transform.position, out navHit, 1, NavMesh.AllAreas))
                    {
                        agent.enabled = true;
                        calculatedPath = new NavMeshPath();

                        if (agent.CalculatePath(cryoEggColliderTransform.position, calculatedPath))
                        {
                            p.Paths[alien.Type] = calculatedPath;
                        }

                        agent.enabled = false;
                    }
                    else
                    {
                        RegisterOffMeshPosition(pos);
                    }
                }

                if (pauseLoop && loopStopwatch.ElapsedMilliseconds >= timeLimitPerFrame)
                {
                    if (debugPathfinding) Debug.Log($"MapController.CalculatePaths(), pause loop, alien: {alien.Type}, x: {p.X}/{xMax}, z: {p.Z}/{zMax}, milliseconds elapsed: {loopStopwatch.ElapsedMilliseconds}/{timeLimitPerFrame}");
                    yield return null;
                    loopStopwatch.Restart();
                }
            }

            Destroy(alien.gameObject);
        }

        //Debug.Log($"MapController.CalculatePaths(), has finished, time elapsed is {totalStopwatch.ElapsedMilliseconds} ms, or {totalStopwatch.ElapsedMilliseconds / 1000} s.");
        finishedCalculatingPaths = true;
        yield return null;
    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    //Availabile Position Methods------------------------------------------------------------------

    /// <summary>
    /// Resets the list of currently available alien spawn positions.
    /// </summary>
    public void ResetCurrentAlienSpawnPoints()
    {
        currentAlienSpawnPoints = new List<Vector3>(StageManager.Instance.CurrentStage.GetID() == EStage.Combat ? tutorialAlienSpawnPoints : gameplayAlienSpawnPoints);
        majorityAlienSpawnPoints.Clear();
        minorityAlienSpawnPoints.Clear();
    }

    /// <summary>
    /// Checks if a given building can legally be placed given its size and position and the spaces available.
    /// </summary>
    /// <param name="building">The building whose placement is being checked.</param>
    /// <returns>Whether the building can legally be placed.</returns>
    public bool PositionAvailableForBuilding(Building building)
    {
        Vector3 buildingPos = building.transform.position;
        //string positions = "";

        //foreach (Vector3 offset in building.BuildingFoundationOffsets)
        //{
        //    positions += $"{offset}, ";
        //}

        //Debug.Log($"MapController checking positions [{positions}] available for building");

        foreach (Vector3 offset in building.BuildingFoundationOffsets)
        {
            if (!PositionAvailableForSpawning(buildingPos + offset, false))
            {
                //Debug.Log($"MapController.PositionAvailableForBuilding returned false for position {buildingPos + offset}");
                return false;
            }
        }

        //Debug.Log("MapController.PositionAvailableForBuilding returned true");
        return true;
    }

    /// <summary>
    /// Checks if something can legally be spawned at a given position given the spaces available.
    /// </summary>
    /// <param name="position">The position the something would be spawned at.</param>
    /// <param name="alien">Whether or not the something is an alien.</param>
    /// <returns>Whether something can legally be spawned.</returns>
    public bool PositionAvailableForSpawning(Vector3 position, bool alien)
    {
        //Debug.Log($"Verifying for spawnable at {position}");
        position.x = Mathf.Round(position.x);
        position.z = Mathf.Round(position.z);

        if (IsPositionOutOfBounds(position))
        {
            return false;
        }

        PositionData positionData = positions[(int)position.x, (int)position.z];

        //Check if already building occupiued
        if (!positionData.IsBuildable)
        {
            //Debug.Log($"Can't spawn at {position}, which is already occupied by a building.");
            return false;
        }

        if (alien)
        {
            //Check if in tutorial spawn area during combat stage of tutorial, or in alien exclusion area otherwise
            if (StageManager.Instance.CurrentStage.GetID() == EStage.Combat ? !positionData.IsInTutorialAlienSpawnArea : positionData.AliensBanned)
            {
                //Debug.Log($"Can't spawn an alien at {position}, which is within the alien exclusion area during gameplay, or outside the tutorial spawn area during the combat stage of the tutorial.");
                return false;       
            }

            RaycastHit hit;
            float instantiationHeight = AlienFactory.Instance.AlienInstantiationHeight;
            float minSpawnHeight = AlienFactory.Instance.MinAlienSpawnHeight;

            //Check if a cliff or pit or too close to either
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector3 testPos = new Vector3(position.x + i, instantiationHeight, position.z + j);
                    //Debug.Log($"TestPos {testPos}");  

                    if (testPos.x < 0 || testPos.x > xMax || testPos.z < 0 || testPos.z > zMax)
                    {
                        //Debug.Log($"Out of bounds");
                        return false;
                    }
                    else if (!Physics.Raycast(testPos, Vector3.down, out hit, 25, groundLayerMask))
                    {
                        //Debug.Log($"Failed to hit on raycast");
                        return false;
                    }
                    else
                    {
                        float hitHeight = hit.point.y;
                        //Debug.Log($"Test modifier ({i}, {j}), adjusted position {position}, raycast down hit at height {hitHeight}, error margin {errorMargin}");

                        if (hitHeight < minSpawnHeight)
                        {
                            //Debug.Log($"Point.y < {minSpawnHeight}, therefore unacceptable pit or off map, therefore not alien spawnable. Marking position as Banned for aliens.");

                            for (int k = -1; k <= 1; k++)
                            {
                                for (int l = -1; l <= 1; l++)
                                {
                                    int m = (int)(position.x + i + k);
                                    int n = (int)(position.z + j + l);

                                    if (m >= 0 && m <= xMax && n >= 0 && n <= zMax)
                                    {
                                        positions[m, n].AliensBanned = true;
                                    }
                                }
                            }
                            
                            return false;
                        }
                    }
                }
            }            
        }

        return true;
    }

    //Entity Registration Methods------------------------------------------------------------------

    /// <summary>
    /// Registers a building with MapController so that it knows that the spaces it occupies are occupied.
    /// </summary>
    /// <param name="building">The building to be registered.</param>
    public void RegisterBuilding(Building building)
    {
        foreach (Vector3 offset in building.BuildingFoundationOffsets)
        {
            UpdatePositionAvailability(building.gameObject, building.transform.position + offset, true, null);
        }
    }
    
    /// <summary>
    /// Registers a mineral with MapController so that it knows that the space it occupies is occupied.
    /// </summary>
    /// <param name="mineral">The mineral to be registered.</param>
    public void RegisterMineral(Mineral mineral)
    {
        UpdatePositionAvailability(mineral.gameObject, mineral.transform.position, null, true);
    }
    
    /// <summary>
    /// Deregisters a building with MapController so that it knows that the spaces it occupied are unoccupied.
    /// </summary>
    /// <param name="building">The building to be deregistered.</param>
    public void DeRegisterBuilding(Building building)
    {
        foreach (Vector3 offset in building.BuildingFoundationOffsets)
        {
            UpdatePositionAvailability(building.gameObject, building.transform.position + offset, false, null);
        }
    }
    
    /// <summary>
    /// Deregisters a mineral with MapController so that it knows that the space it occupied is unoccupied.
    /// </summary>
    /// <param name="mineral">The mineral to be deregistered.</param>
    public void DeRegisterMineral(Mineral mineral)
    {
        UpdatePositionAvailability(mineral.gameObject, mineral.transform.position, null, false);
    }

    /// <summary>
    /// Informs MapController that the passed position is not on the NavMesh and shouldn't allow alien spawning.
    /// </summary>
    /// <param name="position">The position that is not on the NavMesh</param>
    public void RegisterOffMeshPosition(Vector3 position)
    {
        if (!IsPositionOutOfBounds(position))
        {
            int x = Mathf.RoundToInt(position.x);
            int z = Mathf.RoundToInt(position.z);
            positions[x, z].AliensBanned = true;

            Vector3 pos = new Vector3(x, AlienFactory.Instance.AlienInstantiationHeight, z);
            tutorialAlienSpawnPoints.Remove(pos);
            gameplayAlienSpawnPoints.Remove(pos);
            currentAlienSpawnPoints.Remove(pos);
            majorityAlienSpawnPoints?.Remove(pos);
            minorityAlienSpawnPoints?.Remove(pos);
        }

        //Debug.LogError($"{gameObject.name} can't update the availability of position {position}, which is outside the bounds of (0,0) to ({xMax},{zMax})");
    }

    /// <summary>
    /// Updates the availability of the space(s) occupied / to be occupied by a building or mineral.
    /// </summary>
    /// <param name="gameObject">The game object whose space(s) are having their availability updated.</param>
    /// <param name="position">The position having its availability updated.</param>
    /// <param name="hasBuilding">Is this space now occupied by a building, or is now unoccupied, or null if no change?</param>
    /// <param name="hasMineral">Is this space now occupied by a mineral, or is now unoccupied, or null if no change?</param>
    private void UpdatePositionAvailability(GameObject gameObject, Vector3 position, bool? hasBuilding, bool? hasMineral)
    {
        if (!IsPositionOutOfBounds(position))
        {
            int x = (int)Mathf.Round(position.x);
            int z = (int)Mathf.Round(position.z);
            Vector3 pos = new Vector3(x, AlienFactory.Instance.AlienInstantiationHeight, z);

            //Debug.Log($"MapController.UpdateAvailablePositions() offset loop for {gameObject} at position {position}, x: {x}/{xMax}, z: {z}/{zMax}, hasBuilding: {hasBuilding}, hasMineral: {hasMineral}");
            if (hasBuilding != null)
            {
                positions[x, z].HasBuilding = hasBuilding.Value;
            }

            if (hasMineral != null)
            {
                positions[x, z].HasMineral = hasMineral.Value;
            }

            if (StageManager.Instance.CurrentStage.GetID() == EStage.Combat)
            {
                if (positions[x, z].IsAlienSpawnableDuringCombatTutorial)
                {
                    tutorialAlienSpawnPoints.Add(pos);
                }
                else
                {
                    tutorialAlienSpawnPoints.Remove(pos);
                    currentAlienSpawnPoints?.Remove(pos);
                    majorityAlienSpawnPoints?.Remove(pos);
                    minorityAlienSpawnPoints?.Remove(pos);
                }
            }
            else
            {
                if (positions[x, z].IsAlienSpawnableDuringGameplay)
                {
                    gameplayAlienSpawnPoints.Add(pos);
                }
                else
                {
                    gameplayAlienSpawnPoints.Remove(pos);
                    currentAlienSpawnPoints?.Remove(pos);
                    majorityAlienSpawnPoints?.Remove(pos);
                    minorityAlienSpawnPoints?.Remove(pos);
                }
            }
        }
        else
        {
            //Debug.LogError($"{gameObject.name} can't update the availability of position {position}, which is outside the bounds of (0,0) to ({xMax},{zMax})");
        }
    }

    //Other Methods--------------------------------------------------------------------------------

    /// <summary>
    /// Gets the PositionData allocated to the passed position.
    /// </summary>
    /// <param name="position">The position whose data you want to retrieve.</param>
    /// <returns>The PositionData associated with the passed position.</returns>
    public PositionData GetPositionData(Vector3 position)
    {
        if (IsPositionOutOfBounds(position))
        {
            Debug.LogError($"MapController.GetPositionData(), {position} is out of bounds.");
            return null;
        }
        else
        {
            return positions[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)];
        }
    }

    /// <summary>
    /// Checks if a position is outside the bounds of the map on the x and z axes.
    /// </summary>
    /// <param name="position">The position being checked.</param>
    /// <returns>Is the position outside the bounds of the map?</returns>
    private bool IsPositionOutOfBounds(Vector3 position)
    {
        return position.x < 0 || position.x > xMax || position.z < 0 || position.z > zMax;
    }
}
