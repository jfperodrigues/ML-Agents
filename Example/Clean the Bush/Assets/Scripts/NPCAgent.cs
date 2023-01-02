using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.IO;

public class NPCAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2.5f;

    [Tooltip("Agent's camera")]
    public Camera agentCamera;

    [Tooltip("Training mode or gameplay mode")]
    public bool trainingMode;

    [Tooltip("Speed to rotate around de up axis")]
    public float yawSpeed = 100f;

    public RayPerceptionSensor vision;

    public Animator anim;
    //RB of the agent
    new private Rigidbody rigidbody;

    //Allows for smoother yaw change
    private float smoothyawChange = 0f;

    //Area the agent is in
    private ForestArea area;

    //Wheter the agent is frozen (intentionally)
    private bool frozen = false;

    public bool isCleaning = false;

    public bool isBush = false;

    private int roundsCleaned = 1;
    private float startTime;
    private float timeCleaning;
    private float timeInBush;
    private float beginClean;
    private float beginBush;
    private float timeAt6=160;
    private float timeAt10=160;
    private bool begin = false;
    private int goodclean = 0;
    private int badclean = 0;
    private List<string> log;
    private string filename;
    /// <summary>
    /// Ammount of dirt cleaned
    /// </summary>
    public float AmmountCleaned { get; private set; }

    /// <summary>
    /// Initialize the agent
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        area = GetComponentInParent<ForestArea>();

        //If not training mode, no max step, play forever
        if (!trainingMode) MaxStep = 0;
        filename = GetInstanceID().ToString();
        log = new List<string>();
        startTime = Time.fixedTime;
    }

    /// <summary>
    /// Reset the agent when episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (trainingMode)
            area.ResetBushes(); //Only reset when in training

        if (begin)
        {
            
            float time = (Time.fixedTime - startTime);
            if (time > 10f)
            {
                log.Add("Round " + roundsCleaned);
                string tt = "Round time: " + time;
                log.Add(tt);
                tt = "Time Performing Clean Action: " + timeCleaning;
                log.Add(tt);
                tt = "Time in Bush: " + timeInBush;
                log.Add(tt);
                tt = "Amount Cleaned: " + AmmountCleaned;
                log.Add(tt);
                tt = "Time at 6: " + timeAt6;
                log.Add(tt);
                tt = "Time at 10: " + timeAt10;
                log.Add(tt);
                tt = "Instances Cleaned outside Bush: " + badclean;
                log.Add(tt);
                File.WriteAllLines(filename + ".txt", log);
            }
            roundsCleaned++;
            goodclean = 0;
            badclean = 0;
            timeCleaning = 0;
            timeInBush = 0;
            timeAt6 = 160;
            timeAt10 = 160;
        }
        else
        {
            begin = true;
        }

        //Reset ammount cleaned
        AmmountCleaned = 0f;

        //Zero out velocities so that movement stops before new episode
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        startTime = Time.fixedTime;
        bool inFrontOfBush = false;
        //Default to spawning in front of a bush
        if (trainingMode)
        {
            //spawn in front of bush 25% of time
            inFrontOfBush = UnityEngine.Random.value > .75f;
        }

        //Move agent to random position
        MoveToSafeRandomLocation(inFrontOfBush);
        print(roundsCleaned);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W))
            actions[0] = 1;
        else if (Input.GetKey(KeyCode.S))
            actions[0] = 2;
        else
            actions[0] = 0;

        if (Input.GetKey(KeyCode.A))
            actions[1] = 2;
        else if (Input.GetKey(KeyCode.D))
            actions[1] = 1;
        else
            actions[1] = 0;

        if (Input.GetKey(KeyCode.F))
        {
            actions[2] = 1;
        }
        else
        {
            actions[2] = 0;
        }
    }

    /// <summary>
    /// Called when an action is received from either the player input or neural network
    /// 0: move foward backward
    /// 1: yaw angle
    /// 2: is cleaning
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (frozen) return; float speed;
        //Discrete actions: [0] -> Movement   [1] -> Rotation    [2] -> Action
        if (actions.DiscreteActions[0] == 1)
            speed = moveForce;                           //Moving Forward
        else if (actions.DiscreteActions[0] == 2)
            speed = -moveForce * 0.8f;                   //Moving Backward with a penalty to speed
        else
            speed = 0;                                   //Staying Still

        //Moving the Bot
        Vector3 fwd = transform.forward;
        transform.position += fwd * speed * moveForce * Time.deltaTime;

        //Moveemnt Animations
        anim.SetFloat("speed", speed);
        if (speed == 0) anim.SetBool("stoped", true);
        else anim.SetBool("stoped", false);

        Vector3 rot = transform.rotation.eulerAngles;

        float yawChange=0;
        if (actions.DiscreteActions[1] <= 1)            //Rotate right or stay still
        {
            yawChange = actions.DiscreteActions[1];
            if (smoothyawChange < 0)
                smoothyawChange = 0;                    //If bot was in opposite motion, stop previous motion
        }
        else                                            //Rotate Left
        {
            yawChange = -1;
            if (smoothyawChange > 0)
                smoothyawChange = 0;                    //If bot was in opposite motion, stop previous motion
        }

        //Rotate
        smoothyawChange = Mathf.MoveTowards(smoothyawChange, yawChange, 2f * Time.fixedDeltaTime);
        float yaw = rot.y + smoothyawChange * Time.fixedDeltaTime * yawSpeed;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (actions.DiscreteActions[2] > 0)             //Bot is cleaning
        {
            if (!isCleaning)
                beginClean = Time.fixedTime;            //Time measurements 
            isCleaning = true;
        }
        else                                            //Bor is not Cleaning
        {
            if (isCleaning)
                timeCleaning += Time.fixedTime - beginClean; //Time measurements 
            isCleaning = false;
        }

        //Cleaning Animation
        anim.SetBool("isClean", isCleaning);        
    }

    /// <summary>
    /// Collec vector observation from the environments
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor)
    {
        //Bot's local rotation 
        sensor.AddObservation(transform.localRotation.normalized);
        //Bot's forward vector
        sensor.AddObservation(transform.forward.normalized);
        //if bot is cleaning
        sensor.AddObservation(isCleaning);
        //if bot is in bush
        sensor.AddObservation(isBush);
    }

    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }

    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bush"))
            beginBush = Time.fixedTime;
        TriggerEnterOrStay(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }
    private void TriggerEnterOrStay(Collider collider)
    {
        if (collider.CompareTag("Bush"))
        {
            isBush = true;
            if (isCleaning)
            {
                Bush bush = area.GetBushFromColl(collider);
                isBush = bush.HasDirt;
                float cleaned = bush.Clean(.01f,this);

                AmmountCleaned += cleaned;

                if (trainingMode && cleaned > 0)
                {
                    goodclean++;
                    if (anim.GetFloat("speed") == 0) AddReward(.050f);
                    else AddReward(.025f);
                }
                else
                    badclean++;
            }
        }
    }
    private void OnTriggerExit(Collider collider)
    {
        if (collider.CompareTag("Bush"))
        {
            onBushExit();
        }
    }

    public void onBushExit()
    {
        timeInBush += Time.fixedTime - beginBush;
        isBush = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            //Collided with the area boundary, give a negative reward
            AddReward(-.5f);
        }
        if (trainingMode && collision.collider.CompareTag("obstacle"))
        {
            AddReward(-.15f);
        }
    }

    private void MoveToSafeRandomLocation(bool inFront)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100; //prevent infinites
        Vector3 potentialPos = Vector3.zero;
        Quaternion potentialRot = new Quaternion();

        //loop til new pos found
        while (!safePositionFound && attemptsRemaining > 0)
        {
            float limx = area.transform.position.x;
            float limz = area.transform.position.z;
            potentialPos = new Vector3(UnityEngine.Random.Range(limx - 18f, limx + 18f), 0.5f / 4, UnityEngine.Random.Range(limz - 18f, limz + 18f));

            //check to see if the agent will collide
            Collider[] colls = Physics.OverlapSphere(potentialPos, 0.05f);
            safePositionFound = colls.Length <= 1;

        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        float yaw = UnityEngine.Random.Range(-180f, 180f);
        potentialRot = Quaternion.Euler(0f, yaw, 0f);

        transform.position = potentialPos;
        transform.rotation = potentialRot;

    }

    public void Update()
    {
        if(AmmountCleaned >= 24)
        {
            EndEpisode();
        }
        if (AmmountCleaned >= 12 && timeAt6 >= 160f)
            timeAt6 = Time.fixedTime - startTime;
        if(AmmountCleaned >= 20 && timeAt10 >= 160f)
            timeAt10 = Time.fixedTime - startTime;
        if (isCleaning && !isBush)
        {
            AddReward(-.02f);
            badclean++;
        }
    }
}
