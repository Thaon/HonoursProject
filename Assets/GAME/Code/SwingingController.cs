using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public enum SwingingType { swing, half, full }

public class SwingingController : MonoBehaviour
{

    #region swinging member variables

    public SwingingType m_type = SwingingType.swing;
    public Rigidbody m_playerBody;
    public bool m_canPull = true;

    private VRTK_ControllerReference m_controller;
    private float m_dampeningMultiplier, m_distanceCushion, m_distanceCushionThreshold, m_velocityCushionThreshold;
    private Vector3 m_previousPosition = Vector3.zero;
    private Vector3 m_targetPreviousPosition = Vector3.zero;
    private GameObject m_target;
    private GameObject m_targetVisual;
    private float m_maxRopeDistance;
    private float m_controllerMaxDistance;

    #endregion

    #region controller and rope member variables


    private LineRenderer m_ropeVisual;
    private bool m_isGrappled = false;
    private bool m_canSwing = true;
    private bool m_swinging = false;

    #endregion

    //attach VRTK delegates
    void Awake()
    {
        GetComponentInParent<VRTK_ControllerEvents>().TriggerAxisChanged += new ControllerInteractionEventHandler(DoTriggerPressed);
        GetComponentInParent<VRTK_ControllerEvents>().TriggerReleased += new ControllerInteractionEventHandler(DoTriggerReleased);

        GetComponentInParent<VRTK_ControllerEvents>().ButtonTwoPressed += new ControllerInteractionEventHandler(DoMenuButtonPressed);
    }

    void Start()
    {
        //grab settings
        m_dampeningMultiplier = .1f;
        m_distanceCushion = .5f;
        m_distanceCushionThreshold = 5;
        m_velocityCushionThreshold = 1;

        //gather and initialise objects
        m_targetVisual = GameObject.Find("TargetVisualGO");
        m_targetVisual.transform.parent = null;
        m_ropeVisual = GetComponent<LineRenderer>();

        //gather components
        m_controller = VRTK_ControllerReference.GetControllerReference(transform.parent.gameObject);
    }

    void Update()
    {
        //cache previous position
        m_previousPosition = gameObject.transform.position;

        if (m_target == null)
            m_ropeVisual.enabled = false;

        //update rope visual
        if (m_ropeVisual != null && m_ropeVisual.enabled)
        {
            m_ropeVisual.SetPosition(0, m_target.transform.position);
            m_ropeVisual.SetPosition(1, gameObject.transform.position);
        }
    }
    
