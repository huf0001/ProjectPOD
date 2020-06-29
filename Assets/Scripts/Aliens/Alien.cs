﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Demo class for enemies.
/// </summary>
public class Alien : MonoBehaviour, IMessenger
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------

    //Serialized Fields----------------------------------------------------------------------------

    [Header("Components")]
    [SerializeField] private List<Collider> bodyColliders;

    [Header("Stats")] 
    [SerializeField] private int id;
    [SerializeField] private float speed;
    [SerializeField] private float turningSpeed;
    [SerializeField] private float hoverHeight;
    [SerializeField] private float attackRange;
    [SerializeField] private float damage;
    [SerializeField] private float attackCooldown;

    //Non-Serialized Fields------------------------------------------------------------------------
    [Header("Testing")]
    //Componenets
    private List<Collider> colliders;
    private Health health;
    private NavMeshAgent navMeshAgent;
    private Rigidbody rigidbody;

    //Movement
    private bool moving;
    [SerializeField] private float zRotation;

    //Turning
    private Quaternion oldRotation;
    private Quaternion targetRotation;
    private float slerpProgress;
    
    //Targeting
    //private CryoEgg CryoEgg;
    private List<Transform> visibleAliens;
    private List<Transform> visibleTargets;
    [SerializeField] private Transform target;
    [SerializeField] private Health targetHealth;
    [SerializeField] private Size targetSize;
    [SerializeField] private string shotByName;
    [SerializeField] private Transform shotByTransform;
    private float timeOfLastAttack;

    //[SerializeField] private bool conductSweepTesting;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Basic Public Properties----------------------------------------------------------------------

    /// <summary>
    /// The colliders that comprise the alien's body.
    /// </summary>
    public List<Collider> BodyColliders { get => bodyColliders; }

    /// <summary>
    /// Alien's Health component.
    /// </summary>
    public Health Health { get => health; }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    void Awake()
    {
        colliders = new List<Collider>(GetComponents<Collider>());
        health = GetComponent<Health>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        rigidbody = GetComponent<Rigidbody>();
        zRotation = transform.rotation.eulerAngles.z;

        visibleAliens = new List<Transform>();
        visibleTargets = new List<Transform>();
        moving = false;
        navMeshAgent.enabled = false;
    }

    /// <summary>
    /// Prepares the Alien to chase its targets when AlienFactory puts it in the world. 
    /// </summary>
    public void Setup(int id)
    {
        this.id = id;
        gameObject.name = $"Alien {id}";
        health.Reset();

        target = CryoEgg.Instance.ColliderTransform;
        targetHealth = CryoEgg.Instance.Health;
        timeOfLastAttack = attackCooldown * -1;
        moving = true;
        MessageDispatcher.Instance.Subscribe("Alien", this);

        //Rotate to face the Cryo egg
        Vector3 targetRotation = CryoEgg.Instance.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(targetRotation);

        foreach (Collider c in colliders)
        {
            c.enabled = true;
        }

        navMeshAgent.enabled = true;
    }

    //Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// FixedUpdate() is run at a fixed interval independant of framerate.
    /// </summary>
    private void FixedUpdate()
    {
        if (moving)
        {
            CheckHealth();
            SelectTarget();
            Look();
            Move();
        }
    }

    //Recurring Methods (FixedUpdate())-------------------------------------------------------------------------------------------------------------  

    /// <summary>
    /// Checks if alien has 0 health, destroying it if it has.
    /// </summary>
    private void CheckHealth()
    {
        if (health.IsDead())
        {
            AlienFactory.Instance.DestroyAlien(this);
        }
    }

    /// <summary>
    /// Selects the most appropriate target for the alien.
    /// </summary>
    private void SelectTarget()
    {
        switch (visibleTargets.Count)
        {
            case 0:
                //Target Cryo egg
                if (target != CryoEgg.Instance.transform)
                {
                    SetTarget(CryoEgg.Instance.transform);
                }

                break;
            case 1:
                //Get only visible target
                if (target != visibleTargets[0])
                {
                    SetTarget(visibleTargets[0]);
                }

                break;
            default:
                //Prioritise shooter
                if (shotByTransform != null && visibleTargets.Contains(shotByTransform))
                {
                    SetTarget(shotByTransform);
                }
                else
                {
                    //Get closest visible target
                    float distance = 99999999999;
                    float closestDistance = 9999999999999999;
                    Transform closestTarget = null;

                    foreach (Transform t in visibleTargets)
                    {
                        distance = Vector3.Distance(transform.position, t.position);

                        if (closestTarget == null || distance < closestDistance)
                        {
                            closestTarget = t;
                            closestDistance = distance;
                        }
                    }

                    if (target != closestTarget)
                    {
                        SetTarget(closestTarget);
                    }
                }

                break;
        }
    }

    /// <summary>
    /// Sets Alien's target transform, targetHealth and targetSize variables based on the selected target and its components.
    /// </summary>
    /// <param name="selectedTarget">The transform of the selected target.</param>
    private void SetTarget(Transform selectedTarget)
    {
        target = selectedTarget;
        targetHealth = target.GetComponentInParent<Health>();   //Gets Health from target or any of its parents that has it.
        targetSize = target.GetComponentInParent<Size>();   //Gets Radius from target or any of its parents that has it.
    }

    /// <summary>
    /// Alien uses input information to determine which direction it should be facing
    /// </summary>
    private void Look()
    {
        //TODO: swarm-based looking behaviour
        if (navMeshAgent.enabled && target.position != navMeshAgent.destination)
        {
            //NavMeshPath path = null;
            navMeshAgent.destination = target.position;
            //navMeshAgent.CalculatePath(target.position, path);
        }

        //Vector3 newRotation = PositionAtSameHeight(target.position) - transform.position;

        //if (newRotation != targetRotation.eulerAngles)
        //{
        //    oldRotation = transform.rotation;
        //    targetRotation = Quaternion.LookRotation(newRotation);
        //    slerpProgress = 0f;
        //}

        //if (slerpProgress < 1)
        //{
        //    slerpProgress = Mathf.Min(1, slerpProgress + turningSpeed * Time.fixedDeltaTime);
        //    transform.rotation = Quaternion.Slerp(oldRotation, targetRotation, slerpProgress);
        //}
    }

    /// <summary>
    /// Gets the position of a target as if it were at the same height as the alien. 
    /// </summary>
    /// <param name="targetPos">The target's position.</param>
    /// <returns>The target's position if it was at the same height as the alien.</returns>
    private Vector3 PositionAtSameHeight(Vector3 targetPos)
    {
        return new Vector3(targetPos.x, transform.position.y, targetPos.z);
    }

    /// <summary>
    /// Moves alien.
    /// </summary>
    private void Move()
    {
        if (Vector3.Distance(transform.position, PositionAtSameHeight(target.position)) > attackRange + targetSize.Radius)
        {
            //TODO: if navMeshAgent.speed != speed, navMeshAgent.speed = speed

            //RaycastHit hit;
            //float movement = speed * Time.fixedDeltaTime;
            //transform.Translate(new Vector3(0, 0, movement));     //Commented out to test nav mesh agent movement

            //if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 25, LayerMask.GetMask("Ground")))
            //{
            //    float height = (transform.position - hit.point).y;

            //    //Toggle gravity if something has pushed the alien up above hoverHeight
            //    if (rigidbody.useGravity)
            //    {
            //        if (height <= hoverHeight)
            //        {
            //            transform.position = new Vector3(transform.position.x, hoverHeight, transform.position.z);
            //            rigidbody.useGravity = false;
            //            rigidbody.drag = 100;
            //            rigidbody.mass = 0;
            //        }
            //    }
            //    else
            //    {
            //        if (height > hoverHeight)
            //        {
            //            rigidbody.useGravity = true;
            //            rigidbody.drag = 0;
            //            rigidbody.velocity = Vector3.zero;
            //            rigidbody.mass = 1000;
            //        }
            //    }
            //}
        }
        else
        {
            //TODO: if navMeshAgent.speed != 0, navMeshAgent.speed = 0

            if (Time.time - timeOfLastAttack > attackCooldown)
            {
                timeOfLastAttack = Time.time;
                targetHealth.Value -= damage;
                //TODO: trigger attack animation
            }
        }
    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Allows message-sending classes to deliver a message to this alien.
    /// </summary>
    /// <param name="message">The message to send to this messenger.</param>
    public void Receive(Message message)
    {
        if (message.SenderTag == "Turret" && message.MessageContents == "Dead")
        {
            Transform messenger = message.SenderObject.transform;

            if (shotByTransform == messenger)
            {
                shotByName = "";
                shotByTransform = null;
            }

            if (visibleTargets.Contains(messenger))
            {
                visibleTargets.Remove(messenger);
            }
        }
    }

    /// <summary>
    /// Registers with an alien the name and transform of an entity that shot it.
    /// </summary>
    /// <param name="name">The name of the entity that shot the alien.</param>
    /// <param name="transform">The transform of the entity that shot the alien.</param>
    public void ShotBy(string name, Transform transform)
    {
        shotByName = name;
        shotByTransform = transform;
    }

    /// <summary>
    /// Resets the alien to its inactive state.
    /// </summary>
    public void Reset()
    {
        navMeshAgent.enabled = false;
        MessageDispatcher.Instance.SendMessage("Turret", new Message(gameObject.name, "Alien", this.gameObject, "Dead"));
        MessageDispatcher.Instance.Unsubscribe("Alien", this);
        moving = false;
        shotByName = "";
        shotByTransform = null;
        visibleTargets.Clear();
        visibleAliens.Clear();
        target = null;

        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (moving)
    //    {
    //        Debug.Log($"{this} collided with {collision.gameObject}, tag is {collision.gameObject.tag}, force applied is {collision.impulse}");
    //    }
    //}

    /// <summary>
    /// When a GameObject collides with another GameObject, Unity calls OnTriggerEnter.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (/*bodyCollider.enabled &&*/ !other.isTrigger)
        {
            if (other.CompareTag("Alien"))
            {
                visibleAliens.Add(other.transform);
            }
            else if (other.CompareTag("Building"))
            {
                visibleTargets.Add(other.transform.parent);
            }
            else if (other.CompareTag("Player"))
            {
                visibleTargets.Add(other.transform);
            }
            else if (other.CompareTag("Projectile"))
            {
                Debug.Log("Alien.OnTriggerEnter; Alien hit by a projectile");
                Projectile projectile = other.GetComponent<Projectile>();
                shotByTransform = projectile.Owner.GetComponentInChildren<Collider>().transform;
            }
        }
    }

    /// <summary>
    /// OnTriggerExit is called when the Collider other has stopped touching the trigger.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    private void OnTriggerExit(Collider other)
    {
        if (/*bodyCollider.enabled && */!other.isTrigger)
        {
            if (visibleAliens.Contains(other.transform))
            {
                visibleAliens.Remove(other.transform);
            }
            else if (visibleTargets.Contains(other.transform))
            {
                visibleTargets.Remove(other.transform);
            }
        }
    }
}
