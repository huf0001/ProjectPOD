﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A factory class for ammunition.
/// </summary>
public class ProjectileFactory : MonoBehaviour
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------  

    //Serialized Fields----------------------------------------------------------------------------                                                    

    [SerializeField] private List<Projectile> projectilePrefabs;
    [SerializeField] private int pooledProjectiles;

    //Non-Serialized Fields------------------------------------------------------------------------                                                    

    private Transform objectPool;
    private Dictionary<EProjectileType, Projectile> prefabs;
    private Dictionary<EProjectileType, List<Projectile>> projectiles;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Singleton Public Property--------------------------------------------------------------------                                                    

    /// <summary>
    /// AmmoFactory's singleton public property.
    /// </summary>
    public static ProjectileFactory Instance { get; protected set; }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There should never be more than one ProjectileFactory.");
        }

        Instance = this;
        prefabs = new Dictionary<EProjectileType, Projectile>();
        projectiles = new Dictionary<EProjectileType, List<Projectile>>();               
    }

    /// <summary>
    /// Start() is run on the frame when a script is enabled just before any of the Update methods are called for the first time. 
    /// Start() runs after Awake().
    /// </summary>
    private void Start()
    {
        objectPool = ObjectPool.Instance.transform;

        foreach (Projectile p in projectilePrefabs)
        {
            prefabs[p.Type] = p;
            projectiles[p.Type] = new List<Projectile>();

            for (int i = 0; i < pooledProjectiles; i++)
            {
                Projectile q = CreateProjectile(p.Type);
                q.transform.SetPositionAndRotation(objectPool.position, q.transform.rotation);
                q.transform.parent = objectPool;
                projectiles[q.Type].Add(q);
            }
        } 

    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Retrieves Projectiles from a pool if there's any available, and instantiates a new Projectile if there isn't one.
    /// </summary>
    /// <param name="owner">The player or turret firing the projectile from their weapon.</param>
    /// <param name="position">The position the Projectile should be instantiated at.</param>
    /// <returns>A new projectile.</returns>
    public Projectile GetProjectile(EProjectileType type, Transform owner, Transform barrelTip)
    {
        Projectile projectile;

        if (projectiles[type].Count > 0)
        {
            projectile = projectiles[type][0];
            projectiles[type].RemoveAt(0);
        }
        else
        {
            projectile = CreateProjectile(type);
        }

        projectile.Owner = owner;
        projectile.transform.parent = null;
        projectile.transform.position = barrelTip.position;
		projectile.transform.rotation = barrelTip.rotation;
		return projectile;
    }

    /// <summary>
    /// Instantiates a new Projectile.
    /// </summary>
    /// <returns>A new Projectile.</returns>
    private Projectile CreateProjectile(EProjectileType type)
    {
        Projectile projectile = Instantiate(prefabs[type]);
        projectile.Collider.enabled = false;
        return projectile;
    }

    /// <summary>
    /// Handles the destruction of projectiles.
    /// </summary>
    /// <param name="projectile">The projectile to destroy.</param>
    public void DestroyProjectile(Projectile projectile)
    {
        ProjectileManager.Instance.DeRegisterProjectile(projectile);
        projectile.Active = false;
        projectile.Collider.enabled = false;
        projectile.Rigidbody.velocity = Vector3.zero;
        projectile.Rigidbody.isKinematic = true;
        projectile.transform.position = objectPool.position;
        projectile.transform.parent = objectPool;
        projectiles[projectile.Type].Add(projectile);
    }
}