    void FixedUpdate() //this takes care of the swinging mechanic
    {
        if (m_playerBody != null)
        {
            //prevent gravity deactivation if moving
            if (m_playerBody.velocity.magnitude > 0.0f || m_target == null)
            {
                m_playerBody.useGravity = true;
            }


            #region zero elasticity rope

            if (m_type == SwingingType.swing)
            {

                //Calculate rope physics
                if (m_target != null)
                {
                    //pulling, calculate new distances first
                    float distance = 0;
                    float delta = 0;
                    //float relDelta = 0;

                    //dot product calculation:
                    //For normalized vectors Dot returns 1 if they point in exactly the same direction, -1 if they point in completely opposite directions and zero if the vectors are perpendicular.

                    //direction of vectors when V1 - V2 = V1 <-- V2

                    //dir1 is from the point of the gun to the target
                    Vector3 dir1 = (m_target.transform.position - gameObject.transform.position).normalized;
                    //dir2 is the direction of the vector between the previous gun position and the current gun position, used for pulling
                    Vector3 dir2 = (m_previousPosition - gameObject.transform.position).normalized;
                    Vector3 dirUp = m_playerBody.transform.up;


                    #region sinking prevention system

                    if (m_playerBody.velocity.magnitude < 0.1f && Vector3.Dot(dir1, dirUp) > 0.9f)
                    {
                        m_playerBody.useGravity = false;
                    }
                    else
                        m_playerBody.useGravity = true;

                    #endregion


                    #region pulling section

                    if (m_playerBody.velocity.magnitude < m_velocityCushionThreshold) //the treshold prevents pulling at highg speeds, TO BE REVISITED!!!!
                    {
                        //Relative distance is gotten from dir2

                        distance = Vector3.Distance(gameObject.transform.position, m_target.transform.position);

                        if (distance > m_distanceCushionThreshold)
                            distance -= m_distanceCushion;

                        delta = (m_controllerMaxDistance - distance) / m_dampeningMultiplier;
                        //calculate relative delta
                        //relDelta = (delta * Vector3.Distance(m_associatedGun.NaviBase.transform.position, m_associatedGun.target.transform.position)) / distance;
                        //relDelta -= delta;
                        //print("Delta: " + delta + ", Rel: " + relDelta);

                        if (distance > m_controllerMaxDistance && Vector3.Dot(dir1, dir2) > -0.9f) //we pull here!
                        {
                            //calculate delta distance
                            //Vector3 additionalVelocity = ((m_associatedGun.target.transform.position - m_associatedGun.m_muzzleFlash.transform.position).normalized * Mathf.Abs(delta));
                            //only pull towards rope
                            Vector3 additionalVelocity = ((m_target.transform.position - m_playerBody.transform.position).normalized * Mathf.Abs(delta));
                            if (m_canPull)
                                m_playerBody.velocity += additionalVelocity;

                        }
                    }

                    #endregion


                    #region motion calculation when on the arc

                    if (Vector3.Distance(m_playerBody.transform.position, m_target.transform.position) > m_maxRopeDistance)
                    {
                        //calculate prerequisites
                        Vector3 nextPos = m_playerBody.transform.position + m_playerBody.velocity * Time.deltaTime;
                        Vector3 dirToCenter = (nextPos - m_target.transform.position).normalized;
                        Vector3 newPosOnCircle = dirToCenter * m_maxRopeDistance;
                        Vector3 newConstrainedDir = (newPosOnCircle - m_playerBody.transform.position).normalized;

                        //apply motion
                        m_playerBody.velocity = m_playerBody.velocity.magnitude * newConstrainedDir;
                    }

                    #endregion
                }
            }

            #endregion
        }
    }

    #region VRTK delegates

    private void DoMenuButtonPressed(object sender, ControllerInteractionEventArgs e)
    {
    }

    private void DoTriggerReleased(object sender, ControllerInteractionEventArgs e)
    {
        //destroy target and disable rope
        if (m_target != null)
        {
            Destroy(m_target.gameObject);
            m_ropeVisual.enabled= false;
        }

        //reset state so that we can swing again
        m_swinging = false;
        m_target = null;
        m_maxRopeDistance = 0;
        m_targetVisual.SetActive(false);
        m_canSwing = true;
    }

    private void DoTriggerPressed(object sender, ControllerInteractionEventArgs e)
    {
        if (e.buttonPressure >= .9f) //make sure we clicked the button
        {
            if (m_canSwing)
            {
                //find if we can grapple
                RaycastHit raycastHit;
                if (Physics.Raycast(transform.position, gameObject.transform.forward, out raycastHit))
                {
                    //advance storyboard through raycast
                    if (raycastHit.collider.GetComponentInChildren<RaycastedTrigger>() != null)
                        raycastHit.collider.GetComponentInChildren<RaycastedTrigger>().Activate();

                    if (raycastHit.collider.tag == "Grapple")
                    {
                        if (m_target != null)
                        {
                            Destroy(m_target.gameObject);
                            m_target = null;
                        }

                        if (m_ropeVisual != null)
                            m_ropeVisual.enabled = true;

                        m_swinging = true;
                        m_target = new GameObject();

                        m_target.transform.position = raycastHit.point;

                        //parent to object hit
                        m_target.transform.parent = raycastHit.collider.transform.parent;

                        m_targetVisual.SetActive(true);
                        m_targetVisual.transform.position = m_target.transform.position;

                        //pulling data
                        m_controllerMaxDistance = Vector3.Distance(gameObject.transform.position, m_target.transform.position);
                        m_maxRopeDistance = Vector3.Distance(m_playerBody.transform.position, m_target.transform.position);

                        Vector3 direction = m_target.transform.position - m_playerBody.transform.position;

                        m_canSwing = false;
                    }
                }
            }
        }
    }

    #endregion
}
