using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PeerLogic : MonoBehaviour
{
    enum ControlsSchema
    {
        Player1,
        Player2
    }

    /* 
     * The general idea is my script will calculate his mouvements or physics,
     * then sends the result to the net player, the net player will simulate
     * my player and displaySimulationOnNetPlayer will show us if our simulation
     * is in sync, displaySimulationOnNetPlayer is how my local player moved
     * on the networked player.
     */

    [SerializeField]
    bool amIAServer;

    [SerializeField]
    uint playerId = 0;

    [SerializeField]
    PeerLogic otherPeer;

    [SerializeField]
    GameObject localPlayer; // My local player

    [SerializeField]
    GameObject myPlayerOnServer; // My player on the server

    [SerializeField]
    GameObject displayMyPlayerOnServer; // To display how my player behave on the server
    
    // To avoid calling Physics.Simulate a lot in one scene [in one tick]
    // it makes physics bad
    private Scene serverScene; 
    private PhysicsScene serverPhysicsScene;

    [SerializeField]
    ControlsSchema whatControls;

    // to run at fixed tick rate
    private float peerTimer;
    private uint currentTick;
    private float minTimeBetweenTicks;
    private const float PEER_TICK_RATE = 60f;
    private const float latency = 0.1f;

    private const int peerBufferSize = 1024;
    private Structs.PlayerState[] peerBufferStates; // Holds position of local player at a given tick

    public Queue<Structs.InputMessage> peerReceivedInputs;
    public Queue<Structs.StateMessage> peerReceivedStates;

    void Start()
    {
        Physics.autoSimulation = false;
        Application.targetFrameRate = 60;

        peerTimer = 0.0f;
        currentTick = 0;
        minTimeBetweenTicks = 1 / PEER_TICK_RATE;
        peerReceivedStates = new Queue<Structs.StateMessage>();
        peerReceivedInputs = new Queue<Structs.InputMessage>();

        peerBufferStates = new Structs.PlayerState[peerBufferSize];

        if (amIAServer)
        {
            serverScene = SceneManager.LoadScene(
                "ServerScene",
                new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D }
            );

            serverPhysicsScene = serverScene.GetPhysicsScene();

            SceneManager.MoveGameObjectToScene(myPlayerOnServer, serverScene);
        }
    }


    void Update()
    {
        peerTimer += Time.deltaTime;

        while (peerTimer >= minTimeBetweenTicks)
        {
            peerTimer -= minTimeBetweenTicks;

            // Handle a tick

            // We start by getting the input
            Structs.Inputs inputs = GetInput();

            // Simulate physics on local player
            localPlayer.GetComponent<Player>().PhysicsStep(inputs, minTimeBetweenTicks);
            Physics.Simulate(minTimeBetweenTicks);

            // Send simulation result to other player
            //Structs.StateMessage stateMessage;
            //stateMessage.delivery_time = Time.time + latency; // time + lag(to simulate ping)
            //stateMessage.tick_number = currentTick;
            //stateMessage.playerId = playerId;
            //stateMessage.position = localPlayer.transform.position;
            //stateMessage.rotation = localPlayer.transform.rotation;
            //stateMessage.velocity = localPlayer.GetComponent<Rigidbody>().velocity;
            //stateMessage.angular_velocity = localPlayer.GetComponent<Rigidbody>().angularVelocity;

            Structs.InputMessage inputMsg;
            inputMsg.delivery_time = Time.time + latency;
            inputMsg.playerId = playerId;
            inputMsg.inputs = inputs;

            if (!amIAServer)
                otherPeer.peerReceivedInputs.Enqueue(inputMsg);
            else // If am i a server send message to my self
                peerReceivedInputs.Enqueue(inputMsg);

            currentTick++;

            // Now simulate what happens when we receive a state from another player

            /*
             * First check if we have some states in the queue that needs to simulated,
             * and if latest state received is in the past (means should be simulated),
             * this is a bad phrasing i should explain this more
             */
            if (peerReceivedInputs.Count > 0 &&
                Time.time >= peerReceivedInputs.Peek().delivery_time)
            {
                Structs.InputMessage stateMsg = peerReceivedInputs.Dequeue();

                while (peerReceivedInputs.Count > 0 &&
                       Time.time >= peerReceivedInputs.Peek().delivery_time)
                {
                    stateMsg = peerReceivedInputs.Dequeue();
                }

                // Simulate input
                myPlayerOnServer.GetComponent<Player>().PhysicsStep(stateMsg.inputs, minTimeBetweenTicks);
                serverPhysicsScene.Simulate(Time.fixedDeltaTime);

                // To see how my player moves on the server
                displayMyPlayerOnServer.transform.position = myPlayerOnServer.transform.position;
                displayMyPlayerOnServer.transform.rotation = myPlayerOnServer.transform.rotation;

                // Correction
                //otherPlayerOnMyMachine.transform.position = stateMsg.position;
                //otherPlayerOnMyMachine.transform.rotation = stateMsg.rotation;
                //otherPlayerOnMyMachine.GetComponent<Rigidbody>().velocity = stateMessage.velocity;
                //otherPlayerOnMyMachine.GetComponent<Rigidbody>().angularVelocity = stateMessage.angular_velocity;
            }
        }
    }


    Structs.Inputs GetInput()
    {
        Structs.Inputs inputs;
        if (ControlsSchema.Player1 == whatControls)
        {
            inputs.up = Input.GetKey(KeyCode.W);
            inputs.down = Input.GetKey(KeyCode.S);
            inputs.left = Input.GetKey(KeyCode.A);
            inputs.right = Input.GetKey(KeyCode.D);
            inputs.jump = Input.GetKey(KeyCode.Space);
        } else
        {
            inputs.up = Input.GetKey(KeyCode.UpArrow);
            inputs.down = Input.GetKey(KeyCode.DownArrow);
            inputs.left = Input.GetKey(KeyCode.LeftArrow);
            inputs.right = Input.GetKey(KeyCode.RightArrow);
            inputs.jump = Input.GetKey(KeyCode.P);
        }

        return inputs;
    }

}
